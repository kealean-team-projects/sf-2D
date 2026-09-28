#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 숲 씬 2 : 몽환의 숲 → 산 (x -470 ~ -100, 정상 y ≈ 84)
    ///
    ///  C0 몽환의 숲(-470~-397)   : 안개가 걷힌 뒤의 푸른 밤빛 숲. 세이브 #4. 발광 버섯/반딧불
    ///  C1 잊힌 유적(-420~-336)   : 돌을 밀어 석단에 오름 → 유적 오두막 지붕을 넘어 → 반대편에서 금 간 벽(E ×2) 안 유물
    ///                              → 무너지는 돌다리로 가시 구덩이 건너기
    ///  C2 상승 기류·덩굴(-336~-246): 가시 바닥 위 상승 기류로 높은 턱 → 덩굴 두 개 건너기 → 능선의 돌풍(웅크림)
    ///  D1 산 절벽(-246~-214)      : 덩굴 절벽 등반 → 떨어지는 바위가 있는 좁은 길
    ///  D2 산 굴뚝(-219~-203)      : 좌우 덩굴 패치 벽점프 (y 46 → 68)
    ///  D3 마른 덩굴 절벽(-203~-188): 부서지는 덩굴 → 등반 대쉬 (y 68 → 82)
    ///  D4 정상 능선·정상(-188~-100): 강한 돌풍(밀리면 아래 턱으로 떨어짐) → 세이브 #6 → 스톤헨지 → 구덩이(페이드 → 심해)
    /// </summary>
    public static partial class CATestBuilder {
        private static readonly Color DreamFog = new(0.42f, 0.47f, 0.62f);

        public static void BuildForest2Scene() {
            Surfaces.Clear();
            KeepClear.Clear();
            PendingGrounds.Clear();
            var scene = NewMapScene(Forest2ScenePath);
            var rnd = new Random(7771);
            var root = new GameObject("CATest_Forest2_DreamMountain").transform;
            var ground = Group(root, "10_Terrain").transform;
            var gameplay = Group(root, "20_Gameplay").transform;
            var props = Group(root, "30_Props_Play").transform;
            var nearBg = Group(root, "40_NearBG").transform;
            var midBg = Group(root, "50_MidBG").transform;
            var farBg = Group(root, "60_FarBG").transform;
            var fg = Group(root, "05_Foreground").transform;
            var vfx = Group(root, "70_VFX_Light").transform;
            var zones = Group(root, "80_Zones").transform;

            var shapeOld = Load<SpriteShape>(OFShapes + "OldMiddleGround.asset");
            var shapeOldGrass = Load<SpriteShape>(OFShapes + "OLDgrass.asset");
            var shapeStone = Load<SpriteShape>(FSB + "/Dungeon pack/Sprite shapes/Sone ground.asset");
            var shapeBark = shapeOld;

            var dream = C(0.74f, 0.76f, 0.8f);
            var stoneT = C(0.76f, 0.75f, 0.72f);
            var mountT = C(0.82f, 0.78f, 0.76f);

            // ═════════════════ C0 몽환의 숲 ═════════════════
            var gC0 = RoughSurf(1.3f, new[] { (-466f, -458f), (-416f, -397f) }, -470, 12, -462, 12, -452, 11.5f, -440, 8.5f, -430, 6.8f, -420, 5, -410, 4.8f, -397, 4.6f);
            Ground(ground, "C0_Hollow", gC0, -30, shapeOld, dream);
            Clear(-465.0f, -459.0f);
            Inst(PSaveForest, gameplay, new Vector3(-462, 12, 0)).name = "SavePoint_04_DreamForest";
            AreaTitle(zones, new Rect(-470, -10, 70, 60), "몽환의 숲", "안개가 걷힌 자리");
            Say(zones, new Rect(-468, 6, 14, 16), "dream_enter");

            // ═════════════════ C1 잊힌 유적 ═════════════════
            AreaTitle(zones, new Rect(-400, 0, 64, 40), "잊힌 유적");
            Clear(-422.0f, -388.0f);
            var rock = Inst(PPushRock, gameplay, new Vector3(-412, 4.8f + 1.3f, 0));
            rock.name = "C1_PushRock";
            rock.AddComponent<CATestRespawnReset>();
            Ground(ground, "C1_Terrace1", Surf(-397, 13.5f, -372, 13.5f), -30, shapeStone, stoneT, 2, 0.5f);
            Block(ground, "C1_HutWallL", -392, -390.5f, 13.5f, 18, shapeStone, Mul(stoneT, 0.85f), 3, 0.3f);
            Ledge(ground, "C1_HutRoof", -392, -384, 18, 19, shapeStone, stoneT, 3, 0.4f);
            Clear(-388.6f, -382.6f);
            var wall = Inst(PBreakWall, gameplay, new Vector3(-385.6f, 13.5f, 0));
            wall.name = "C1_CrackedWall";
            Clear(-391.5f, -385.5f);
            Inst(PRelicForest, gameplay, new Vector3(-388.5f, 15f, 0)).name = "Relic_04_RuinHut";
            Ground(ground, "C1_ThornPit", Surf(-372, 0, -354, 0), -30, shapeOld, Mul(dream, 0.5f));
            Thorns(gameplay, rnd, "C1_Thorns", -363, 0, 18, false);
            for (var i = 0; i < 6; i++) Inst(PCrumble, gameplay, new Vector3(-370.5f + i * 3f, 13.5f, 0)).name = $"C1_CrumbleSlab_{i}";
            Inst(PWisp, gameplay, new Vector3(-361, 20.5f, 0)).name = "C1_Wisp_Bridge";
            Ground(ground, "C1_Terrace2", Surf(-354, 13.5f, -336, 13.5f), -30, shapeStone, stoneT, 2, 0.5f);

            // ═════════════════ C2 상승 기류 · 덩굴 · 능선 돌풍 ═════════════════
            Ground(ground, "C2_Low", Surf(-336, 4, -322, 4), -30, shapeOld, dream);
            Ground(ground, "C2_UpdraftFloor", Surf(-322, 2.5f, -310, 2.5f), -30, shapeOld, Mul(dream, 0.6f));
            Thorns(gameplay, rnd, "C2_UpdraftThorns", -316, 2.5f, 12, false);
            var up = Inst(PWindColumn, gameplay, new Vector3(-316, 2.5f, 0));
            up.name = "C2_Updraft";
            var ub = up.GetComponent<BoxCollider2D>();
            ub.size = new Vector2(8.2f, 30f);
            ub.offset = new Vector2(0, 15f);
            Ledge(ground, "C2_HighLedge", -309, -296, 26.5f, 28, shapeStone, stoneT, 3, 0.5f);
            Ground(ground, "C2_Chasm", Surf(-296, 0, -274, 0), -30, shapeOld, Mul(dream, 0.5f));
            Thorns(gameplay, rnd, "C2_ChasmThorns", -285, 0, 22, false);
            Block(ground, "C2_CanopyBranch", -293, -277, 38, 39.2f, shapeBark, C(0.5f, 0.45f, 0.45f), 3, 0.4f);
            Liana(gameplay, rnd, "C2_Liana1", -289, 38, 22);
            Liana(gameplay, rnd, "C2_Liana2", -281.5f, 38, 22);
            var gRidge = RoughSurf(0.8f, new[] { (-256f, -246f) }, -274, 29, -262, 29.8f, -246, 29.5f);
            Ground(ground, "C2_Ridge", gRidge, -30, shapeStone, mountT, 2, 0.5f);
            Gust(gameplay, "C2_RidgeGust", new Rect(-274, 29.5f, 24, 5), new Vector2(-11f, 0f), 2.0f, 1.8f);
            Clear(-255.0f, -249.0f);
            Inst(PSaveForest, gameplay, new Vector3(-252, 29.5f, 0)).name = "SavePoint_05_MountainFoot";

            // ═════════════════ D1 산 절벽 ═════════════════
            AreaTitle(zones, new Rect(-252, 25, 60, 70), "바람의 산", "정상까지 오르자");
            Say(zones, new Rect(-252, 28, 8, 8), "mountain_enter");
            var gD1 = RoughSurf(0.9f, new[] { (-219f, -203f) }, -246, 46, -236, 46.8f, -226, 46.2f, -214, 46, -203, 46);
            Ground(ground, "D1_Mountain", gD1, -30, shapeStone, mountT, 1, 0.6f);
            VineWall(gameplay, rnd, "D1_Climb_Cliff", -246, 29.5f, 46, -1);
            var rockS = Sp(DG + "rocks.png", "rocks_2");
            FallingRock(gameplay, "D1_FallingRock1", -236, 58, SurfaceY(gD1, -236), new Rect(-241, 46, 8, 5), rockS, 0.55f, C(0.7f, 0.68f, 0.65f));
            FallingRock(gameplay, "D1_FallingRock2", -224, 59, SurfaceY(gD1, -224), new Rect(-229, 46, 7, 5), rockS, 0.6f, C(0.7f, 0.68f, 0.65f));

            // ═════════════════ D2 산 굴뚝 ═════════════════
            Block(ground, "D2_ChimL", -219, -214, 50, 68, shapeStone, Mul(mountT, 0.9f), 3, 0.5f);
            Block(ground, "D2_ChimR", -209.5f, -203, 46, 68, shapeStone, Mul(mountT, 0.9f), 3, 0.5f);
            Surf(-209.5f, 68, -203, 68);
            VineWall(gameplay, rnd, "D2_R1", -209.5f, 46.5f, 53, -1);
            VineWall(gameplay, rnd, "D2_L1", -214, 51, 56.5f, 1);
            VineWall(gameplay, rnd, "D2_R2", -209.5f, 56, 61.5f, -1);
            VineWall(gameplay, rnd, "D2_L2", -214, 61, 66.5f, 1);
            VineWall(gameplay, rnd, "D2_R3", -209.5f, 65.5f, 68, -1);

            // ═════════════════ D3 마른 덩굴 절벽 ═════════════════
            Ground(ground, "D3_Ledge", Surf(-203, 68, -188, 68), -30, shapeStone, mountT, 1, 0.6f);
            var gD4 = RoughSurf(1f, new[] { (-160f, -152f) }, -188, 82, -176, 83f, -160, 84, -152, 84.5f);
            Ground(ground, "D4_Summit", gD4, -30, shapeStone, mountT, 1, 0.6f);
            VineWall(gameplay, rnd, "D3_Climb_Lower", -188, 68, 70, -1);
            CrumbleVine(gameplay, rnd, "D3_CrumbleVines", -188, 70, 82, -1, 0.8f);

            // ═════════════════ D4 정상 ═════════════════
            Gust(gameplay, "D4_SummitGust", new Rect(-188, 82, 26, 5), new Vector2(-12f, 0f), 1.8f, 2.0f, 1f);
            Clear(-160.0f, -154.0f);
            Inst(PSaveForest, gameplay, new Vector3(-157, SurfaceY(gD4, -157), 0)).name = "SavePoint_06_Summit";
            var gRim = RoughSurf(1.2f, null, -138, 84.5f, -120, 85.5f, -100, 85.5f);
            Ground(ground, "D4_Rim", gRim, -30, shapeStone, mountT, 1, 0.6f);
            Block(ground, "WallRight", -100, -80, -30, 150, shapeStone, Mul(mountT, 0.6f), 0);
            Ground(ground, "D4_PitSafetyFloor", Surf(-152, 10, -138, 10), -30, shapeStone, Mul(mountT, 0.3f));
            FallZone(gameplay, new Rect(-152, 10, 14, 76), 18f);
            PitTransition(gameplay, new Rect(-152, 30, 14, 48), new Vector2(269f, -28f));
            AreaTitle(zones, new Rect(-170, 80, 70, 40), "산 정상", "심연으로 통하는 구멍");
            Say(zones, new Rect(-168, 82, 12, 8), "summit_pit");

            var dz = Inst(PDeadZone, gameplay, new Vector3(-290, -36, 0));
            if (dz != null) { dz.name = "DeadZone_Bottom"; dz.transform.localScale = Vector3.one; dz.GetComponent<BoxCollider2D>().size = new Vector2(420, 4); }

            // ═════════════════ 식생 / 소품 ═════════════════
            bool Hazard(float x) => (x > -372 && x < -354) || (x > -322 && x < -310) || (x > -296 && x < -274) || (x > -152 && x < -138);
            var oldGreens = Sps(OF + "Old greens.png", "Old greens", 0, 3, 4, 21); // 22~25 는 사선 덩굴이라 바닥 소품으로 어색 → 제외
            var smallGreens = Sps(OF + "Old greens.png", "Old greens", 7, 8, 10); // 낮은 풀 띠만 (1,5 흰 석판·12 뜬 잎뭉치 제외)
            var vox = Sps(OF + "Old Vox.png", "Old Vox", 0, 1, 2);
            var fallen = Sps(OF + "Fallen leafs.png", "Fallen leafs", 0, 1, 2, 3);
            ClusterSurf(props, rnd, oldGreens, -470, -100, 5f, 12f, 3, 0.6f, 0.95f, -0.6f, -0.3f, "Architecture", 2, C(0.7f, 0.78f, 0.75f), C(0.95f, 1f, 0.95f), 0.15f, 0.5f, Hazard);
            ClusterSurf(props, rnd, smallGreens, -470, -246, 3f, 8f, 4, 0.7f, 1.1f, -0.2f, 0.2f, "Item", 15, C(0.5f, 0.75f, 0.7f), C(0.7f, 0.95f, 0.85f), 0f, 0.2f, Hazard);
            ClusterSurf(props, rnd, vox, -470, -100, 9f, 18f, 2, 0.45f, 0.75f, 1f, 2.2f, "Item", 0, C(0.6f, 0.65f, 0.68f), C(0.8f, 0.82f, 0.82f), 0.3f, 0.6f, Hazard);
            ClusterSurf(props, rnd, fallen, -470, -100, 6f, 14f, 3, 0.5f, 0.8f, 0.3f, 0.6f, "Item", 10, C(0.8f, 0.75f, 0.65f), C(0.95f, 0.88f, 0.72f), 0.1f, 0.3f, Hazard);
            GrassOn(ground, shapeOldGrass, gC0, C(0.72f, 0.82f, 0.75f));
            GrassOn(ground, shapeOldGrass, gRidge, C(0.8f, 0.85f, 0.75f));
            GrassOn(ground, shapeOldGrass, gD1, C(0.8f, 0.85f, 0.75f));
            GrassOn(ground, shapeOldGrass, gD4, C(0.85f, 0.88f, 0.78f));
            GrassOn(ground, shapeOldGrass, gRim, C(0.85f, 0.88f, 0.78f));
            // 발광 버섯(명암 대비의 핵심: 어두운 숲 속 밝은 점)
            var mush = Sps(DG + "dungeon items.png", "dungeon items", 8, 9, 12, 13, 15);
            foreach (var x in new[] { -466f, -455f, -441f, -430f, -417f, -404f, -345f, -330f, -300f, -266f }) {
                var y = SurfAt(x, 40);
                if (float.IsNaN(y)) continue;
                var s = Pick(rnd, mush);
                var sc = R(rnd, 0.4f, 0.6f);
                Put(props, s, new Vector3(x, y - 0.05f - s.bounds.min.y * sc, R(rnd, 0.1f, 0.4f)), sc, "Item", 20, C(0.85f, 1f, 0.95f), rnd.Next(2) == 0);
                Light(vfx, "MushroomGlow", new Vector3(x, y + 0.9f, 0), C(0.35f, 1f, 0.85f), R(rnd, 0.9f, 1.3f), R(rnd, 4f, 5.5f), 0.3f);
            }
            // 유적 기둥
            var pillarsTall = Sps(OF + "Old stounes.png", "Old stounes", 0, 1, 2, 3, 8, 9, 10);
            var pillarsShort = Sps(OF + "Old stounes.png", "Old stounes", 12, 13, 14, 15);
            foreach (var x in new[] { -395f, -377f, -351f, -340f }) {
                var s = Pick(rnd, pillarsTall);
                var sc = R(rnd, 0.95f, 1.2f);
                Put(nearBg, s, new Vector3(x, 13.4f - s.bounds.min.y * sc, R(rnd, 1.5f, 3f)), sc, "Default", 10, C(0.72f, 0.72f, 0.72f), rnd.Next(2) == 0);
            }
            StoneCircle(nearBg, rnd, -186, -156, 83.5f, pillarsTall, pillarsShort, C(0.75f, 0.7f, 0.75f));
            StoneCircle(nearBg, rnd, -134, -104, 85f, pillarsTall, pillarsShort, C(0.75f, 0.7f, 0.75f));
            var rimStones = Sps(DG + "rocks.png", "rocks", 2, 5, 7, 9);
            foreach (var x in new[] { -153f, -150.5f, -140f, -137.5f })
                Put(props, Pick(rnd, rimStones), new Vector3(x, 84.8f, R(rnd, -0.3f, 0.5f)), R(rnd, 0.6f, 0.9f), "Item", 12, C(0.6f, 0.6f, 0.6f), rnd.Next(2) == 0);

            // ═════════════════ 배경 ═════════════════
            var bgImg = Sp(OF + "background.png");
            // 몽환의 숲 구간에만 원경 판(산 구간에서는 판의 윗선이 수평으로 드러나므로 두지 않음, 하늘은 카메라 배경색)
            for (var x = -620f; x < -330f; x += 245f) Put(farBg, bgImg, new Vector3(x, 25f, 260f), 18f, "BackGround", -200, C(0.48f, 0.52f, 0.68f), (int)(x / 245f) % 2 == 0);
            var mount = Sp(FF + "mount big.png");
            Put(farBg, mount, new Vector3(-190, 40, 230), 6f, "BackGround", -190, C(0.72f, 0.66f, 0.76f));
            Put(farBg, mount, new Vector3(-60, 30, 250), 6.5f, "BackGround", -191, C(0.66f, 0.6f, 0.72f), true);
            var farTrees = Sps(OF + "Old forest far back tree.png", "Old forest far back tree", 0, 1, 2);
            var backTrees = Sps(OF + "Old forest back tree.png", "Old forest back tree", 1, 2, 3, 4, 5);
            var midTrees = Sps(OF + "Old forest midle tree.png", "Old forest midle tree", 0, 1, 2);
            var farLeaves = Sps(OF + "Old forest far back leafs blur.png", "Old forest far back leafs blur", 0, 1, 2, 3, 4, 5);
            var backLeaves = Sps(OF + "Old forest back leafs.png", "Old forest back leafs", 0, 1, 2, 3, 4, 5);
            var midLeaves = Sps(OF + "Old forest midle leafs.png", "Old forest midle leafs", 0, 1, 2, 3, 4, 5, 6);
            ScatterBand(farBg, rnd, farTrees, -500, -240, 14, 24, -24, -10, 2.8f, 3.8f, 110, 150, "BackGround", -150, Toward(C(0.3f, 0.34f, 0.42f), DreamFog, 0.55f), Toward(C(0.36f, 0.4f, 0.48f), DreamFog, 0.65f));
            // (피드백: 공중에 잎만 떠 있어 어색함) → 잎 뭉치는 반드시 줄기 끝에 붙인 "나무"로만 배치한다.
            //   먼 줄 : 흐린 나무 실루엣(farTrees)만 — 잎만 따로 뿌리던 farLeaves 제거
            //   중간 줄: back tree 줄기 + back leafs 를 가지 끝에
            //   가까운 줄: midle tree 줄기(가지가 굵은 고목) + midle leafs 를 가지 끝에, 지면 높이에 맞춰 심는다
            for (var x = -490f + R(rnd, 0f, 8f); x < -250f; x += R(rnd, 9f, 17f)) {
                var z = R(rnd, 35f, 60f);
                var gb = SurfLow(x, 40f);
                if (float.IsNaN(gb)) continue;
                DreamTree(midBg, rnd, backTrees, backLeaves, x, gb - ParallaxSink(z), z, R(rnd, 1.5f, 2.1f),
                    Toward(C(0.38f, 0.36f, 0.4f), DreamFog, 0.35f), Toward(C(0.48f, 0.5f, 0.42f), DreamFog, 0.3f), "BackGround", -80, 0.9f);
            }
            for (var x = -472f + R(rnd, 0f, 6f); x < -250f; x += R(rnd, 15f, 26f)) {
                if (!TreeGround(x, 2.5f, 40f, out var gy)) continue;
                var zn = R(rnd, 5f, 12f);
                DreamTree(nearBg, rnd, midTrees, midLeaves, x, gy - ParallaxSink(zn), zn, R(rnd, 1.1f, 1.45f),
                    Toward(C(0.8f, 0.82f, 0.88f), DreamFog, 0.12f), C(0.78f, 0.8f, 0.68f), "Default", -12, 1f);
            }
            // 산: 뒤쪽 바위산 실루엣 + 구름바다
            var farRocks = Sps(DG + "dungeon background.png", "dungeon background", 9, 11, 12);
            ScatterBand(midBg, rnd, farRocks, -260, -80, 14, 24, 10, 40, 3.5f, 5f, 50, 85, "BackGround", -90, C(0.52f, 0.48f, 0.56f), C(0.6f, 0.55f, 0.62f));
            var clouds = Sps(DG + "dungeon items 2.png", "dungeon items 2", 2, 11);
            ScatterBand(midBg, rnd, clouds, -280, -60, 10, 16, 22, 45, 2.5f, 3.8f, 25, 60, "BackGround", -85, C(1f, 0.9f, 0.9f, 0.55f), C(1f, 0.95f, 0.95f, 0.75f), false);
            var pitDark = Put(nearBg, Sp(DG + "Square.png"), new Vector3(-145, 45f, 3f), 1f, "Default", -25, C(0.02f, 0.03f, 0.05f));
            if (pitDark != null) pitDark.transform.localScale = new Vector3(15f, 80f, 1f);
            Put(vfx, Glow, new Vector3(-145, 70, 1f), 6f, "Default", 45, C(0.2f, 0.45f, 0.6f, 0.35f), mat: MatAdd);
            Light(vfx, "PitColdLight", new Vector3(-145, 70, 0), C(0.4f, 0.7f, 1f), 0.9f, 16f, 2f);

            // ═════════════════ 전경 실루엣 ═════════════════
            // 카메라 쪽(z<0) 검은 땅 띠 + 고목 실루엣 (공중에 매달려 있던 덩굴 조각은 제거)
            var fgTint = C(0.045f, 0.04f, 0.065f);
            FgSilhouettes(fg, rnd, -480f, -250f, 100f, Load<SpriteShape>(FFShapes + "Forest ground.asset"), midTrees, midLeaves, fgTint, 46f, 72f, treeScaleMin: 1.2f, treeScaleMax: 1.6f);
            FgSilhouettes(fg, rnd, -250f, -160f, 100f, Load<SpriteShape>(FFShapes + "Forest ground.asset"), null, null, C(0.08f, 0.06f, 0.08f));

            // ═════════════════ 빛 / VFX ═════════════════
            var shaft1 = Sp(OF + "light 1..png");
            var shaft2 = Sp(OF + "light 2..png");
            foreach (var x in new[] { -460f, -440f, -372f, -350f, -300f, -270f }) {
                var gy = SurfAt(x, 40);
                SunShaft(vfx, rnd, rnd.Next(2) == 0 ? shaft1 : shaft2, x, float.IsNaN(gy) ? 8f : gy, 16f, C(1f, 0.86f, 0.62f, R(rnd, 0.25f, 0.38f)), R(rnd, 0.55f, 0.8f));
            }
            Motes(vfx, "DreamDust", new Rect(-470, -2, 225, 40), 1f, MatAddDot, C(1f, 0.92f, 0.75f, 0.55f), C(0.8f, 0.9f, 1f, 0.35f), 55f, 0.04f, 0.12f,
                new Vector2(0.15f, 0.08f), 0.35f, 9f, "Player", 40);
            Motes(vfx, "Fireflies", new Rect(-470, 2, 130, 14), -0.5f, MatAddDot, C(0.5f, 1f, 0.85f, 0.9f), C(0.9f, 1f, 0.6f, 0.8f), 14f, 0.06f, 0.16f,
                new Vector2(0f, 0.05f), 0.8f, 6f, "Player", 41);
            Motes(vfx, "MountainWindDust", new Rect(-250, 30, 150, 60), 0f, MatAddDot, C(1f, 0.9f, 0.85f, 0.45f), C(0.9f, 0.9f, 1f, 0.3f), 45f, 0.04f, 0.1f,
                new Vector2(-1.2f, 0.1f), 0.4f, 8f, "Player", 40);
            var wind = Load<GameObject>(SrcWind);
            foreach (var p in new[] { new Vector3(-262, 33, -1), new Vector3(-176, 86, -1), new Vector3(-120, 88, -1) }) Inst(wind, vfx, p);
            var fogBand = Sp(ArtDir + "/CATest_FogBand.png");
            for (var x = -470f; x < -250f; x += R(rnd, 14f, 22f)) {
                var gy = SurfAt(x, 40);
                var z = R(rnd, 2f, 25f);
                var sr = Put(vfx, fogBand, new Vector3(x, (float.IsNaN(gy) ? 5f : gy) + R(rnd, -1.5f, 2.5f), z), R(rnd, 1.6f, 2.6f), z > 10 ? "BackGround" : "Default", 30,
                    C(0.75f, 0.85f, 0.95f, R(rnd, 0.22f, 0.38f)), rnd.Next(2) == 0, mat: MatAlpha);
                if (sr != null) { var am = sr.gameObject.AddComponent<CATestAmbientMotion>(); am.driftSpeed = R(rnd, 0.2f, 0.45f); am.driftRange = 8f; }
            }

            // ═════════════════ 분위기 / 카메라 / 볼륨 ═════════════════
            Zone(zones, "Dream", new Rect(-470, -30, 224, 90), 22, C(0.78f, 0.8f, 1f), 0.45f, C(0.3f, 0.33f, 0.48f));
            Zone(zones, "Mountain", new Rect(-252, 25, 152, 100), 22, C(1f, 0.88f, 0.74f), 0.85f, C(0.7f, 0.6f, 0.52f), 1);
            Zone(zones, "Summit", new Rect(-190, 78, 90, 40), 14, C(1f, 0.91f, 0.76f), 1f, C(0.86f, 0.74f, 0.6f), 2);
            Zone(zones, "PitShaft", new Rect(-152, -20, 14, 100), 6, C(0.4f, 0.55f, 0.8f), 0.3f, C(0.03f, 0.05f, 0.08f), 3);
            CamZone(zones, "Ruins", new Rect(-400, 0, 64, 24), 12, 108f, new Vector3(0, 2f, 0), Vector3.one);
            CamZone(zones, "Updraft", new Rect(-322, 2, 30, 36), 8, 116f, new Vector3(0, 4f, 0), new Vector3(1, 0.6f, 1));
            CamZone(zones, "Lianas", new Rect(-296, 20, 50, 20), 8, 112f, new Vector3(0, 2f, 0), Vector3.one);
            CamZone(zones, "Climb", new Rect(-248, 28, 50, 60), 10, 118f, new Vector3(2, 4f, 0), new Vector3(1, 0.6f, 1));
            CamZone(zones, "Summit", new Rect(-190, 80, 90, 20), 14, 130f, new Vector3(0, 4f, 0), new Vector3(1.2f, 1.2f, 1));
            CamZone(zones, "Fall", new Rect(-152, 10, 14, 75), 4, 112f, new Vector3(0, -5f, 0), new Vector3(0.6f, 0.05f, 1), 3);
            var dreamProf = Profile("VP_CATest_Dream", p => {
                var b = p.Add<Bloom>(true); b.intensity.Override(1f); b.threshold.Override(0.72f); b.scatter.Override(0.72f); b.tint.Override(C(1f, 0.92f, 0.82f));
                var v = p.Add<Vignette>(true); v.intensity.Override(0.36f); v.smoothness.Override(0.45f); v.color.Override(C(0.04f, 0.03f, 0.1f));
                var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0.05f); ca.contrast.Override(20f); ca.saturation.Override(-4f); ca.colorFilter.Override(C(1f, 0.97f, 1f));
                var st = p.Add<SplitToning>(true); st.shadows.Override(C(0.3f, 0.36f, 0.66f)); st.highlights.Override(C(1f, 0.85f, 0.7f)); st.balance.Override(-15f);
                var fg2 = p.Add<FilmGrain>(true); fg2.intensity.Override(0.12f);
            });
            LocalVolume(zones, "Volume_Dream", new Rect(-470, -30, 225, 90), dreamProf, 18f);
            var mountProf = Profile("VP_CATest_Mountain", p => {
                var b = p.Add<Bloom>(true); b.intensity.Override(0.9f); b.threshold.Override(0.78f); b.scatter.Override(0.75f); b.tint.Override(C(1f, 0.88f, 0.82f));
                var v = p.Add<Vignette>(true); v.intensity.Override(0.28f); v.smoothness.Override(0.45f); v.color.Override(C(0.08f, 0.04f, 0.08f));
                var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0.1f); ca.contrast.Override(20f); ca.saturation.Override(-6f); ca.colorFilter.Override(C(1f, 0.95f, 0.88f));
                var st = p.Add<SplitToning>(true); st.shadows.Override(C(0.42f, 0.36f, 0.46f)); st.highlights.Override(C(1f, 0.84f, 0.66f)); st.balance.Override(-5f);
            });
            LocalVolume(zones, "Volume_Mountain", new Rect(-252, 20, 152, 110), mountProf, 16f, 2f);

            EditorSceneManager.MarkSceneDirty(scene);
            FlushGrounds();
            EditorSceneManager.SaveScene(scene, Forest2ScenePath);
        }

        /// <summary>줄기 스프라이트 + 가지 끝(줄기 위쪽 45% 영역)에 잎 뭉치 여러 개 → 떠 있는 잎 없이 하나의 나무로 보이게.</summary>
        private static void DreamTree(Transform parent, Random rnd, Sprite[] trunks, Sprite[] leaves, float x, float groundY, float z, float scale,
            Color trunkTint, Color leafTint, string layer, int order, float leafMul) {
            if (trunks == null || trunks.Length == 0) return;
            var s = Pick(rnd, trunks);
            var flip = rnd.Next(2) == 0;
            Put(parent, s, new Vector3(x, groundY - 0.6f - s.bounds.min.y * scale, z), scale, layer, order, trunkTint, flip);
            if (leaves == null || leaves.Length == 0) return;
            var h = s.bounds.size.y * scale;
            var w = s.bounds.size.x * scale;
            var top = groundY - 0.6f + h;
            var n = rnd.Next(5, 9);
            for (var i = 0; i < n; i++) {
                var p = new Vector3(x + R(rnd, -0.45f, 0.45f) * w, top - R(rnd, 0.05f, 0.45f) * h, z - 0.05f - i * 0.02f);
                var lt = leafTint * R(rnd, 0.88f, 1.06f);
                lt.a = 1f;
                var sr = Put(parent, Pick(rnd, leaves), p, scale * R(rnd, 1.3f, 2.0f) * leafMul, layer, order + 1 + i, lt, rnd.Next(2) == 0, R(rnd, -12f, 12f));
                if (sr == null) continue;
                var am = sr.gameObject.AddComponent<CATestAmbientMotion>();
                am.swayAngle = R(rnd, 0.6f, 1.4f);
                am.swaySpeed = R(rnd, 0.25f, 0.5f);
            }
        }
    }
}
#endif
