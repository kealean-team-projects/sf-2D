#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 마지막 챕터 "황혼의 성역" (x 1360 ~ 2040). 심해 "빛이 드는 틈"에서 흰 빛에 삼켜져 도착한다(CATestRealmTransition).
    /// 분위기 참고: 보르미르(일식이 걸린 황혼, 거울 같은 얕은 물, 모래 평원) + 흰 돌 구조물.
    ///
    /// ■ 이전 챕터와 겹치지 않는 그림만 사용
    ///   - 지형: Ice and snow pack(눈 땅 SpriteShape), Desert pack(모래 언덕·바위 기둥·석재 블록·가시)
    ///   - 구조물/함정/퍼즐: 직접 그린 스프라이트(Art/Twilight: 기둥·아치·비석·휘어진 거대 암석·룬·석판·문)
    ///   - 하늘: 일식 셰이더(CATest_EclipseSky), 물: 거울 물 셰이더(CATest_MirrorWater)
    ///
    /// ■ 구역
    ///  W 백색 협곡 (1398~1640)
    ///    W0 도착 공터(세이브) → W1 가시 구덩이(8m) → W2 흰 바위 기둥 2개 + 등반 절벽(높이 12)
    ///    → W3 금 간 석판 다리(오래 서 있으면 무너짐, 아래는 가시) → W4 머리 위 흰 종유석 낙하
    ///    → W5 계단으로 내려와 모래바람 평지(바람 불 때 웅크려 버티기) + 바닥 가시 → 세이브
    ///  S 백색 성역 (1640~1885)
    ///    S1 흰 블록 계단 → S2 룬 순서 퍼즐(벽화 순서대로 발판 밟기 → 성역 문 열림)
    ///    → S3 심판의 빛(바닥 룬 원 예고 후 빛기둥, 4개 엇갈림) → S4 떠다니는 석판 3개로 심연 건너기
    ///    → S5 성역 꼭대기(세이브 = 엔딩 전 체크포인트, 이어하기 지점)
    ///  N 용의 둥지 (1885~1995): 함정 없음. 거대한 휘어진 암석들로 둘러싸인 원형 광장 → 엔딩(CATestEndingDirector)
    /// </summary>
    public static partial class CATestBuilder {
        public const string TwilightScenePath = ScenesDir + "/CATest_Twilight.unity";
        private const string TWS = FSB + "/Ice and snow pack/Sprites/";
        private const string TWD = FSB + "/Desert pack/Sprites/";
        private const string TWA = ArtDir + "/Twilight/";

        public static GameObject PSaveTwilight, PCrumbleTwilight, PNeedlesTwilight;
        public static Material MatSky, MatMirror;

        // 색
        private static readonly Color TwGround = new(0.86f, 0.84f, 0.95f);
        private static readonly Color TwStone = new(0.92f, 0.9f, 0.98f);
        private static readonly Color TwWall = new(0.76f, 0.74f, 0.88f);
        private static readonly Color TwBone = new(0.97f, 0.95f, 1f);
        private static readonly Color TwBg = new(0.09f, 0.07f, 0.15f);
        // 먼 흰 구조물이 섞여 들어갈 황혼 안개색(검정이 아니라 연보라 → 멀수록 뿌옇게 흰빛 유지)
        private static readonly Color TwHaze = new(0.66f, 0.56f, 0.74f);

        // 엔딩 관련 위치 (EndingDirector / CoreScene 시작 지점과 공유)
        public static readonly Vector2 TwArrival = new(1412f, 18f);
        public static readonly Vector2 TwPreDragonSave = new(1858f, 14f);

        // ───────────────────────── 프리팹 (BuildPrefabs 에서 호출) ─────────────────────────
        public static void BuildTwilightPrefabs(Transform holder) {
            PSaveTwilight = SavePrefab(BuildSavePointTwilight(holder), "P_CATest_SavePoint_Twilight");
            PCrumbleTwilight = SavePrefab(BuildCrumbleTwilight(holder), "P_CATest_CrumbleTile_Twilight");
            PNeedlesTwilight = Variant(SrcUrchin, "PV_CATest_Needles_Twilight", go => HidePlaceholder(go));
        }

        private static GameObject BuildSavePointTwilight(Transform holder) {
            var root = NewRoot(holder, "SavePoint_Twilight");
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3f, 3f);
            box.offset = new Vector2(0f, 1.2f);
            var sp = root.AddComponent<CATestSavePoint>();
            // 받침: 작은 흰 비석 + 떠 있는 초승달 룬
            var baseS = Put(root.transform, Sp(TWA + "TW_Monolith.png"), new Vector3(0, 0.85f, 0.1f), 0.26f, "Item", 5, TwStone);
            var bob = new GameObject("Rune").transform;
            bob.SetParent(root.transform, false);
            bob.localPosition = new Vector3(0, 2.6f, 0);
            var rune = Put(bob, Sp(TWA + "TW_Rune_3.png"), bob.position, 0.32f, "Item", 8, C(1f, 1f, 1f, 0.9f), mat: MatAdd);
            Put(bob, Glow, bob.position, 0.5f, "Item", 7, C(1f, 0.8f, 0.7f, 0.6f), mat: MatAdd);
            var light = Light(root.transform, "Glow", new Vector3(0, 2.4f, 0), C(0.8f, 0.78f, 1f), 0.4f, 7f, 0.5f);
            var burst = Burst(root.transform, "ActivateBurst", new Vector3(0, 2.4f, -0.2f), MatAddDot, C(1f, 0.85f, 0.75f), 45, 6f, 0.3f, 1.4f, "Player", 20, -0.2f);
            var loop = Motes(root.transform, "Motes", new Rect(-0.8f, 1.8f, 1.6f, 1.2f), -0.2f, MatAddDot, C(1f, 0.9f, 0.85f, 0.8f), C(0.8f, 0.8f, 1f, 0.6f),
                5f, 0.04f, 0.1f, new Vector2(0f, 0.5f), 0.3f, 2.5f, "Player", 19);
            loop.transform.localPosition = new Vector3(0, 2.4f, -0.2f);
            Set(sp, "glow", light);
            Set(sp, "tintTargets", new Object[] { baseS, rune }.Where(o => o != null).ToArray());
            Set(sp, "activateBurst", burst);
            Set(sp, "loopParticles", loop);
            Set(sp, "bobTarget", bob);
            Set(sp, "idleColor", C(0.62f, 0.6f, 0.85f));
            Set(sp, "activeColor", C(1f, 0.78f, 0.62f));
            return root;
        }

        private static GameObject BuildCrumbleTwilight(Transform holder) {
            var root = NewRoot(holder, "CrumbleTile", LayerGround);
            var b = root.AddComponent<BoxCollider2D>();
            b.size = new Vector2(3f, 0.7f);
            b.offset = new Vector2(0f, -0.35f);
            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var tile = Put(vis.transform, Sp(TWA + "TW_CrackedTile.png"), new Vector3(0, -0.42f, 0), 1.02f, "Ground", 20, TwStone);
            var crumble = root.AddComponent<CATestCrumblingPlatform>();
            var dust = Burst(root.transform, "Dust", new Vector3(0, -0.5f, -0.3f), MatAlphaDot, C(0.9f, 0.88f, 1f, 0.8f), 24, 1.6f, 0.3f, 1.2f, "Player", 12, 0.4f);
            var sh = dust.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(3f, 0.3f, 0.1f);
            Set(crumble, "visual", vis.transform);
            Set(crumble, "dust", dust);
            Set(crumble, "shakeTime", 0.5f);
            Set(crumble, "sfxKey", "tile_crack");
            _ = tile;
            return root;
        }

        // ───────────────────────── 씬 ─────────────────────────
        public static void BuildTwilightScene() {
            Surfaces.Clear();
            KeepClear.Clear();
            PendingGrounds.Clear();
            var scene = NewMapScene(TwilightScenePath);
            var rnd = new Random(2046);
            var root = new GameObject("CATest_Twilight").transform;
            var ground = Group(root, "10_Terrain").transform;
            var gameplay = Group(root, "20_Gameplay").transform;
            var props = Group(root, "30_Props").transform;
            var nearBg = Group(root, "40_NearBG").transform;
            var midBg = Group(root, "50_MidBG").transform;
            var farBg = Group(root, "60_FarBG").transform;
            var fg = Group(root, "05_Foreground").transform;
            var vfx = Group(root, "70_VFX_Light").transform;
            var zones = Group(root, "80_Zones").transform;

            var shapeSnow = Load<SpriteShape>(FSB + "/Ice and snow pack/Sprite shape/Snow ground.asset");
            var shapeIce = Load<SpriteShape>(FSB + "/Ice and snow pack/Sprite shape/Ice ground.asset");
            var shapeSand = Load<SpriteShape>(FSB + "/Desert pack/Sprite shapes/Desert ground.asset");
            var shapeStone = shapeSnow; // 흰 돌: 눈 땅 모양을 회백색으로 칠해 씀

            // ═════════════════ W 백색 협곡 ═════════════════
            // 왼쪽 끝 협곡 벽(뒤로 못 가게)
            Block(ground, "W_LeftWall", 1368f, 1398f, -30f, 44f, shapeStone, Mul(TwWall, 0.85f), 2, 0.6f);
            // W0 도착 공터
            var gW0 = RoughSurf(0.8f, new[] { (1412f, 1424f) }, 1398, 0, 1412, 0.2f, 1424, 0.2f, 1438, 0f);
            Ground(ground, "W0_Arrival", gW0, -30, shapeSnow, TwGround);
            Clear(1414f, 1424f);
            Inst(PSaveTwilight, gameplay, new Vector3(1420, SurfaceY(gW0, 1420), 0)).name = "SavePoint_T0_Arrival";
            SetSavePlace("SavePoint_T0_Arrival", gameplay, "백색 협곡");
            AreaTitle(zones, new Rect(1398, -4, 30, 30), "백색 협곡", "마지막 챕터 · 황혼의 성역");
            Say(zones, new Rect(1426, -2, 8, 10), "canyon_enter");

            // W1 가시 구덩이 (8m, 걸어서 점프로 넘을 수 있음)
            TwNeedlePit(ground, gameplay, rnd, "W1_NeedlePit", 1438f, 1446f, -6f, -30f);
            Say(zones, new Rect(1430, -2, 6, 10), "canyon_needles");
            var gW1 = RoughSurf(0.5f, new[] { (1446f, 1470f) }, 1446, 0.5f, 1470, 0.5f);
            Ground(ground, "W1_Floor", gW1, -30, shapeSnow, TwGround);

            // W2 흰 바위 기둥 두 개(징검다리) + 등반 절벽(높이 12)
            TwPillarStep(ground, props, rnd, "W2_PillarA", 1456f, 5f);
            TwPillarStep(ground, props, rnd, "W2_PillarB", 1463.5f, 9f);
            Ledge(ground, "W2_Cliff", 1470f, 1500f, -30f, 12f, shapeStone, TwWall, 2, 0.6f);
            ClimbEdge(ground, new Vector2(1470f, 0.8f), new Vector2(1470f, 11.6f), "W2_ClimbFace");
            TwClimbMarks(props, 1470f, 1f, 11.5f);

            // W3 금 간 석판 다리 (오래 서 있으면 무너짐) — 아래 깊은 곳은 가시
            TwNeedlePit(ground, gameplay, rnd, "W3_Chasm", 1500f, 1532f, -14f, -40f);
            foreach (var x in new[] { 1506f, 1513f, 1520f, 1527f }) Inst(PCrumbleTwilight, gameplay, new Vector3(x, 12f, 0)).name = "W3_CrumbleTile";
            Say(zones, new Rect(1490, 12, 8, 8), "canyon_crumble");
            Ledge(ground, "W3_FarLedge", 1532f, 1560f, -40f, 12f, shapeStone, TwWall, 2, 0.6f);

            // W4 머리 위 흰 종유석 (지나가면 떨어짐)
            Block(ground, "W4_Overhang", 1528f, 1566f, 22f, 34f, shapeStone, Mul(TwWall, 0.9f), 2, 0.6f);
            var icicle = Sp(TWA + "TW_Icicle.png");
            var k = 0;
            foreach (var x in new[] { 1538f, 1546f, 1554f }) {
                FallingRock(gameplay, "W4_Stalactite_" + k++, x, 20.2f, 12f, new Rect(x - 5f, 12f, 4f, 8f), icicle, 0.55f, TwBone);
            }

            // W5 계단으로 내려와 모래바람 평지
            Ledge(ground, "W5_Step1", 1560f, 1568f, -20f, 8f, shapeStone, TwWall, 2, 0.5f);
            Ledge(ground, "W5_Step2", 1568f, 1576f, -20f, 4f, shapeStone, TwWall, 2, 0.5f);
            var gW5 = RoughSurf(0.9f, new[] { (1608f, 1620f), (1626f, 1640f) }, 1576, 1f, 1600, 1.2f, 1640, 1f);
            Ground(ground, "W5_WindFlat", gW5, -30, shapeSnow, Mul(TwGround, 0.95f));
            var wind = Gust(gameplay, "W5_SandWind", new Rect(1586, 1, 36, 10), new Vector2(-9f, 0f), 2.4f, 1.9f);
            TintGust(wind, C(0.95f, 0.9f, 1f, 0.7f), C(1f, 0.85f, 0.8f, 0.5f));
            Say(zones, new Rect(1580, 0, 6, 10), "canyon_wind");
            TwNeedles(gameplay, rnd, "W5_FloorNeedles", 1612f, 1616f, SurfaceY(gW5, 1614f));
            Clear(1626f, 1636f);
            Inst(PSaveTwilight, gameplay, new Vector3(1632, SurfaceY(gW5, 1632), 0)).name = "SavePoint_T1_CanyonEnd";
            SetSavePlace("SavePoint_T1_CanyonEnd", gameplay, "백색 협곡 끝");

            // ═════════════════ S 백색 성역 ═════════════════
            AreaTitle(zones, new Rect(1642, -2, 30, 30), "백색 성역", "잊힌 신의 사원");
            var gS1 = Surf(1640, 1f, 1660, 1f);
            Ground(ground, "S1_Floor", gS1, -30, shapeSnow, TwGround);
            Say(zones, new Rect(1646, -2, 8, 10), "sanctum_enter");
            // 흰 블록 계단 (석재 블록 스프라이트를 흰색으로)
            TwStoneStep(ground, props, "S1_Step1", 1660f, 1664f, 3.5f);
            TwStoneStep(ground, props, "S1_Step2", 1664f, 1668f, 6f);
            // 성역 테라스 (y 8.5)
            var gS2 = Surf(1668, 8.5f, 1800, 8.5f);
            Ground(ground, "S2_Terrace", gS2, -30, shapeSnow, TwGround);
            Clear(1694f, 1745f);

            // S2 룬 순서 퍼즐
            var order = new[] { 1, 2, 0 };
            var seqGo = new GameObject("S2_RuneSequence");
            seqGo.transform.SetParent(gameplay, false);
            var seq = seqGo.AddComponent<CATestRuneSequence>();
            var plates = new List<CATestRunePlate>();
            var px = new[] { 1700f, 1712f, 1724f };
            for (var i = 0; i < 3; i++) plates.Add(TwRunePlate(gameplay, i, px[i], 8.5f, seq));
            // 문 + 문 위 벽(넘어갈 수 없게) + 벽화
            var door = TwDoor(gameplay, "S2_SanctumDoor", 1738f, 8.5f, 8f, new MonoBehaviour[] { seq });
            Block(ground, "S2_DoorWallTop", 1733.5f, 1742.5f, 16.5f, 34f, shapeStone, TwStone, 3, 0.5f);
            var mural = new List<SpriteRenderer>();
            for (var i = 0; i < 3; i++) {
                var g = Put(props, Sp(TWA + $"TW_Rune_{order[i]}.png"), new Vector3(1735.5f + i * 2.5f, 20.5f, -0.2f), 0.7f, "Ground", 30,
                    C(0.85f, 0.82f, 1f, 0.3f), mat: MatAdd);
                mural.Add(g);
            }
            Put(props, Sp(TWA + "TW_RoseWindow.png"), new Vector3(1738f, 27f, 0.3f), 1.1f, "Ground", 28, TwStone);
            SetIntArray(seq, "order", order);
            Set(seq, "plates", plates.Cast<Object>().ToArray());
            Set(seq, "muralGlyphs", mural.Cast<Object>().ToArray());
            Say(zones, new Rect(1690, 8, 8, 10), "sanctum_mural");
            _ = door;

            // S3 심판의 빛 (4개, 박자 엇갈림)
            Say(zones, new Rect(1744, 8, 6, 10), "sanctum_beam");
            var beamX = new[] { 1756f, 1768f, 1780f, 1792f };
            for (var i = 0; i < beamX.Length; i++) TwJudgmentBeam(gameplay, "S3_JudgmentBeam_" + i, beamX[i], 8.5f, i * 0.78f);

            // S4 떠다니는 석판으로 심연 건너기
            TwNeedlePit(ground, gameplay, rnd, "S4_Abyss", 1800f, 1846f, -10f, -40f);
            Say(zones, new Rect(1794, 8, 6, 10), "sanctum_float");
            TwMovingSlab(gameplay, "S4_Slab1", new Vector2(1804.5f, 8.2f), new Vector2(11f, 0f), 3.2f, 0.7f, 0f);
            TwMovingSlab(gameplay, "S4_Slab2", new Vector2(1821.5f, 6f), new Vector2(0f, 8f), 2.6f, 0.8f, 1.2f);
            TwMovingSlab(gameplay, "S4_Slab3", new Vector2(1832f, 14f), new Vector2(8.5f, 0f), 2.6f, 0.6f, 0.4f);

            // S5 성역 꼭대기 (엔딩 전 체크포인트)
            var gS5 = Surf(1846, 14f, 1885, 14f);
            Ground(ground, "S5_Top", gS5, -40, shapeSnow, TwGround);
            Clear(1852f, 1864f);
            Inst(PSaveTwilight, gameplay, new Vector3(TwPreDragonSave.x, 14f, 0)).name = "SavePoint_T2_BeforeNest";
            SetSavePlace("SavePoint_T2_BeforeNest", gameplay, "용의 둥지 앞");
            Say(zones, new Rect(1866, 14, 8, 8), "sanctum_top");
            Put(props, Sp(TWA + "TW_Altar.png"), new Vector3(1872f, 14f + 1.4f, 0.5f), 0.95f, "Default", -5, TwStone);

            // ═════════════════ N 용의 둥지 ═════════════════
            AreaTitle(zones, new Rect(1886, 12, 20, 20), "용의 둥지", "");
            var gN = RoughSurf(0.6f, new[] { (1910f, 1940f) }, 1885, 14f, 1910, 14.2f, 1940, 14.2f, 1995, 14f);
            Ground(ground, "N_Arena", gN, -40, shapeSnow, Mul(TwGround, 0.92f));
            Block(ground, "N_RightWall", 1995f, 2030f, -40f, 80f, shapeStone, Mul(TwWall, 0.7f), 2, 0.6f);
            // 둥지를 둘러싼 거대한 휘어진 암석 (뒤쪽 여러 겹 + 앞쪽 어두운 실루엣)
            var horns = new[] { Sp(TWA + "TW_HornRock_A.png"), Sp(TWA + "TW_HornRock_B.png"), Sp(TWA + "TW_HornRock_C.png") };
            (float x, float z, float s, bool flip, int h)[] hornSet = {
                (1880f, 30f, 2.6f, false, 0), (1900f, 55f, 3.2f, false, 2), (1935f, 70f, 3.6f, true, 1), (1968f, 50f, 3f, true, 0),
                (1992f, 28f, 2.8f, true, 2), (2012f, 60f, 3.4f, true, 1), (1918f, 22f, 2.2f, false, 1),
            };
            foreach (var hs in hornSet) {
                var s = horns[hs.h];
                if (s == null) continue;
                var y = 14f - ParallaxSink(hs.z) - s.bounds.min.y * hs.s;
                var tint = Color.Lerp(TwStone, TwBg, Mathf.Clamp01(hs.z / 110f));
                Put(nearBg, s, new Vector3(hs.x, y, hs.z), hs.s, "Default", -20 - Mathf.RoundToInt(hs.z * 0.2f), tint, hs.flip);
            }
            // 앞쪽(카메라 쪽) 어두운 휘어진 바위
            Put(fg, horns[0], new Vector3(1884f, 14f - 11f, -16f), 2.4f, "Architecture", 70, C(0.07f, 0.05f, 0.1f));
            Put(fg, horns[2], new Vector3(2004f, 14f - 10f, -18f), 2.6f, "Architecture", 70, C(0.07f, 0.05f, 0.1f), true);

            // 엔딩 감독 + 수호룡
            // 용은 몸 중심이 루트. 머리 뼈가 원하는 곳(headAt)에 오도록 루트 위치를 거꾸로 계산한다.
            //  - 리그 기준(원본 캔버스 px): head_02 뼈 (120,345), 주둥이 끝 x=15, 앞발톱 y=517 → ×0.09(=100PPU·9배)
            //    → 주둥이는 머리 뼈보다 약 9.5 유닛 왼쪽, 앞발톱은 약 15.5 유닛 아래
            //  - headAt (1963.5, 30.5) → 주둥이 x≈1954, 앞발톱 y≈15(바닥 14 바로 위) — 몸통/꼬리는 화면 오른쪽 밖으로
            var headAt = new Vector3(1963.5f, 30.5f, 4f);
            var dragon = BuildDragonInstance(gameplay, new Vector3(2100f, 70f, 4f));
            var headOff = dragon != null ? dragon.Head.position - dragon.transform.position : new Vector3(-80f, 0f, 0f);
            headOff.z = 0f;
            var hover = headAt - headOff;
            var start = hover + new Vector3(110f, 45f, 0f);
            if (dragon != null) dragon.transform.position = start;
            var dirGo = new GameObject("N_EndingDirector");
            dirGo.transform.SetParent(gameplay, false);
            var dir = dirGo.AddComponent<CATestEndingDirector>();
            Set(dir, "triggerArea", new Rect(1918f, 12f, 10f, 12f));
            Set(dir, "dragon", dragon);
            Set(dir, "dragonStart", start);
            Set(dir, "dragonHover", hover);
            Set(dir, "cameraPoint", new Vector3(1948f, 26f, 0f));
            Set(dir, "cameraDistance", 200f); // 거대한 용이 들어오도록 평소(약 110)보다 훨씬 멀리

            // ═════════════════ 배경 ═════════════════
            BuildTwilightBackdrop(rnd, farBg, midBg, nearBg, props, fg);

            // ═════════════════ 빛 / 입자 ═════════════════
            Motes(vfx, "TW_Ash", new Rect(1398, -2, 600, 40), 1f, MatAddDot, C(1f, 0.9f, 0.9f, 0.45f), C(0.8f, 0.8f, 1f, 0.3f), 70f, 0.03f, 0.08f,
                new Vector2(-0.4f, 0.12f), 0.5f, 10f, "Player", 40);
            // 일식 쪽(오른쪽 위)에서 오는 붉은 역광 — 흰 구조물 오른쪽 면이 장밋빛으로 물듦
            foreach (var x in new[] { 1450f, 1560f, 1680f, 1800f, 1940f })
                Light(vfx, "EclipseRim", new Vector3(x + 30f, 60f, 0f), C(1f, 0.5f, 0.45f), 0.45f, 70f, 10f, 0.8f);
            // 성역 빛기둥(심판의 빛과 구분되는 약한 황혼빛)
            foreach (var x in new[] { 1500f, 1650f, 1720f, 1870f })
            {
                var rayY = SurfAt(x, 40f);
                GodRay(vfx, "DuskRay", x, float.IsNaN(rayY) ? 8f : rayY, 34f, 8f, -18f, C(1f, 0.65f, 0.6f), 0.55f, x * 0.1f,
                    "Default", 42, 3f, 0.4f, 5f, 0.5f);
            }
            Light(vfx, "NestGlow", new Vector3(1955f, 26f, 0f), C(1f, 0.6f, 0.5f), 0.8f, 30f, 6f);

            // ═════════════════ 분위기 / 카메라 / 볼륨 ═════════════════
            Zone(zones, "Twilight", new Rect(1370, -40, 680, 160), 20, C(0.68f, 0.62f, 0.88f), 0.85f, TwBg);
            Zone(zones, "Nest", new Rect(1885, 0, 120, 80), 16, C(0.8f, 0.62f, 0.8f), 0.9f, TwBg, 1);
            CamZone(zones, "Canyon", new Rect(1398, -10, 242, 50), 12, 112f, new Vector3(3f, 5f, 0), Vector3.one);
            CamZone(zones, "Crumble", new Rect(1496, 0, 40, 40), 8, 120f, new Vector3(4f, 1f, 0), Vector3.one, 2);
            CamZone(zones, "Sanctum", new Rect(1640, -10, 245, 50), 12, 118f, new Vector3(4f, 5f, 0), Vector3.one);
            CamZone(zones, "Nest", new Rect(1885, 0, 115, 60), 14, 140f, new Vector3(8f, 5f, 0), Vector3.one, 2);
            var prof = Profile("VP_CATest_Twilight", p => {
                var b = p.Add<Bloom>(true); b.intensity.Override(1.1f); b.threshold.Override(0.85f); b.scatter.Override(0.75f); b.tint.Override(C(1f, 0.75f, 0.72f));
                var v = p.Add<Vignette>(true); v.intensity.Override(0.34f); v.smoothness.Override(0.55f); v.color.Override(C(0.06f, 0.02f, 0.08f));
                var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0f); ca.contrast.Override(16f); ca.saturation.Override(-8f); ca.colorFilter.Override(C(0.96f, 0.92f, 1f));
                var smh = p.Add<ShadowsMidtonesHighlights>(true); smh.shadows.Override(new Vector4(0.85f, 0.8f, 1.05f, -0.08f)); smh.highlights.Override(new Vector4(1.05f, 0.92f, 0.88f, 0.04f));
            });
            LocalVolume(zones, "Volume_Twilight", new Rect(1370, -40, 680, 160), prof, 25f);

            EditorSceneManager.MarkSceneDirty(scene);
            FlushGrounds();
            EditorSceneManager.SaveScene(scene, TwilightScenePath);
        }

        // ───────────────────────── 배경 ─────────────────────────
        private static void BuildTwilightBackdrop(Random rnd, Transform farBg, Transform midBg, Transform nearBg, Transform props, Transform fg) {
            // 하늘: 일식 셰이더 사각형을 카메라 뒤 먼 곳(z 800)에 두고 CATestSkyDome 이 카메라를 따라가게
            var skyGo = new GameObject("TW_EclipseSky");
            skyGo.transform.SetParent(farBg, false);
            skyGo.transform.position = new Vector3(1600f, 20f, 800f);
            var sky = skyGo.AddComponent<SpriteRenderer>();
            sky.sprite = WhiteSprite;
            sky.sharedMaterial = MatSky;
            sky.sortingLayerID = SL("BackGround");
            sky.sortingOrder = -500;
            var ss = WhiteSprite.bounds.size;
            skyGo.transform.localScale = new Vector3(760f / ss.x, 400f / ss.y, 1f);
            var dome = skyGo.AddComponent<CATestSkyDome>();
            Set(dome, "areaMinX", 1360f);
            Set(dome, "areaMaxX", 2040f);
            Set(dome, "baseY", 20f);
            Set(dome, "eclipseUV", new Vector2(0.607f, 0.62f));

            BuildPlainLayers(farBg, midBg, rnd, 1300f, 2140f);

            var mountC = Sp(TWS + "Snow Background C.png");
            // 협곡 벽: 흰 능선(눈 배경 C)을 라벤더 흰색으로, 가까운 층
            for (var x = 1380f; x < 1660f; x += 40f)
                PutTop(midBg, mountC, x + R(rnd, -6f, 6f), 6f, 45f, 1.1f, "BackGround", -200, C(0.6f, 0.57f, 0.74f), rnd.Next(2) == 0);
            // 바위 기둥(후두) — 흰 탑처럼
            var hoodoos = TwHoodoos;
            for (var x = 1400f; x < 1990f; x += R(rnd, 14f, 26f)) {
                var z = R(rnd, 14f, 40f);
                var s = Pick(rnd, hoodoos);
                if (s == null) continue;
                var gy = SurfLow(x, 40f);
                if (float.IsNaN(gy)) gy = 0f;
                var sc = R(rnd, 0.9f, 1.4f);
                var tint = Color.Lerp(TwBone, TwHaze, Mathf.Clamp01(z / 45f));
                Put(nearBg, s, new Vector3(x, gy - ParallaxSink(z) - s.bounds.min.y * sc, z), sc, "Default", -40 - Mathf.RoundToInt(z * 0.2f), tint, rnd.Next(2) == 0);
            }
            // 성역 뒤 구조물: 아치 / 기둥 / 부서진 기둥 / 비석 / 원형 창
            var arch = Sp(TWA + "TW_Arch.png");
            var pillar = Sp(TWA + "TW_Pillar.png");
            var pillarB = Sp(TWA + "TW_PillarBroken.png");
            var mono = Sp(TWA + "TW_Monolith.png");
            foreach (var (x, z, s) in new[] { (1652f, 12f, 1.6f), (1704f, 18f, 2.2f), (1760f, 10f, 1.9f), (1790f, 22f, 2.4f), (1864f, 14f, 1.7f), (1884f, 8f, 1.5f) }) {
                var gy = SurfLow(x, 40f); if (float.IsNaN(gy)) gy = 8.5f;
                Put(nearBg, arch, new Vector3(x, gy - ParallaxSink(z) - arch.bounds.min.y * s, z), s, "Default", -30, Color.Lerp(TwBone, TwHaze, z / 30f));
            }
            for (var x = 1644f; x < 1885f; x += R(rnd, 7f, 11f)) {
                var z = R(rnd, 3f, 9f);
                var gy = SurfLow(x, 40f); if (float.IsNaN(gy)) continue;
                var s = rnd.Next(3) == 0 ? pillarB : pillar;
                var sc = R(rnd, 0.75f, 1.05f);
                Put(nearBg, s, new Vector3(x, gy - ParallaxSink(z) - s.bounds.min.y * sc, z), sc, "Default", -25, Color.Lerp(TwBone, TwHaze, z / 14f));
            }
            foreach (var x in new[] { 1422f, 1590f, 1848f, 1978f }) {
                var gy = SurfLow(x, 40f); if (float.IsNaN(gy)) continue;
                Put(nearBg, mono, new Vector3(x, gy - 0.9f - mono.bounds.min.y * 0.9f, 4f), 0.9f, "Default", -15, TwStone);
            }
            // 바닥 소품: 흰 돌 부스러기 / 얼음 결정(연보라)
            var rubble = new[] { Sp(TWA + "TW_Rubble_A.png"), Sp(TWA + "TW_Rubble_B.png") };
            var crystals = new[] { Sp(TWA + "TW_Crystal_0.png"), Sp(TWA + "TW_Crystal_2.png"), Sp(TWA + "TW_Crystal_4.png") };
            for (var x = 1400f; x < 1990f; x += R(rnd, 5f, 11f)) {
                if (InClear(x)) continue;
                var gy = SurfAt(x, 40f); if (float.IsNaN(gy)) continue;
                if (rnd.Next(3) == 0 && crystals.Length > 0) {
                    var c = Pick(rnd, crystals);
                    Put(props, c, new Vector3(x, gy - 0.3f - c.bounds.min.y * 0.45f, 0.4f), R(rnd, 0.35f, 0.55f), "Default", 5, C(0.9f, 0.86f, 1f, 0.9f), rnd.Next(2) == 0);
                }
                else {
                    var r = Pick(rnd, rubble);
                    Put(props, r, new Vector3(x, gy - 0.25f - r.bounds.min.y * 0.5f, 0.3f), R(rnd, 0.4f, 0.6f), "Default", 4, TwStone, rnd.Next(2) == 0);
                }
            }
            // 앞쪽 검은 모래 띠 + 흰 바위 기둥 실루엣 (둥지 1885~ 는 용이 가려지지 않도록 비움)
            FgSilhouettes(fg, rnd, 1400f, 1880f, 40f, Load<SpriteShape>(FSB + "/Desert pack/Sprite shapes/Desert ground.asset"),
                TwHoodoos.Take(3).ToArray(), null, C(0.06f, 0.045f, 0.1f), 50f, 80f, 3.4f, -12f, 1.1f, 1.5f);
        }

        /// <summary>거울 물 띠: 흰 사각 스프라이트를 가로로 길게 늘이고 MirrorWater 셰이더.</summary>
        private static void MirrorBand(Transform parent, string name, float cx, float cy, float z, float width, float height, int order) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(cx, cy, z);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite;
            sr.sharedMaterial = MatMirror;
            sr.sortingLayerID = SL("BackGround");
            sr.sortingOrder = order;
            var s = WhiteSprite.bounds.size;
            go.transform.localScale = new Vector3(width / s.x, height / s.y, 1f);
        }

        // ───────────────────────── 함정 / 장치 도우미 ─────────────────────────
        /// <summary>가시 구덩이: 양옆 흰 절벽 사이 x0~x1, 바닥 floorY 에 흰 가시(사망 판정 = 원본 UrchinTrap Variant).</summary>
        private static void TwNeedlePit(Transform ground, Transform gameplay, Random rnd, string name, float x0, float x1, float floorY, float bottom) {
            Terrain(ground, name + "_Floor", new Vector2[] { new(x0 - 0.2f, floorY), new(x1 + 0.2f, floorY), new(x1 + 0.2f, bottom), new(x0 - 0.2f, bottom) },
                Load<SpriteShape>(FSB + "/Desert pack/Sprite shapes/Desert ground.asset"), "Ground", 0, Mul(TwWall, 0.55f));
            TwNeedles(gameplay, rnd, name + "_Needles", x0, x1, floorY);
        }

        private static void TwNeedles(Transform gameplay, Random rnd, string name, float x0, float x1, float floorY) {
            var t = Inst(PNeedlesTwilight, gameplay, new Vector3((x0 + x1) * 0.5f, floorY + 0.7f, 0f));
            if (t == null) return;
            t.name = name;
            SizeBox(t, x1 - x0, 1.4f);
            var row = Sp(TWA + "TW_Needles_0.png");
            var clusters = new[] { Sp(TWA + "TW_Needles_1.png"), Sp(TWA + "TW_Needles_2.png"), Sp(TWA + "TW_Needles_3.png") };
            for (var x = x0 + 0.6f; x < x1 - 0.4f; x += R(rnd, 1.4f, 2.2f)) {
                var s = rnd.Next(3) == 0 && clusters.Length > 0 ? Pick(rnd, clusters) : row;
                if (s == null) continue;
                var sc = s == row ? R(rnd, 0.28f, 0.36f) : R(rnd, 0.5f, 0.7f);
                Put(t.transform, s, new Vector3(x, floorY - 0.2f - s.bounds.min.y * sc, -0.05f), sc, "Item", 6, TwBone, rnd.Next(2) == 0);
            }
        }

        /// <summary>흰 바위 기둥 징검다리: 윗면 topY, 폭 3. 판정은 사각 발판, 그림은 사막 바위 기둥을 흰색으로.</summary>
        private static void TwPillarStep(Transform ground, Transform props, Random rnd, string name, float cx, float topY) {
            Ledge(ground, name, cx - 1.5f, cx + 1.5f, -20f, topY, Load<SpriteShape>(FSB + "/Ice and snow pack/Sprite shape/Snow ground.asset"), TwStone, 3, 0.45f);
            var s = Sp(TWA + "TW_Hoodoo_10.png");
            if (s != null) {
                var sc = 3.2f / s.bounds.size.x;
                Put(props, s, new Vector3(cx, topY - 0.3f - s.bounds.max.y * sc, -0.2f), sc, "Ground", 1, TwStone, rnd.Next(2) == 0);
            }
        }

        /// <summary>등반 가능한 면 표시: 흰 절벽에 파인 손잡이 홈(작은 룬 점).</summary>
        private static void TwClimbMarks(Transform props, float x, float y0, float y1) {
            var d = Sp(ArtDir + "/UI/UI_Diamond.png");
            for (var y = y0 + 0.8f; y < y1; y += 1.6f)
                Put(props, d, new Vector3(x + 0.25f, y, -0.1f), 0.22f, "Ground", 40, C(1f, 0.85f, 0.75f, 0.55f), mat: MatAdd);
        }

        private static void TwStoneStep(Transform ground, Transform props, string name, float x0, float x1, float topY) {
            Ledge(ground, name, x0, x1, -20f, topY, Load<SpriteShape>(FSB + "/Ice and snow pack/Sprite shape/Snow ground.asset"), TwStone, 3, 0.4f);
            var s = Sp(TWA + "TW_Block_A.png");
            if (s == null) return;
            var sc = (x1 - x0 + 0.2f) / s.bounds.size.x;
            for (var y = topY; y > topY - 8f; y -= s.bounds.size.y * sc)
                Put(props, s, new Vector3((x0 + x1) * 0.5f, y - s.bounds.max.y * sc, -0.1f), sc, "Ground", 5, TwStone);
        }

        private static CATestRunePlate TwRunePlate(Transform parent, int runeId, float x, float floorY, CATestRuneSequence seq) {
            var root = new GameObject("S2_RunePlate_" + runeId);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, floorY, 0f);
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.4f, 1.8f);
            box.offset = new Vector2(0f, 0.9f);
            Put(root.transform, Sp(TWA + "TW_RunePlate.png"), root.transform.position + new Vector3(0, -0.15f, 0.05f), 0.9f, "Item", 4, TwStone);
            var glyph = Put(root.transform, Sp(TWA + $"TW_Rune_{runeId}.png"), root.transform.position + new Vector3(0, 1.35f, -0.05f), 0.55f, "Item", 8,
                C(0.75f, 0.72f, 0.9f, 0.3f), mat: MatAdd);
            var am = glyph != null ? glyph.gameObject.AddComponent<CATestAmbientMotion>() : null;
            if (am != null) { am.bobHeight = 0.12f; am.bobSpeed = 1.4f + runeId * 0.2f; }
            var l = Light(root.transform, "Glow", root.transform.position + new Vector3(0, 1f, 0), C(1f, 0.85f, 0.65f), 0f, 4.5f, 0.5f);
            var plate = root.AddComponent<CATestRunePlate>();
            Set(plate, "runeId", runeId);
            Set(plate, "glyph", glyph);
            Set(plate, "glow", l);
            Set(plate, "sequence", seq);
            return plate;
        }

        private static CATestGate TwDoor(Transform parent, string name, float x, float floorY, float height, MonoBehaviour[] sources) {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, floorY, 0f);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = new GameObject("Solid") { layer = LayerGround };
            col.transform.SetParent(root.transform, false);
            var b = col.AddComponent<BoxCollider2D>();
            b.size = new Vector2(2.6f, height);
            b.offset = new Vector2(0f, height * 0.5f);
            var s = Sp(TWA + "TW_Door.png");
            if (s != null) {
                var sr = Put(root.transform, s, root.transform.position + new Vector3(0, height * 0.5f, 0.05f), 1f, "Ground", 2, TwStone); // 문 위 벽(순서 3) 뒤로 올라가 숨도록
                sr.transform.localScale = new Vector3(2.8f / s.bounds.size.x, height / s.bounds.size.y, 1f);
            }
            var dust = Burst(root.transform, "Dust", root.transform.position + new Vector3(0, 0.2f, -0.4f), MatAlphaDot, C(0.9f, 0.88f, 1f, 0.7f), 0, 1.5f,
                0.3f, 1f, "Player", 14, 0.2f);
            var dm = dust.main; dm.loop = true;
            var em = dust.emission; em.rateOverTime = 25f;
            var gate = root.AddComponent<CATestGate>();
            Set(gate, "sources", sources);
            Set(gate, "openOffset", new Vector2(0f, height + 0.3f));
            Set(gate, "openSpeed", 3f);
            Set(gate, "closeSpeed", 3f);
            Set(gate, "moveDust", dust);
            return gate;
        }

        private static void TwJudgmentBeam(Transform parent, string name, float x, float floorY, float phase) {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, floorY, 0f);
            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(root.transform, false);
            var height = 26f;
            var beam = GodRay(beamGo.transform, "BeamRay", x, floorY + 1f, height, 3.6f, 0f, C(1f, 0.95f, 0.88f), 1.8f, x, "Player", 90, -0.5f, 0.7f, 4f, 1.2f);
            var warn = GodRay(beamGo.transform, "WarnRay", x, floorY + 1f, height, 1.2f, 0f, C(1f, 0.5f, 0.4f), 0.9f, x + 7f, "Default", 43, 0.5f, 0.8f, 2f, 0.4f);
            var circle = Put(root.transform, Sp(TWA + "TW_RuneCircle.png"), new Vector3(x, floorY + 0.15f, -0.3f), 0.75f, "Item", 12, C(1f, 0.55f, 0.45f, 0f), mat: MatAdd);
            if (circle != null) circle.transform.localScale = new Vector3(0.9f, 0.28f, 1f); // 바닥에 누운 원처럼 납작하게
            var l = Light(root.transform, "Glow", new Vector3(x, floorY + 2f, 0f), C(1f, 0.6f, 0.5f), 0f, 7f, 1f);
            var jb = root.AddComponent<CATestJudgmentBeam>();
            Set(jb, "height", height);
            Set(jb, "width", 3.2f);
            Set(jb, "phaseOffset", phase);
            Set(jb, "beam", beam != null ? beam.GetComponent<SpriteRenderer>() : null);
            Set(jb, "warnBeam", warn != null ? warn.GetComponent<SpriteRenderer>() : null);
            Set(jb, "runeCircle", circle);
            Set(jb, "glow", l);
            // 심판의 빛 내리꽂는 자리 위쪽 하늘 구멍 느낌: 작은 룬 원
            Put(root.transform, Sp(TWA + "TW_RuneCircle.png"), new Vector3(x, floorY + height, 6f), 0.8f, "Default", 30, C(1f, 0.85f, 0.8f, 0.35f), mat: MatAdd);
        }

        private static void TwMovingSlab(Transform parent, string name, Vector2 top, Vector2 offset, float travel, float pause, float phase) {
            var root = new GameObject(name) { layer = LayerGround };
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(top.x, top.y, 0f);
            root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var b = root.AddComponent<BoxCollider2D>();
            b.size = new Vector2(3.6f, 0.6f);
            b.offset = new Vector2(0f, -0.3f);
            var s = Sp(TWA + "TW_Slab.png");
            Put(root.transform, s, root.transform.position + new Vector3(0, -0.5f, 0f), 0.92f, "Ground", 22, TwStone);
            Put(root.transform, Glow, root.transform.position + new Vector3(0, -1.1f, 0.05f), 1.1f, "Ground", 21, C(1f, 0.75f, 0.65f, 0.35f), mat: MatAdd)
                .transform.localScale = new Vector3(2.2f, 0.5f, 1f);
            var mp = root.AddComponent<CATestMovingPlatform>();
            Set(mp, "offset", offset);
            Set(mp, "travelTime", travel);
            Set(mp, "pause", pause);
            Set(mp, "phase", phase);
            Set(mp, "riderCheckSize", new Vector2(3.4f, 0.6f));
            Set(mp, "topY", 0f);
        }

        private static void TintGust(CATestGustZone g, Color warn, Color gust) {
            if (g == null) return;
            foreach (var ps in g.GetComponentsInChildren<ParticleSystem>(true)) {
                var m = ps.main;
                m.startColor = ps.name.StartsWith("Warn") ? new ParticleSystem.MinMaxGradient(warn, gust) : new ParticleSystem.MinMaxGradient(gust, warn);
            }
        }

        private static void SetSavePlace(string objName, Transform parent, string place) {
            var t = parent.Find(objName);
            if (t == null) return;
            var sp = t.GetComponent<CATestSavePoint>();
            if (sp != null) Set(sp, "placeName", place);
        }

        private static void SetIntArray(Object target, string field, int[] values) {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).intValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite[] TwHoodoos => new[] { 3, 4, 5, 10, 12 }.Select(i => Sp(TWA + $"TW_Hoodoo_{i}.png")).Where(x => x != null).ToArray();

        /// <summary>윗변이 topY 에 오도록 놓기(스프라이트 피벗 위치와 무관).</summary>
        private static SpriteRenderer PutTop(Transform parent, Sprite s, float x, float topY, float z, float scale, string layer, int order, Color tint, bool flip = false) {
            if (s == null) return null;
            return Put(parent, s, new Vector3(x, topY - s.bounds.max.y * scale, z), scale, layer, order, tint, flip);
        }

        /// <summary>
        /// 먼 배경: 산맥 + 모래 언덕 3겹 + 그 사이 거울 물 3줄. 원근 카메라에서 "화면상 높이"가 차곡차곡 쌓이도록 역산한 값.
        ///   화면상 높이 a = (y − 카메라 y) × D/(D+z)   (D ≈ 112 = 카메라 거리, 게임 중 카메라 y ≈ 땅 + 6.5)
        ///   → 땅 선(a ≈ −6.5) 바로 위에 가까운 언덕, 그 위로 먼 물·언덕·산이 층층이 보인다.
        /// </summary>
        private static void BuildPlainLayers(Transform far, Transform mid, Random rnd, float x0, float x1) {
            var mountB = Sp(TWS + "Snow Background B.png");
            var duneA = Sp(TWD + "Background Dune A.png");
            var duneB = Sp(TWD + "Background Dune B.png");
            var duneC = Sp(TWD + "Background Dune C.png");
            var cx = (x0 + x1) * 0.5f;
            var w = x1 - x0 + 300f;
            for (var x = x0 - 60f; x < x1 + 60f; x += 34f)
                PutTop(far, mountB, x + R(rnd, -6f, 6f), R(rnd, 14f, 18f), 330f, 0.83f, "BackGround", -300, C(0.26f, 0.21f, 0.38f), rnd.Next(2) == 0);
            MirrorBand(far, "TW_MirrorFar", cx, -5.1f, 260f, w, 4.7f, -290);
            for (var x = x0 - 40f; x < x1 + 40f; x += 58f)
                PutTop(far, duneA, x, -3.5f + R(rnd, -0.4f, 0.4f), 200f, 1.6f, "BackGround", -280, C(0.2f, 0.16f, 0.28f), rnd.Next(2) == 0);
            MirrorBand(mid, "TW_MirrorMid", cx, -4.3f, 150f, w, 2.8f, -260);
            for (var x = x0 - 30f; x < x1 + 30f; x += 44f)
                PutTop(mid, duneB, x, -3f + R(rnd, -0.3f, 0.3f), 120f, 1.2f, "BackGround", -250, C(0.25f, 0.2f, 0.32f), rnd.Next(2) == 0);
            MirrorBand(mid, "TW_MirrorNear", cx, -2.8f, 70f, w, 1.8f, -230);
            for (var x = x0 - 20f; x < x1 + 20f; x += 34f)
                PutTop(mid, duneC, x, -1.95f + R(rnd, -0.3f, 0.3f), 60f, 0.9f, "BackGround", -220, C(0.31f, 0.26f, 0.38f), rnd.Next(2) == 0);
        }

        /// <summary>일식 하늘 머티리얼 값 (게임·타이틀 공용). 사진처럼 어둡고 차분하게: 작은 원판 + 얇은 고리, 붉은 기는 한쪽만.</summary>
        public static void ConfigureSkyMaterial(Material m) {
            if (m == null) return;
            m.SetFloat("_Aspect", 1.9f);
            m.SetFloat("_HorizonY", 0.44f);
            m.SetFloat("_MidY", 0.7f);
            m.SetVector("_EclipsePos", new Vector4(0.607f, 0.62f, 0f, 0f));
            m.SetFloat("_Radius", 0.072f);
            m.SetFloat("_RingWidth", 0.004f);
            m.SetFloat("_HotPower", 4f);
            m.SetFloat("_GlowSize", 0.1f);
            m.SetFloat("_FlareStrength", 0.45f);
            m.SetColor("_GlowColor", new Color(0.7f, 0.18f, 0.24f, 1f));
            m.SetColor("_HorizonColor", new Color(0.36f, 0.22f, 0.32f, 1f));
            m.SetColor("_MidColor", new Color(0.12f, 0.1f, 0.21f, 1f));
            m.SetColor("_CloudLit", new Color(0.5f, 0.26f, 0.32f, 1f));
            EditorUtility.SetDirty(m);
        }
    }
}
#endif
