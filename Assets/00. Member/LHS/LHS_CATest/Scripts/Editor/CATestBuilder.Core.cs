#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// LHS_CATest 맵 빌더. 메뉴 한 번으로 CoreScene / 숲 / 심해 씬과 필요한 프리팹·머티리얼·볼륨을 "처음부터 다시" 생성한다.
    /// 코드로 만드는 이유: 레벨 수치(점프 거리, 높이)를 한 곳에서 고치고 몇 초 만에 다시 만들어 볼 수 있게 하기 위해서.
    /// 생성된 씬은 일반 씬이므로 빌드 후 손으로 자유롭게 다듬어도 된다. (단, 다시 Build하면 덮어써짐)
    /// </summary>
    public static partial class CATestBuilder {
        public const string Root = "Assets/00. Member/LHS/LHS_CATest";
        public const string ScenesDir = Root + "/Scenes";
        public const string PrefabDir = Root + "/Prefabs";
        public const string MatDir = Root + "/Materials";
        public const string ArtDir = Root + "/Art";
        public const string SettingsDir = Root + "/Settings";
        public const string FSB = "Assets/FantasySpriteBundle";
        public const string OF = FSB + "/Old Forest pack/Sprites/";
        public const string UW = FSB + "/Underwater area pack/Sprites/";
        public const string DG = FSB + "/Dungeon pack/Sprites/";
        public const string FF = FSB + "/Forest sprite pack/Sprites/";

        public const string CoreScenePath = ScenesDir + "/CATest_CoreScene.unity";
        public const string ForestScenePath = ScenesDir + "/CATest_Forest.unity"; // (구버전, 빌드 시 삭제)
        public const string Forest0ScenePath = ScenesDir + "/CATest_Forest0_Sunny.unity";
        public const string Forest1ScenePath = ScenesDir + "/CATest_Forest1_LushFog.unity";
        public const string Forest2ScenePath = ScenesDir + "/CATest_Forest2_DreamMountain.unity";
        public const string DeepSeaScenePath = ScenesDir + "/CATest_DeepSea.unity";
        public const string SourceCoreScene = "Assets/00. Member/LHS/Scene/CoreScene.unity";

        public const int LayerGround = 6;
        public const int LayerPlayer = 7;
        public const int LayerInteractable = 8;
        public const int LayerClimbWall = 9;

        // ───────────────────────── Menu ─────────────────────────
        [MenuItem("Tools/LHS_CATest/1. Build ALL (Core + Forest0~2 + DeepSea + Twilight + Title)", priority = 1)]
        public static void BuildAll() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try {
                EditorUtility.DisplayProgressBar("LHS_CATest", "Assets", 0.05f);
                CATestAnimatorFix.Fix(); // 플레이어 Animator 등반 전환 보정(이미 되어 있으면 아무것도 안 함)
                PrepareAssets();
                EditorUtility.DisplayProgressBar("LHS_CATest", "Prefabs", 0.2f);
                BuildPrefabs();
                EditorUtility.DisplayProgressBar("LHS_CATest", "Forest 0 (화창한 숲)", 0.25f);
                BuildForest0Scene();
                EditorUtility.DisplayProgressBar("LHS_CATest", "Forest 1 (울창한 숲 + 안개 골짜기)", 0.35f);
                BuildForest1Scene();
                EditorUtility.DisplayProgressBar("LHS_CATest", "Forest 2 (몽환의 숲 + 산)", 0.5f);
                BuildForest2Scene();
                EditorUtility.DisplayProgressBar("LHS_CATest", "DeepSea", 0.65f);
                BuildDeepSeaScene();
                EditorUtility.DisplayProgressBar("LHS_CATest", "Twilight (황혼의 성역)", 0.75f);
                BuildTwilightScene();
                EditorUtility.DisplayProgressBar("LHS_CATest", "Core", 0.85f);
                BuildCoreScene();
                EditorUtility.DisplayProgressBar("LHS_CATest", "Title", 0.95f);
                BuildTitleScene();
                OpenEditSetup();
                Debug.Log("[LHS_CATest] Build ALL 완료. CATest_CoreScene 을 연 상태에서 Play 하세요.");
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/LHS_CATest/2. Rebuild Forest (0+1+2) only", priority = 20)]
        public static void MenuForest() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets(); BuildPrefabs(); BuildForest0Scene(); BuildForest1Scene(); BuildForest2Scene(); OpenEditSetup();
        }

        [MenuItem("Tools/LHS_CATest/3. Rebuild DeepSea only", priority = 21)]
        public static void MenuDeepSea() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets(); BuildPrefabs(); BuildDeepSeaScene(); OpenEditSetup();
        }

        [MenuItem("Tools/LHS_CATest/4. Rebuild Core only", priority = 22)]
        public static void MenuCore() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets(); BuildPrefabs(); BuildCoreScene(); OpenEditSetup();
        }

        [MenuItem("Tools/LHS_CATest/5. Rebuild Twilight only (마지막 챕터)", priority = 23)]
        public static void MenuTwilight() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets(); BuildPrefabs(); BuildTwilightScene(); OpenEditSetup();
        }

        [MenuItem("Tools/LHS_CATest/6. Rebuild Title + Core (UI)", priority = 24)]
        public static void MenuTitle() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets(); BuildPrefabs(); BuildCoreScene(); BuildTitleScene(); OpenEditSetup();
        }

        [MenuItem("Tools/LHS_CATest/Open Title (타이틀부터 Play)", priority = 42)]
        public static void OpenTitle() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(TitleScenePath)) EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/LHS_CATest/Open Edit Setup (Core + 모든 맵)", priority = 40)]
        public static void OpenEditSetup() {
            if (!File.Exists(CoreScenePath)) return;
            EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
            if (File.Exists(Forest0ScenePath)) EditorSceneManager.OpenScene(Forest0ScenePath, OpenSceneMode.Additive);
            if (File.Exists(Forest1ScenePath)) EditorSceneManager.OpenScene(Forest1ScenePath, OpenSceneMode.Additive);
            if (File.Exists(Forest2ScenePath)) EditorSceneManager.OpenScene(Forest2ScenePath, OpenSceneMode.Additive);
            if (File.Exists(DeepSeaScenePath)) EditorSceneManager.OpenScene(DeepSeaScenePath, OpenSceneMode.Additive);
            if (File.Exists(TwilightScenePath)) EditorSceneManager.OpenScene(TwilightScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByPath(CoreScenePath));
        }

        [MenuItem("Tools/LHS_CATest/Open Core only (Play 테스트용)", priority = 41)]
        public static void OpenCoreOnly() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
        }

        // ───────────────────────── Asset helpers ─────────────────────────
        public static void EnsureFolder(string path) {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static readonly Dictionary<string, Sprite> SpriteCache = new();

        /// <summary>경로의 스프라이트(멀티 스프라이트면 이름으로)를 찾는다.</summary>
        public static Sprite Sp(string path, string name = null) {
            var key = path + "|" + name;
            if (SpriteCache.TryGetValue(key, out var cached) && cached != null) return cached;
            Sprite found = null;
            if (name == null) found = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (found == null)
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is Sprite s && (name == null || s.name == name)) { found = s; break; }
            if (found == null) Debug.LogWarning($"[LHS_CATest] 스프라이트 없음: {path} / {name}");
            SpriteCache[key] = found;
            return found;
        }

        /// <summary>atlas 이름 접두사(prefix_번호)로 여러 개를 가져온다.</summary>
        public static Sprite[] Sps(string path, string prefix, params int[] ids) {
            var list = new List<Sprite>();
            foreach (var i in ids) {
                var s = Sp(path, prefix + "_" + i);
                if (s != null) list.Add(s);
            }
            return list.ToArray();
        }

        public static T Load<T>(string path) where T : Object {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogWarning($"[LHS_CATest] 에셋 없음: {path}");
            return a;
        }

        public static Material LitMat {
            get {
                var p = AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"); // 프로젝트의 SpriteShape에서 쓰던 Sprite-Lit-Default
                var m = string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null && GraphicsSettings.defaultRenderPipeline != null) m = GraphicsSettings.defaultRenderPipeline.default2DMaterial;
                return m;
            }
        }

        // 빌더가 만든 머티리얼 (PrepareAssets 에서 생성)
        public static Material MatAdd, MatAlpha, MatAddDot, MatAlphaFog, MatAlphaDot, MatBeam, MatStreak, MatBubble, MatGodRay;

        // ───────────────────────── Scene object helpers ─────────────────────────
        public static int SL(string layer) => SortingLayer.NameToID(layer);

        public static GameObject Group(Transform parent, string name, float z = 0f) {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0, z);
            return go;
        }

        public static SpriteRenderer Put(Transform parent, Sprite s, Vector3 pos, float scale, string layer, int order,
            Color tint, bool flip = false, float rot = 0f, string name = null, Material mat = null) {
            if (s == null) return null;
            var go = new GameObject(name ?? s.name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localRotation = Quaternion.Euler(0, 0, rot);
            go.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sharedMaterial = mat != null ? mat : LitMat;
            sr.sortingLayerID = SL(layer);
            sr.sortingOrder = order;
            sr.color = tint;
            return sr;
        }

        /// <summary>
        /// 지형 생성: SpriteShape(비주얼) + PolygonCollider2D(판정)를 분리해서 만든다.
        /// 판정은 설계한 꼭짓점 그대로, 비주얼은 SpriteShape 가장자리 스프라이트가 덮는다.
        /// 꼭짓점은 "윗면을 왼쪽→오른쪽"으로 시작하는 시계 방향으로 넣는다(윗면에 top 엣지 스프라이트가 붙도록).
        /// </summary>
        public static GameObject Terrain(Transform parent, string name, Vector2[] pts, SpriteShape shape, string layer,
            int order, Color tint, bool collide = true, float edgeHeight = 0.6f, float z = 0f, int physLayer = LayerGround,
            Vector2 visualOffset = default, bool closed = true) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0, z);
            if (shape != null) {
                var ssc = go.AddComponent<SpriteShapeController>();
                ssc.spriteShape = shape;
                var sp = ssc.spline;
                sp.Clear();
                var idx = 0;
                for (var i = 0; i < pts.Length; i++) {
                    var p = pts[i] + visualOffset;
                    if (idx > 0 && Vector2.Distance(p, (Vector2)sp.GetPosition(idx - 1)) < 0.05f) continue;
                    sp.InsertPointAt(idx, p);
                    sp.SetTangentMode(idx, ShapeTangentMode.Linear);
                    sp.SetHeight(idx, edgeHeight);
                    idx++;
                }
                sp.isOpenEnded = !closed;
                ssc.splineDetail = 8;
                ssc.autoUpdateCollider = false;
                var r = go.GetComponent<SpriteShapeRenderer>();
                r.sharedMaterial = LitMat;
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++) mats[i] = LitMat;
                r.sharedMaterials = mats;
                r.sortingLayerID = SL(layer);
                r.sortingOrder = order;
                r.color = tint;
                ssc.RefreshSpriteShape();
            }
            if (collide && closed) {
                var c = new GameObject("Collider");
                c.transform.SetParent(go.transform, false);
                c.layer = physLayer;
                var pc = c.AddComponent<PolygonCollider2D>();
                pc.points = pts;
            }
            return go;
        }

        /// <summary>벽타기 가능한 면. 기존 맵과 동일하게 ClimbWall(9) 레이어 EdgeCollider2D.</summary>
        public static GameObject ClimbEdge(Transform parent, Vector2 a, Vector2 b, string name = "ClimbWallSurface") {
            var go = new GameObject(name) { layer = LayerClimbWall };
            go.transform.SetParent(parent, false);
            var e = go.AddComponent<EdgeCollider2D>();
            e.points = new[] { a, b };
            return go;
        }

        public static BoxCollider2D Box(Transform parent, string name, Rect r, bool trigger, int layer = 0) {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(r.center.x, r.center.y, 0f);
            var b = go.AddComponent<BoxCollider2D>();
            b.size = r.size;
            b.isTrigger = trigger;
            return b;
        }

        public static Light2D Light(Transform parent, string name, Vector3 pos, Color color, float intensity, float radius,
            float innerRadius = 0f, float falloff = 0.6f) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = innerRadius;
            l.falloffIntensity = falloff;
            ApplyAllSortingLayers(l);
            return l;
        }

        public static void ApplyAllSortingLayers(Light2D l) {
            var so = new SerializedObject(l);
            var prop = so.FindProperty("m_ApplyToSortingLayers");
            if (prop == null) return;
            var layers = SortingLayer.layers;
            prop.arraySize = layers.Length;
            for (var i = 0; i < layers.Length; i++) prop.GetArrayElementAtIndex(i).intValue = layers[i].id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameObject Inst(GameObject prefab, Transform parent, Vector3 pos, float scale = 1f) {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            if (!Mathf.Approximately(scale, 1f)) go.transform.localScale = go.transform.localScale * scale;
            return go;
        }

        /// <summary>private [SerializeField] 값을 에디터에서 설정한다.</summary>
        public static void Set(Object target, string field, object value) {
            if (target == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) {
                Debug.LogWarning($"[LHS_CATest] {target.GetType().Name}.{field} 필드를 찾지 못했습니다.");
                return;
            }
            switch (value) {
                case float f: p.floatValue = f; break;
                case int i when p.propertyType == SerializedPropertyType.LayerMask: p.intValue = i; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Vector2 v2: p.vector2Value = v2; break;
                case Vector3 v3: p.vector3Value = v3; break;
                case Color c: p.colorValue = c; break;
                case Rect r: p.rectValue = r; break;
                case Vector2[] va:
                    p.arraySize = va.Length;
                    for (var k = 0; k < va.Length; k++) p.GetArrayElementAtIndex(k).vector2Value = va[k];
                    break;
                case Object o: p.objectReferenceValue = o; break;
                case Object[] arr:
                    p.arraySize = arr.Length;
                    for (var k = 0; k < arr.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                    break;
                case null: p.objectReferenceValue = null; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Color C(float r, float g, float b, float a = 1f) => new(r, g, b, a);
        public static Color Mul(Color c, float k) => new(c.r * k, c.g * k, c.b * k, c.a);
        public static Color Toward(Color c, Color fog, float t) => new(Mathf.Lerp(c.r, fog.r, t), Mathf.Lerp(c.g, fog.g, t), Mathf.Lerp(c.b, fog.b, t), c.a);

        public static float R(Random rnd, float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        public static T Pick<T>(Random rnd, IList<T> list) => list[rnd.Next(list.Count)];

        /// <summary>폴리라인(윗면) 위의 x에서의 높이.</summary>
        public static float SurfaceY(Vector2[] line, float x) {
            if (x <= line[0].x) return line[0].y;
            for (var i = 0; i < line.Length - 1; i++)
                if (x <= line[i + 1].x) {
                    var t = Mathf.InverseLerp(line[i].x, line[i + 1].x, x);
                    return Mathf.Lerp(line[i].y, line[i + 1].y, t);
                }
            return line[^1].y;
        }

        /// <summary>
        /// 지표면을 따라 프랍을 "불규칙하게" 흩뿌린다. 간격, 크기, 좌우반전, 색, 깊이(z)를 모두 난수로 흔들어
        /// 같은 스프라이트가 규칙적으로 반복돼 보이지 않게 한다.
        /// </summary>
        public static void Scatter(Transform parent, Random rnd, Sprite[] sprites, Vector2[] surface, float x0, float x1,
            float gapMin, float gapMax, float scaleMin, float scaleMax, float zMin, float zMax, string layer, int order,
            Color tintA, Color tintB, float sink = 0.1f, float yJitter = 0f, Func<float, bool> skip = null) {
            if (sprites == null || sprites.Length == 0) return;
            var x = x0 + R(rnd, 0f, gapMax * 0.5f);
            var last = -1;
            while (x < x1) {
                if (skip == null || !skip(x)) {
                    var idx = rnd.Next(sprites.Length);
                    if (idx == last && sprites.Length > 1) idx = (idx + 1) % sprites.Length;
                    last = idx;
                    var s = sprites[idx];
                    var sc = R(rnd, scaleMin, scaleMax);
                    var y = SurfaceY(surface, x) - sink + R(rnd, -yJitter, yJitter);
                    var h = s.bounds.size.y * sc;
                    var pivotToBottom = (s.bounds.center.y - s.bounds.extents.y) * sc;
                    var pos = new Vector3(x, y - pivotToBottom, R(rnd, zMin, zMax));
                    var tint = Color.Lerp(tintA, tintB, (float)rnd.NextDouble());
                    var sr = Put(parent, s, pos, sc, layer, order + rnd.Next(0, 3), tint, rnd.Next(2) == 0);
                    if (sr != null && h > 0) { }
                }
                x += R(rnd, gapMin, gapMax);
            }
        }

        /// <summary>넓은 영역에 배경 레이어(나무/바위 등)를 깊이별로 흩뿌린다.</summary>
        public static void ScatterBand(Transform parent, Random rnd, Sprite[] sprites, float x0, float x1, float gapMin,
            float gapMax, float yMin, float yMax, float scaleMin, float scaleMax, float zMin, float zMax, string layer,
            int order, Color tintA, Color tintB, bool bottomAnchored = true, float rotJitter = 0f) {
            if (sprites == null || sprites.Length == 0) return;
            var x = x0 + R(rnd, 0, gapMin);
            var last = -1;
            while (x < x1) {
                var idx = rnd.Next(sprites.Length);
                if (idx == last && sprites.Length > 1) idx = (idx + 1) % sprites.Length;
                last = idx;
                var s = sprites[idx];
                var sc = R(rnd, scaleMin, scaleMax);
                var y = R(rnd, yMin, yMax);
                if (bottomAnchored) y -= (s.bounds.center.y - s.bounds.extents.y) * sc;
                var z = R(rnd, zMin, zMax);
                Put(parent, s, new Vector3(x, y, z), sc, layer, order + Mathf.RoundToInt(-z * 0.2f),
                    Color.Lerp(tintA, tintB, (float)rnd.NextDouble()), rnd.Next(2) == 0, R(rnd, -rotJitter, rotJitter));
                x += R(rnd, gapMin, gapMax);
            }
        }

        // ───────────────────────── Particles ─────────────────────────
        public static ParticleSystem Particles(Transform parent, string name, Vector3 pos, Material mat, string layer, int order) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.sortingLayerID = SL(layer);
            r.sortingOrder = order;
            var main = ps.main;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            return ps;
        }

        /// <summary>넓은 영역에 떠다니는 먼지/반딧불/마린스노우.</summary>
        public static ParticleSystem Motes(Transform parent, string name, Rect area, float z, Material mat, Color c0, Color c1,
            float rate, float sizeMin, float sizeMax, Vector2 vel, float noise, float life, string layer, int order) {
            var ps = Particles(parent, name, new Vector3(area.center.x, area.center.y, z), mat, layer, order);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(c0, c1);
            main.maxParticles = 1000;
            main.prewarm = true;
            main.loop = true;
            var em = ps.emission;
            em.rateOverTime = rate;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(area.width, area.height, 2f);
            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(vel.x * 0.5f, vel.x * 1.5f);
            v.y = new ParticleSystem.MinMaxCurve(vel.y * 0.5f, vel.y * 1.5f);
            v.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            if (noise > 0f) {
                var n = ps.noise;
                n.enabled = true;
                n.strength = noise;
                n.frequency = 0.25f;
                n.scrollSpeed = 0.2f;
            }
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
            return ps;
        }

        public static ParticleSystem Burst(Transform parent, string name, Vector3 pos, Material mat, Color c, int count,
            float speed, float size, float life, string layer, int order, float gravity = 0f) {
            var ps = Particles(parent, name, pos, mat, layer, order);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.4f, size);
            main.startColor = c;
            main.gravityModifier = gravity;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.4f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            return ps;
        }

        // ───────────────────────── Volume ─────────────────────────
        public static VolumeProfile Profile(string name, Action<VolumeProfile> fill) {
            EnsureFolder(SettingsDir);
            var path = $"{SettingsDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);
            fill(p);
            foreach (var c in p.components)
                if (!AssetDatabase.Contains(c)) {
                    c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(c, p);
                }
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return p;
        }

        public static Volume LocalVolume(Transform parent, string name, Rect area, VolumeProfile profile, float blend, float priority = 1f) {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(area.center.x, area.center.y, 0f);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(area.width, area.height, 600f);
            var v = go.AddComponent<Volume>();
            v.isGlobal = false;
            v.blendDistance = blend;
            v.priority = priority;
            v.sharedProfile = profile;
            return v;
        }

        public static CATestAtmosphereZone Zone(Transform parent, string name, Rect area, float blend, Color light,
            float intensity, Color bg, int priority = 0) {
            var go = new GameObject("Atmo_" + name);
            go.transform.SetParent(parent, false);
            var z = go.AddComponent<CATestAtmosphereZone>();
            z.area = area;
            z.blend = blend;
            z.globalLightColor = light;
            z.globalLightIntensity = intensity;
            z.backgroundColor = bg;
            z.priority = priority;
            return z;
        }

        public static CATestAtmosphereZone CamZone(Transform parent, string name, Rect area, float blend, float distance,
            Vector3 offset, Vector3 damping, int priority = 1) {
            var go = new GameObject("Cam_" + name);
            go.transform.SetParent(parent, false);
            var z = go.AddComponent<CATestAtmosphereZone>();
            z.area = area;
            z.blend = blend;
            z.overrideLight = false;
            z.overrideCamera = true;
            z.cameraDistance = distance;
            z.targetOffset = offset;
            z.damping = damping;
            z.priority = priority;
            return z;
        }

        public static Scene NewMapScene(string path) {
            EnsureFolder(ScenesDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            return scene;
        }
    }
}
#endif
