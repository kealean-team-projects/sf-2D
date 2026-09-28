#if UNITY_EDITOR
using _02._Script._03_TrapAndEnemy.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;
using Random = System.Random;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 심해 챕터 (x 225 ~ 1095). 숲보다 짧지만 함정·퍼즐이 어렵고, 후반은 보스 "심연의 눈"과의 숨바꼭질 → 추격전 → 탈출.
    ///  (도착) 숲 산 정상의 구덩이 → 화면 암전(CATestPitTransition) → (269, -28)에서 착수 웅덩이로 떨어지며 화면이 밝아짐
    ///  S0 착수 웅덩이(240~312) / S1 해저 동굴 입구(312~392) / S2 잠긴 통로(392~466) / S3 해구(466~506)  ← 기존 유지
    ///  ── 보스 구역 (전부 물속: 떨어질 때 천천히 가라앉아 걷기 점프 ≈15, 달리기 점프 ≈30) ──
    ///  H0 입구(506~530)   : 세이브(보스전 실패 시 여기서 재시작). 입장하면 컷신 → 멀리 떠 있는 눈이 깨어남
    ///  H1 숨는 법(530~626): 평지 + 바위 아치(지붕이 빛을 막음) + 해초 덤불(웅크리면 숨음)
    ///  H2 성게 구덩이(626~705): 솟은 발판 사이 성게 구덩이 2개 + 역류, 발판 위 아치
    ///  H3 절벽·해파리(705~780): 산호 벽 등반 → 바위 지붕 아래 윗길 → 전기 해파리가 오가는 해초 바닥
    ///  H4 성게 해구(780~850): 무너지는 석판 + 아치가 있는 바위섬 2개 + 역류 + 위쪽을 오가는 해파리
    ///  분노(~880)        : 컷신 — 눈이 붉게 변하며 가까이 다가옴 → 추격전
    ///  C  추격(885~1010) : 성게 틈, 언덕, 떨어지는 돌, 성게 틈 → 굴 입구
    ///  T  굴(1010~1024)  : 높이 1.4 — 웅크려야만 들어감. 안쪽 방에 들어가면 컷신: 눈이 입구를 들이받다 포기하고 사라짐
    ///  A  상승(1034~1040): 좁은 세로 통로, 좌우 산호 패치를 번갈아 벽점프 + 기포 분출구 → 물 밖 → 빛이 드는 틈(끝)
    /// </summary>
    public static partial class CATestBuilder {
        private static readonly Color SeaFog = new(0.05f, 0.16f, 0.22f);

        private static readonly Vector2[] DS1 = {
            new(240, -60), new(248, -62.5f), new(258, -64), new(270, -64.3f), new(282, -63.8f), new(292, -62), new(300, -58.5f),
            new(306, -55), new(312, -52.5f), new(320, -51.5f), new(332, -51.6f), new(342, -52), new(351, -52.4f)
        };
        private static readonly Vector2[] DS2 = {
            new(359, -53.2f), new(363, -54), new(376, -54.1f), new(384, -56.5f), new(392, -58.3f), new(400, -60), new(408, -62.2f),
            new(414, -63.5f), new(420, -64)
        };
        private static readonly Vector2[] DS3 = { new(436, -66), new(444, -67), new(452, -67.5f), new(460, -68), new(466, -68.5f) };
        private static readonly Vector2[] DT1 = { new(466, -88), new(474, -88.2f), new(484, -88) };
        // ── 보스 구역 이후 바닥 윗면 ──
        private static readonly Vector2[] DH1 = {
            new(506, -88), new(514, -88), new(520, -88.2f), new(530, -88), new(545, -88.4f), new(560, -88), new(575, -88.3f), new(590, -88),
            new(600, -87.6f), new(610, -86.5f), new(626, -86)
        };
        private static readonly Vector2[] DH2b = { new(640, -84), new(648, -83.8f), new(655, -84) };
        private static readonly Vector2[] DH2c = { new(668, -86), new(676, -86.3f), new(690, -86), new(698, -88), new(705, -88) };
        private static readonly Vector2[] DH3 = { new(705, -72), new(720, -71.8f), new(735, -72) };
        private static readonly Vector2[] DH3b = { new(735, -90), new(750, -90.3f), new(765, -90), new(780, -90) };
        private static readonly Vector2[] DH4 = { new(850, -88), new(865, -88.2f), new(885, -88), new(905, -88) };
        private static readonly Vector2[] DC2 = {
            new(917, -88), new(930, -87.5f), new(936, -84), new(944, -80), new(950, -80), new(956, -84), new(962, -88)
        };
        private static readonly Vector2[] DC3 = { new(974, -89), new(990, -89.5f), new(1010, -90), new(1040, -90) };

        private static float SeaSurfaceY(float x) {
            if (x < 351) return SurfaceY(DS1, x);
            if (x < 359) return -62f;
            if (x < 420) return SurfaceY(DS2, x);
            if (x < 436) return -78f;
            if (x < 466) return SurfaceY(DS3, x);
            if (x < 484) return SurfaceY(DT1, x);
            if (x < 506) return -94f;
            if (x < 626) return SurfaceY(DH1, x);
            if (x < 640) return -94f;
            if (x < 655) return SurfaceY(DH2b, x);
            if (x < 668) return -94f;
            if (x < 705) return SurfaceY(DH2c, x);
            if (x < 735) return SurfaceY(DH3, x);
            if (x < 780) return SurfaceY(DH3b, x);
            if (x < 850) return (x >= 798 && x <= 806) || (x >= 822 && x <= 830) ? -86f : -96f;
            if (x < 905) return SurfaceY(DH4, x);
            if (x < 917) return -94f;
            if (x < 962) return SurfaceY(DC2, x);
            if (x < 974) return x >= 967 && x <= 969 ? -88f : -94f;
            if (x < 1040) return SurfaceY(DC3, x);
            return -30f;
        }

        public static void BuildDeepSeaScene() {
            var scene = NewMapScene(DeepSeaScenePath);
            var rnd = new Random(1204);
            var root = new GameObject("CATest_DeepSea").transform;
            var ground = Group(root, "10_Terrain").transform;
            var gameplay = Group(root, "20_Gameplay").transform;
            var props = Group(root, "30_Props_Play").transform;
            var nearBg = Group(root, "40_NearBG").transform;
            var midBg = Group(root, "50_MidBG").transform;
            var farBg = Group(root, "60_FarBG").transform;
            var fg = Group(root, "05_Foreground").transform;
            var vfx = Group(root, "70_VFX_Light").transform;
            var zones = Group(root, "80_Zones").transform;
            var boss = Group(root, "90_Boss_AbyssEye").transform;

            var shapeSea = Load<SpriteShape>(FSB + "/Underwater area pack/Sprite shapes/Underworld ground.asset");
            var shapeStone = Load<SpriteShape>(FSB + "/Dungeon pack/Sprite shapes/Sone ground.asset");
            var shapeCoral = Load<SpriteShape>(FSB + "/Underwater area pack/Sprite shapes/Coral.asset");

            var seaTint = C(0.72f, 0.85f, 0.9f);
            var caveTint = C(0.55f, 0.65f, 0.75f);
            var ceilTint = C(0.18f, 0.26f, 0.32f);

            // ─── 1. 지형 ───
            Terrain(ground, "WallLeft", new Vector2[] { new(225, -8), new(240, -8), new(240, -130), new(225, -130) }, shapeStone, "Ground", 0, Mul(caveTint, 0.5f));
            Terrain(ground, "Ceiling_ShaftL", new Vector2[] { new(240, -8), new(262, -8), new(262, -44), new(255, -46), new(246, -45.5f), new(240, -44) }, shapeStone, "Ground", 0, ceilTint);
            Terrain(ground, "Ceiling_CaveEntrance", new Vector2[] {
                new(276, -8), new(400, -8), new(400, -48.5f), new(390, -47), new(378, -45.5f), new(365, -47.2f), new(352, -45),
                new(340, -44), new(328, -42.5f), new(312, -42), new(296, -43), new(284, -44.5f), new(276, -44)
            }, shapeStone, "Ground", 0, ceilTint);
            Terrain(ground, "Ceiling_Submerged", new Vector2[] {
                new(400, -8), new(472, -8), new(472, -52), new(462, -50.5f), new(450, -51.5f), new(438, -50.2f), new(425, -51),
                new(410, -49.5f), new(400, -48.5f)
            }, shapeStone, "Ground", 0, ceilTint);

            Terrain(ground, "DS1_PoolAndCaveFloor", Close(DS1, -130), shapeStone, "Ground", 1, caveTint);
            Terrain(ground, "SpikePit1_Floor", new Vector2[] { new(351, -62), new(359, -62), new(359, -130), new(351, -130) }, shapeStone, "Ground", 1, Mul(caveTint, 0.6f));
            Terrain(ground, "DS2_Slope", Close(DS2, -130), shapeSea, "Ground", 1, seaTint);
            Terrain(ground, "UrchinGap_Floor", new Vector2[] { new(420, -78), new(436, -78), new(436, -130), new(420, -130) }, shapeSea, "Ground", 1, Mul(seaTint, 0.6f));
            Terrain(ground, "DS3_KelpShelf", Close(DS3, -130), shapeSea, "Ground", 1, seaTint);
            ClimbEdge(ground, new Vector2(466f, -69f), new Vector2(466f, -87.5f), "Climb_TrenchWall");
            Terrain(ground, "T1_TrenchFloor", Close(DT1, -130), shapeSea, "Ground", 1, seaTint);
            Terrain(ground, "SpikeField_Floor", new Vector2[] { new(484, -94), new(506, -94), new(506, -130), new(484, -130) }, shapeSea, "Ground", 1, Mul(seaTint, 0.6f));
            BuildAbyssTerrain(ground, props, gameplay, rnd, shapeSea, shapeStone, seaTint, caveTint);

            // ─── 2. 게임플레이 ───
            Inst(PSaveSea, gameplay, new Vector3(324, SurfaceY(DS1, 324), 0)).name = "SavePoint_04_CaveMouth";
            Inst(PSaveSea, gameplay, new Vector3(511.5f, -88, 0)).name = "SavePoint_05_BeforeEye";
            Inst(PRelicSea, gameplay, new Vector3(527, -65.6f, 0)).name = "Relic_03_VentShelf";

            // 물 영역 (부력)
            WaterVolume(gameplay, "Water_LandingPool", new Rect(240, -66, 68, 11.5f));
            WaterVolume(gameplay, "Water_Sea", new Rect(386, -132, 648, 75));
            WaterVolume(gameplay, "Water_Shaft", new Rect(1034, -132, 6, 84)); // 상승 통로: 수면 -48

            // 낙하 연출 이어받기 (숲 씬 FallSequence 아래쪽)
            FallZone(gameplay, new Rect(262.2f, -54f, 13.6f, 42f));

            // S0 조개 함정 (ClamTrap_AI 원화 + 기존 Clamp 로직)
            Inst(PClam, gameplay, new Vector3(337, SurfaceY(DS1, 337) + 0.5f, 0)).name = "Clam_01";
            // 가시 구덩이 바로 뒤 착지 지점의 두 번째 조개: 천장이 낮아 점프가 짧으므로 달리기 점프로 구덩이를 넘은 뒤
            // 바로 작은 점프로 조개를 넘어야 한다(연속 동작)
            Inst(PClam, gameplay, new Vector3(362.5f, SurfaceY(DS2, 362.5f) + 0.5f, 0)).name = "Clam_02_Landing";
            // 통로 천장의 종유석: 지나가면 떨어진다
            var stal = Sp(DG + "rocks.png", "rocks_4");
            FallingRock(gameplay, "Stalactite_1", 369, -47.2f, -54.1f, new Rect(364, -55, 7, 6), stal, 0.5f, C(0.45f, 0.55f, 0.62f));
            FallingRock(gameplay, "Stalactite_2", 381, -46.6f, -55.5f, new Rect(376, -57, 7, 7), stal, 0.5f, C(0.45f, 0.55f, 0.62f));
            // S1 작은 가시 구덩이
            var u1 = Inst(PUrchinSea, gameplay, new Vector3(355, -61.3f, 0)); u1.name = "UrchinPit_01";
            ThornArt(u1.transform, 8f, rnd, true); SizeBox(u1, 8f, 1.4f);
            // S1 전기 해파리: 머리 높이로 왕복 → 웅크리거나 타이밍으로 통과
            var j1 = Inst(PJelly, gameplay, new Vector3(364.5f, -51.6f, 0)); j1.name = "ElectricJelly_Corridor";
            Set(j1.GetComponent<ElectronicEnemy>(), "moveDistance", 11f);
            Set(j1.GetComponent<ElectronicEnemy>(), "moveSpeed", 2.6f);
            // S2 넓은 성게 틈 (16 유닛: 지상 점프로는 어렵고 수중 체공으로 건넌다)
            var u2 = Inst(PUrchinSea, gameplay, new Vector3(428, -77.3f, 0)); u2.name = "UrchinGap_Wide";
            ThornArt(u2.transform, 16f, rnd, true); SizeBox(u2, 16f, 1.4f);
            // 성게 틈 위로 주기적으로 밀려오는 역류(해류 파도): 파도가 칠 때 건너면 뒤로 밀려 성게에 떨어진다 → 파도 사이 타이밍
            Gust(gameplay, "SurgeCurrent_UrchinGap", new Rect(412, -78, 30, 18), new Vector2(-5.5f, 0f), 1.8f, 2.2f);
            // S2 해초 덤불 2개 + 벽 속 눈
            Inst(PSeaweed, gameplay, new Vector3(442.5f, SurfaceY(DS3, 442.5f), 0)).name = "Seaweed_A";
            Inst(PSeaweed, gameplay, new Vector3(455f, SurfaceY(DS3, 455f), 0)).name = "Seaweed_B";
            var eye = Inst(PEyeTrap, gameplay, new Vector3(449f, -63.2f, 0)); eye.name = "WallEye_Watcher";
            // S3 해구: 등불 + 빛 발판 + 가시밭 + 해파리
            var lamp = Inst(PLamp, gameplay, new Vector3(478.5f, -88f, 0.2f)); lamp.name = "LampFlower";
            var u3 = Inst(PUrchinSea, gameplay, new Vector3(495, -93.3f, 0)); u3.name = "UrchinField";
            ThornArt(u3.transform, 22f, rnd, true); SizeBox(u3, 22f, 1.4f);
            var lampLight = lamp.GetComponent<_02._Script._04_Interaction.InteractLight>();
            var timer = lamp.AddComponent<CATestLampTimer>();
            Set(timer, "duration", 5.5f);
            foreach (var (x, y, d) in new[] { (488.5f, -85.2f, 0f), (495f, -83.2f, 0.25f), (501.5f, -85.2f, 0.5f) }) {
                var pad = Inst(PBloomPad, gameplay, new Vector3(x, y, 0));
                pad.name = $"LightBloomPad_{x}";
                var bp = pad.GetComponent<CATestLightBloomPlatform>();
                Set(bp, "sources", new Object[] { lampLight });
                Set(bp, "delay", d);
            }
            var j2 = Inst(PJelly, gameplay, new Vector3(487f, -78.3f, 0)); j2.name = "ElectricJelly_Trench";
            Set(j2.GetComponent<ElectronicEnemy>(), "moveDistance", 17f);
            Set(j2.GetComponent<ElectronicEnemy>(), "moveSpeed", 3.8f);
            // 기포 분출구(수중에서만 동작하는 상승 해류) → 선반의 유물
            var vent = Inst(PCurrentColumn, gameplay, new Vector3(517.5f, -88f, 0)); vent.name = "BubbleVent";
            var vb = vent.GetComponent<BoxCollider2D>(); vb.size = new Vector2(3.2f, 23f); vb.offset = new Vector2(0, 11.5f);
            Put(props, Sp(UW + "Ground elements.png", "Ground elements_1"), new Vector3(517.5f, -87.6f, 0.3f), 0.28f, "Item", 8, C(0.5f, 0.75f, 0.8f));

            AreaTitle(zones, new Rect(236, -70, 76, 40), "해저 동굴", "심해 챕터");
            AreaTitle(zones, new Rect(392, -82, 74, 40), "잠긴 통로");
            AreaTitle(zones, new Rect(466, -100, 54, 40), "해구");
            AreaTitle(zones, new Rect(1036, -40, 50, 30), "빛이 드는 틈");
            Say(zones, new Rect(236, -66, 50, 14), "sea_land");
            Say(zones, new Rect(392, -66, 12, 10), "sea_passage");
            Say(zones, new Rect(1026, -92, 8, 12), "ascent", false);
            Say(zones, new Rect(1040, -32, 14, 8), "ascent_top");
            Say(zones, new Rect(990, -92, 12, 10), "boss_tunnel_hint", false);

            // ─── 3. 보스: 심연의 눈 (찾아다니는 눈) + 연출 감독 ───
            BuildEyeHunter(boss);

            // ─── 4. 플레이 프랍 ───
            var coralSmall = Sps(UW + "Seaweed.png", "Seaweed", 6, 7, 8, 9);
            var coralRed = Sps(UW + "Seaweed.png", "Seaweed", 11, 14, 15);
            var glowBulbs = Sps(UW + "Seaweed.png", "Seaweed", 12, 16, 17, 18);
            var shells = Sps(UW + "Seaweed.png", "Seaweed", 10, 13, 19, 20);
            var kelp = Sps(UW + "Seaweed.png", "Seaweed", 2, 3, 4, 5);
            var crystals = Sps(DG + "cristals blue.png", "cristals blue", 0, 1, 2, 3, 4);
            var caveRocks = Sps(DG + "rocks.png", "rocks", 0, 2, 3, 5, 7, 9);
            var stalactites = Sps(DG + "rocks.png", "rocks", 1, 4, 8);
            var hangMass = Sps(DG + "green on stone2 .png", "green on stone2 ", 2, 3, 4, 11);
            var surfaceAll = new[] {
                new Vector2(240, -60), new(300, -58.5f), new(351, -52.4f), new(420, -64), new(466, -68.5f), new(484, -88), new(645, -87)
            };
            bool Hazard(float x) => (x > 350 && x < 360) || (x > 419 && x < 437) || (x > 483 && x < 507) || (x > 334 && x < 340)
                                    || (x > 625 && x < 641) || (x > 654 && x < 669) || (x > 779 && x < 851) || (x > 904 && x < 918)
                                    || (x > 961 && x < 975) || (x > 1008);
            // 마른 동굴: 바위, 수정, 물기
            ScatterOn(props, rnd, caveRocks, 300, 392, 5f, 9f, 0.35f, 0.6f, 0.4f, 1.2f, "Item", 3, C(0.45f, 0.55f, 0.62f), C(0.6f, 0.7f, 0.78f), Hazard);
            ScatterOn(props, rnd, crystals, 305, 395, 7f, 13f, 0.35f, 0.6f, 0.3f, 0.8f, "Item", 6, Color.white, C(0.8f, 0.95f, 1f), Hazard);
            // 수중: 산호/해초/조개/발광 식물
            ScatterOn(props, rnd, coralSmall, 392, 1010, 4f, 8f, 0.18f, 0.3f, 0.3f, 1.2f, "Item", 3, C(0.6f, 0.8f, 0.8f), C(0.8f, 0.95f, 0.95f), Hazard);
            ScatterOn(props, rnd, coralRed, 395, 1010, 6f, 12f, 0.28f, 0.42f, 0.5f, 1.5f, "Item", 2, C(0.85f, 0.55f, 0.5f), C(1f, 0.7f, 0.6f), Hazard);
            ScatterOn(props, rnd, shells, 300, 1010, 8f, 15f, 0.18f, 0.28f, -0.3f, 0.1f, "Architecture", 3, C(0.7f, 0.8f, 0.85f), Color.white, Hazard);
            ScatterOn(props, rnd, kelp, 392, 1010, 7f, 12f, 0.22f, 0.32f, 1f, 2.5f, "Item", 0, C(0.3f, 0.55f, 0.5f), C(0.45f, 0.7f, 0.6f), Hazard, true);
            foreach (var x in new[] { 402f, 447f, 470f, 481f, 512f, 553f, 579f, 603f, 622f, 672f, 718f, 758f, 803f, 862f, 928f, 986f }) {
                var s = Pick(rnd, glowBulbs);
                var sc = R(rnd, 0.22f, 0.32f);
                var y = SeaSurfaceY(x);
                var sr = Put(props, s, new Vector3(x, y - s.bounds.min.y * sc - 0.1f, R(rnd, 0.2f, 0.8f)), sc, "Item", 9, Color.white, rnd.Next(2) == 0);
                Light(vfx, "BioGlow", new Vector3(x, y + 2f, 0), C(1f, 0.85f, 0.4f), R(rnd, 0.35f, 0.55f), R(rnd, 3.5f, 5f), 0.3f);
                if (sr != null) { var am = sr.gameObject.AddComponent<CATestAmbientMotion>(); am.swayAngle = 4f; am.swaySpeed = 0.9f; }
            }
            // 천장: 종유석 / 매달린 바위 덩어리
            foreach (var x in new[] { 286f, 300f, 318f, 333f, 347f, 362f, 374f, 388f, 405f, 420f, 436f, 452f, 465f }) {
                var s = rnd.Next(3) == 0 ? Pick(rnd, hangMass) : Pick(rnd, stalactites);
                var sc = R(rnd, 0.5f, 0.85f);
                var yTop = x < 400 ? -44f : -50f;
                Put(props, s, new Vector3(x + R(rnd, -2f, 2f), yTop + 0.8f - s.bounds.max.y * sc, R(rnd, 0.2f, 1.5f)), sc, "Ground", 5, C(0.35f, 0.45f, 0.55f), rnd.Next(2) == 0);
            }

            // ─── 5. 배경 ───
            var bgBlur = Sp(UW + "Background Blur ver/Underwater background blur.png");
            var bgB = Sp(UW + "Background Blur ver/Underwater background b blur.png");
            var bgC = Sp(UW + "Underwater background c.png");
            for (var x = 160f; x < 1220f; x += 225f)
                Put(farBg, bgBlur, new Vector3(x, -70f, 240f), 5f, "BackGround", -200, C(0.18f, 0.32f, 0.42f), (int)(x / 225f) % 2 == 0);
            for (var x = 220f; x < 1150f; x += 110f)
                Put(midBg, bgB, new Vector3(x, -78f, 90f), 2.4f, "BackGround", -120, C(0.13f, 0.26f, 0.34f), (int)(x / 110f) % 2 == 1);
            for (var x = 380f; x < 1100f; x += 65f)
                Put(midBg, bgC, new Vector3(x, -98f, 28f), 1.45f, "BackGround", -60, C(0.06f, 0.14f, 0.2f), (int)(x / 65f) % 2 == 0);
            var pillars = Sps(UW + "Underwater rocks.png", "Underwater rocks", 0, 1, 2);
            ScatterBand(midBg, rnd, pillars, 380, 1100, 16, 30, -108, -100, 2.2f, 3.2f, 55, 95, "BackGround", -100, C(0.08f, 0.18f, 0.25f), C(0.12f, 0.24f, 0.32f));
            var fishFar = Sps(UW + "Fishs.png", "Fishs", 0, 1);
            ScatterBand(midBg, rnd, fishFar, 400, 1060, 40, 70, -82, -64, 1.2f, 1.8f, 60, 90, "BackGround", -95, C(0.1f, 0.22f, 0.3f, 0.8f), C(0.14f, 0.28f, 0.36f, 0.8f), false);
            // 동굴 뒷벽 (마른 동굴 구간의 깊이)
            Terrain(nearBg, "CaveBackWall", new Vector2[] { new(236, -38), new(400, -40), new(470, -45), new(470, -110), new(236, -110) }, shapeStone, "Default", -40, C(0.1f, 0.14f, 0.19f), false, 0.6f, 7f);
            // 사원 유적(해저 사원 느낌): 푸르게 물든 돌기둥
            var pillarsTall = Sps(OF + "Old stounes.png", "Old stounes", 0, 1, 2, 3, 8, 9, 10);
            var templeTint = C(0.35f, 0.55f, 0.6f);
            foreach (var x in new[] { 372f, 381f, 526f, 598f, 700f, 790f, 868f, 940f }) {
                var s = Pick(rnd, pillarsTall);
                var sc = R(rnd, 1.1f, 1.4f);
                var z = R(rnd, 4f, 10f);
                Put(nearBg, s, new Vector3(x, SeaSurfaceY(x) - 0.3f - s.bounds.min.y * sc, z), sc, "Default", -5, Toward(templeTint, SeaFog, z * 0.03f), rnd.Next(2) == 0);
            }

            // ─── 6. 전경 실루엣 ───
            var fgTint = C(0.01f, 0.035f, 0.05f);
            foreach (var x in new[] { 250f, 290f, 332f, 372f, 412f, 452f, 494f, 540f, 590f, 632f, 700f, 760f, 830f, 900f, 960f }) {
                var s = Pick(rnd, kelp);
                Put(fg, s, new Vector3(x + R(rnd, -6f, 6f), SeaSurfaceY(x) - 5f, R(rnd, -18f, -10f)), R(rnd, 0.6f, 0.85f), "Architecture", 60, fgTint, rnd.Next(2) == 0, R(rnd, -6f, 6f)).gameObject
                    .AddComponent<CATestAmbientMotion>().swayAngle = 3f;
            }
            foreach (var x in new[] { 268f, 318f, 360f, 405f, 445f }) {
                var s = Pick(rnd, hangMass);
                var yTop = x < 400 ? -44f : -50f;
                Put(fg, s, new Vector3(x + R(rnd, -4f, 4f), yTop + 2f, R(rnd, -22f, -14f)), R(rnd, 1.2f, 1.7f), "Architecture", 62, fgTint, rnd.Next(2) == 0);
            }
            foreach (var x in new[] { 520f, 560f, 610f, 690f, 770f, 850f, 930f, 1000f }) {
                var s = Pick(rnd, pillars);
                Put(fg, s, new Vector3(x + R(rnd, -5f, 5f), -103f, R(rnd, -24f, -18f)), R(rnd, 1.1f, 1.5f), "Architecture", 61, fgTint, rnd.Next(2) == 0);
            }

            // ─── 7. VFX / 빛 ───
            // 구덩이로 새어 들어오는 빛줄기 → 착수 웅덩이
            // (God Ray 셰이더) 구덩이 위에서 곧게 내려오는 빛기둥 — 먼지 입자가 물속 "마린 스노우"처럼 보인다
            GodRay(vfx, "PitGodRay", 269f, -57f, 24f, 7f, 0f, C(0.6f, 0.88f, 1f), 1.1f, 11f, "Default", 50, 2f, 0.3f, 6f, 1f);
            Light(vfx, "ShaftLight", new Vector3(269, -48, 0), C(0.55f, 0.85f, 1f), 1.1f, 14f, 2f);
            foreach (var x in new[] { 405f, 440f, 478f, 540f, 590f, 660f, 740f, 820f, 900f }) {
                var gy = SeaSurfaceY(x);
                GodRay(vfx, "SeaGodRay", x, float.IsNaN(gy) ? -86f : gy + 1f, 30f, R(rnd, 5f, 9f), R(rnd, -6f, 6f), C(0.45f, 0.8f, 1f), R(rnd, 0.45f, 0.75f),
                    R(rnd, 0f, 100f), "Default", 45, R(rnd, 6f, 14f), R(rnd, 0.3f, 0.5f), R(rnd, 4f, 8f), 1.1f);
            }
            foreach (var x in new[] { 318f, 345f, 368f, 390f }) Light(vfx, "CrystalGlow", new Vector3(x, SeaSurfaceY(x) + 1.5f, 0), C(0.4f, 0.8f, 1f), 0.45f, 5.5f, 0.3f);
            // 물 표면 / 물 영역 비주얼 (LHS _VFX_Lib WaterSample: 영역형 물)
            WaterVisual(vfx, new Rect(240, -66, 68, 11), -55f);
            WaterVisual(vfx, new Rect(386, -132, 648, 75), -57f);
            WaterVisual(vfx, new Rect(1034, -132, 6, 84), -48f);
            // 입자: 마린 스노우 / 기포 / 물방울
            Motes(vfx, "MarineSnow", new Rect(386, -110, 650, 55), -0.5f, MatAlphaDot, C(0.75f, 0.9f, 1f, 0.5f), C(0.6f, 0.8f, 0.9f, 0.3f), 70f, 0.04f, 0.1f, new Vector2(0.05f, -0.25f), 0.25f, 14f, "Player", 40);
            Motes(vfx, "MarineSnow_Far", new Rect(386, -110, 650, 55), 12f, MatAlphaDot, C(0.5f, 0.75f, 0.9f, 0.4f), C(0.4f, 0.6f, 0.8f, 0.2f), 60f, 0.06f, 0.16f, new Vector2(0.05f, -0.2f), 0.2f, 16f, "Default", 40);
            Motes(vfx, "CaveDrips", new Rect(290, -50, 100, 6), 0f, MatAddDot, C(0.6f, 0.9f, 1f, 0.8f), C(0.8f, 1f, 1f, 0.6f), 4f, 0.05f, 0.09f, new Vector2(0f, -6f), 0f, 1.2f, "Player", 41);
            var bubblesP = Load<GameObject>(SrcBubbles);
            foreach (var x in new[] { 262f, 290f, 430f, 470f, 530f, 580f, 625f, 700f, 760f, 860f, 950f }) Inst(bubblesP, vfx, new Vector3(x, SeaSurfaceY(x) + 0.5f, -0.5f));
            Motes(vfx, "SeaBubbles", new Rect(386, -100, 650, 40), -1f, MatBubble, C(0.8f, 0.95f, 1f, 0.55f), C(1f, 1f, 1f, 0.35f), 10f, 0.08f, 0.22f, new Vector2(0f, 1.2f), 0.4f, 8f, "Player", 42);

            // ─── 8. 분위기/카메라/볼륨 ───
            Zone(zones, "LandingPool", new Rect(236, -70, 76, 30), 16, C(0.5f, 0.72f, 0.92f), 0.55f, C(0.03f, 0.08f, 0.12f), 1);
            Zone(zones, "DryCave", new Rect(312, -62, 80, 22), 16, C(0.55f, 0.68f, 0.85f), 0.5f, C(0.04f, 0.07f, 0.1f));
            Zone(zones, "Submerged", new Rect(392, -80, 80, 30), 18, C(0.32f, 0.62f, 0.85f), 0.45f, C(0.01f, 0.06f, 0.1f));
            Zone(zones, "Trench", new Rect(466, -96, 54, 32), 14, C(0.28f, 0.5f, 0.75f), 0.32f, C(0.005f, 0.03f, 0.06f));
            Zone(zones, "AbyssArena", new Rect(506, -104, 380, 60), 16, C(0.24f, 0.4f, 0.6f), 0.4f, C(0.0f, 0.015f, 0.035f), 1);
            Zone(zones, "Chase", new Rect(886, -104, 124, 60), 12, C(0.35f, 0.3f, 0.5f), 0.26f, C(0.02f, 0.01f, 0.03f), 1);
            Zone(zones, "Tunnel", new Rect(1008, -92, 32, 18), 8, C(0.3f, 0.45f, 0.6f), 0.2f, C(0.0f, 0.01f, 0.02f), 2);
            Zone(zones, "Ascent", new Rect(1030, -76, 14, 50), 10, C(0.5f, 0.75f, 0.95f), 0.5f, C(0.03f, 0.08f, 0.12f), 2);
            Zone(zones, "LightCrack", new Rect(1034, -34, 60, 40), 12, C(0.95f, 0.95f, 0.9f), 0.9f, C(0.35f, 0.5f, 0.6f), 3);
            CamZone(zones, "FallLanding", new Rect(240, -66, 40, 22), 8, 104f, new Vector3(0, -2f, 0), new Vector3(0.8f, 0.3f, 1), 2);
            CamZone(zones, "Corridor", new Rect(345, -58, 35, 10), 8, 92f, new Vector3(0, 1f, 0), new Vector3(1, 1, 1));
            CamZone(zones, "TrenchDrop", new Rect(462, -92, 20, 26), 6, 110f, new Vector3(0, -2f, 0), new Vector3(1, 0.4f, 1));
            CamZone(zones, "BossArena", new Rect(506, -100, 380, 40), 14, 150f, new Vector3(3f, 8f, 0), new Vector3(1.2f, 1f, 1));
            CamZone(zones, "Chase", new Rect(886, -100, 124, 40), 10, 124f, new Vector3(9f, 4f, 0), new Vector3(0.8f, 1f, 1), 2);
            CamZone(zones, "Tunnel", new Rect(1004, -94, 32, 16), 6, 92f, new Vector3(2f, 2f, 0), Vector3.one, 3);
            CamZone(zones, "Shaft", new Rect(1032, -92, 10, 64), 6, 108f, new Vector3(0, 4f, 0), new Vector3(1f, 0.5f, 1), 3);
            CamZone(zones, "Exit", new Rect(1040, -34, 50, 26), 8, 112f, new Vector3(4f, 3f, 0), Vector3.one, 2);
            var prof = Profile("VP_CATest_DeepSea", p => {
                var b = p.Add<Bloom>(true); b.intensity.Override(1.3f); b.threshold.Override(0.6f); b.scatter.Override(0.78f); b.tint.Override(C(0.7f, 0.95f, 1f));
                var v = p.Add<Vignette>(true); v.intensity.Override(0.42f); v.smoothness.Override(0.5f); v.color.Override(C(0f, 0.03f, 0.06f));
                var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0.1f); ca.contrast.Override(12f); ca.saturation.Override(5f); ca.colorFilter.Override(C(0.85f, 0.97f, 1f));
                var chrom = p.Add<ChromaticAberration>(true); chrom.intensity.Override(0.18f);
                var st = p.Add<SplitToning>(true); st.shadows.Override(C(0.15f, 0.35f, 0.55f)); st.highlights.Override(C(0.7f, 0.95f, 1f)); st.balance.Override(-20f);
                var fg2 = p.Add<FilmGrain>(true); fg2.intensity.Override(0.18f);
            });
            LocalVolume(zones, "Volume_DeepSea", new Rect(225, -132, 870, 140), prof, 14f, 2f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DeepSeaScenePath);
        }

        // ── DeepSea helpers ──
        private static void SizeBox(GameObject go, float w, float h) {
            var b = go.GetComponent<BoxCollider2D>();
            if (b == null) return;
            go.transform.localScale = Vector3.one;
            b.size = new Vector2(w, h);
            b.offset = Vector2.zero;
        }

        private static void ScatterOn(Transform parent, Random rnd, Sprite[] sprites, float x0, float x1, float gapMin, float gapMax,
            float scMin, float scMax, float zMin, float zMax, string layer, int order, Color a, Color b, System.Func<float, bool> skip, bool sway = false) {
            var x = x0 + R(rnd, 0, gapMin);
            while (x < x1) {
                if (!skip(x)) {
                    var s = Pick(rnd, sprites);
                    var sc = R(rnd, scMin, scMax);
                    var y = SeaSurfaceY(x) - 0.15f;
                    var sr = Put(parent, s, new Vector3(x, y - s.bounds.min.y * sc, R(rnd, zMin, zMax)), sc, layer, order + rnd.Next(3),
                        Color.Lerp(a, b, (float)rnd.NextDouble()), rnd.Next(2) == 0);
                    if (sway && sr != null) {
                        // 해초는 밑동을 축으로 흔들려야 하므로 피벗 오브젝트를 하나 끼운다
                        var pivot = new GameObject("KelpPivot").transform;
                        pivot.SetParent(parent, false);
                        pivot.position = new Vector3(x, y, sr.transform.position.z);
                        sr.transform.SetParent(pivot, true);
                        var am = pivot.gameObject.AddComponent<CATestAmbientMotion>();
                        am.swayAngle = R(rnd, 3f, 6f);
                        am.swaySpeed = R(rnd, 0.6f, 1f);
                    }
                }
                x += R(rnd, gapMin, gapMax);
            }
        }

        private static void WaterVolume(Transform parent, string name, Rect r) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(r.center.x, r.center.y, 0);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = r.size;
            var wv = go.AddComponent<CATestWaterVolume>();
            var bubbles = Particles(go.transform, "PlayerBubbles", go.transform.position, MatBubble, "Player", 30);
            var m = bubbles.main;
            m.loop = true; m.playOnAwake = false; m.startLifetime = 2.2f; m.startSpeed = 0f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f); m.startColor = C(0.85f, 0.97f, 1f, 0.8f);
            var em = bubbles.emission; em.rateOverTime = 3.5f;
            var sh = bubbles.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.15f;
            var v = bubbles.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            v.y = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); v.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); v.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var n = bubbles.noise; n.enabled = true; n.strength = 0.3f; n.frequency = 0.8f;
            var splash = Burst(go.transform, "Splash", new Vector3(r.center.x, r.yMax, -0.5f), MatBubble, C(0.85f, 0.97f, 1f, 0.9f), 40, 9f, 0.4f, 1.1f, "Player", 31, 1.2f);
            var ssh = splash.shape; ssh.shapeType = ParticleSystemShapeType.Cone; ssh.angle = 35f; ssh.rotation = new Vector3(-90f, 0, 0);
            Set(wv, "bubblesFollow", bubbles);
            Set(wv, "splash", splash);
        }

        private static void WaterVisual(Transform parent, Rect r, float surfaceY) {
            var src = Load<GameObject>("Assets/00. Member/LHS/_VFX_Lib/01. Prefabs/WaterSample.prefab");
            if (src != null) {
                var w = Inst(src, parent, new Vector3(r.center.x, (surfaceY + r.yMin) * 0.5f, -1.5f));
                w.name = "WaterArea_" + r.x;
                w.transform.localScale = new Vector3(r.width, surfaceY - r.yMin, 1f);
                foreach (var sr in w.GetComponentsInChildren<SpriteRenderer>(true)) {
                    sr.sortingLayerID = SL("Architecture");
                    sr.sortingOrder = 1;
                    sr.color = C(1f, 1f, 1f, 0.35f);
                }
            }
            // 수면선: 얇은 가산 발광 띠
            var line = Put(parent, Sp(ArtDir + "/CATest_Glow.png"), new Vector3(r.center.x, surfaceY, -1.2f), 1f, "Architecture", 2, C(0.6f, 0.95f, 1f, 0.35f), mat: MatAdd);
            if (line != null) line.transform.localScale = new Vector3(r.width / 2.56f * 1.1f, 0.12f, 1f);
        }

        // ─────────────────────────────── 보스 구역 지형 + 함정 ───────────────────────────────
        private static void BuildAbyssTerrain(Transform ground, Transform props, Transform gameplay, Random rnd, SpriteShape shapeSea, SpriteShape shapeStone,
            Color seaTint, Color caveTint) {
            var pitTint = Mul(seaTint, 0.6f);
            var rockTint = Mul(seaTint, 0.85f);
            // 바닥
            Terrain(ground, "H1_Floor", Close(DH1, -130), shapeSea, "Ground", 1, seaTint);
            Pit(ground, gameplay, rnd, "H2_Pit1", 626, 640, -94, pitTint);
            Terrain(ground, "H2_PlatformB", Close(DH2b, -130), shapeSea, "Ground", 1, seaTint);
            Pit(ground, gameplay, rnd, "H2_Pit2", 655, 668, -94, pitTint);
            Terrain(ground, "H2_PlatformC", Close(DH2c, -130), shapeSea, "Ground", 1, seaTint);
            Terrain(ground, "H3_UpperLedge", Close(DH3, -130), shapeSea, "Ground", 1, seaTint);
            Terrain(ground, "H3_LowerFloor", Close(DH3b, -130), shapeSea, "Ground", 1, seaTint);
            Pit(ground, gameplay, rnd, "H4_Trench", 780, 850, -96, pitTint);
            Terrain(ground, "H4_Island1", new Vector2[] { new(798, -86), new(806, -86), new(806.5f, -97), new(797.5f, -97) }, shapeSea, "Ground", 2, rockTint);
            Terrain(ground, "H4_Island2", new Vector2[] { new(822, -86), new(830, -86), new(830.5f, -97), new(821.5f, -97) }, shapeSea, "Ground", 2, rockTint);
            Terrain(ground, "H4_Runway", Close(DH4, -130), shapeSea, "Ground", 1, seaTint);
            Pit(ground, gameplay, rnd, "C_Pit1", 905, 917, -94, pitTint);
            Terrain(ground, "C_Hump", Close(DC2, -130), shapeSea, "Ground", 1, seaTint);
            Pit(ground, gameplay, rnd, "C_Pit2", 962, 974, -94, pitTint);
            Terrain(ground, "C_Pit2_Pillar", new Vector2[] { new(967, -88), new(969, -88), new(969.3f, -94.5f), new(966.7f, -94.5f) }, shapeSea, "Ground", 2, rockTint);
            Terrain(ground, "C_ToTunnel", Close(DC3, -130), shapeSea, "Ground", 1, seaTint);
            // 굴 + 상승 통로를 이루는 거대한 바위 덩어리 (입구 높이 1.4 → 웅크려야만 들어감)
            Terrain(ground, "TunnelMass", new Vector2[] {
                new(1010, -88.6f), new(1024, -88.6f), new(1024, -76), new(1034, -76), new(1034, -8), new(1006, -8), new(1004, -40), new(1008, -70), new(1009.2f, -86)
            }, shapeStone, "Ground", 2, Mul(caveTint, 0.7f));
            Terrain(ground, "ExitRock", new Vector2[] { new(1040, -30), new(1080, -30), new(1080, -130), new(1040, -130) }, shapeStone, "Ground", 2, Mul(caveTint, 0.8f));
            Terrain(ground, "EndWall", new Vector2[] { new(1080, 12), new(1100, 12), new(1100, -130), new(1080, -130) }, shapeStone, "Ground", 0, Mul(caveTint, 0.5f));
            Terrain(ground, "CrackCeilingL", new Vector2[] { new(1034, -8), new(1062, -8), new(1061, -5), new(1034, 12) }, shapeStone, "Ground", 2, Mul(caveTint, 0.6f));
            Terrain(ground, "CrackCeilingR", new Vector2[] { new(1068, -8), new(1080, -8), new(1080, 12), new(1069, -5) }, shapeStone, "Ground", 2, Mul(caveTint, 0.6f));
            // 기포 분출구 선반 (해구 → 유물)
            Terrain(ground, "VentShelf", new Vector2[] { new(519.5f, -67), new(531, -66.8f), new(530.5f, -68.6f), new(520, -68.8f) }, shapeSea, "Ground", 2, seaTint);

            // ── H1 숨는 법 ──
            Arch(ground, props, rnd, 550, -88f, rockTint);
            Kelp(gameplay, 565f, -88f);
            Arch(ground, props, rnd, 582, -88.2f, rockTint);
            Kelp(gameplay, 600f, SurfaceY(DH1, 600));
            Arch(ground, props, rnd, 618, SurfaceY(DH1, 618), rockTint);
            // ── H2 성게 구덩이 + 역류 ──
            Arch(ground, props, rnd, 647.5f, -83.8f, rockTint);
            Gust(gameplay, "H2_Surge", new Rect(652, -88, 20, 16), new Vector2(-6.5f, 0f), 1.9f, 1.8f);
            Kelp(gameplay, 681f, -86.2f);
            // ── H3 산호 벽 등반 + 윗길 지붕 + 해파리 바닥 ──
            ClimbEdge(ground, new Vector2(705f, -87.6f), new Vector2(705f, -72f), "H3_CoralWall");
            CoralVine(props, rnd, 705f, -87.5f, -72.5f);
            Terrain(ground, "H3_Overhang", new Vector2[] { new(711, -65.2f), new(731, -65.4f), new(730.5f, -66.8f), new(711.5f, -66.6f) }, shapeSea, "Ground", 3, rockTint);
            Kelp(gameplay, 748f, -90.2f);
            Kelp(gameplay, 766f, -90f);
            Jelly(gameplay, "H3_Jelly1", 742f, -88.3f, 9f, 3.2f);
            Jelly(gameplay, "H3_Jelly2", 770f, -88.3f, 9f, 3.6f);
            // ── H4 성게 해구: 무너지는 석판 + 바위섬(아치) + 역류 + 위쪽 해파리 ──
            foreach (var x in new[] { 785.5f, 792f, 812f, 817f, 836f, 843f }) Inst(PCrumble, gameplay, new Vector3(x, -86f, 0)).name = $"H4_Crumble_{x}";
            Arch(ground, props, rnd, 802f, -86f, rockTint);
            Arch(ground, props, rnd, 826f, -86f, rockTint);
            Gust(gameplay, "H4_Surge", new Rect(780, -92, 70, 14), new Vector2(-5f, 0f), 2.4f, 1.6f, 0.7f);
            Jelly(gameplay, "H4_Jelly", 816f, -80.5f, 22f, 3.4f);
            Arch(ground, props, rnd, 860f, -88.1f, rockTint);
            Kelp(gameplay, 874f, -88f);
            // ── 추격 코스 ──
            var rockS = Sp(DG + "rocks.png", "rocks_2");
            FallingRock(gameplay, "C_FallingRock1", 950f, -62f, -80f, new Rect(942, -84, 7, 10), rockS, 0.6f, C(0.4f, 0.5f, 0.58f));
            FallingRock(gameplay, "C_FallingRock2", 983f, -62f, -89.5f, new Rect(976, -92, 6, 10), rockS, 0.6f, C(0.4f, 0.5f, 0.58f));
            // ── 상승 통로: 좌우 산호 패치를 번갈아 (벽점프) + 기포 분출구 ──
            foreach (var (x, y0, y1) in new[] { (1040f, -89.5f, -79f), (1034f, -75.5f, -64f), (1040f, -66f, -54f), (1034f, -58f, -46f), (1040f, -50f, -30f) }) {
                ClimbEdge(ground, new Vector2(x, y0), new Vector2(x, y1), $"Shaft_Coral_{y0}");
                CoralVine(props, rnd, x, y0, y1);
            }
            var vent = Inst(PCurrentColumn, gameplay, new Vector3(1037f, -90f, 0)); vent.name = "Shaft_BubbleVent";
            var vb = vent.GetComponent<BoxCollider2D>(); vb.size = new Vector2(4.5f, 30f); vb.offset = new Vector2(0, 15f);
            Inst(PSaveSea, gameplay, new Vector3(1052f, -30f, 0)).name = "SavePoint_06_LightCrack";
            // 마지막 챕터로: 틈으로 쏟아지는 흰 빛 웅덩이에 들어서면 빛에 삼켜져 "황혼의 성역"으로 (CATestRealmTransition)
            var realm = new GameObject("RealmTransition_ToTwilight");
            realm.transform.SetParent(gameplay, false);
            realm.transform.position = new Vector3(1066f, -27f, 0f);
            var rbox = realm.AddComponent<BoxCollider2D>();
            rbox.isTrigger = true;
            rbox.size = new Vector2(6f, 6f);
            var rt = realm.AddComponent<CATestRealmTransition>();
            Set(rt, "arrival", TwArrival);
            GodRay(gameplay, "RealmLight", 1066f, -30f, 24f, 5f, 0f, C(1f, 0.97f, 0.93f), 1.6f, 13f, "Default", 44, 1f, 0.6f, 4f, 1.4f);
            Light(gameplay, "RealmPool", new Vector3(1066f, -28f, 0f), C(1f, 0.95f, 0.9f), 1.4f, 8f, 1.5f);
            Motes(gameplay, "RealmMotes", new Rect(1062f, -30f, 8f, 10f), -0.3f, MatAddDot, C(1f, 1f, 1f, 0.8f), C(1f, 0.9f, 0.85f, 0.5f),
                18f, 0.04f, 0.1f, new Vector2(0f, 0.8f), 0.3f, 3f, "Player", 40);
        }

        private static void Pit(Transform ground, Transform gameplay, Random rnd, string name, float x0, float x1, float floorY, Color tint) {
            Terrain(ground, name + "_Floor", new Vector2[] { new(x0, floorY), new(x1, floorY), new(x1, -130), new(x0, -130) }, null, "Ground", 1, tint);
            var u = Inst(PUrchinSea, gameplay, new Vector3((x0 + x1) * 0.5f, floorY + 0.7f, 0));
            u.name = name + "_Urchins";
            ThornArt(u.transform, x1 - x0, rnd, true);
            SizeBox(u, x1 - x0, 1.4f);
            // 구덩이 바닥 비주얼 (셰이프 없이 만든 콜라이더 위에 어두운 모래)
            Terrain(ground, name + "_Visual", new Vector2[] { new(x0 - 0.2f, floorY + 0.2f), new(x1 + 0.2f, floorY + 0.2f), new(x1 + 0.2f, -110), new(x0 - 0.2f, -110) },
                Load<SpriteShape>(FSB + "/Underwater area pack/Sprite shapes/Underworld ground.asset"), "Ground", 0, tint, false);
        }

        /// <summary>엄폐용 바위 아치: 지붕 슬랩만 판정(Ground 레이어 → 눈의 레이캐스트를 막음), 다리는 뒤쪽 비주얼.</summary>
        private static void Arch(Transform ground, Transform props, Random rnd, float x, float floorY, Color tint) {
            var shape = Load<SpriteShape>(FSB + "/Underwater area pack/Sprite shapes/Underworld ground.asset");
            Terrain(ground, $"CoverArch_{x}", new Vector2[] {
                new(x - 3.2f, floorY + 4.7f), new(x + 3.2f, floorY + 4.6f), new(x + 3f, floorY + 3.4f), new(x - 3f, floorY + 3.4f)
            }, shape, "Ground", 3, tint);
            Put(props, Sp(UW + "Ground elements.png", "Ground elements_2"), new Vector3(x, floorY + 2.4f, 0.6f), 0.43f, "Item", 1, C(0.45f, 0.6f, 0.62f), rnd.Next(2) == 0);
        }

        private static void Kelp(Transform gameplay, float x, float floorY) =>
            Inst(PKelpCover, gameplay, new Vector3(x, floorY, 0)).name = $"KelpCover_{x}";

        private static void Jelly(Transform gameplay, string name, float x, float y, float distance, float speed) {
            var j = Inst(PJelly, gameplay, new Vector3(x, y, 0));
            j.name = name;
            Set(j.GetComponent<ElectronicEnemy>(), "moveDistance", distance);
            Set(j.GetComponent<ElectronicEnemy>(), "moveSpeed", speed);
        }

        /// <summary>등반 가능한 산호/해초 줄기 비주얼 (판정은 ClimbEdge).</summary>
        private static void CoralVine(Transform parent, Random rnd, float x, float y0, float y1) {
            var kelp = Sps(UW + "Seaweed.png", "Seaweed", 2, 3, 4, 5);
            for (var y = y0; y < y1; y += 2.2f) {
                var sr = Put(parent, Pick(rnd, kelp), new Vector3(x + R(rnd, -0.25f, 0.25f), y, -0.2f), R(rnd, 0.16f, 0.22f), "Ground", 22,
                    C(0.45f, 0.95f, 0.8f), rnd.Next(2) == 0, R(rnd, -15f, 15f));
                if (sr != null) sr.gameObject.AddComponent<CATestAmbientMotion>().swayAngle = 2f;
            }
            Light(parent, "CoralGlow", new Vector3(x, (y0 + y1) * 0.5f, -0.5f), C(0.4f, 1f, 0.85f), 0.35f, Mathf.Max(4f, (y1 - y0) * 0.6f), 0.5f);
        }

        // ─────────────────────────────── 보스: 찾아다니는 눈 + 감독 ───────────────────────────────
        private static void BuildEyeHunter(Transform parent) {
            var go = new GameObject("AbyssEye_Hunter");
            go.transform.SetParent(parent, false);
            var hunter = go.AddComponent<CATestEyeHunter>();

            var eyeRoot = new GameObject("EyeRoot").transform;
            eyeRoot.SetParent(go.transform, false);
            eyeRoot.position = new Vector3(545f, -55f, 40f);
            var body = Put(eyeRoot, Sp(ArtDir + "/CATest_EyeBody.png"), eyeRoot.position + Vector3.forward * 0.5f, 3.2f, "BackGround", -30, C(0.45f, 0.4f, 0.5f));
            var ball = new GameObject("Eyeball").transform;
            ball.SetParent(eyeRoot, false);
            ball.localPosition = Vector3.zero;
            var ballSr = Put(ball, Sp(ArtDir + "/CATest_Eyeball.png"), ball.position + Vector3.forward * 0.2f, 1.8f, "BackGround", -28, C(0.72f, 0.72f, 0.66f));
            var iris = Put(ball, Sp(ArtDir + "/CATest_Iris.png"), ball.position, 2.1f, "BackGround", -27, Color.white, mat: MatAlpha);
            var lidSprite = Sp(ArtDir + "/CATest_EyeLid.png");
            var lidTopPivot = new GameObject("LidTop").transform;
            lidTopPivot.SetParent(eyeRoot, false);
            lidTopPivot.localPosition = new Vector3(0, 4.8f, -0.1f);
            var lt = Put(lidTopPivot, lidSprite, lidTopPivot.position + new Vector3(0, -2.45f, 0), 1f, "BackGround", -26, C(0.36f, 0.3f, 0.4f));
            if (lt != null) lt.transform.localScale = new Vector3(1.65f, 1.2f, 1f);
            var lidBotPivot = new GameObject("LidBottom").transform;
            lidBotPivot.SetParent(eyeRoot, false);
            lidBotPivot.localPosition = new Vector3(0, -4.8f, -0.1f);
            var lb = Put(lidBotPivot, lidSprite, lidBotPivot.position + new Vector3(0, 2.45f, 0), 1f, "BackGround", -26, C(0.3f, 0.26f, 0.35f));
            if (lb != null) lb.transform.localScale = new Vector3(1.65f, -1.2f, 1f);
            var eyeGlow = Light(eyeRoot, "EyeGlow", eyeRoot.position + Vector3.back, C(0.55f, 0.95f, 1f), 0.4f, 30f, 3f);
            var halo = Put(eyeRoot, Glow, eyeRoot.position + Vector3.back * 0.3f, 7f, "BackGround", -25, C(0.4f, 0.8f, 1f, 0.18f), mat: MatAdd);

            var origin = new GameObject("ScanOrigin").transform;
            origin.SetParent(go.transform, false);
            origin.position = new Vector3(545f, -62f, 0f);
            var beamLight = Light(go.transform, "BeamLight", origin.position, C(0.55f, 0.95f, 1f), 1.2f, 44f, 4f, 0.4f);
            beamLight.pointLightInnerAngle = 12f;
            beamLight.pointLightOuterAngle = 22f;
            var beam = new GameObject("BeamMesh");
            beam.transform.SetParent(go.transform, false);
            beam.transform.position = Vector3.zero;
            var mf = beam.AddComponent<MeshFilter>();
            var mr = beam.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MatBeam;
            mr.sortingLayerID = SL("Default");
            mr.sortingOrder = 80;

            Set(hunter, "eyeRoot", eyeRoot);
            Set(hunter, "eyeball", ball);
            Set(hunter, "iris", iris != null ? iris.transform : null);
            Set(hunter, "irisRenderer", iris);
            Set(hunter, "lidTop", lidTopPivot);
            Set(hunter, "lidBottom", lidBotPivot);
            Set(hunter, "bodyRenderers", new Object[] { body, ballSr, lt, lb, halo });
            Set(hunter, "eyeGlow", eyeGlow);
            Set(hunter, "origin", origin);
            Set(hunter, "beamLight", beamLight);
            Set(hunter, "beamMesh", mf);
            Set(hunter, "coverMask", 1 << LayerGround);
            Set(hunter, "visualLift", 1.5f); // 눈을 시야 원점 바로 위에 → 화면 안에 들어오게
            // 앵커: 시야 원점이 머무는 곳 (바닥에서 약 18 위 — 화면 안에 눈이 들어오도록 낮춤). 뒤로 갈수록 훑는 속도가 빨라진다(sweepGain).
            Set(hunter, "anchors", new[] {
                new Vector2(548, -70), new Vector2(590, -70), new Vector2(640, -68), new Vector2(682, -68), new Vector2(722, -56),
                new Vector2(762, -71), new Vector2(805, -69), new Vector2(840, -69), new Vector2(870, -70)
            });

            var dirGo = new GameObject("BossDirector");
            dirGo.transform.SetParent(parent, false);
            var dir = dirGo.AddComponent<CATestBossDirector>();
            Set(dir, "hunter", hunter);
            Set(dir, "introArea", new Rect(521, -92, 9, 12));
            Set(dir, "rageArea", new Rect(878, -100, 6, 40));
            Set(dir, "escapeArea", new Rect(1025, -92, 8, 14));
            Set(dir, "tunnelMouth", new Vector2(1010, -89f));
            Set(dir, "tunnelSafeX", 1011.5f);
        }
    }
}
#endif
