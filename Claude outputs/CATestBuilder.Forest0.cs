#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;
using Random = System.Random;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 숲 씬 0 : 화창한 숲 (튜토리얼) — x -1945 ~ -1262. 끝에서 울창한 숲(Forest1, x -1262 ~)으로 끊김 없이 이어진다.
    /// 분위기 참고: LHS/Scene/C1Map (밝은 하늘, 흰 구름, 창백한 설산, 둥근 잎 나무, 산뜻한 초록)
    ///
    ///  S0 오프닝(-1962~-1862)  : 바위 언덕 속 비스듬한 좁은 틈을 굴러 내려와 숲 한가운데로 떨어짐 (CATestIntroCutscene). 세이브 #0
    ///  S1 이동·점프(-1862~-1782): 언덕길, 바위 턱(점프), 얕은 웅덩이                     [힌트: A/D, Space]
    ///  S2 달리기 점프(-1782~-1768): 폭 14 개울 협곡 — 걷기 점프(≈9)로는 못 넘고 달리기 점프(≈16)로 넘음 [힌트: Shift]
    ///  S3 웅크리기(-1768~-1716) : 거대한 바위 아래 높이 1.4 틈 — 서서는 못 지나감           [힌트: C]
    ///  S4 등반(-1716~-1690)     : 높이 10.6 덩굴 절벽 (점프로 닿지 않음)                   [힌트: W/S]
    ///  S5 굴뚝(-1690~-1672)     : 좌우 벽 덩굴 패치를 번갈아 → 벽점프 필수, 벽대쉬 소개     [힌트: 벽점프/벽대쉬]  세이브 #1
    ///  S6 레버(-1672~-1580)     : 높은 길의 레버로 문을 열고 내리막                         [힌트: E]
    ///  S7 상자(-1580~-1522)     : 높이 9.5 턱 → 상자를 밀어 발판으로                        [힌트: 밀기]
    ///  S8 은빛 강(-1522~-1430)  : 돌 → 출렁이는 통나무 → 가라앉는 연잎 → 통나무 → 달리기 점프. 빠지면 강가로. 세이브 #2, #3
    ///  S9 응용 퍼즐(-1430~-1262):
    ///     P1 선반 위 상자를 덩굴로 올라가 떨어뜨림 → 압력판에 올려 문 열기
    ///     P2 덩굴 기둥 위 시간 레버(5.5초) → 뛰어내려 달려서 문 통과
    ///     P3 바람 부는 통나무 다리 — 돌풍이 불 땐 웅크려 버티기                              [힌트: 돌풍]
    /// </summary>
    public static partial class CATestBuilder {
        private static readonly Color SunSky = new(0.62f, 0.8f, 0.95f);

        public static void BuildForest0Scene() {
            Surfaces.Clear();
            KeepClear.Clear();
            PendingGrounds.Clear();
            var scene = NewMapScene(Forest0ScenePath);
            var rnd = new Random(1987);
            var root = new GameObject("CATest_Forest0_Sunny").transform;
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
            var shapeBark = Load<SpriteShape>(OFShapes + "OldMiddleGround.asset");

            var sunGround = C(0.78f, 0.9f, 0.66f);   // 밝고 산뜻한 초록 (C1Map 톤)
            var rock = C(0.78f, 0.8f, 0.76f);
            var darkRock = C(0.36f, 0.38f, 0.4f);
            var bark = C(0.68f, 0.55f, 0.44f);

            // ═════════════════ S0 오프닝: 바위 언덕 속 비스듬한 좁은 틈 ═════════════════
            // 플레이어는 바위 언덕 안의 좁은 틈(폭 2.8m, 몸이 겨우 지나갈 정도)을 대각선으로 굴러 내려와
            // 절벽 면의 구멍으로 튕겨 나가 숲 한가운데 풀밭에 떨어진다.
            //  - 틈 중심선 CrevicePath: A(위) → M(꺾임) → B(출구). 컷신(CATestIntroCutscene)이 이 선을 따라 몸 중심을 움직인다.
            //  - 바위는 틈을 사이에 둔 두 덩어리(위·오른쪽 / 아래·왼쪽). 각 덩어리의 틈 쪽 경계를 중심선에서
            //    법선 방향으로 ±폭/2 만큼 띄운 선으로 만든다(꺾이는 곳은 두 법선의 평균 = 마이터).
            var crev = CrevicePath;
            var crevUp = new Vector2[crev.Length];
            var crevLo = new Vector2[crev.Length];
            for (var i = 0; i < crev.Length; i++) {
                var dPrev = i > 0 ? (crev[i] - crev[i - 1]).normalized : (crev[1] - crev[0]).normalized;
                var dNext = i < crev.Length - 1 ? (crev[i + 1] - crev[i]).normalized : dPrev;
                var n1 = new Vector2(-dPrev.y, dPrev.x);
                var n2 = new Vector2(-dNext.y, dNext.x);
                var n = (n1 + n2).normalized;
                var k = CreviceWidth * 0.5f / Mathf.Max(0.5f, Vector2.Dot(n, n1));
                crevUp[i] = crev[i] + n * k;   // 위·오른쪽 벽
                crevLo[i] = crev[i] - n * k;   // 아래·왼쪽 벽
            }
            // 위·오른쪽 바위: 윗면(왼→오) → 오른쪽 절벽 면(아래로) → 출구 윗입술 → 틈 윗벽을 따라 거슬러 올라감
            var upper = new List<Vector2> {
                new(crevUp[0].x, 97f), new(-1912f, 98.4f), new(-1896f, 97.6f), new(-1880f, 98.2f), new(-1868f, 96.5f),
                new(-1866.5f, 84f), new(-1869f, 70f), new(-1871.5f, 57f), new(-1874f, 44f), new(-1877.5f, 31f)
            };
            for (var i = crevUp.Length - 1; i >= 0; i--) upper.Add(crevUp[i]);
            Terrain(ground, "S0_RockUpper", upper.ToArray(), shapeStone, "Ground", 2, Mul(darkRock, 0.72f), true, 0.6f);
            // 아래·왼쪽 바위: 윗면 → 틈 맨 위를 막는 끝 → 틈 아랫벽 → 출구 아랫입술 → 절벽 면 → 땅속 → 왼쪽 면
            var lower = new List<Vector2> { new(-1962f, 96.2f), new(-1948f, 97.8f), new(-1936f, 96.8f), new(crevUp[0].x - 0.01f, 97f) };
            lower.Add(crevUp[0]);
            foreach (var q in crevLo) lower.Add(q);
            lower.AddRange(new Vector2[] { new(crevLo[^1].x - 0.3f, 16.5f), new(-1886.4f, 10f), new(-1887.6f, 4f), new(-1888f, -40f), new(-1962f, -40f) });
            Terrain(ground, "S0_RockLower", lower.ToArray(), shapeStone, "Ground", 2, Mul(darkRock, 0.72f), true, 0.6f);
            // 틈 속 어두운 뒷벽: 틈 사이로 뒤 배경(하늘)이 비치지 않도록 선분마다 어두운 판을 비스듬히 깐다
            for (var i = 0; i < crev.Length - 1; i++) {
                var d = crev[i + 1] - crev[i];
                var mid = (crev[i] + crev[i + 1]) * 0.5f;
                var sr = Put(nearBg, WhiteSprite, new Vector3(mid.x, mid.y, 0.5f), 1f, "Default", -40, C(0.05f, 0.055f, 0.07f),
                    rot: Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, name: "S0_CreviceBack" + i);
                if (sr != null) {
                    var bs = sr.sprite.bounds.size;
                    sr.transform.localScale = new Vector3((d.magnitude + 2.5f) / bs.x, (CreviceWidth + 1.4f) / bs.y, 1f);
                }
            }
            // 숲 한가운데 풀밭 (절벽 발치에서 시작)
            var gS0 = RoughSurf(1.0f, new[] { (-1890f, -1866f), (-1838f, -1826f) }, -1890, 3, -1876, 4.0f, -1862, 5.0f, -1850, 4.3f,
                -1838, 3.4f, -1826, 3.4f, -1814, 3.8f);
            Ground(ground, "S0_Clearing", gS0, -30, shapeLush, sunGround);
            Clear(-1880f, -1864f);
            Inst(PSaveForest, gameplay, new Vector3(-1868, SurfaceY(gS0, -1868), 0)).name = "SavePoint_00_Clearing";
            // 오프닝 컷신 실행기 (CATestBootstrap 이 "처음부터 시작"일 때 Pending 을 켜면 동작)
            var intro = new GameObject("IntroCutscene").AddComponent<CATestIntroCutscene>();
            intro.transform.SetParent(gameplay, false);
            Set(intro, "path", CrevicePath);
            Hint(zones, new Rect(-1888, 0, 30, 16), "A / D : 이동");
            // S1 점프: 바위 턱
            Block(ground, "S1_Rock", -1834.5f, -1829f, 2.6f, 5.8f, shapeStone, rock, 3, 0.4f);
            Surf(-1834.5f, 5.8f, -1829f, 5.8f);
            Hint(zones, new Rect(-1852, 0, 24, 16), "Space : 점프");
            // S1 얕은 웅덩이(빠져도 괜찮음)
            Ground(ground, "S1_Dip", Surf(-1814, 0.4f, -1808, 0.4f), -30, shapeLush, Mul(sunGround, 0.8f));
            var gS1 = RoughSurf(0.8f, new[] { (-1792f, -1782f) }, -1808, 3.8f, -1796, 4.6f, -1782, 5f);
            Ground(ground, "S1_Ridge", gS1, -30, shapeLush, sunGround);

            // ═════════════════ S2 달리기 점프: 개울 협곡 ═════════════════
            Ground(ground, "S2_ChasmBed", Surf(-1782, -10, -1768, -10), -30, shapeStone, Mul(rock, 0.6f));
            RiverReset(gameplay, "S2_Stream", new Rect(-1782, -12, 14, 5), new Vector2(-1790, 6.2f), null);
            WaterVisual(vfx, new Rect(-1782, -12, 14, 5), -7.5f);
            Hint(zones, new Rect(-1800, 2, 18, 14), "Shift 를 누른 채 달리다가 Space : 멀리 뛰기");
            Say(zones, new Rect(-1796, 2, 8, 12), "sunny_gap");

            // ═════════════════ S3 웅크리기: 바위 아래 틈 ═════════════════
            var gS2 = RoughSurf(0.8f, new[] { (-1754f, -1734f) }, -1768, 5, -1758, 5.4f, -1754, 5.2f, -1734, 5.2f, -1726, 6f, -1716, 6.4f);
            Ground(ground, "S2_Landing", gS2, -30, shapeLush, sunGround);
            Block(ground, "S3_Boulder", -1752, -1736, 6.6f, 36, shapeStone, rock, 3, 0.6f);
            Hint(zones, new Rect(-1768, 4, 16, 12), "C 를 누르고 있으면 웅크리기 — 낮은 틈을 지나가자");
            Say(zones, new Rect(-1764, 4, 6, 10), "sunny_crouch");

            // ═════════════════ S4 등반: 덩굴 절벽 ═════════════════
            var gS4 = Surf(-1712, 17, -1690, 17);
            Ground(ground, "S4_Plateau", gS4, -30, shapeLush, sunGround);
            VineWall(gameplay, rnd, "S4_Vine", -1712, 6.6f, 17, -1);
            Hint(zones, new Rect(-1730, 5, 18, 14), "덩굴 벽으로 점프해 붙은 뒤  W / S : 오르내리기");
            Say(zones, new Rect(-1728, 5, 8, 10), "sunny_vine");

            // ═════════════════ S5 굴뚝: 벽점프 ═════════════════
            Clear(-1706f, -1698f);
            Inst(PSaveForest, gameplay, new Vector3(-1702, 17, 0)).name = "SavePoint_01_Chimney";
            Block(ground, "S5_ChimL", -1690, -1685, 21.5f, 40, shapeStone, rock, 3, 0.5f);
            Ground(ground, "S5_ChimFloor", Surf(-1690, 17, -1679, 17), -30, shapeLush, sunGround);
            Ledge(ground, "S5_ChimR", -1679, -1672, 5, 38, shapeStone, rock, 3, 0.5f);
            VineWall(gameplay, rnd, "S5_R1", -1679, 17.5f, 25, -1);
            VineWall(gameplay, rnd, "S5_L1", -1685, 23.5f, 31, 1);
            VineWall(gameplay, rnd, "S5_R2", -1679, 29.5f, 38, -1);
            Hint(zones, new Rect(-1690, 16, 11, 9), "벽에 붙은 채  반대 방향 + Space : 벽 점프");
            Hint(zones, new Rect(-1690, 25, 11, 14), "벽에 붙은 채  W + Space : 벽 대쉬 (위로 빠르게)");
            Say(zones, new Rect(-1690, 16, 11, 6), "sunny_chimney");
            Inst(PRelicForest, gameplay, new Vector3(-1687.5f, 42.5f, 0)).name = "Relic_00_ChimneyTop";
            Ledge(ground, "S5_RelicPerch", -1690, -1685, 40, 41.2f, shapeBark, bark, 3, 0.3f);

            // ═════════════════ S6 레버: 높은 길 → 문 → 내리막 ═════════════════
            var gS6 = RoughSurf(0.6f, new[] { (-1662f, -1632f) }, -1672, 38, -1662, 38.4f, -1632, 38.4f, -1624, 34, -1614, 27, -1602, 19, -1592, 12.5f,
                -1580, 8.2f);
            Ground(ground, "S6_HighPath", gS6, -30, shapeLush, sunGround);
            var lever6 = Lever(gameplay, "S6_Lever", -1652, SurfaceY(gS6, -1652), CATestLever.Mode.OneShot);
            StakeGate(gameplay, "S6_Gate", -1638, SurfaceY(gS6, -1638), 7.5f, new MonoBehaviour[] { lever6 }, 8f, 5f);
            Block(ground, "S6_GateRock", -1640.5f, -1630, 46.4f, 70, shapeStone, rock, 3, 0.6f);
            Hint(zones, new Rect(-1660, 37, 14, 10), "E : 상호작용 (레버 당기기)");
            Say(zones, new Rect(-1664, 37, 6, 8), "sunny_lever");

            // ═════════════════ S7 상자: 9.5 턱 ═════════════════
            var gS7 = Surf(-1580, 8.2f, -1552, 8.2f);
            Ground(ground, "S7_Floor", gS7, -30, shapeLush, sunGround);
            Crate(gameplay, "Crate_S7", -1570, 8.2f);
            Clear(-1576f, -1550f);
            Ground(ground, "S7_HighLedge", Surf(-1552, 17.7f, -1522, 17.7f), -30, shapeLush, sunGround);
            Hint(zones, new Rect(-1582, 6, 22, 14), "상자에 몸을 대고 밀자 — 상자를 발판 삼아 올라가기");
            Say(zones, new Rect(-1580, 6, 8, 10), "sunny_crate");

            // ═════════════════ S8 은빛 강 ═════════════════
            var gS8L = Surf(-1522, 6.2f, -1512, 6.2f);
            Ground(ground, "S8_BankL", gS8L, -30, shapeLush, sunGround);
            Clear(-1520f, -1514f);
            Inst(PSaveForest, gameplay, new Vector3(-1517, 6.2f, 0)).name = "SavePoint_02_RiverBank";
            Ground(ground, "S8_RiverBed", Surf(-1512, -3, -1452, -3), -30, shapeStone, Mul(rock, 0.55f));
            WaterVisual(vfx, new Rect(-1512, -4, 60, 7.5f), 3.5f);
            RiverReset(gameplay, "S8_River", new Rect(-1512, -4, 60, 6.6f), new Vector2(-1516, 7.4f), "sunny_river_fall");
            Say(zones, new Rect(-1522, 5, 10, 8), "sunny_river");
            AreaTitle(zones, new Rect(-1524, -6, 94, 40), "은빛 강", "화창한 숲");
            Block(ground, "S8_Stone1", -1504, -1500, -3, 4.8f, shapeStone, rock, 3, 0.4f);
            Surf(-1504, 4.8f, -1500, 4.8f);
            Bobber(gameplay, "S8_Log1", -1493, 4.5f, 5f, 0.6f, shapeBark, bark, 0.3f, 1.1f, 0f, false);
            Bobber(gameplay, "S8_Lily1", -1484, 4.2f, 3f, 0.35f, shapeLush, C(0.55f, 0.85f, 0.5f), 0.12f, 1.6f, 1f, true);
            Bobber(gameplay, "S8_Lily2", -1477, 4.2f, 3f, 0.35f, shapeLush, C(0.55f, 0.85f, 0.5f), 0.12f, 1.6f, 2.2f, true);
            Bobber(gameplay, "S8_Log2", -1468, 4.5f, 5f, 0.6f, shapeBark, bark, 0.35f, 0.9f, 3f, false);
            var gS8R = RoughSurf(0.6f, new[] { (-1450f, -1440f) }, -1452, 6.2f, -1440, 6.2f, -1430, 6f);
            Ground(ground, "S8_BankR", gS8R, -30, shapeLush, sunGround);
            Clear(-1449f, -1443f);
            Inst(PSaveForest, gameplay, new Vector3(-1446, 6.2f, 0)).name = "SavePoint_03_FarBank";
            Say(zones, new Rect(-1452, 5, 10, 8), "sunny_river_done");

            // ═════════════════ S9-P1 선반 상자 → 압력판 → 문 ═════════════════
            var gP = Surf(-1430, 6, -1320, 6);
            Ground(ground, "P_Floor", gP, -30, shapeLush, sunGround);
            Ledge(ground, "P1_Shelf", -1426, -1414, 5, 16, shapeStone, rock, 3, 0.5f);
            VineWall(gameplay, rnd, "P1_Vine", -1426, 6.2f, 16, -1);
            Crate(gameplay, "Crate_P1", -1418.5f, 16);
            Clear(-1428f, -1380f);
            Block(ground, "P1_Curb", -1396.3f, -1395.5f, 5.2f, 6.9f, shapeStone, rock, 2, 0.2f);
            var plateP1 = Plate(gameplay, "P1_Plate", -1398, 6, false);
            StakeGate(gameplay, "P1_Gate", -1384, 6, 8.5f, new MonoBehaviour[] { plateP1 }, 8f, 12f);
            Block(ground, "P1_GateRock", -1386.5f, -1376, 14.6f, 60, shapeStone, rock, 3, 0.6f);
            Say(zones, new Rect(-1432, 4, 8, 10), "sunny_puzzle");

            // ═════════════════ S9-P2 기둥 위 시간 레버 → 달리기 ═════════════════
            Ledge(ground, "P2_Pillar", -1366, -1358, 5, 24, shapeStone, rock, 3, 0.5f);
            VineWall(gameplay, rnd, "P2_Vine", -1366, 6.2f, 24, -1);
            var leverP2 = Lever(gameplay, "P2_TimedLever", -1361, 24, CATestLever.Mode.Timed, 5.5f);
            StakeGate(gameplay, "P2_Gate", -1328, 6, 8.5f, new MonoBehaviour[] { leverP2 }, 8f, 5f);
            Block(ground, "P2_GateRock", -1330.5f, -1320, 14.6f, 60, shapeStone, rock, 3, 0.6f);
            Clear(-1368f, -1356f);
            Inst(PRelicForest, gameplay, new Vector3(-1419.5f, 29f, 0)).name = "Relic_01_ShelfSky";
            Ledge(ground, "P1_HighBranch", -1424, -1415, 27.2f, 28f, shapeBark, bark, 3, 0.3f);
            VineWall(gameplay, rnd, "P1_BranchVine", -1415, 17, 27.2f, 1);

            // ═════════════════ S9-P3 바람 부는 통나무 다리 ═════════════════
            Ground(ground, "P3_ChasmBed", Surf(-1320, -12, -1290, -12), -30, shapeStone, Mul(rock, 0.55f));
            WaterVisual(vfx, new Rect(-1320, -14, 30, 6), -9f);
            RiverReset(gameplay, "P3_Stream", new Rect(-1320, -14, 30, 6), new Vector2(-1324, 7f), "sunny_river_fall");
            Ledge(ground, "P3_LogBridge", -1320, -1290, 5.3f, 6f, shapeBark, bark, 4, 0.35f);
            Gust(gameplay, "P3_Gust", new Rect(-1320, 6, 30, 7), new Vector2(-12f, 0f), 1.6f, 1.8f);
            Hint(zones, new Rect(-1326, 5, 14, 12), "돌풍이 불 땐  C 로 웅크려 버티자");
            AreaTitle(zones, new Rect(-1322, -10, 34, 40), "바람 부는 다리");
            var gEnd = RoughSurf(0.5f, null, -1290, 6, -1276, 5f, -1262, 4f);
            Ground(ground, "P3_ToLushForest", gEnd, -30, shapeLush, sunGround);
            Surf(-1262, 4, -1250, 5.2f); // 이음새: 울창한 숲 첫 지면(윗면만 등록) → 절벽 깎기 판정에서 "이어진 땅"으로 인식
            Say(zones, new Rect(-1282, 3, 10, 10), "sunny_exit");

            // 추락 방지 바닥
            var dz = Inst(PDeadZone, gameplay, new Vector3(-1600, -40, 0));
            if (dz != null) { dz.name = "DeadZone_Bottom"; dz.transform.localScale = Vector3.one; dz.GetComponent<BoxCollider2D>().size = new Vector2(720, 4); }

            // ═════════════════ 식생 / 소품 (C1Map 톤) ═════════════════
            bool Hazard(float x) => (x > -1782 && x < -1768) || (x > -1512 && x < -1452) || (x > -1320 && x < -1290) || (x > -1752 && x < -1736);
            var flowers = Sps(FF + "Flowers.png", "Flowers", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
            var tufts = Sps(FF + "first layer greens.png", "first layer greens", 12, 19);
            var softBush = Sps(FF + "waterfall tree.png", "waterfall tree", 9, 10, 17, 18, 19, 21);
            var rocks = Sps(FF + "grass ferr.png", "grass ferr", 4, 5, 6, 7);
            var bushes = Sps(FF + "bushes.png", "bushes", 0, 1, 2, 3);
            ClusterSurf(props, rnd, tufts, -1906, -1262, 7f, 16f, 3, 0.35f, 0.6f, -0.6f, -0.2f, "Architecture", 2, C(0.85f, 1f, 0.75f), C(1f, 1f, 0.9f), 0.25f, 0.5f, Hazard, 9999f, 0.3f);
            ClusterSurf(props, rnd, flowers, -1906, -1262, 4f, 11f, 5, 0.5f, 0.85f, -0.3f, 0.4f, "Item", 8, Color.white, C(1f, 0.97f, 0.9f), 0f, 0.15f, Hazard, 9999f, 0.25f);
            ClusterSurf(props, rnd, softBush, -1906, -1262, 8f, 18f, 3, 0.8f, 1.3f, 0.8f, 2.4f, "Item", 0, C(0.85f, 1f, 0.8f), Color.white, 0.15f, 0.5f, Hazard);
            ClusterSurf(props, rnd, rocks, -1906, -1262, 20f, 44f, 2, 0.45f, 0.8f, 0.1f, 0.8f, "Item", 4, C(0.95f, 0.95f, 0.9f), Color.white, 0.25f, 0.55f, Hazard, 9999f, 0.3f);
            ClusterSurf(nearBg, rnd, bushes, -1906, -1262, 12f, 24f, 2, 0.9f, 1.3f, 3f, 6f, "Default", -20, C(0.72f, 0.88f, 0.62f), C(0.85f, 0.96f, 0.72f), 0.6f, 1.2f, Hazard);
            GrassOn(ground, shapeGrass, gS0, C(1f, 1f, 0.9f));
            GrassOn(ground, shapeGrass, gS1, C(1f, 1f, 0.9f));
            GrassOn(ground, shapeGrass, gS2, C(1f, 1f, 0.9f));
            GrassOn(ground, shapeGrass, gS6, C(1f, 1f, 0.9f));
            GrassOn(ground, shapeGrass, gS8R, C(1f, 1f, 0.9f));
            GrassOn(ground, shapeGrass, gP, C(1f, 1f, 0.9f));
            // 강가 갈대/풀
            var reeds = Sps(FF + "grass ferr.png", "grass ferr", 3);
            foreach (var x in new[] { -1515.5f, -1513f, -1450.5f, -1448f })
                Put(props, Pick(rnd, reeds), new Vector3(x, 5.6f, R(rnd, -0.3f, 0.3f)), R(rnd, 0.5f, 0.7f), "Item", 6, C(0.85f, 1f, 0.85f), rnd.Next(2) == 0);

            // ═════════════════ 배경 (하늘 → 설산 → 구름 → 먼 숲 → 들판 → 나무) ═════════════════
            // 하늘: 한 장을 아주 넓게 늘려 이음새가 없게 (가로로 균일한 그라데이션이라 늘려도 티가 안 남)
            var sky = Put(farBg, Sp(FF + "sky 3.png"), new Vector3(-1600, 40, 420), 1f, "BackGround", -260, C(0.9f, 0.97f, 1f));
            if (sky != null) sky.transform.localScale = new Vector3(40f, 9f, 1f);
            var mount = Sp(FF + "mount big.png");
            foreach (var (x, s, z) in new[] { (-1880f, 5.5f, 260f), (-1640f, 6.2f, 280f), (-1400f, 5.6f, 265f) })
                Put(farBg, mount, new Vector3(x, 14, z), s, "BackGround", -210, C(0.86f, 0.92f, 0.98f), rnd.Next(2) == 0);
            var cloudsA = Sps(FSB + "/Mount pack/Sprites/Clouds A.png", "Clouds A", 0, 1, 2, 3, 4);
            var cloudsB = Sps(FF + "clouds.png", "clouds", 2, 4, 5, 10, 11);
            foreach (var x in new[] { -1960f, -1880f, -1790f, -1700f, -1610f, -1520f, -1430f, -1340f, -1260f }) {
                var sr = Put(farBg, Pick(rnd, cloudsA), new Vector3(x + R(rnd, -20f, 20f), R(rnd, 30f, 55f), R(rnd, 180f, 240f)), R(rnd, 2.2f, 3.4f), "BackGround", -190,
                    C(1f, 1f, 1f, 0.95f), rnd.Next(2) == 0);
                if (sr != null) { var am = sr.gameObject.AddComponent<CATestAmbientMotion>(); am.driftSpeed = R(rnd, 0.15f, 0.35f); am.driftRange = 12f; }
            }
            foreach (var x in new[] { -1930f, -1850f, -1760f, -1660f, -1560f, -1470f, -1380f, -1290f }) {
                var sr = Put(midBg, Pick(rnd, cloudsB), new Vector3(x + R(rnd, -15f, 15f), R(rnd, 26f, 42f), R(rnd, 110f, 150f)), R(rnd, 2f, 3f), "BackGround", -170,
                    C(1f, 1f, 1f, 0.9f), rnd.Next(2) == 0);
                if (sr != null) { var am = sr.gameObject.AddComponent<CATestAmbientMotion>(); am.driftSpeed = R(rnd, 0.2f, 0.45f); am.driftRange = 10f; }
            }
            var backTrees = Sps(FF + "back trees.png", "back trees", 0, 1, 2, 3, 4, 5);
            ScatterBand(farBg, rnd, backTrees, -1980, -1240, 16, 26, -14, -6, 2.6f, 3.4f, 110, 150, "BackGround", -150, C(0.62f, 0.78f, 0.78f), C(0.7f, 0.84f, 0.82f));
            var fields = Sps(FF + "fields. 1.png", "fields.", 1, 2, 4, 5, 7, 8);
            ScatterBand(midBg, rnd, fields, -1980, -1240, 10, 16, -6, -2, 2.2f, 3f, 30, 45, "BackGround", -70, C(0.72f, 0.9f, 0.62f), C(0.82f, 0.96f, 0.7f));
            // 둥근 잎 나무 (C1Map처럼 밝은 연두·노란 잎을 섞어서) — 두 줄로 빽빽하게
            //  앞줄 z 8~20 : 9~16m 간격 / 뒷줄 z 26~40 : 더 크고 하늘색으로 흐리게(공기 원근)
            //  TreeGround: 나무 폭 안에 땅이 고르게 있을 때만 심고 가장 낮은 높이에 맞춤,
            //  ParallaxSink: 멀수록 밑동을 더 묻어 원근 때문에 떠 보이지 않게.
            for (var x = -1878f + R(rnd, 0f, 5f); x < -1266f; x += R(rnd, 9f, 16f)) {
                if (!TreeGround(x, 2.5f, 60f, out var gy)) continue;
                var z = R(rnd, 8f, 20f);
                SunnyTree(nearBg, rnd, x, gy - ParallaxSink(z), z, R(rnd, 1.1f, 1.55f), "Default", -30);
            }
            for (var x = -1874f + R(rnd, 0f, 8f); x < -1262f; x += R(rnd, 12f, 20f)) {
                if (!TreeGround(x, 3f, 60f, out var gy, 4f)) continue;
                var z = R(rnd, 26f, 40f);
                SunnyTree(nearBg, rnd, x, gy - ParallaxSink(z), z, R(rnd, 1.6f, 2.1f), "Default", -50, 0.35f);
            }
            // 오프닝 절벽 위 나무 몇 그루(굴이 숲속 절벽이라는 느낌)
            foreach (var x in new[] { -1954f, -1940f, -1903f, -1886f }) SunnyTree(nearBg, rnd, x, 96.5f, R(rnd, 4f, 8f), R(rnd, 1.2f, 1.5f), "Default", -12);

            // 전경(카메라 쪽) 검은 실루엣: 땅 띠 + 큰 나무. 오프닝 굴 구간(-1890 왼쪽)은 비움.
            FgSilhouettes(fg, rnd, -1880f, -1262f, 60f, shapeLush, Sps(FF + "new trees.png", "new trees", 0, 1, 2, 3, 4),
                Sps(FF + "new tree leafs.png", "new tree leafs", 0, 3, 4, 6, 8, 9, 10, 12), C(0.07f, 0.1f, 0.07f), 38f, 62f);

            // ═════════════════ 빛 / VFX ═════════════════
            var shaft = Sp(FF + "light 1. 1.png");
            foreach (var x in new[] { -1878f, -1840f, -1795f, -1724f, -1655f, -1600f, -1540f, -1480f, -1440f, -1350f, -1300f }) {
                var gy = SurfAt(x, 60);
                SunShaft(vfx, rnd, shaft, x, float.IsNaN(gy) ? 5f : gy, 26f, C(1f, 0.97f, 0.82f, R(rnd, 0.3f, 0.45f)), R(rnd, 0.5f, 0.75f));
            }
            // 굴 출구로 쏟아지는 빛(굴 아래쪽) + 굴 속 희미한 빛
            GodRay(vfx, "ShaftExitRay", -1872f, 3f, 32f, 9f, 0f, C(1f, 0.96f, 0.85f), 0.9f, 7f, "Default", 44, 2f, 0.55f, 5f, 0.8f);
            Light(vfx, "ShaftDim", new Vector3(-1909, 55, 0), C(0.5f, 0.55f, 0.7f), 0.3f, 10f, 2f);
            Light(vfx, "CreviceExitLeak", new Vector3(-1883, 21, 0), C(1f, 0.9f, 0.7f), 0.6f, 7f, 1.5f);
            Motes(vfx, "Sunny_Pollen", new Rect(-1906, 0, 644, 40), 1f, MatAddDot, C(1f, 1f, 0.8f, 0.55f), C(0.9f, 1f, 0.85f, 0.35f), 80f, 0.04f, 0.1f,
                new Vector2(0.25f, 0.05f), 0.4f, 9f, "Player", 40);
            var leavesP = Load<GameObject>(SrcLeaves);
            foreach (var x in new[] { -1850f, -1720f, -1600f, -1480f, -1340f }) {
                var gy = SurfAt(x, 60);
                if (leavesP != null) Inst(leavesP, vfx, new Vector3(x, (float.IsNaN(gy) ? 5f : gy) + 18f, -0.5f));
            }

            // ═════════════════ 분위기 / 카메라 / 볼륨 ═════════════════
            Zone(zones, "Sunny", new Rect(-1906, -30, 650, 140), 20, C(1f, 0.98f, 0.92f), 1f, SunSky);
            Zone(zones, "Shaft", new Rect(-1934, 24, 48, 66), 10, C(0.35f, 0.38f, 0.5f), 0.35f, C(0.04f, 0.05f, 0.08f), 2);
            CamZone(zones, "Shaft", new Rect(-1936, 22, 52, 70), 8, 85f, new Vector3(3f, -2f, 0), new Vector3(0.6f, 0.2f, 1), 2);
            CamZone(zones, "Chimney", new Rect(-1692, 17, 22, 24), 6, 104f, new Vector3(0, 3f, 0), new Vector3(1, 0.6f, 1), 2);
            CamZone(zones, "River", new Rect(-1522, -6, 92, 30), 10, 118f, new Vector3(4f, 1.5f, 0), Vector3.one);
            var prof = Profile("VP_CATest_Sunny", p => {
                var b = p.Add<Bloom>(true); b.intensity.Override(0.8f); b.threshold.Override(0.85f); b.scatter.Override(0.7f); b.tint.Override(C(1f, 0.97f, 0.9f));
                var v = p.Add<Vignette>(true); v.intensity.Override(0.2f); v.smoothness.Override(0.5f); v.color.Override(C(0.05f, 0.08f, 0.05f));
                var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0.12f); ca.contrast.Override(10f); ca.saturation.Override(6f); ca.colorFilter.Override(C(1f, 1f, 0.97f));
                var wb = p.Add<WhiteBalance>(true); wb.temperature.Override(4f); wb.tint.Override(-3f);
            });
            LocalVolume(zones, "Volume_Sunny", new Rect(-1906, -30, 650, 170), prof, 20f);

            EditorSceneManager.MarkSceneDirty(scene);
            FlushGrounds();
            EditorSceneManager.SaveScene(scene, Forest0ScenePath);
        }

        // ── Forest0 helpers ──
        private static void Say(Transform parent, Rect area, string key, bool grounded = true) {
            var go = new GameObject("Say_" + key);
            go.transform.SetParent(parent, false);
            var z = go.AddComponent<CATestSayZone>();
            z.area = area;
            z.lineKey = key;
            z.requireGrounded = grounded;
        }

        private static void RiverReset(Transform parent, string name, Rect area, Vector2 safe, string lineKey) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<CATestRiverReset>();
            r.area = area;
            r.safePoint = safe;
            r.lineKey = lineKey;
        }

        /// <summary>물 위에 뜬 발판 (통나무/연잎). 윗면 중심 (x, topY), 폭 width, 두께 thick.</summary>
        private static void Bobber(Transform parent, string name, float x, float topY, float width, float thick, SpriteShape shape, Color tint,
            float amp, float speed, float phase, bool sink) {
            var go = new GameObject(name) { layer = LayerGround };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, topY - thick * 0.5f, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, thick);
            var vis = Terrain(go.transform, "Visual", new Vector2[] {
                new(-width * 0.5f, thick * 0.5f), new(width * 0.5f, thick * 0.5f), new(width * 0.5f - 0.1f, -thick * 0.5f), new(-width * 0.5f + 0.1f, -thick * 0.5f)
            }, shape, "Ground", 4, tint, false, Mathf.Min(0.35f, thick));
            vis.transform.localPosition = Vector3.zero;
            var bp = go.AddComponent<CATestBobPlatform>();
            Set(bp, "bobAmplitude", amp);
            Set(bp, "bobSpeed", speed);
            Set(bp, "phase", phase);
            Set(bp, "sinkWhenStood", sink);
            Set(bp, "maxSink", 3.2f); // 연잎: 오래 서 있으면 수면 아래로 → 강에 빠짐(강가로 되돌아감)
            Set(bp, "sinkSpeed", 0.75f);
            Set(bp, "standCheckSize", new Vector2(width * 0.9f, 0.5f));
        }

        /// <summary>C1Map 느낌의 둥근 잎 나무: 녹색/연두-노랑 잎 시트를 섞어 밝게.</summary>
        /// <summary>오프닝 틈 중심선(위 → 꺾임 → 출구). CoreScene 시작 위치도 CrevicePath[0] 근처.</summary>
        public static readonly Vector2[] CrevicePath = { new(-1928f, 84f), new(-1909f, 55f), new(-1884f, 21f) };
        private const float CreviceWidth = 2.8f;

        private static void SunnyTree(Transform parent, Random rnd, float x, float groundY, float z, float scale, string layer, int order, float haze = 0f) {
            var sky = C(0.74f, 0.86f, 0.95f); // 먼 나무일수록 하늘색에 가깝게(공기 원근)
            var trunks = Sps(FF + "new trees.png", "new trees", 0, 1, 2, 3, 4);
            var green = Sps(FF + "new tree leafs.png", "new tree leafs", 0, 3, 4, 6, 8, 9, 10, 12);
            var yellow = Sps(FF + "new tree leafs ellow.png", "new tree leafs ellow", 0, 5, 6, 8, 9, 11);
            if (trunks.Length == 0) return;
            var s = Pick(rnd, trunks);
            Put(parent, s, new Vector3(x, groundY - 0.4f - s.bounds.min.y * scale, z), scale, layer, order, Color.Lerp(C(0.85f, 0.78f, 0.7f), sky, haze), rnd.Next(2) == 0);
            var h = s.bounds.size.y * scale;
            var w = s.bounds.size.x * scale;
            var top = groundY + h;
            var useYellow = rnd.Next(3) == 0 && yellow.Length > 0;
            var n = rnd.Next(4, 7);
            for (var i = 0; i < n; i++) {
                var sheet = useYellow && i % 2 == 0 ? yellow : green;
                if (sheet.Length == 0) continue;
                var p = new Vector3(x + R(rnd, -0.5f, 0.5f) * w, top - R(rnd, 0.0f, 0.35f) * h, z - 0.05f - i * 0.02f);
                var tint = Color.Lerp(Color.Lerp(C(0.8f, 0.95f, 0.72f), C(1f, 1f, 0.9f), (float)rnd.NextDouble()), sky, haze);
                var sr = Put(parent, Pick(rnd, sheet), p, scale * R(rnd, 1.2f, 1.8f), layer, order + 1 + i, tint, rnd.Next(2) == 0, R(rnd, -10f, 10f));
                if (sr == null) continue;
                var am = sr.gameObject.AddComponent<CATestAmbientMotion>();
                am.swayAngle = R(rnd, 0.8f, 1.8f);
                am.swaySpeed = R(rnd, 0.3f, 0.6f);
            }
        }
    }
}
#endif
