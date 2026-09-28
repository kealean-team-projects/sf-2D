#if UNITY_EDITOR
using System.Collections.Generic;
using _02._Script._01_Players.Components.CheckComponent;
using _02._Script._03_TrapAndEnemy.Enemies;
using _02._Script._03_TrapAndEnemy.Traps;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 숲 두 씬(Forest1: 울창한 숲 + 안개 골짜기 / Forest2: 몽환의 숲 + 산)이 같이 쓰는 배치 도우미.
    ///
    /// 좌표 약속
    ///  - 플레이 평면 z = 0. 배경은 z &gt; 0(멀수록 큼), 전경 실루엣은 z &lt; 0 (원근 카메라라서 z만으로 시차가 생긴다).
    ///  - 지형 윗면은 "왼쪽 → 오른쪽" 폴리라인으로 쓰고 Close()로 아래를 닫아 다각형을 만든다.
    ///  - 발판 윗면 폴리라인은 Surf() 로 등록해 두고, 장식(풀/꽃/버섯)을 뿌릴 때 SurfAt(x)로 높이를 찾는다.
    ///
    /// 플레이어 수치(설계 기준, CATest Core 에서 moveSpeed 7 로 조정)
    ///  - 점프 높이 ≈ 8, 걷기 점프 거리 ≈ 8, 달리기 점프 거리 ≈ 16
    ///  - 서 있는 높이 2, 웅크림 높이 1 (→ 웅크림 통로 천장 높이 1.5)
    ///  - 벽점프(수정 후): 상승이 끝날 때까지 입력 잠금, 수평 ≈ 5.6 / 수직 ≈ +3.8
    ///  - 등반 대쉬(수정 후): 0.22초 동안 22유닛/초 ≈ 5유닛
    /// </summary>
    public static partial class CATestBuilder {
        private const string FFShapes = FSB + "/Forest sprite pack/Sprite shapes/";
        private const string OFShapes = FSB + "/Old Forest pack/Sprite shapes/";

        private static readonly List<Vector2[]> Surfaces = new();
        /// <summary>
        /// 퍼즐 물체(상자·압력판·레버·문) 주변 x 구간. 플레이어보다 앞에 그려지는 "Architecture" 레이어 소품은
        /// 이 구간에 놓지 않는다 → 앞쪽 덤불이 상자/판을 가려 "상자가 사라진 것처럼" 보이는 문제 방지.
        /// </summary>
        private static readonly List<Vector2> KeepClear = new();
        private static void Clear(float x0, float x1) => KeepClear.Add(new Vector2(x0, x1));
        private static bool InClear(float x) {
            foreach (var r in KeepClear) if (x > r.x && x < r.y) return true;
            return false;
        }

        private static Vector2[] Surf(params float[] xy) {
            var pts = new Vector2[xy.Length / 2];
            for (var i = 0; i < pts.Length; i++) pts[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            Surfaces.Add(pts);
            return pts;
        }

        /// <summary>
        /// 자연스러운 굴곡: 핵심 점(퍼즐/발판 높이를 결정하는 점)은 그대로 두고, 긴 구간(10유닛 이상) 사이에만
        /// 완만한 언덕/둔덕을 끼워 넣는다. 끝점에서 0 이 되는 포락선(sin)을 곱해 이웃 구간과 매끄럽게 이어진다.
        /// flat 목록의 x 범위(상자 밀기 길, 문/압력판 주변 등)는 평평하게 유지한다.
        /// </summary>
        private static Vector2[] RoughSurf(float amp, (float a, float b)[] flat, params float[] xy) {
            var keys = new List<Vector2>();
            for (var i = 0; i + 1 < xy.Length; i += 2) keys.Add(new Vector2(xy[i], xy[i + 1]));
            var pts = new List<Vector2>();
            for (var i = 0; i < keys.Count - 1; i++) {
                var a = keys[i];
                var b = keys[i + 1];
                pts.Add(a);
                var len = b.x - a.x;
                if (len < 10f) continue;
                var n = Mathf.FloorToInt(len / 2.6f);
                var seed = a.x * 0.37f + b.x * 0.11f;
                var localAmp = Mathf.Min(amp, len * 0.07f);
                for (var k = 1; k < n; k++) {
                    var t = k / (float)n;
                    var x = Mathf.Lerp(a.x, b.x, t);
                    var inFlat = false;
                    if (flat != null) foreach (var f in flat) if (x > f.a - 2f && x < f.b + 2f) { inFlat = true; break; }
                    if (inFlat) continue;
                    var env = Mathf.Sin(t * Mathf.PI);
                    var noise = Mathf.Sin(x * 0.55f + seed) * 0.6f + Mathf.Sin(x * 0.23f + seed * 1.7f) * 0.9f + Mathf.Sin(x * 1.3f + seed) * 0.15f;
                    pts.Add(new Vector2(x, Mathf.Lerp(a.y, b.y, t) + noise * localAmp * env * 0.6f));
                }
            }
            pts.Add(keys[^1]);
            var arr = pts.ToArray();
            Surfaces.Add(arr);
            return arr;
        }

        /// <summary>
        /// 소품을 "무리" 단위로 불규칙하게 뿌린다: 무리 사이 간격(gapMin~gapMax)은 크게, 무리 안에서는 1~maxInCluster 개를 가깝게.
        /// 같은 간격으로 줄지어 보이는 문제를 피하고, 크기/깊이/가라앉는 정도/좌우반전을 모두 흔든다.
        /// </summary>
        private static void ClusterSurf(Transform parent, Random rnd, Sprite[] sprites, float x0, float x1, float gapMin, float gapMax, int maxInCluster,
            float scMin, float scMax, float zMin, float zMax, string layer, int order, Color a, Color b, float sinkMin, float sinkMax,
            System.Func<float, bool> skip = null, float yBelow = 9999f, float skipChance = 0.2f) {
            if (sprites == null || sprites.Length == 0) return;
            var x = x0 + R(rnd, 0f, gapMax);
            while (x < x1) {
                if (rnd.NextDouble() > skipChance) {
                    var count = rnd.Next(1, maxInCluster + 1);
                    var baseSc = R(rnd, scMin, scMax);
                    for (var i = 0; i < count; i++) {
                        var cx = x + R(rnd, -1.6f, 1.6f) * (i == 0 ? 0.3f : 1f);
                        var y = SurfAt(cx, yBelow);
                        if (float.IsNaN(y) || (skip != null && skip(cx))) continue;
                        if (layer == "Architecture" && InClear(cx)) continue;
                        var s = Pick(rnd, sprites);
                        var sc = baseSc * R(rnd, 0.7f, 1.15f) * (i == 0 ? 1f : 0.8f);
                        Put(parent, s, new Vector3(cx, y - R(rnd, sinkMin, sinkMax) - s.bounds.min.y * sc, R(rnd, zMin, zMax)), sc, layer,
                            order + rnd.Next(3), Color.Lerp(a, b, (float)rnd.NextDouble()), rnd.Next(2) == 0, R(rnd, -4f, 4f));
                    }
                }
                x += R(rnd, gapMin, gapMax);
            }
        }

        /// <summary>등록된 윗면들 중 x 위에 있고 yBelow 이하인 가장 높은 면의 높이. 없으면 NaN.</summary>
        private static float SurfAt(float x, float yBelow = 9999f) {
            var best = float.NaN;
            foreach (var line in Surfaces) {
                if (x < line[0].x || x > line[^1].x) continue;
                var y = SurfaceY(line, x);
                if (y > yBelow) continue;
                if (float.IsNaN(best) || y > best) best = y;
            }
            return best;
        }

        private static Vector2[] Close(Vector2[] top, float bottom) {
            var list = new List<Vector2>(top) { new(top[^1].x, bottom), new(top[0].x, bottom) };
            return list.ToArray();
        }

        // ── 지면은 바로 만들지 않고 모아 두었다가(FlushGrounds) 한꺼번에 만든다 ──
        // 이유: 절벽 옆면을 울퉁불퉁하게 깎으려면 "옆에 다른 지면이 붙어 있는지"를 알아야 하는데,
        // 그건 모든 지면 윗면이 등록된 뒤에야 알 수 있기 때문.
        private sealed class PendingGround {
            public Transform Parent; public string Name; public Vector2[] Top; public float Bottom;
            public SpriteShape Shape; public Color Tint; public int Order; public float Edge;
        }
        private static readonly List<PendingGround> PendingGrounds = new();

        private static GameObject Ground(Transform parent, string name, Vector2[] top, float bottom, SpriteShape shape, Color tint,
            int order = 1, float edge = 0.6f) {
            PendingGrounds.Add(new PendingGround { Parent = parent, Name = name, Top = top, Bottom = bottom, Shape = shape, Tint = tint, Order = order, Edge = edge });
            return null;
        }

        /// <summary>모아 둔 지면을 생성. 씬 저장 직전에 반드시 호출.</summary>
        private static void FlushGrounds() {
            foreach (var g in PendingGrounds)
                Terrain(g.Parent, g.Name, CliffClose(g.Top, g.Bottom), g.Shape, "Ground", g.Order, g.Tint, true, g.Edge);
            PendingGrounds.Clear();
        }

        /// <summary>
        /// Close()와 같지만, 옆에 이어지는 지면이 없는 "진짜 절벽" 쪽 옆면을 안쪽으로 불규칙하게 깎는다.
        /// - 윗모서리 1m 는 수직 유지(발 디딤/벽 오르기 판정이 모서리에서 안정적이도록)
        /// - 바깥으로는 절대 튀어나오지 않음(안쪽으로만 0.2~1.1m) → 벽 점프/등반 판정을 방해하지 않음
        /// - 옆 지면 높이(floor) 아래로는 깎지 않음 → 두 지면 사이에 틈(배경이 비치는 균열)이 생기지 않음
        /// </summary>
        private static Vector2[] CliffClose(Vector2[] top, float bottom) {
            var list = new List<Vector2>(top);
            var r = top[^1];
            var l = top[0];
            list.AddRange(CliffSide(top, r, +1, bottom));
            var left = CliffSide(top, l, -1, bottom);
            left.Reverse();
            list.AddRange(left);
            return list.ToArray();
        }

        private static List<Vector2> CliffSide(Vector2[] own, Vector2 e, int dir, float bottom) {
            var pts = new List<Vector2>();
            var probe = e.x + dir * 0.8f;
            var floor = float.NaN;
            foreach (var line in Surfaces) {
                if (ReferenceEquals(line, own) || probe < line[0].x || probe > line[^1].x) continue;
                var y = SurfaceY(line, probe);
                if (y > e.y + 3f) continue; // 한참 위에 떠 있는 발판은 무시
                if (float.IsNaN(floor) || y > floor) floor = y;
            }
            if (float.IsNaN(floor)) floor = bottom;
            if (floor < bottom) floor = bottom;
            if (floor > e.y - 2.5f) { // 옆 지면과 높이가 비슷 → 이어진 땅, 깎지 않음
                pts.Add(new Vector2(e.x, bottom));
                return pts;
            }
            pts.Add(new Vector2(e.x, e.y - 1.0f)); // 윗모서리 1m 수직
            var d = 1.8f;
            var limit = Mathf.Min(e.y - floor - 0.8f, 16f);
            while (d < limit) {
                var n = Hash01(e.x * 1.7f + d * 3.1f);
                pts.Add(new Vector2(e.x - dir * Mathf.Lerp(0.2f, 1.1f, n), e.y - d));
                d += Mathf.Lerp(1.1f, 2.1f, Hash01(e.y * 2.3f + d));
            }
            pts.Add(new Vector2(e.x, Mathf.Max(floor, e.y - d)));
            pts.Add(new Vector2(e.x, bottom));
            // 중복/역순 점 제거(아래로만 내려가도록)
            for (var i = pts.Count - 1; i > 0; i--)
                if (pts[i].y >= pts[i - 1].y - 0.05f) pts.RemoveAt(i);
            return pts;
        }

        private static float Hash01(float v) {
            var h = Mathf.Sin(v * 12.9898f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        /// <summary>직사각형 지형 (x0~x1, y0~y1). 윗면은 등록하지 않는다(벽/천장/기둥용).</summary>
        private static GameObject Block(Transform parent, string name, float x0, float x1, float y0, float y1, SpriteShape shape, Color tint,
            int order = 2, float edge = 0.5f) =>
            Terrain(parent, name, RoughBox(x0, x1, y0, y1), shape, "Ground", order, tint, true, edge);

        /// <summary>
        /// 직사각형 대신 자연스러운 바위 윤곽.
        /// - 윗면: 1.4~2.2m 간격으로 ±0.18m 잔굴곡(걷는 데 지장 없는 정도)
        /// - 옆면: 윗모서리 0.9m·아랫모서리 0.4m는 수직 유지, 그 사이만 "안쪽으로" 0.12~0.6m 깎음
        ///   (바깥으로 튀어나오지 않으므로 벽 오르기/벽 점프 판정 위치는 그대로)
        /// - 아랫면(천장 역할): 절대 건드리지 않음 → 웅크려 지나가는 통로 높이가 바뀌지 않음
        /// 너무 작은 블록(턱, 문틀 등)은 그대로 직사각형.
        /// </summary>
        private static Vector2[] RoughBox(float x0, float x1, float y0, float y1) {
            var w = x1 - x0;
            var h = y1 - y0;
            var pts = new List<Vector2> { new(x0, y1) };
            var amp = Mathf.Min(0.6f, w * 0.18f); // 얇은 기둥은 양쪽을 깎아도 서로 겹치지 않게
            var sides = h >= 3f && w >= 1.2f;
            if (w >= 3f) {
                var x = x0 + 1.2f;
                while (x < x1 - 1.2f) {
                    pts.Add(new Vector2(x, y1 + (Hash01(x * 0.91f + y1) - 0.5f) * 0.36f));
                    x += Mathf.Lerp(1.4f, 2.2f, Hash01(x * 1.3f + h));
                }
            }
            pts.Add(new Vector2(x1, y1));
            if (sides) {
                pts.Add(new Vector2(x1, y1 - 0.9f));
                var d = 1.6f;
                while (d < h - 0.9f) {
                    pts.Add(new Vector2(x1 - Mathf.Lerp(0.2f, 1f, Hash01(x1 * 2.1f + d)) * amp, y1 - d));
                    d += Mathf.Lerp(0.9f, 1.8f, Hash01(x1 + d * 0.7f));
                }
                pts.Add(new Vector2(x1, y0 + 0.4f));
            }
            pts.Add(new Vector2(x1, y0));
            pts.Add(new Vector2(x0, y0));
            if (sides) {
                var left = new List<Vector2> { new(x0, y0 + 0.4f) };
                var ups = new List<Vector2>();
                var dd = 1.6f;
                while (dd < h - 0.9f) {
                    ups.Add(new Vector2(x0 + Mathf.Lerp(0.2f, 1f, Hash01(x0 * 2.1f + dd)) * amp, y1 - dd));
                    dd += Mathf.Lerp(0.9f, 1.8f, Hash01(x0 + dd * 0.7f));
                }
                ups.Reverse();
                left.AddRange(ups);
                left.Add(new Vector2(x0, y1 - 0.9f));
                pts.AddRange(left);
            }
            return pts.ToArray();
        }

        /// <summary>윗면을 등록하는 블록(발판).</summary>
        private static GameObject Ledge(Transform parent, string name, float x0, float x1, float y0, float y1, SpriteShape shape, Color tint,
            int order = 2, float edge = 0.5f) {
            Surf(x0, y1, x1, y1);
            return Block(parent, name, x0, x1, y0, y1, shape, tint, order, edge);
        }

        // ───────────────────────── 등반 ─────────────────────────
        /// <summary>
        /// 덩굴 벽(등반 가능 면). faceDir = 벽면이 향하는 방향(-1: 벽의 왼쪽 면에 붙음, +1: 오른쪽 면에 붙음).
        /// 판정은 ClimbWall 레이어 EdgeCollider2D(기존 맵과 동일), 비주얼은 덩굴 줄기 + 잎 → "여기는 붙을 수 있다"는 표시.
        /// </summary>
        private static GameObject VineWall(Transform parent, Random rnd, string name, float x, float y0, float y1, int faceDir) {
            var go = ClimbEdge(parent, new Vector2(x, y0), new Vector2(x, y1), name);
            VineArt(go.transform, rnd, x, y0, y1, faceDir, C(0.85f, 1f, 0.8f));
            return go;
        }

        private static List<SpriteRenderer> VineArt(Transform parent, Random rnd, float x, float y0, float y1, int faceDir, Color tint) {
            var list = new List<SpriteRenderer>();
            var leaf = Sps(OF + "old wall leafs.png", "old wall leafs", 1, 4, 7, 9);
            var liana = Sp(FF + "liana 2.png");
            if (liana != null) {
                var sc = 0.55f;
                var len = liana.bounds.size.x * sc;
                var n = Mathf.Max(1, Mathf.CeilToInt((y1 - y0) / len));
                for (var i = 0; i < n; i++) {
                    var yy = Mathf.Min(y0 + (i + 0.5f) * len, y1 - len * 0.3f);
                    var sr = Put(parent, liana, new Vector3(x + faceDir * 0.12f, yy, -0.3f), sc, "Architecture", 4, tint, i % 2 == 0, 90f);
                    if (sr != null) list.Add(sr);
                }
            }
            for (var y = y0 + 0.6f; y < y1; y += R(rnd, 1.4f, 2f)) {
                var sr = Put(parent, Pick(rnd, leaf), new Vector3(x + faceDir * R(rnd, 0.05f, 0.3f), y, -0.35f), R(rnd, 0.55f, 0.75f),
                    "Architecture", 6, tint, faceDir > 0);
                if (sr != null) list.Add(sr);
            }
            return list;
        }

        /// <summary>
        /// 공중에 매달린 덩굴(로프). 양면 등반 면(ClimbSurface)이라 누르는 방향으로 벽점프할 수 있다.
        /// → 덩굴에서 덩굴로 건너뛰는 구간.
        /// </summary>
        private static GameObject Liana(Transform parent, Random rnd, string name, float x, float yTop, float yBottom) {
            var go = ClimbEdge(parent, new Vector2(x, yBottom), new Vector2(x, yTop), name);
            go.AddComponent<ClimbSurface>();
            var shape = Load<SpriteShape>(FFShapes + "Liana.asset");
            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            if (shape != null) {
                var pts = new List<Vector2>();
                var n = Mathf.Max(3, Mathf.CeilToInt((yTop - yBottom) / 3f));
                for (var i = 0; i <= n; i++) {
                    var t = i / (float)n;
                    pts.Add(new Vector2(x + Mathf.Sin(t * 5.1f + x) * 0.18f, Mathf.Lerp(yTop + 0.5f, yBottom - 0.4f, t)));
                }
                Terrain(vis.transform, "LianaShape", pts.ToArray(), shape, "Architecture", 3, C(0.85f, 0.95f, 0.8f), false, 0.55f, -0.2f, closed: false);
            }
            var leaf = new[] { Sp(FF + "greens..png", "greens._3"), Sp(FF + "greens..png", "greens._23"), Sp(FF + "greens..png", "greens._6") };
            for (var y = yBottom + 0.5f; y < yTop; y += R(rnd, 2f, 3.2f))
                Put(vis.transform, Pick(rnd, leaf), new Vector3(x + R(rnd, -0.3f, 0.3f), y, -0.35f), R(rnd, 0.25f, 0.4f), "Architecture", 5,
                    C(0.8f, 0.95f, 0.8f), rnd.Next(2) == 0, R(rnd, -40f, 40f));
            Put(vis.transform, Sp(FF + "waterfall tree.png", "waterfall tree_15"), new Vector3(x, yBottom, -0.3f), 0.5f, "Architecture", 6, C(0.85f, 1f, 0.85f));
            Light(vis.transform, "LianaTip", new Vector3(x, yBottom + 0.5f, 0f), C(0.7f, 1f, 0.7f), 0.4f, 3f, 0.2f);
            return go;
        }

        /// <summary>붙잡으면 무너지는 마른 덩굴 벽 (CATestCrumbleClimb). 등반 대쉬로 빠르게 올라야 한다.</summary>
        private static GameObject CrumbleVine(Transform parent, Random rnd, string name, float x, float y0, float y1, int faceDir, float delay) {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var edgeGo = ClimbEdge(root.transform, new Vector2(x, y0), new Vector2(x, y1), "ClimbEdge");
            var vis = new GameObject("Visual").transform;
            vis.SetParent(root.transform, false);
            var srs = VineArt(vis, rnd, x, y0, y1, faceDir, C(1f, 0.78f, 0.5f));
            var debris = Burst(root.transform, "Debris", new Vector3(x + faceDir * 0.3f, (y0 + y1) * 0.5f, -0.4f), MatAlphaDot, C(0.6f, 0.5f, 0.35f), 40, 3f,
                0.3f, 1.4f, "Player", 15, 1.5f);
            var sh = debris.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(0.6f, y1 - y0, 0.2f);
            var cc = root.AddComponent<CATestCrumbleClimb>();
            Set(cc, "climbEdge", edgeGo.GetComponent<EdgeCollider2D>());
            Set(cc, "visualRoot", vis);
            Set(cc, "fadeTargets", srs.ConvertAll(s => (Object)s).ToArray());
            Set(cc, "crumbleDelay", delay);
            Set(cc, "debris", debris);
            return root;
        }

        // ───────────────────────── 장치 ─────────────────────────
        private static GameObject Crate(Transform parent, string name, float x, float groundY) {
            Clear(x - 4f, x + 4f);
            var go = Inst(PCrate, parent, new Vector3(x, groundY + 0.92f, 0f));
            if (go != null) go.name = name;
            return go;
        }

        private static CATestPressurePlate Plate(Transform parent, string name, float x, float groundY, bool playerCanPress) {
            Clear(x - 3.5f, x + 3.5f);
            var go = Inst(PPlate, parent, new Vector3(x, groundY, 0f));
            go.name = name;
            var p = go.GetComponent<CATestPressurePlate>();
            Set(p, "playerCanPress", playerCanPress);
            Set(p, "detectSize", new Vector2(3.6f, 1.4f));
            return p;
        }

        private static CATestLever Lever(Transform parent, string name, float x, float groundY, CATestLever.Mode mode, float duration = 5f) {
            Clear(x - 2f, x + 2f);
            var go = Inst(PLever, parent, new Vector3(x, groundY, 0.2f));
            go.name = name;
            var l = go.GetComponent<CATestLever>();
            Set(l, "mode", (int)mode);
            Set(l, "duration", duration);
            return l;
        }

        /// <summary>
        /// 통나무 말뚝 문. 닫힘 = 바닥(groundY)부터 height 높이로 막음, 열림 = openUp 만큼 위로 올라감.
        /// 문 위쪽은 반드시 바위/천장으로 막아서(점프로 넘지 못하게) 배치할 것.
        /// </summary>
        private static CATestGate StakeGate(Transform parent, string name, float x, float groundY, float height, MonoBehaviour[] sources,
            float openUp, float closeSpeed = 4f, bool requireAll = false) {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x, groundY, 0f);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = new GameObject("Solid") { layer = LayerGround };
            col.transform.SetParent(root.transform, false);
            var b = col.AddComponent<BoxCollider2D>();
            b.size = new Vector2(1.3f, height);
            b.offset = new Vector2(0f, height * 0.5f);
            var logs = Sps(DG + "dungeon items 2.png", "dungeon items 2", 19, 24);
            for (var i = 0; i < 3; i++) {
                var s = logs[i % logs.Length];
                var sr = Put(root.transform, s, root.transform.position + new Vector3(-0.42f + i * 0.42f, height * 0.5f, 0.05f * i), 1f, "Ground", 26 + i,
                    C(0.8f, 0.72f, 0.62f), i == 1, 90f);
                if (sr != null) sr.transform.localScale = new Vector3(height / s.bounds.size.x, 0.85f, 1f);
            }
            var rope = Sp(DG + "dungeon items 2.png", "dungeon items 2_9");
            Put(root.transform, rope, root.transform.position + new Vector3(0, height * 0.75f, -0.1f), 0.8f, "Ground", 30, C(0.7f, 0.6f, 0.5f));
            Put(root.transform, rope, root.transform.position + new Vector3(0, height * 0.25f, -0.1f), 0.8f, "Ground", 30, C(0.7f, 0.6f, 0.5f));
            var dust = Burst(root.transform, "Dust", root.transform.position + new Vector3(0, 0.2f, -0.4f), MatAlphaDot, C(0.6f, 0.55f, 0.45f, 0.7f), 0, 1.5f,
                0.3f, 1f, "Player", 14, 0.2f);
            var dm = dust.main;
            dm.loop = true;
            var em = dust.emission;
            em.rateOverTime = 25f;
            var gate = root.AddComponent<CATestGate>();
            Set(gate, "sources", sources);
            Set(gate, "openOffset", new Vector2(0f, openUp));
            Set(gate, "closeSpeed", closeSpeed);
            Set(gate, "openSpeed", 7f);
            Set(gate, "requireAll", requireAll);
            Set(gate, "moveDust", dust);
            return gate;
        }

        /// <summary>도개교(통나무 다리): pivot 을 축으로 세워져 있다가(90°) 신호가 켜지면 수평(0°)으로 내려온다.</summary>
        private static CATestGate DrawBridge(Transform parent, string name, Vector2 pivot, float length, MonoBehaviour[] sources) {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(pivot.x, pivot.y, 0f);
            root.transform.rotation = Quaternion.Euler(0, 0, 90f);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = new GameObject("Solid") { layer = LayerGround };
            col.transform.SetParent(root.transform, false);
            var b = col.AddComponent<BoxCollider2D>();
            b.size = new Vector2(length, 0.8f);
            b.offset = new Vector2(length * 0.5f, -0.4f);
            var log = Sp(DG + "dungeon items 2.png", "dungeon items 2_24");
            var sr = Put(root.transform, log, root.transform.position, 1f, "Ground", 28, C(0.9f, 0.85f, 0.75f));
            if (sr != null) {
                sr.transform.localPosition = new Vector3(length * 0.5f, -0.4f, 0f);
                sr.transform.localRotation = Quaternion.identity;
                sr.transform.localScale = new Vector3(length / log.bounds.size.x, 1.25f, 1f);
            }
            var moss = new[] { Sp(FF + "greens..png", "greens._16"), Sp(FF + "greens..png", "greens._12") };
            var i = 0;
            for (var x = 1.5f; x < length - 1f; x += 4f, i++) {
                var m = Put(root.transform, moss[i % 2], root.transform.position, 0.5f, "Ground", 29, Color.white);
                if (m == null) continue;
                m.transform.localPosition = new Vector3(x, 0.05f, -0.05f);
                m.transform.localRotation = Quaternion.identity;
            }
            var gate = root.AddComponent<CATestGate>();
            Set(gate, "sources", sources);
            Set(gate, "openOffset", Vector2.zero);
            Set(gate, "openAngle", -90f);
            Set(gate, "openSpeed", 30f);
            Set(gate, "closeSpeed", 30f);
            return gate;
        }

        private static CATestGustZone Gust(Transform parent, string name, Rect area, Vector2 push, float calm, float blow, float offset = 0f) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<CATestGustZone>();
            g.area = area;
            Set(g, "push", push);
            Set(g, "calmTime", calm);
            Set(g, "blowTime", blow);
            Set(g, "phaseOffset", offset);
            var dir = Mathf.Sign(push.x);
            // 예고: 잎이 조금 날림 / 돌풍: 굵은 바람 줄기
            var warn = Motes(go.transform, "WarnLeaves", area, -0.6f, MatAlphaDot, C(0.75f, 0.9f, 0.6f, 0.8f), C(1f, 0.95f, 0.7f, 0.6f),
                14f, 0.06f, 0.14f, new Vector2(dir * 6f, 0.4f), 0.6f, 1.6f, "Player", 45);
            var wm = warn.main;
            wm.prewarm = false;
            wm.playOnAwake = false;
            warn.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var gust = Particles(go.transform, "GustStreaks", new Vector3(area.center.x, area.center.y, -0.8f), MatStreak, "Architecture", 72);
            var m = gust.main;
            m.playOnAwake = false;
            m.loop = true;
            m.startLifetime = 0.5f;
            m.startSpeed = 0f;
            m.startSize3D = true;
            m.startSizeX = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            m.startSizeY = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            m.startSizeZ = 1f;
            m.startColor = new ParticleSystem.MinMaxGradient(C(1f, 1f, 1f, 0.35f), C(0.9f, 1f, 0.9f, 0.18f));
            var em = gust.emission;
            em.rateOverTime = 70f;
            var sh = gust.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(area.width, area.height, 2f);
            var v = gust.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(dir * 30f, dir * 45f);
            v.y = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            v.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            gust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Set(g, "warnParticles", warn);
            Set(g, "gustParticles", gust);
            return g;
        }

        private static CATestFallingHazard FallingRock(Transform parent, string name, float x, float yTop, float groundY, Rect trigger, Sprite sprite,
            float scale, Color tint, float rot = 0f) {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var hz = new GameObject("Hazard").transform;
            hz.SetParent(root.transform, false);
            hz.position = new Vector3(x, yTop, -0.1f);
            Put(hz, sprite, hz.position, scale, "Player", 12, tint, false, rot);
            var dust = Burst(root.transform, "WarnDust", new Vector3(x, yTop + 0.8f, -0.3f), MatAlphaDot, C(0.6f, 0.55f, 0.45f, 0.8f), 14, 1f, 0.25f, 1f,
                "Player", 13, 1f);
            var shatter = Burst(root.transform, "Shatter", new Vector3(x, groundY, -0.3f), MatAlphaDot, C(0.55f, 0.5f, 0.42f), 30, 7f, 0.35f, 1f,
                "Player", 13, 1.8f);
            var fh = root.AddComponent<CATestFallingHazard>();
            fh.triggerArea = trigger;
            Set(fh, "hazard", hz);
            Set(fh, "groundY", groundY + 0.4f);
            Set(fh, "warnDust", dust);
            Set(fh, "shatter", shatter);
            return fh;
        }

        private static void SeedPatrol(Transform parent, string name, float x, float y, float distance, float speed) {
            var go = Inst(PThornSeed, parent, new Vector3(x, y, 0f));
            if (go == null) return;
            go.name = name;
            var e = go.GetComponent<ElectronicEnemy>();
            Set(e, "moveDistance", distance);
            Set(e, "moveSpeed", speed);
        }

        private static void AreaTitle(Transform parent, Rect area, string title, string subtitle = "") {
            var go = new GameObject("Area_" + title);
            go.transform.SetParent(parent, false);
            var z = go.AddComponent<CATestAreaZone>();
            z.area = area;
            z.title = title;
            z.subtitle = subtitle;
        }

        private static void Hint(Transform parent, Rect area, string message, bool once = false) {
            var go = new GameObject("Hint");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<CATestHintZone>();
            h.area = area;
            h.message = message;
            h.onlyOnce = once;
        }

        /// <summary>기존 Fog 함정이 쓰는 화면 안개 이미지(Fog 스크립트가 같은 씬 안의 Image 를 참조해야 해서 맵 씬에 둔다).</summary>
        private static Image FogCanvas(Transform parent) {
            var go = new GameObject("FogTrapCanvas", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 300;
            var imgGo = new GameObject("FogImage", typeof(RectTransform));
            imgGo.transform.SetParent(go.transform, false);
            var img = imgGo.AddComponent<Image>();
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.color = new Color(0.78f, 0.82f, 0.86f, 0f);
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// 기존 Fog 함정(02. Script/03_TrapAndEnemy/Traps/Fog.cs) 배치: 짙은 안개에 들어가면 화면이 하얗게 덮이고 back 지점으로 되돌아간다.
        /// → 안개 골짜기의 "잘못된 길 / 빠지면 길을 잃는 곳".
        /// </summary>
        private static void FogTrap(Transform parent, string name, Rect area, Vector2 back, Image fogImage) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(area.center.x, area.center.y, 0f);
            var b = go.AddComponent<BoxCollider2D>();
            b.isTrigger = true;
            b.size = area.size;
            var tp = new GameObject("ReturnPoint").transform;
            tp.SetParent(go.transform, false);
            tp.position = new Vector3(back.x, back.y, 0f);
            var fog = go.AddComponent<Fog>();
            Set(fog, "fogImage", fogImage);
            Set(fog, "teleport", tp);
            Set(fog, "durationIn", 0.9f);
            Set(fog, "durationOut", 1.2f);
            var fogSp = Sp(ArtDir + "/CATest_FogBand.png");
            var k = 0;
            for (var x = area.xMin; x < area.xMax; x += 6f, k++)
                Put(go.transform, fogSp, new Vector3(x + 3f, area.yMin + area.height * 0.55f, k % 2 == 0 ? -1.5f : 1.5f), 1.4f, "Architecture", 20,
                    C(0.82f, 0.86f, 0.9f, 0.6f), k % 2 == 0, mat: MatAlpha);
        }

        private static void PitTransition(Transform parent, Rect area, Vector2 arrival) {
            var go = new GameObject("PitTransition_ToDeepSea");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(area.center.x, area.center.y, 0f);
            var b = go.AddComponent<BoxCollider2D>();
            b.isTrigger = true;
            b.size = area.size;
            var t = go.AddComponent<CATestPitTransition>();
            Set(t, "arrivalPosition", arrival);
        }

        // ───────────────────────── 식생 / 장식 ─────────────────────────
        /// <summary>큰 잎 뭉치를 영역 안에 여러 깊이로 겹쳐 "숲 지붕"을 만든다.</summary>
        private static void Canopy(Transform parent, Random rnd, Sprite[] leaves, float x0, float x1, float yMin, float yMax, float zMin, float zMax,
            float scMin, float scMax, float gapMin, float gapMax, Color a, Color b, string layer, int order, bool sway = true) {
            if (leaves == null || leaves.Length == 0) return;
            var x = x0 + R(rnd, 0f, gapMin);
            while (x < x1) {
                var z = R(rnd, zMin, zMax);
                var sr = Put(parent, Pick(rnd, leaves), new Vector3(x, R(rnd, yMin, yMax), z), R(rnd, scMin, scMax), layer,
                    order - Mathf.RoundToInt(z * 0.3f), Color.Lerp(a, b, (float)rnd.NextDouble()), rnd.Next(2) == 0, R(rnd, -8f, 8f));
                if (sway && sr != null) {
                    var am = sr.gameObject.AddComponent<CATestAmbientMotion>();
                    am.swayAngle = R(rnd, 0.6f, 1.6f);
                    am.swaySpeed = R(rnd, 0.25f, 0.5f);
                }
                x += R(rnd, gapMin, gapMax);
            }
        }

        /// <summary>줄기 + 잎 뭉치를 합친 나무 (초록 숲 팩의 잎 없는 나무 위에 큰 잎 뭉치를 얹는다).</summary>
        private static void LeafyTree(Transform parent, Random rnd, float x, float groundY, float z, float scale, Color trunkTint, Color leafTint,
            string layer, int order) {
            var trunks = Sps(FF + "new trees.png", "new trees", 0, 1, 2, 3, 4);
            var leaves = Sps(FF + "new tree leafs.png", "new tree leafs", 0, 3, 4, 6, 8, 9, 10, 12);
            if (trunks.Length == 0) return;
            var s = Pick(rnd, trunks);
            var t = Put(parent, s, new Vector3(x, groundY - 0.4f - s.bounds.min.y * scale, z), scale, layer, order, trunkTint, rnd.Next(2) == 0);
            if (t == null || leaves.Length == 0) return;
            var h = s.bounds.size.y * scale;
            var w = s.bounds.size.x * scale;
            var top = groundY + h;
            var n = rnd.Next(4, 7);
            for (var i = 0; i < n; i++) {
                var p = new Vector3(x + R(rnd, -0.5f, 0.5f) * w, top - R(rnd, 0.0f, 0.35f) * h, z - 0.05f - i * 0.02f);
                var lt = leafTint * R(rnd, 0.85f, 1.05f);
                lt.a = 1f;
                var sr = Put(parent, Pick(rnd, leaves), p, scale * R(rnd, 1.2f, 1.8f), layer, order + 1 + i, lt, rnd.Next(2) == 0, R(rnd, -10f, 10f));
                if (sr == null) continue;
                var am = sr.gameObject.AddComponent<CATestAmbientMotion>();
                am.swayAngle = R(rnd, 0.8f, 1.8f);
                am.swaySpeed = R(rnd, 0.3f, 0.6f);
            }
        }

        /// <summary>등록된 윗면을 따라 식생을 흩뿌린다 (skip(x) = true 면 건너뜀: 함정/구멍 위).</summary>
        private static void ScatterSurf(Transform parent, Random rnd, Sprite[] sprites, float x0, float x1, float gapMin, float gapMax, float scMin,
            float scMax, float zMin, float zMax, string layer, int order, Color a, Color b, float sink = 0.1f, System.Func<float, bool> skip = null,
            float yBelow = 9999f) {
            if (sprites == null || sprites.Length == 0) return;
            var x = x0 + R(rnd, 0f, gapMin);
            while (x < x1) {
                var y = SurfAt(x, yBelow);
                if (!float.IsNaN(y) && (skip == null || !skip(x))) {
                    var s = Pick(rnd, sprites);
                    var sc = R(rnd, scMin, scMax);
                    Put(parent, s, new Vector3(x, y - sink - s.bounds.min.y * sc, R(rnd, zMin, zMax)), sc, layer, order + rnd.Next(3),
                        Color.Lerp(a, b, (float)rnd.NextDouble()), rnd.Next(2) == 0);
                }
                x += R(rnd, gapMin, gapMax);
            }
        }

        /// <summary>윗면을 따라 풀 띠(열린 스프라인, 판정 없음, 플레이어 앞 레이어)를 깐다.</summary>
        private static void GrassOn(Transform parent, SpriteShape grass, Vector2[] line, Color tint, float lift = 0.12f) {
            if (grass == null || line.Length < 2) return;
            var pts = new List<Vector2>();
            for (var i = 0; i < line.Length - 1; i++) {
                var n = Mathf.Max(1, Mathf.CeilToInt((line[i + 1].x - line[i].x) / 3f));
                for (var k = 0; k < n; k++) pts.Add(Vector2.Lerp(line[i], line[i + 1], k / (float)n) + Vector2.up * lift);
            }
            pts.Add(line[^1] + Vector2.up * lift);
            // Ground 레이어 20: 지형(1~3) 앞, 상자(25)·압력판(22) 뒤. (예전엔 Architecture 레이어라 플레이어/상자보다 앞에 그려져
            // 바닥에 내려놓은 상자가 풀에 거의 다 가려져 "사라진 것처럼" 보였다.)
            Terrain(parent, "Grass_" + line[0].x, pts.ToArray(), grass, "Ground", 20, tint, false, 0.4f, -0.05f, closed: false);
        }

        private static void ThornArt(Transform trap, float width, Random rnd, bool sea) {
            var needles = sea ? Sps(UW + "Spikes.png", "Spikes", 0, 1, 2) : Sps(OF + "Old Needle SM.png", "Old Needle SM", 0, 1, 2);
            var n = Mathf.CeilToInt(width / 0.7f);
            for (var i = 0; i < n; i++) {
                var x = -width * 0.5f + (i + 0.5f) * (width / n) + R(rnd, -0.2f, 0.2f);
                var s = Pick(rnd, needles);
                var sc = R(rnd, sea ? 0.5f : 0.75f, sea ? 0.75f : 1.05f);
                Put(trap, s, trap.position + new Vector3(x, -0.7f - s.bounds.min.y * sc, R(rnd, -0.3f, 0.3f)), sc, "Player", 5 + rnd.Next(3),
                    sea ? C(0.55f, 0.75f, 0.8f) : C(0.75f, 0.7f, 0.65f), rnd.Next(2) == 0, R(rnd, -8f, 8f));
            }
        }

        /// <summary>가시 바닥 함정(UrchinTrap 재사용): 중심 x, 바닥 y, 폭. lush = 초록 숲 팩 가시 덤불 모양.</summary>
        private static void Thorns(Transform parent, Random rnd, string name, float cx, float floorY, float width, bool lush) {
            var t = Inst(PThornsForest, parent, new Vector3(cx, floorY + 0.7f, 0f));
            if (t == null) return;
            t.name = name;
            if (lush) {
                var art = Sps(FF + "danger.png", "danger", 4, 5);
                for (var x = -width * 0.5f + 1.8f; x < width * 0.5f; x += R(rnd, 2.4f, 3.2f)) {
                    var s = Pick(rnd, art);
                    var sc = R(rnd, 0.6f, 0.8f);
                    Put(t.transform, s, t.transform.position + new Vector3(x, -0.7f - s.bounds.min.y * sc, R(rnd, -0.3f, 0.3f)), sc, "Player", 6,
                        C(0.85f, 0.85f, 0.8f), rnd.Next(2) == 0);
                }
            }
            else ThornArt(t.transform, width, rnd, false);
            SizeBox(t, width, 1.4f);
        }

        private static void StoneCircle(Transform parent, Random rnd, float x0, float x1, float y, Sprite[] tall, Sprite[] shortOnes, Color fog) {
            // 불규칙한 폐허: 간격/깊이/기울기/가라앉음/부서짐을 모두 흔들고, 기둥 밑동은 실제 지면 높이에 맞춘다.
            var lintel = Sp(OF + "Old stounes.png", "Old stounes_6");
            var x = x0 + R(rnd, 0f, 3f);
            while (x < x1) {
                if (rnd.NextDouble() < 0.25) { x += R(rnd, 3f, 7f); continue; } // 비어 있는 구간
                var z = R(rnd, 1.5f, 9f);
                var sc = R(rnd, 0.9f, 1.45f) * (z > 5 ? 1.2f : 1f);
                var broken = rnd.Next(3) == 0;
                var s = broken ? Pick(rnd, shortOnes) : Pick(rnd, tall);
                var gy = SurfAt(x, y + 6f);
                if (float.IsNaN(gy)) gy = y;
                var sink = R(rnd, 0.3f, 1.4f);
                var tint = Toward(C(0.8f, 0.74f, 0.68f), fog, Mathf.InverseLerp(1.5f, 9f, z) * 0.35f);
                Put(parent, s, new Vector3(x, gy - sink - s.bounds.min.y * sc, z), sc, "Default", 5 - (int)z, tint, rnd.Next(2) == 0, R(rnd, -7f, 7f));
                if (!broken && rnd.Next(4) == 0) {
                    var x2 = x + 2.6f * sc;
                    var s2 = Pick(rnd, tall);
                    var gy2 = SurfAt(x2, y + 6f);
                    if (float.IsNaN(gy2)) gy2 = gy;
                    Put(parent, s2, new Vector3(x2, gy2 - sink - s2.bounds.min.y * sc, z), sc, "Default", 5 - (int)z, tint, rnd.Next(2) == 0, R(rnd, -3f, 3f));
                    var top = Mathf.Min(gy, gy2) - sink + s.bounds.size.y * sc;
                    Put(parent, lintel, new Vector3((x + x2) * 0.5f, top + 0.2f, z - 0.05f), sc * 0.85f, "Default", 6 - (int)z, tint, false, R(rnd, -8f, 8f));
                    x = x2;
                }
                x += R(rnd, 3f, 11f);
            }
        }

        private static void Foreground(Transform parent, Random rnd, Sprite[] sprites, float x0, float x1, float gapMin, float gapMax,
            System.Func<float, float> yAt, float scMin, float scMax, float zNear, float zFar, Color tint, bool hanging) {
            var x = x0;
            while (x < x1) {
                var y = yAt(x);
                if (!float.IsNaN(y)) {
                    var s = Pick(rnd, sprites);
                    var z = R(rnd, zFar, zNear);
                    Put(parent, s, new Vector3(x, y + R(rnd, -1.5f, 1.5f), z), R(rnd, scMin, scMax), "Architecture", 50 + (int)(-z), tint, rnd.Next(2) == 0,
                        hanging ? 180f + R(rnd, -8, 8) : R(rnd, -5, 5));
                }
                x += R(rnd, gapMin, gapMax);
            }
        }

        private static void FallZone(Transform parent, Rect r, float maxFall = 24f) {
            var go = new GameObject("FallSequence");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(r.center.x, r.center.y, 0);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = r.size;
            var fs = go.AddComponent<CATestFallSequence>();
            Set(fs, "maxFallSpeed", maxFall);
            var lines = Particles(go.transform, "SpeedLines", go.transform.position, MatStreak, "Architecture", 70);
            var m = lines.main;
            m.loop = true; m.playOnAwake = false;
            m.startLifetime = 0.6f; m.startSpeed = 0f; m.startSize3D = true;
            m.startSizeX = new ParticleSystem.MinMaxCurve(0.08f, 0.2f); m.startSizeY = new ParticleSystem.MinMaxCurve(2.5f, 5f); m.startSizeZ = 1f;
            m.startColor = new ParticleSystem.MinMaxGradient(C(0.75f, 0.85f, 1f, 0.35f), C(1f, 1f, 1f, 0.15f));
            var em = lines.emission; em.rateOverTime = 45;
            var sh = lines.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(26f, 4f, 6f);
            var v = lines.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.y = new ParticleSystem.MinMaxCurve(30f, 45f);
            v.x = new ParticleSystem.MinMaxCurve(0f, 0f); v.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var debris = Particles(go.transform, "FallingDebris", go.transform.position, MatAlphaDot, "Architecture", 69);
            var dm = debris.main;
            dm.loop = true; dm.playOnAwake = false; dm.startLifetime = 1.2f; dm.startSpeed = 0f;
            dm.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f); dm.startColor = C(0.2f, 0.2f, 0.25f, 0.8f);
            var dem = debris.emission; dem.rateOverTime = 14;
            var dsh = debris.shape; dsh.shapeType = ParticleSystemShapeType.Box; dsh.scale = new Vector3(20f, 3f, 14f);
            var dv = debris.velocityOverLifetime; dv.enabled = true; dv.space = ParticleSystemSimulationSpace.World; dv.y = new ParticleSystem.MinMaxCurve(12f, 20f);
            dv.x = new ParticleSystem.MinMaxCurve(0f, 0f); dv.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            Set(fs, "speedLines", lines);
            Set(fs, "debris", debris);
        }

        /// <summary>빛줄기(가산 스프라이트) + 바닥에 떨어지는 밝은 빛 웅덩이(포인트 라이트) → 명암 대비를 만든다.</summary>
        /// <summary>
        /// 햇살 한 줄기 = God Ray 셰이더 빛기둥(뒤) + 옅은 빛기둥(앞) + 바닥의 빛 웅덩이(Light2D).
        /// - 뒤 기둥: "Default" 레이어 → 배경/나무 앞, 지형·플레이어 뒤. 주된 빛줄기.
        /// - 앞 기둥: "Player" 레이어 맨 앞, 세기 25% → 플레이어 위로도 빛이 살짝 걸려 공기감이 생김.
        ///   (퍼즐 물체 주변(KeepClear)에는 앞 기둥을 두지 않아 상자·판이 하얗게 날아가지 않게 함)
        /// - 모든 햇살을 같은 방향(해가 왼쪽 위)으로 약 12° 기울이고 ±3° 만 흔든다 → 해가 한 곳에 있는 것처럼 보임.
        /// shaft 스프라이트 인자는 이전 방식(그림 한 장)과의 호환을 위해 남겨 두었고 더 이상 쓰지 않는다.
        /// </summary>
        private static void SunShaft(Transform parent, Random rnd, Sprite shaft, float x, float groundY, float height, Color c, float lightIntensity) {
            var tilt = 12f + R(rnd, -3f, 3f);
            var seed = R(rnd, 0f, 100f);
            GodRay(parent, "GodRay_Back", x, groundY, height * 1.25f, R(rnd, 6f, 10f), tilt, c, c.a * 2.3f, seed, "Default", 42, R(rnd, 1f, 4f),
                R(rnd, 0.25f, 0.42f), R(rnd, 5f, 9f), 0.7f);
            if (!InClear(x))
                GodRay(parent, "GodRay_Front", x + R(rnd, -1f, 1f), groundY, height * 1.1f, R(rnd, 4f, 7f), tilt, c, c.a * 0.6f, seed + 31f, "Player", 100, -1f,
                    R(rnd, 0.3f, 0.45f), R(rnd, 3f, 6f), 1.2f);
            Light(parent, "SunPool", new Vector3(x, groundY + 1.5f, 0f), new Color(c.r, c.g, c.b), lightIntensity, 8f, 2f, 0.5f);
        }

        /// <summary>God Ray 셰이더를 쓰는 빛기둥 하나. 바닥 중심이 (x, groundY-1) 에 오도록 기울기를 고려해 배치.</summary>
        private static CATestGodRay GodRay(Transform parent, string name, float x, float groundY, float length, float width, float tiltDeg, Color c,
            float intensity, float seed, string layer, int order, float z, float topWidth, float rayCount, float dust) {
            var sprite = WhiteSprite;
            if (sprite == null || MatGodRay == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            // 스프라이트를 θ 만큼 반시계 회전하면 (0,-L/2) 벡터는 (L/2·sinθ, -L/2·cosθ) 가 된다 → 중심 = 바닥점 - 그 벡터
            var th = tiltDeg * Mathf.Deg2Rad;
            var bottom = new Vector2(x, groundY - 1f);
            var center = bottom - new Vector2(length * 0.5f * Mathf.Sin(th), -length * 0.5f * Mathf.Cos(th));
            go.transform.position = new Vector3(center.x, center.y, z);
            go.transform.rotation = Quaternion.Euler(0f, 0f, tiltDeg);
            var size = sprite.bounds.size;
            go.transform.localScale = new Vector3(width / size.x, length / size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = MatGodRay;
            sr.sortingLayerID = SL(layer);
            sr.sortingOrder = order;
            sr.color = new Color(c.r, c.g, c.b, 1f);
            var gr = go.AddComponent<CATestGodRay>();
            gr.seed = seed;
            gr.intensity = intensity;
            gr.topWidth = topWidth;
            gr.rayCount = rayCount;
            gr.raySpeed = 0.04f + Hash01(seed) * 0.05f;
            gr.dustAmount = dust;
            gr.flicker = 0.15f;
            gr.core = 0.45f;
            gr.Apply();
            return gr;
        }

        // ───────────────────────── 나무 심기 / 전경 실루엣 ─────────────────────────

        /// <summary>x 에서 가장 "낮은" 등록 윗면(= 진짜 바닥). 위쪽 턱·상자 윗면 대신 바닥을 기준으로 삼기 위함.</summary>
        private static float SurfLow(float x, float yBelow = 9999f) {
            var best = float.NaN;
            foreach (var line in Surfaces) {
                if (x < line[0].x || x > line[^1].x) continue;
                var y = SurfaceY(line, x);
                if (y > yBelow) continue;
                if (float.IsNaN(best) || y < best) best = y;
            }
            return best;
        }

        /// <summary>
        /// 나무를 심을 땅 높이. 나무 폭(±halfW) 안의 5곳을 재서
        ///  - 한 곳이라도 땅이 없으면(구덩이/강) 심지 않음
        ///  - 높낮이 차가 maxSpread 보다 크면(좁은 턱 위, 절벽 가장자리) 심지 않음
        ///  - 통과하면 가장 낮은 높이를 돌려줌 → 줄기 밑동이 땅 위로 떠 보이지 않는다
        /// </summary>
        private static bool TreeGround(float x, float halfW, float yBelow, out float gy, float maxSpread = 3f) {
            gy = float.NaN;
            float lo = float.MaxValue, hi = float.MinValue;
            for (var i = 0; i < 5; i++) {
                var y = SurfAt(x - halfW + halfW * 0.5f * i, yBelow);
                if (float.IsNaN(y)) return false;
                lo = Mathf.Min(lo, y);
                hi = Mathf.Max(hi, y);
            }
            if (hi - lo > maxSpread) return false;
            gy = lo;
            return true;
        }

        /// <summary>
        /// 뒤(z&gt;0)에 있는 나무를 얼마나 땅속으로 묻을지.
        /// 원근 카메라에서 z 가 클수록 물체가 화면 중심 쪽으로 모인다. 카메라가 땅보다 위에 있으면
        /// 멀리 있는 나무의 밑동은 앞쪽 땅 윤곽보다 "위로" 보이게 되어 공중에 뜬 것처럼 보인다.
        /// → z 에 비례해 밑동을 더 깊이 묻는다(묻힌 부분은 앞쪽 지형(Ground 레이어)이 가린다).
        /// </summary>
        private static float ParallaxSink(float z) => z > 0f ? 0.8f + 0.12f * z : 0.4f;

        /// <summary>
        /// 카메라 쪽(z&lt;0)의 검은 실루엣: 땅 띠 + 큰 나무.
        ///  ■ 땅 띠: 실제 바닥(SurfLow)보다 groundDrop 아래를 따라가는 윤곽을 SpriteShape(콜라이더 없음)로 만든다.
        ///    SpriteShape 의 풀 가장자리가 검게 칠해져 자연스러운 수풀 윤곽이 된다. z=bandZ 로 카메라에 가까워
        ///    플레이 평면보다 빠르게 움직인다(시차) → 입체감.
        ///    땅이 없는 구간(구덩이)이 6m 이상이면 띠를 끊는다.
        ///  ■ 나무: 줄기 밑동을 띠 속에 묻고(바닥 - groundDrop - 1) 위로 화면 밖까지 뻗게. 잎 뭉치는 줄기 끝에만.
        ///    퍼즐 구간(KeepClear) ±8m 안에는 두지 않는다(상자·레버를 가리지 않게).
        /// </summary>
        private static void FgSilhouettes(Transform parent, Random rnd, float x0, float x1, float yBelow, SpriteShape bandShape,
            Sprite[] trunks, Sprite[] leaves, Color tint, float treeGapMin = 40f, float treeGapMax = 70f,
            float groundDrop = 3.2f, float bandZ = -12f, float treeScaleMin = 2.2f, float treeScaleMax = 3f) {
            // 땅 띠
            var seg = new List<Vector2>();
            var miss = 0f;
            var lastY = float.NaN;
            var segIdx = 0;
            void FlushBand() {
                if (seg.Count >= 3 && bandShape != null) {
                    var pts = new List<Vector2>(seg) { new(seg[^1].x, lastY - 60f), new(seg[0].x, lastY - 60f) };
                    // 아랫변은 가장 낮은 점보다 충분히 아래
                    var minY = float.MaxValue;
                    foreach (var q in seg) minY = Mathf.Min(minY, q.y);
                    pts[^2] = new Vector2(seg[^1].x, minY - 50f);
                    pts[^1] = new Vector2(seg[0].x, minY - 50f);
                    var go = Terrain(parent, "FgBand_" + segIdx++, pts.ToArray(), bandShape, "Architecture", 40, tint, false, 1.2f, bandZ);
                    if (go != null) go.transform.localPosition = new Vector3(0f, 0f, bandZ);
                }
                seg.Clear();
            }
            // 윤곽 기준 높이 = 주변 ±12m 안에서 가장 낮은 바닥. 높은 턱·다리만 등록된 구간에서
            // 띠가 턱 높이까지 솟아 플레이 화면을 가리는 것을 막는다.
            float WindowLow(float cx) {
                var m = float.NaN;
                for (var dx = -12f; dx <= 12f; dx += 3f) {
                    var v = SurfLow(cx + dx, yBelow);
                    if (!float.IsNaN(v) && (float.IsNaN(m) || v < m)) m = v;
                }
                return m;
            }
            for (var x = x0; x <= x1; x += 3f) {
                var y = float.IsNaN(SurfLow(x, yBelow)) ? float.NaN : WindowLow(x);
                if (float.IsNaN(y)) {
                    miss += 3f;
                    if (miss >= 6f) FlushBand();
                    continue;
                }
                miss = 0f;
                lastY = y;
                // 완만한 큰 굴곡(언덕) + 잔굴곡. 가끔 언덕이 바닥 근처까지 솟아 수풀 덩어리처럼 보인다.
                var top = y - groundDrop + (Mathf.PerlinNoise(x * 0.035f, 3.1f) - 0.5f) * 5f + (Mathf.PerlinNoise(x * 0.19f, 7.7f) - 0.5f) * 1.4f;
                if (seg.Count > 0 && Mathf.Abs(top - seg[^1].y) > 4f) top = seg[^1].y + Mathf.Sign(top - seg[^1].y) * 4f;
                seg.Add(new Vector2(x, top));
                if (seg.Count >= 40) { var keep = seg[^1]; FlushBand(); seg.Add(keep); }
            }
            FlushBand();

            // 나무
            if (trunks == null || trunks.Length == 0) return;
            for (var x = x0 + R(rnd, 0f, treeGapMin); x < x1; x += R(rnd, treeGapMin, treeGapMax)) {
                var blocked = false;
                for (var dx = -8f; dx <= 8f; dx += 2f) if (InClear(x + dx)) { blocked = true; break; }
                if (blocked) continue;
                var gy = SurfLow(x, yBelow);
                if (float.IsNaN(gy)) continue;
                var z = R(rnd, -22f, -15f);
                var sc = R(rnd, treeScaleMin, treeScaleMax);
                var s = Pick(rnd, trunks);
                var baseY = gy - groundDrop - 3f;
                var order = 50 + Mathf.RoundToInt(-z);
                // 나무 하나 = 부모(FgTree) + 줄기(첫 자식) + 잎 뭉치들. 부모의 CATestFgOccluder 가
                // 줄기가 플레이어를 가릴 때 나무 전체를 반투명하게 만든다.
                var treeRoot = new GameObject("FgTree").transform;
                treeRoot.SetParent(parent, false);
                Put(treeRoot, s, new Vector3(x, baseY - s.bounds.min.y * sc, z), sc, "Architecture", order, tint, rnd.Next(2) == 0, 0f, "Trunk");
                treeRoot.gameObject.AddComponent<CATestFgOccluder>();
                if (leaves == null || leaves.Length == 0) continue;
                var h = s.bounds.size.y * sc;
                var w = s.bounds.size.x * sc;
                var n = rnd.Next(5, 8);
                for (var i = 0; i < n; i++) {
                    var p = new Vector3(x + R(rnd, -0.5f, 0.5f) * w, baseY + h - R(rnd, 0.05f, 0.35f) * h, z - 0.05f - i * 0.02f);
                    var sr = Put(treeRoot, Pick(rnd, leaves), p, sc * R(rnd, 1.2f, 1.8f), "Architecture", order + 1 + i, tint, rnd.Next(2) == 0, R(rnd, -10f, 10f), "Leaves");
                    if (sr == null) continue;
                    var am = sr.gameObject.AddComponent<CATestAmbientMotion>();
                    am.swayAngle = R(rnd, 0.5f, 1.2f);
                    am.swaySpeed = R(rnd, 0.25f, 0.45f);
                }
            }
        }
    }
}
#endif
