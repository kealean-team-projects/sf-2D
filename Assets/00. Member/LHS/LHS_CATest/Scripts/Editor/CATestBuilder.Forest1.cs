#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 숲 씬 1 : 울창한 숲(초록, 밝은 낮) → 안개 골짜기 (x -1262 ~ -470)
    ///
    ///  A0 숲 입구(-1262~-1150)   : 굴곡진 언덕길, 세이브 #1, 이동/점프 힌트
    ///  A1 상자 언덕(-1180~-1108) : 9 높이 턱 → 상자를 밀어 발판으로 (밀기 퍼즐)
    ///  A2 덩굴 골짜기(-1108~-1080): 가시 골짜기 위 매달린 덩굴 3개 → 덩굴 벽점프로 건넘. 가운데 덩굴 끝 = 나무 위 유물
    ///  A3 뿌리 굴(-1080~-1016)   : 거목 뿌리 아래 웅크림 통로 + 머리 높이로 오가는 가시 씨앗(웅크려 피함)
    ///  A4 나무 굴뚝(-1016~-992)  : 덩굴 절벽 등반 → 세이브 #2 → 좌우 덩굴 패치를 번갈아 벽점프로 오르는 굴뚝
    ///     우듬지 길(-998~-888)   : 무너지는 가지 → 돌풍(웅크려 버팀) → 레버로 도개교 내리기. 덩굴 위 가지에 유물
    ///  A5 내리막(-888~-856)      : 계단식 가지 + 떨어지는 가지 함정
    ///  A6 시간 문(-856~-798)     : 레버(4.8초) → 달려서(Shift) 닫히기 전에 통과
    ///  A7 상자·압력판(-798~-744) : 선반의 상자를 떨어뜨려 압력판에 올려 문 열기
    ///  B0 안개 골짜기 입구(-744~-697) : 내리막, 세이브 #3, 안개가 짙어짐
    ///  B1 등불 섬(-697~-649)     : 안개 구덩이(빠지면 Fog 함정 → 되돌아감). 버섯 등불을 켜면 빛 발판이 생김(2단계)
    ///  B2 갈림길(-649~-584)      : 위 길 = 유물 후 짙은 안개(되돌아감) / 아래 길 = 웅크림 통로 → 상자·압력판 문
    ///  B3 안개 끝(-584~-470)     : 안개 구덩이 2개 + 가시 씨앗 → 마른 덩굴 벽(등반 대쉬) → 안개가 걷힘
    /// </summary>
    public static partial class CATestBuilder {
        public static void BuildForest1Scene() {
            // 구버전(숲 1개 씬) 산출물 정리
            if (System.IO.File.Exists(ForestScenePath)) AssetDatabase.DeleteAsset(ForestScenePath);
            if (System.IO.File.Exists(SettingsDir + "/VP_CATest_Forest.asset")) AssetDatabase.DeleteAsset(SettingsDir + "/VP_CATest_Forest.asset");
            Surfaces.Clear();
            KeepClear.Clear();
            PendingGrounds.Clear();
            var scene = NewMapScene(Forest1ScenePath);
            var rnd = new Random(20260928);
            var root = new GameObject("CATest_Forest1_LushFog").transform;
            var ground = Group(root, "10_Terrain").transform;
            var gameplay = Group(root, "20_Gameplay").transform;
            var props = Group(root, "30_Props_Play").transform;
            var nearBg = Group(root, "40_NearBG").transform;
            var midBg = Group(root, "50_MidBG").transform;
            var farBg = Group(root, "60_FarBG").transform;
            var fg = Group(root, "05_Foreground").transform;
            var vfx = Group(root, "70_VFX_Light").transform;
            var zones = Group(root, "80_Zones").transform;

            var shapeLush = Load<SpriteShape>(FFShapes + "Forest ground.asset");
            var shapeGrass = Load<SpriteShape>(FFShapes + "Grass field.asset");
            var shapeStone = Load<SpriteShape>(FSB + "/Dungeon pack/Sprite shapes/Sone ground.asset");
            var shapeOld = Load<SpriteShape>(OFShapes + "OldMiddleGround.asset");
            var shapeOldGrass = Load<SpriteShape>(OFShapes + "OLDgrass.asset");
            var shapeBark = shapeOld; // 나무 줄기/가지: 어두운 흙 셰이프를 갈색으로 (OldForestBack 은 채움이 검게 보임)

            // 초록 숲 지형 채움 텍스처가 잎사귀 무늬라 밝으면 시끄럽다 → 어둡게 눌러서 캐릭터/풀과 대비
            var lush = C(0.64f, 0.7f, 0.55f); // 부드러운 초록 (피드백1: 너무 진한 초록 → 연하게 / 피드백2: 누렇다 → 초록 쪽으로 되돌림)
            var bark = C(0.62f, 0.5f, 0.42f);
            var fogT = C(0.6f, 0.64f, 0.7f);
            var fogImg = FogCanvas(gameplay);

            // ═════════════════ A0 숲 입구 ═════════════════
            // (화창한 숲이 왼쪽에 생겨 벽 제거) 이음새: 화창한 숲 마지막 지면의 윗면만 등록 → 절벽 깎기 판정에서 "이어진 땅"
            Surf(-1276, 5f, -1262, 4f);
            // 굴곡진 언덕길. 상자를 미는 구간(-1182~-1150)만 평평하게.
            var gA0 = RoughSurf(1.4f, new[] { (-1182f, -1150f) }, -1262, 4, -1250, 5.2f, -1240, 7.4f, -1230, 8.2f, -1220, 6.6f, -1210, 4.2f, -1200, 2.6f,
                -1192, 2.4f, -1182, 3.4f, -1170, 3.3f, -1160, 3.1f, -1150, 3f);
            Ground(ground, "A0_Hills", gA0, -30, shapeLush, lush);
            // 이끼 바위(작은 점프 연습)
            var ry = SurfaceY(gA0, -1206) - 0.2f;
            Terrain(ground, "A0_MossRock_Col", new Vector2[] { new(-1207.8f, ry + 1.9f), new(-1205, ry + 2.2f), new(-1204.2f, ry + 1.4f), new(-1204.3f, ry), new(-1207.9f, ry) },
                null, "Ground", 0, Color.white);
            Put(props, Sp(FF + "greens..png", "greens._0"), new Vector3(-1206, ry + 1.1f, 0.1f), 0.85f, "Ground", 12, C(0.95f, 0.93f, 0.8f));
            Clear(-1241.0f, -1235.0f);
            Inst(PSaveForest, gameplay, new Vector3(-1238, SurfaceY(gA0, -1238), 0)).name = "SavePoint_01_ForestEntrance";
            Put(props, Sp(FF + "danger.png", "danger_6"), new Vector3(-1228, SurfaceY(gA0, -1228) + 1.2f, 0.6f), 0.7f, "Item", 3, C(0.95f, 0.92f, 0.85f));
            AreaTitle(zones, new Rect(-1262, -10, 110, 60), "울창한 숲", "숲 챕터 · 후반부");
            Say(zones, new Rect(-1262, 0, 20, 16), "lush_enter");

            // ═════════════════ A1 상자 언덕 ═════════════════
            var gA1 = RoughSurf(1f, null, -1150, 11.6f, -1139, 12.4f, -1128, 13.4f, -1118, 12.4f, -1108, 11.4f);
            Ground(ground, "A1_HighLedge", gA1, -30, shapeLush, lush);
            Crate(gameplay, "Crate_A1", -1170, SurfaceY(gA0, -1170));
            Clear(-1180f, -1148f); // 상자를 미는 경로

            // ═════════════════ A2 덩굴 골짜기 ═════════════════
            Ground(ground, "A2_GullyFloor", Surf(-1108, -4, -1080, -4), -30, shapeLush, Mul(lush, 0.55f));
            Thorns(gameplay, rnd, "A2_ThornGully", -1094, -4, 26, true);
            Liana(gameplay, rnd, "A2_Liana1", -1102.5f, 24, 5);
            Liana(gameplay, rnd, "A2_Liana2", -1095f, 31, 4);
            Liana(gameplay, rnd, "A2_Liana3", -1087.5f, 25, 5);
            Ledge(ground, "A2_CanopyL", -1106, -1098, 24, 25.2f, shapeLush, bark, 3, 0.4f);
            Ledge(ground, "A2_CanopyR", -1091, -1078, 25, 26.2f, shapeLush, bark, 3, 0.4f);
            Clear(-1086.0f, -1080.0f);
            Inst(PRelicForest, gameplay, new Vector3(-1083, 27.4f, 0)).name = "Relic_01_GullyCanopy";
            Put(props, Sp(FF + "danger.png", "danger_3"), new Vector3(-1110.5f, 12.8f, 0.5f), 0.6f, "Item", 3, C(0.9f, 0.88f, 0.82f));

            // ═════════════════ A3 뿌리 굴 ═════════════════
            var gA3 = Surf(-1080, 11, -1074, 10, -1068, 7, -1062, 4.2f, -1058, 3.6f, -1022, 3.6f, -1016, 3.6f);
            Ground(ground, "A3_Floor", gA3, -30, shapeLush, lush);
            Terrain(ground, "A3_RootMass", new Vector2[] {
                new(-1062, 9), new(-1055, 12), new(-1045, 13.5f), new(-1035, 12.8f), new(-1026, 11.5f), new(-1022, 10), new(-1022, 6.3f),
                new(-1036, 6.3f), new(-1036.5f, 5.1f), new(-1049.5f, 5.1f), new(-1050, 6.3f), new(-1060, 6.3f), new(-1062, 6.8f)
            }, shapeOld, "Ground", 3, C(0.45f, 0.38f, 0.33f), true, 0.7f);
            Block(ground, "A3_GiantTrunk", -1063, -1054, 9, 60, shapeBark, C(0.5f, 0.42f, 0.36f), 2, 0.8f);
            // 가시 씨앗 높이: 웅크린 머리 4.6 < 씨앗 판정 아래 4.9 < 서 있는 머리 5.6  (천장 6.3)
            SeedPatrol(gameplay, "A3_ThornSeed_1", -1059.5f, 5.3f, 8f, 3f);
            SeedPatrol(gameplay, "A3_ThornSeed_2", -1035f, 5.3f, 11f, 4f);
            var roots = Sps(DG + "root.png", "root", 0, 1, 2);
            for (var x = -1060f; x < -1024f; x += R(rnd, 3f, 4.5f))
                Put(props, Pick(rnd, roots), new Vector3(x, 6.6f, -0.25f), R(rnd, 0.55f, 0.75f), "Ground", 10, C(0.4f, 0.33f, 0.3f), rnd.Next(2) == 0,
                    180f + R(rnd, -15f, 15f));
            var mush = Sps(DG + "dungeon items.png", "dungeon items", 12, 13, 15);
            foreach (var x in new[] { -1057f, -1049f, -1042f, -1033f, -1026f }) {
                Put(props, Pick(rnd, mush), new Vector3(x, 3.6f + 0.35f, 0.2f), R(rnd, 0.45f, 0.6f), "Item", 20, Color.white, rnd.Next(2) == 0);
                Light(vfx, "RootGlow", new Vector3(x, 4.4f, 0), C(0.95f, 0.85f, 0.45f), R(rnd, 1.2f, 1.5f), R(rnd, 4.5f, 5.5f), 0.4f);
            }

            // ═════════════════ A4 나무 굴뚝 + 우듬지 ═════════════════
            Ground(ground, "A4_Cliff", Surf(-1016, 17, -992, 17), -30, shapeLush, lush);
            VineWall(gameplay, rnd, "A4_Climb_Cliff", -1016, 4, 17, -1);
            Clear(-1015.0f, -1009.0f);
            Inst(PSaveForest, gameplay, new Vector3(-1012, 17, 0)).name = "SavePoint_02_TreeChimney";
            Block(ground, "A4_TrunkL", -1008, -1003, 21.5f, 40, shapeBark, bark, 3, 0.6f);
            Block(ground, "A4_TrunkR", -998.5f, -992, 17, 40, shapeBark, bark, 3, 0.6f);
            VineWall(gameplay, rnd, "A4_Chim_R1", -998.5f, 17.5f, 24, -1);
            VineWall(gameplay, rnd, "A4_Chim_L1", -1003, 22, 27.5f, 1);
            VineWall(gameplay, rnd, "A4_Chim_R2", -998.5f, 27, 32.5f, -1);
            VineWall(gameplay, rnd, "A4_Chim_L2", -1003, 32, 37.5f, 1);
            VineWall(gameplay, rnd, "A4_Chim_R3", -998.5f, 36.5f, 40, -1);
            // 우듬지 아래 숲 바닥(떨어지면 굴뚝으로 돌아가야 함)
            Ground(ground, "A4_UnderFloor", RoughSurf(1.5f, null, -992, 12, -975, 13.5f, -960, 10, -945, 8.5f, -930, 9.5f, -905, 10), -30, shapeLush, Mul(lush, 0.8f));
            Ledge(ground, "A4_Branch1", -998.5f, -976, 38.6f, 40, shapeLush, bark, 3, 0.45f);
            for (var i = 0; i < 5; i++) Inst(PCrumble, gameplay, new Vector3(-974.5f + i * 3f, 40, 0)).name = $"A4_CrumbleBranch_{i}";
            Ledge(ground, "A4_Branch2", -961, -938, 39, 40.5f, shapeLush, bark, 3, 0.45f);
            Gust(gameplay, "A4_CanopyGust", new Rect(-961, 40.5f, 23, 5), new Vector2(-10f, 0f), 2.2f, 1.7f);
            Say(zones, new Rect(-996, 40, 10, 8), "lush_canopy");
            Ledge(ground, "A4_Branch3", -938, -925, 39.5f, 41, shapeLush, bark, 3, 0.45f);
            var bridgeLever = Lever(gameplay, "A4_BridgeLever", -930, 41, CATestLever.Mode.OneShot);
            DrawBridge(gameplay, "A4_DrawBridge", new Vector2(-925, 41), 20.5f, new MonoBehaviour[] { bridgeLever });
            Block(ground, "A4_Column", -905, -888, -30, 41, shapeBark, bark, 3, 0.6f);
            Surf(-905, 41, -888, 41);
            // 유물: 매달린 덩굴 위 가지
            Liana(gameplay, rnd, "A4_Liana_Relic", -940f, 53, 44.5f);
            Ledge(ground, "A4_HighBranch", -938, -931, 50, 51, shapeLush, bark, 3, 0.4f);
            Clear(-937.0f, -931.0f);
            Inst(PRelicForest, gameplay, new Vector3(-934, 52.4f, 0)).name = "Relic_02_HighBranch";

            // ═════════════════ A5 내리막 ═════════════════
            Ledge(ground, "A5_Step1", -886, -879, 32, 33.5f, shapeLush, bark, 3, 0.45f);
            Ledge(ground, "A5_Step2", -876, -868, 25, 26.5f, shapeLush, bark, 3, 0.45f);
            Ledge(ground, "A5_Step3", -865, -858, 18.5f, 20, shapeLush, bark, 3, 0.45f);
            var gA5 = RoughSurf(1.2f, new[] { (-860f, -848f), (-812f, -744f) }, -888, 12, -872, 13.2f, -856, 12, -846, 11.5f, -836, 10.2f, -826, 9, -816, 8, -806, 7.4f, -796, 7.2f, -786, 7, -776, 6.6f,
                -766, 6.5f, -756, 6.4f, -744, 6f);
            Ground(ground, "A5to7_Floor", gA5, -30, shapeLush, lush);
            var branch = Sp(FF + "waterfall tree.png", "waterfall tree_23");
            FallingRock(gameplay, "A5_FallingBranch", -872, 36, 26.5f, new Rect(-878, 26.5f, 11, 5), branch, 0.45f, C(0.8f, 0.72f, 0.62f), 30f);
            FallingRock(gameplay, "A5_FallingBranch2", -847, 23, 11.5f, new Rect(-852, 11, 9, 6), branch, 0.45f, C(0.8f, 0.72f, 0.62f), -20f);

            // ═════════════════ A6 시간 문 ═════════════════
            var timedLever = Lever(gameplay, "A6_TimedLever", -853, SurfaceY(gA5, -853), CATestLever.Mode.Timed, 4.8f);
            StakeGate(gameplay, "A6_TimedGate", -806, SurfaceY(gA5, -806), 7f, new MonoBehaviour[] { timedLever }, 7.5f, 5f);
            Block(ground, "A6_GateRock", -808.5f, -798, 14.4f, 48, shapeStone, C(0.7f, 0.72f, 0.68f), 3, 0.6f);

            // ═════════════════ A7 상자·압력판 ═════════════════
            Ledge(ground, "A7_Shelf", -800, -790, 7.2f, 10.5f, shapeStone, C(0.75f, 0.76f, 0.7f), 2, 0.4f);
            Crate(gameplay, "Crate_A7", -794, 10.5f);
            Clear(-802f, -744f); // 선반 → 압력판 → 문 경로
            Block(ground, "A7_Curb", -768.3f, -767.5f, 6.3f, 7.3f, shapeStone, C(0.7f, 0.7f, 0.66f), 2, 0.2f); // 상자가 판 위에서 멈추도록 턱을 판 바로 옆에
            var plateA7 = Plate(gameplay, "A7_Plate", -770, SurfaceY(gA5, -770), false);
            StakeGate(gameplay, "A7_PlateGate", -752, SurfaceY(gA5, -752), 7f, new MonoBehaviour[] { plateA7 }, 7.5f, 12f);
            Block(ground, "A7_GateRock", -754.5f, -744, 13.4f, 48, shapeStone, C(0.7f, 0.72f, 0.68f), 3, 0.6f);

            // ═════════════════ B0 안개 골짜기 입구 ═════════════════
            var gB0 = RoughSurf(1.2f, new[] { (-716f, -697f) }, -744, 6, -736, 4.5f, -728, 0.5f, -720, -2.5f, -712, -5, -704, -6.5f, -697, -7);
            Ground(ground, "B0_Descent", gB0, -40, shapeOld, fogT);
            Clear(-716.0f, -710.0f);
            Inst(PSaveForest, gameplay, new Vector3(-713, SurfaceY(gB0, -713), 0)).name = "SavePoint_03_FogValley";
            AreaTitle(zones, new Rect(-744, -30, 50, 60), "안개 골짜기", "길을 잃은 자는 되돌아온다");
            Say(zones, new Rect(-730, -6, 14, 14), "fog_enter");

            // ═════════════════ B1 등불 섬 ═════════════════
            Ground(ground, "B1_HiddenFloor", Surf(-697, -22, -649, -22), -40, shapeOld, Mul(fogT, 0.5f));
            FogTrap(gameplay, "B1_FogAbyss", new Rect(-697, -22, 48, 9), new Vector2(-707, -5.5f), fogImg);
            Clear(-703.5f, -697.5f);
            var lampA = Inst(PLampForest, gameplay, new Vector3(-700.5f, -7, 0.2f));
            lampA.name = "B1_LampA";
            Ground(ground, "B1_Island", Surf(-676, -5, -670, -5), -40, shapeOld, fogT);
            Clear(-675.5f, -669.5f);
            var lampB = Inst(PLampForest, gameplay, new Vector3(-672.5f, -5, 0.2f));
            lampB.name = "B1_LampB";
            var la = lampA.GetComponent<_02._Script._04_Interaction.InteractLight>();
            var lb = lampB.GetComponent<_02._Script._04_Interaction.InteractLight>();
            foreach (var (x, y, d, src) in new[] { (-690f, -6.5f, 0f, la), (-682f, -5.5f, 0.2f, la), (-662f, -4.5f, 0f, lb), (-654f, -5f, 0.2f, lb) }) {
                var pad = Inst(PBloomPadForest, gameplay, new Vector3(x, y, 0));
                pad.name = $"B1_BloomPad_{x}";
                var bp = pad.GetComponent<CATestLightBloomPlatform>();
                Set(bp, "sources", new Object[] { src });
                Set(bp, "delay", d);
            }
            Say(zones, new Rect(-704, -8, 8, 8), "fog_lamp");

            // ═════════════════ B2 갈림길 ═════════════════
            var gB2 = RoughSurf(0.8f, new[] { (-652f, -640f), (-630f, -570f) }, -649, -5, -640, -5.5f, -630, -6, -616, -6, -604, -6.1f, -590, -6.2f, -584, -6, -576, -6, -570, -5.5f);
            Ground(ground, "B2_Floor", gB2, -40, shapeOld, fogT);
            Ledge(ground, "B2_UpperSlab", -640, -604, 1, 5, shapeStone, C(0.62f, 0.64f, 0.66f), 3, 0.5f);
            VineWall(gameplay, rnd, "B2_Climb_Upper", -640, 1, 5, -1);
            Clear(-611.0f, -605.0f);
            Inst(PRelicForest, gameplay, new Vector3(-608, 6.5f, 0)).name = "Relic_03_FogUpper";
            FogTrap(gameplay, "B2_FogDeadEnd", new Rect(-604, 5, 14, 12), new Vector2(-652, -4), fogImg);
            Block(ground, "B2_RootWall", -628, -616, -4.5f, 1, shapeOld, C(0.42f, 0.38f, 0.36f), 3, 0.6f);
            Crate(gameplay, "Crate_B2", -612, SurfaceY(gB2, -612));
            Clear(-616f, -586f);
            Block(ground, "B2_Curb", -594.3f, -593.5f, -6.4f, -5.3f, shapeStone, C(0.6f, 0.62f, 0.64f), 2, 0.2f);
            var plateB2 = Plate(gameplay, "B2_Plate", -596, SurfaceY(gB2, -596), false);
            StakeGate(gameplay, "B2_PlateGate", -588, SurfaceY(gB2, -588), 7f, new MonoBehaviour[] { plateB2 }, 7.5f, 12f);
            Block(ground, "B2_GateRock", -590.5f, -580, 0.8f, 32, shapeStone, C(0.6f, 0.62f, 0.64f), 3, 0.6f);
            var blueMush = Sps(DG + "dungeon items.png", "dungeon items", 15, 14);
            foreach (var x in new[] { -645f, -633f, -619f, -606f, -598f }) {
                var y = SurfaceY(gB2, x);
                Put(props, Pick(rnd, blueMush), new Vector3(x, y - 0.1f + 0.8f, 0.3f), R(rnd, 0.45f, 0.6f), "Item", 12, C(0.7f, 1f, 1f), rnd.Next(2) == 0);
                Light(vfx, "PathMushroom", new Vector3(x, y + 1.2f, 0), C(0.35f, 0.85f, 1f), 1.1f, 5f, 0.4f);
            }

            // ═════════════════ B3 안개 끝 ═════════════════
            Ground(ground, "B3_PitA_Floor", Surf(-570, -20, -563, -20), -40, shapeOld, Mul(fogT, 0.5f));
            FogTrap(gameplay, "B3_FogPitA", new Rect(-570, -20, 7, 11), new Vector2(-578, -5), fogImg);
            Ground(ground, "B3_Mid", Surf(-563, -5, -550, -5), -40, shapeOld, fogT);
            SeedPatrol(gameplay, "B3_ThornSeed", -562, -3.45f, 11f, 3f);
            Ground(ground, "B3_PitB_Floor", Surf(-550, -20, -543, -20), -40, shapeOld, Mul(fogT, 0.5f));
            FogTrap(gameplay, "B3_FogPitB", new Rect(-550, -20, 7, 11), new Vector2(-556, -4), fogImg);
            Ground(ground, "B3_Approach", Surf(-543, -4.5f, -534, -4, -522, -4), -40, shapeOld, fogT);
            Ground(ground, "B3_Plateau", RoughSurf(1.2f, null, -522, 12, -508, 12.9f, -495, 12.2f, -482, 11.5f, -470, 12), -40, shapeOld, C(0.72f, 0.72f, 0.8f));
            VineWall(gameplay, rnd, "B3_Climb_Lower", -522, -4, 0, -1);
            CrumbleVine(gameplay, rnd, "B3_CrumbleVines", -522, 0, 12, -1, 0.8f);
            Block(ground, "WallSeamCeiling", -520, -470, 34, 60, shapeOld, Mul(fogT, 0.4f), 0); // 산 쪽으로 넘어가기 전 하늘을 가리는 가지 덩어리 (시야 연출)

            // DeadZone 보험
            var dz = Inst(PDeadZone, gameplay, new Vector3(-870, -36, 0));
            if (dz != null) { dz.name = "DeadZone_Bottom"; dz.transform.localScale = Vector3.one; dz.GetComponent<BoxCollider2D>().size = new Vector2(820, 4); }

            // ═════════════════ 식생 / 소품 ═════════════════
            bool Hazard(float x) => (x > -1108 && x < -1080) || (x > -697 && x < -649) || (x > -570 && x < -563) || (x > -550 && x < -543);
            // "grass ferr" 시트는 실제로는 바위/묘목/꽃 그림이라 자주 뿌리면 규칙적으로 튄다 → 바위만 드물게
            var rocksLush = Sps(FF + "grass ferr.png", "grass ferr", 4, 5, 6, 7);
            var flowers = Sps(FF + "Flowers.png", "Flowers", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
            // 세로로 긴 풀 기둥(first layer greens 0,1,3,4,9,10,11)과 키 큰 관목은 빼고, 낮고 넓게 퍼진 것만 사용 (피드백: 어색한 자잘한 스프라이트)
            var firstGreens = Sps(FF + "first layer greens.png", "first layer greens", 12, 19); // 낮은 풀잎 뭉치(5,6,8,14 는 늘어진 덩굴이라 제외)
            var shrubs = Sps(FF + "shrubs..png", "shrubs.", 2, 9); // 4,15 는 형광 초록이라 제외
            var bushes = Sps(FF + "bushes.png", "bushes", 0, 1, 2, 3);
            var smallBush = Sps(FF + "waterfall tree.png", "waterfall tree", 9, 10, 12, 13, 14, 16, 17, 18, 19, 21);
            // 초록 숲 지면
            // 무리 단위 불규칙 배치 (ClusterSurf): 무리 사이는 넓게, 무리 안은 1~N개
            var warmA = C(0.8f, 0.9f, 0.68f);
            var warmB = C(0.94f, 1f, 0.84f);
            ClusterSurf(props, rnd, firstGreens, -1262, -744, 7f, 18f, 3, 0.35f, 0.6f, -0.6f, -0.2f, "Architecture", 2, warmA, warmB, 0.25f, 0.5f, Hazard, 9999f, 0.3f);
            ClusterSurf(props, rnd, rocksLush, -1262, -744, 18f, 40f, 2, 0.45f, 0.8f, 0.1f, 0.8f, "Item", 4, C(0.85f, 0.8f, 0.72f), C(0.95f, 0.9f, 0.82f), 0.25f, 0.55f, Hazard, 9999f, 0.3f);
            ClusterSurf(props, rnd, flowers, -1262, -744, 5f, 13f, 5, 0.5f, 0.8f, -0.3f, 0.4f, "Item", 8, C(1f, 0.95f, 0.88f), Color.white, 0f, 0.15f, Hazard, 9999f, 0.35f);
            ClusterSurf(props, rnd, smallBush, -1262, -744, 8f, 18f, 3, 0.8f, 1.3f, 0.8f, 2.4f, "Item", 0, warmA, warmB, 0.15f, 0.5f, Hazard);
            ClusterSurf(props, rnd, shrubs, -1262, -744, 14f, 26f, 2, 0.8f, 1.2f, 2.5f, 5f, "Default", 0, C(0.78f, 0.8f, 0.62f), C(0.92f, 0.92f, 0.72f), 0.3f, 0.8f, Hazard);
            // 안개 골짜기 지면: 마른 풀, 죽은 덤불, 흰 버섯
            var oldGreens = Sps(OF + "Old greens.png", "Old greens", 0, 3, 4, 21, 22, 23, 24, 25);
            var vox = Sps(OF + "Old Vox.png", "Old Vox", 0, 1, 2);
            ClusterSurf(props, rnd, oldGreens, -744, -470, 5f, 12f, 3, 0.55f, 0.9f, -0.6f, -0.3f, "Architecture", 2, C(0.6f, 0.65f, 0.65f), C(0.8f, 0.82f, 0.8f), 0.15f, 0.5f, Hazard);
            ClusterSurf(props, rnd, vox, -744, -470, 9f, 18f, 2, 0.45f, 0.7f, 1f, 2.5f, "Item", 0, C(0.55f, 0.6f, 0.62f), C(0.7f, 0.72f, 0.74f), 0.3f, 0.6f, Hazard);
            GrassOn(ground, shapeGrass, gA0, C(0.98f, 0.95f, 0.76f));
            GrassOn(ground, shapeGrass, gA1, C(0.98f, 0.95f, 0.76f));
            GrassOn(ground, shapeGrass, gA5, C(0.98f, 0.95f, 0.76f));
            GrassOn(ground, shapeOldGrass, gB0, C(0.7f, 0.75f, 0.72f));
            GrassOn(ground, shapeOldGrass, gB2, C(0.7f, 0.75f, 0.72f));

            // ═════════════════ 배경 ═════════════════
            var mount = Sp(FF + "mount big.png");
            Put(farBg, mount, new Vector3(-1120, 10, 240), 5.5f, "BackGround", -200, C(0.8f, 0.78f, 0.7f));
            Put(farBg, mount, new Vector3(-880, 5, 260), 6f, "BackGround", -201, C(0.76f, 0.74f, 0.66f), true);
            var backTrees = Sps(FF + "back trees.png", "back trees", 0, 1, 2, 3, 4, 5);
            ScatterBand(farBg, rnd, backTrees, -1320, -700, 14, 22, -15, -8, 2.8f, 3.6f, 110, 160, "BackGround", -150, C(0.66f, 0.68f, 0.6f), C(0.74f, 0.74f, 0.64f));
            ScatterBand(midBg, rnd, backTrees, -1300, -740, 9, 15, -12, -6, 1.8f, 2.4f, 45, 75, "BackGround", -90, C(0.55f, 0.6f, 0.48f), C(0.64f, 0.67f, 0.52f));
            var fields = Sps(FF + "fields. 1.png", "fields.", 1, 2, 4, 5, 7, 8);
            ScatterBand(midBg, rnd, fields, -1300, -740, 10, 16, -6, -2, 2.2f, 3f, 30, 45, "BackGround", -70, C(0.66f, 0.7f, 0.52f), C(0.76f, 0.78f, 0.58f));
            // 중경: 잎이 무성한 큰 나무들 — 두 줄로 빽빽하게 (공중에 잎만 떠 있던 Canopy 는 제거)
            //  앞줄 z 8~18, 뒷줄 z 24~38(크고 배경색 쪽으로 흐리게). 밑동은 TreeGround + ParallaxSink 로 땅에 묻는다.
            var lushBg = C(0.72f, 0.8f, 0.64f);
            for (var x = -1256f + R(rnd, 0f, 4f); x < -752f; x += R(rnd, 9f, 15f)) {
                if (!TreeGround(x, 2.5f, 60f, out var gy)) continue;
                var z = R(rnd, 8f, 18f);
                LeafyTree(nearBg, rnd, x, gy - ParallaxSink(z), z, R(rnd, 1.25f, 1.6f), C(0.66f, 0.6f, 0.52f), C(0.76f, 0.88f, 0.64f), "Default", -30);
            }
            for (var x = -1258f + R(rnd, 0f, 8f); x < -748f; x += R(rnd, 11f, 18f)) {
                if (!TreeGround(x, 3f, 60f, out var gy, 4f)) continue;
                var z = R(rnd, 24f, 38f);
                LeafyTree(nearBg, rnd, x, gy - ParallaxSink(z), z, R(rnd, 1.7f, 2.2f), Color.Lerp(C(0.62f, 0.56f, 0.48f), lushBg, 0.4f),
                    Color.Lerp(C(0.7f, 0.82f, 0.58f), lushBg, 0.35f), "Default", -50);
            }
            ClusterSurf(nearBg, rnd, bushes, -1262, -744, 12f, 24f, 2, 0.9f, 1.3f, 3f, 6f, "Default", -20, C(0.64f, 0.76f, 0.56f), C(0.76f, 0.86f, 0.64f), 0.6f, 1.2f, Hazard);
            // 안개 골짜기 배경: 앙상한 나무 + 먼 안개 숲
            var farOld = Sps(OF + "Old forest far back tree.png", "Old forest far back tree", 0, 1, 2);
            var backOld = Sps(OF + "Old forest back tree.png", "Old forest back tree", 1, 2, 3, 4, 5);
            ScatterBand(farBg, rnd, farOld, -760, -440, 14, 24, -26, -14, 2.6f, 3.4f, 100, 140, "BackGround", -150, C(0.58f, 0.62f, 0.68f), C(0.66f, 0.7f, 0.75f));
            ScatterBand(midBg, rnd, backOld, -760, -450, 8, 14, -18, -10, 1.4f, 2f, 30, 55, "BackGround", -80, C(0.45f, 0.48f, 0.52f), C(0.55f, 0.58f, 0.62f));
            ScatterBand(nearBg, rnd, backOld, -744, -470, 12, 20, -12, -6, 1.1f, 1.4f, 8, 16, "Default", -20, C(0.32f, 0.34f, 0.38f), C(0.42f, 0.44f, 0.48f));
            var bgOld = Sp(OF + "background.png");
            for (var x = -780f; x < -380f; x += 245f) Put(farBg, bgOld, new Vector3(x, 10f, 270f), 18f, "BackGround", -210, C(0.62f, 0.66f, 0.72f), (int)(x / 245f) % 2 == 0);
            // 뿌리 굴·거목
            Put(nearBg, Sp(FF + "new trees.png", "new trees_2"), new Vector3(-1058, 20, 1.5f), 2.4f, "Default", -12, C(0.55f, 0.48f, 0.42f));
            Put(nearBg, Sp(FF + "new trees.png", "new trees_4"), new Vector3(-1000, 24, 1.2f), 2.2f, "Default", -12, C(0.55f, 0.48f, 0.42f), true);
            Put(nearBg, Sp(FF + "new trees.png", "new trees_1"), new Vector3(-897, 22, 1.2f), 2.3f, "Default", -12, C(0.55f, 0.48f, 0.42f));

            // ═════════════════ 전경 실루엣 ═════════════════
            // 카메라 쪽(z<0) 검은 땅 띠 + 큰 나무. (예전: 공중에 뜬 잎/덤불 조각 → 줄기에 붙은 나무로 교체)
            var fgTint = C(0.08f, 0.07f, 0.04f);
            FgSilhouettes(fg, rnd, -1262f, -744f, 60f, shapeLush, Sps(FF + "new trees.png", "new trees", 0, 1, 2, 3, 4),
                Sps(FF + "new tree leafs.png", "new tree leafs", 0, 3, 4, 6, 8, 9, 10, 12), fgTint, 36f, 60f);
            // 안개 골짜기: 앙상한 고목 실루엣(잎 적게)
            FgSilhouettes(fg, rnd, -744f, -470f, 60f, shapeLush, Sps(OF + "Old forest midle tree.png", "Old forest midle tree", 0, 1, 2),
                Sps(OF + "Old forest midle leafs.png", "Old forest midle leafs", 0, 1, 2, 3), C(0.1f, 0.11f, 0.13f), 44f, 70f, treeScaleMin: 1.3f, treeScaleMax: 1.7f);

            // ═════════════════ 빛 / VFX ═════════════════
            var shaft = Sp(FF + "light 1. 1.png");
            foreach (var x in new[] { -1244f, -1214f, -1196f, -1132f, -1118f, -1010f, -968f, -935f, -880f, -838f, -818f }) { // 퍼즐 물체(상자·압력판) 위에는 두지 않음: 강한 빛+블룸에 상자가 하얗게 날아가 "사라진 것처럼" 보였음
                var gy = SurfAt(x, 60);
                SunShaft(vfx, rnd, shaft, x, float.IsNaN(gy) ? 8f : gy, 22f, C(1f, 0.93f, 0.7f, R(rnd, 0.28f, 0.42f)), R(rnd, 0.6f, 0.85f));
            }
            Motes(vfx, "Lush_Pollen", new Rect(-1262, 0, 520, 50), 1f, MatAddDot, C(1f, 0.95f, 0.7f, 0.55f), C(0.85f, 1f, 0.8f, 0.35f), 90f, 0.04f, 0.1f,
                new Vector2(0.2f, 0.05f), 0.4f, 9f, "Player", 40);
            var butterflies = new[] { Load<GameObject>(FSB + "/Forest sprite pack/Prefabs/Butterfly ellow.prefab"), Load<GameObject>(FSB + "/Forest sprite pack/Prefabs/Butterfly red.prefab") };
            foreach (var x in new[] { -1230f, -1160f, -1070f, -960f, -830f, -775f }) {
                var gy = SurfAt(x, 60);
                var b = Pick(rnd, butterflies);
                if (b != null) Inst(b, vfx, new Vector3(x, (float.IsNaN(gy) ? 6f : gy) + 2.5f, -0.5f), 0.6f);
            }
            var leafPs = Load<GameObject>(FSB + "/Forest sprite pack/Prefabs/LEAFS Particle.prefab");
            foreach (var x in new[] { -1240f, -1180f, -1110f, -1040f, -980f, -930f, -870f, -800f }) {
                var gy = SurfAt(x, 60);
                if (leafPs != null) Inst(leafPs, vfx, new Vector3(x, (float.IsNaN(gy) ? 6f : gy) + 16f, -2f));
            }
            // 안개: 여러 깊이의 안개 띠 + 앞쪽 옅은 안개
            var fogBand = Sp(ArtDir + "/CATest_FogBand.png");
            for (var x = -750f; x < -500f; x += R(rnd, 9f, 14f)) {
                var gy = SurfAt(x, 60);
                var y = (float.IsNaN(gy) ? -6f : gy) + R(rnd, -2f, 4f);
                var z = R(rnd, -3f, 25f);
                var sr = Put(vfx, fogBand, new Vector3(x, y, z), R(rnd, 1.8f, 2.8f), z < 0 ? "Architecture" : z > 10 ? "BackGround" : "Default", 30,
                    C(0.8f, 0.85f, 0.9f, z < 0 ? 0.28f : R(rnd, 0.35f, 0.55f)), rnd.Next(2) == 0, mat: MatAlpha);
                if (sr != null) { var am = sr.gameObject.AddComponent<CATestAmbientMotion>(); am.driftSpeed = R(rnd, 0.2f, 0.4f); am.driftRange = 7f; }
            }
            Motes(vfx, "Fog_Dust", new Rect(-744, -12, 270, 30), 0f, MatAlphaDot, C(0.85f, 0.9f, 0.95f, 0.35f), C(0.7f, 0.75f, 0.8f, 0.2f), 60f, 0.15f, 0.45f,
                new Vector2(0.2f, 0f), 0.3f, 10f, "Player", 38);
            var ff = Load<GameObject>(SrcFireflies);
            foreach (var x in new[] { -700f, -660f, -620f, -540f }) Inst(ff, vfx, new Vector3(x, SurfAt(x, 60) + 2f, -1f));

            // ═════════════════ 분위기 / 카메라 / 볼륨 ═════════════════
            Zone(zones, "Lush", new Rect(-1262, -30, 500, 120), 25, C(0.97f, 0.97f, 0.84f), 0.85f, C(0.72f, 0.8f, 0.64f));
            Zone(zones, "RootTunnel", new Rect(-1064, 0, 44, 12), 8, C(0.8f, 0.7f, 0.55f), 0.5f, C(0.14f, 0.12f, 0.08f), 1);
            Zone(zones, "Canopy", new Rect(-1000, 36, 120, 30), 14, C(1f, 0.98f, 0.84f), 1f, C(0.8f, 0.87f, 0.7f), 1);
            Zone(zones, "FogValley", new Rect(-744, -40, 222, 80), 26, C(0.74f, 0.8f, 0.86f), 0.48f, C(0.55f, 0.6f, 0.66f));
            Zone(zones, "FogClears", new Rect(-522, -10, 52, 40), 18, C(0.82f, 0.84f, 1f), 0.6f, C(0.4f, 0.45f, 0.6f), 1);
            CamZone(zones, "RootTunnel", new Rect(-1064, 0, 44, 12), 8, 86f, new Vector3(0, 1f, 0), Vector3.one);
            CamZone(zones, "Gully", new Rect(-1110, -4, 32, 30), 10, 112f, new Vector3(0, 3f, 0), Vector3.one);
            CamZone(zones, "Chimney", new Rect(-1010, 17, 20, 24), 6, 104f, new Vector3(0, 3f, 0), new Vector3(1, 0.6f, 1), 2);
            CamZone(zones, "Canopy", new Rect(-998, 38, 112, 16), 10, 114f, new Vector3(0, 2.5f, 0), Vector3.one);
            CamZone(zones, "TimedGate", new Rect(-858, 5, 60, 12), 10, 112f, new Vector3(6, 2f, 0), Vector3.one);
            CamZone(zones, "Fog", new Rect(-744, -30, 222, 50), 16, 92f, new Vector3(0, 1.5f, 0), Vector3.one);
            var lushProf = Profile("VP_CATest_Lush", p => {
                var b = p.Add<Bloom>(true); b.intensity.Override(0.9f); b.threshold.Override(0.78f); b.scatter.Override(0.72f); b.tint.Override(C(1f, 0.95f, 0.82f));
                var v = p.Add<Vignette>(true); v.intensity.Override(0.3f); v.smoothness.Override(0.45f); v.color.Override(C(0.03f, 0.06f, 0.03f));
                var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0.08f); ca.contrast.Override(18f); ca.saturation.Override(-5f); ca.colorFilter.Override(C(0.98f, 1f, 0.93f));
                var wb = p.Add<WhiteBalance>(true); wb.temperature.Override(6f); wb.tint.Override(-6f); // 약간만 따뜻하게 + 초록 쪽 틴트
                var st = p.Add<SplitToning>(true); st.shadows.Override(C(0.34f, 0.42f, 0.4f)); st.highlights.Override(C(1f, 0.94f, 0.78f)); st.balance.Override(0f);
                var smh = p.Add<ShadowsMidtonesHighlights>(true); smh.shadows.Override(new Vector4(0.9f, 0.95f, 1f, -0.12f)); smh.highlights.Override(new Vector4(1f, 0.97f, 0.9f, 0.05f));
            });
            LocalVolume(zones, "Volume_Lush", new Rect(-1262, -30, 520, 120), lushProf, 20f);
            var fogProf = Profile("VP_CATest_FogValley", p => {
                var b = p.Add<Bloom>(true); b.intensity.Override(1.2f); b.threshold.Override(0.7f); b.scatter.Override(0.85f); b.tint.Override(C(0.85f, 0.95f, 1f));
                var v = p.Add<Vignette>(true); v.intensity.Override(0.42f); v.smoothness.Override(0.55f); v.color.Override(C(0.05f, 0.06f, 0.08f));
                var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0f); ca.contrast.Override(14f); ca.saturation.Override(-25f); ca.colorFilter.Override(C(0.92f, 0.97f, 1f));
                var fgr = p.Add<FilmGrain>(true); fgr.intensity.Override(0.15f);
            });
            LocalVolume(zones, "Volume_Fog", new Rect(-744, -40, 250, 90), fogProf, 16f, 2f);

            EditorSceneManager.MarkSceneDirty(scene);
            FlushGrounds();
            EditorSceneManager.SaveScene(scene, Forest1ScenePath);
        }
    }
}
#endif
