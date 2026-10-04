#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 전체 비주얼 보강 — 각 맵 빌더 마지막에 호출되어 파티클/셰이더 효과를 더한다.
    ///
    ///  화창한 숲 : 흩날리는 나뭇잎(회전하며 떨어짐) · 숲 위 햇살 줄기 · 은빛 강 물비늘 반짝임
    ///  울창한 숲 : 짙은 잎 · 뿌리 근처에서 떠오르는 푸른 포자
    ///  안개 골짜기 : 천천히 흘러가는 큰 안개 덩어리
    ///  몽환의 숲 : 반짝이는 별가루(4갈래 빛, 커졌다 작아짐) · 흩날리는 꽃잎
    ///  바람의 산 : 옆으로 빠르게 지나가는 바람 줄기 · 눈발
    ///  심해      : 물빛 일렁임(코스틱 셰이더) · 보스 구역 푸른 발광 플랑크톤
    ///  황혼의 성역 : 협곡 흰 모래 반짝임 · 성역에 떨어지는 흰 깃털 · 둥지에서 솟는 불티
    /// 모든 효과는 Light2D 영향을 받지 않는(스스로 빛나는) 입자/셰이더라 어두운 곳에서도 보인다.
    ///
    /// ■ 맵 씬 파일은 건드리지 않는다(사용자가 직접 다듬은 씬 보존).
    ///   효과를 맵별 프리팹(Resources/CATestBeauty/씬이름.prefab)으로 저장해 두면,
    ///   실행 중 그 맵 씬이 로드될 때 CATestBeautyLoader 가 프리팹을 꺼내 그 씬 안에 넣는다(씬이 내려가면 같이 사라짐).
    ///   → 효과를 빼고 싶으면 해당 프리팹을 지우거나, 프리팹을 열어 필요 없는 입자만 끄면 된다.
    /// </summary>
    public static partial class CATestBuilder {
        public const string BeautyResDir = Root + "/Resources/CATestBeauty";
        private static Material _matLeaf, _matPetal, _matStar, _matFeather, _matCaustic, _matCausticDeep;

        [MenuItem("Tools/LHS_CATest/8. Build 추가 효과 (파티클·셰이더 프리팹, 씬 보존)", priority = 26)]
        public static void MenuBuildBeauty() {
            PrepareAssets();
            BuildBeautyPrefabs();
            Debug.Log("[LHS_CATest] 추가 효과 프리팹 생성 완료 (Resources/CATestBeauty). 맵 씬 파일은 바뀌지 않았습니다.");
        }

        /// <summary>맵별 효과 프리팹 생성. 씬을 열거나 저장하지 않는다.</summary>
        public static void BuildBeautyPrefabs() {
            EnsureFolder(Root + "/Resources");
            EnsureFolder(BeautyResDir);
            PrepareBeautyMaterials();
            void Make(string sceneName, System.Action<Transform> fill) {
                var go = new GameObject("CATest_Beauty_" + sceneName);
                fill(go.transform);
                PrefabUtility.SaveAsPrefabAsset(go, $"{BeautyResDir}/{sceneName}.prefab");
                Object.DestroyImmediate(go);
            }
            Make("CATest_Forest0_Sunny", BeautyForest0);
            Make("CATest_Forest1_LushFog", BeautyForest1);
            Make("CATest_Forest2_DreamMountain", BeautyForest2);
            Make("CATest_DeepSea", BeautyDeepSea);
            Make("CATest_Twilight", BeautyTwilight);
            AssetDatabase.SaveAssets();
        }

        private static void PrepareBeautyMaterials() {
            var shader = Shader.Find("LHS_CATest/SpriteBlend");
            _matLeaf = Mat("M_CATest_Leaf", shader, Tex(ArtDir + "/CATest_Leaf.png"), false);
            _matPetal = Mat("M_CATest_Petal", shader, Tex(ArtDir + "/CATest_Petal.png"), false);
            _matStar = Mat("M_CATest_Star4", shader, Tex(ArtDir + "/CATest_Star4.png"), true);
            _matFeather = Mat("M_CATest_Feather", shader, Tex(ArtDir + "/CATest_Feather.png"), false);
            var caustics = Shader.Find("LHS_CATest/Caustics2D");
            if (caustics == null) Debug.LogError("[LHS_CATest] LHS_CATest/Caustics2D 셰이더를 찾을 수 없습니다.");
            else {
                _matCaustic = Mat("M_CATest_Caustics", caustics, Tex(ArtDir + "/CATest_White.png"), true);
                _matCaustic.SetFloat("_Scale", 0.45f);
                _matCausticDeep = Mat("M_CATest_Caustics_Deep", caustics, Tex(ArtDir + "/CATest_White.png"), true);
                _matCausticDeep.SetFloat("_Scale", 0.3f);
                _matCausticDeep.SetFloat("_Speed", 0.4f);
            }
        }

        // ───────────── 공통 도구 ─────────────
        /// <summary>회전하며 흩날리는 입자(나뭇잎·꽃잎·깃털). Motes 에 회전 + 좌우 흔들림(노이즈)을 더함.</summary>
        private static ParticleSystem Drifters(Transform parent, string name, Rect area, float z, Material mat, Color c0, Color c1,
            float rate, float sizeMin, float sizeMax, Vector2 vel, float spinDeg, float life, string layer, int order) {
            var ps = Motes(parent, name, area, z, mat, c0, c1, rate, sizeMin, sizeMax, vel, 0.7f, life, layer, order);
            var main = ps.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-spinDeg * Mathf.Deg2Rad, spinDeg * Mathf.Deg2Rad);
            var n = ps.noise;
            n.frequency = 0.35f;
            n.scrollSpeed = 0.3f;
            return ps;
        }

        /// <summary>반짝임: 생겼다가 커지고 사라지는 4갈래 별빛.</summary>
        private static ParticleSystem Twinkles(Transform parent, string name, Rect area, float z, Color c0, Color c1, float rate,
            float sizeMin, float sizeMax, float life, string layer, int order) {
            var ps = Motes(parent, name, area, z, _matStar, c0, c1, rate, sizeMin, sizeMax, new Vector2(0f, 0.05f), 0.05f, life, layer, order);
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.35f, 1f), new Keyframe(0.5f, 0.55f), new Keyframe(0.65f, 1f), new Keyframe(1f, 0f)));
            var main = ps.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 0.6f);
            return ps;
        }

        /// <summary>물빛 일렁임 사각형(코스틱 셰이더). rect 윗변에서 가장 밝고 아래로 사라짐.</summary>
        private static void CausticsQuad(Transform parent, string name, Rect r, float z, Color c, float scale = 0.45f) {
            var s = WhiteSprite;
            if (s == null || _matCaustic == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(r.center.x, r.center.y, z);
            var size = s.bounds.size;
            go.transform.localScale = new Vector3(r.width / size.x, r.height / size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sharedMaterial = scale < 0.4f && _matCausticDeep != null ? _matCausticDeep : _matCaustic; // 프리팹에 저장되도록 머티리얼로 구분
            sr.sortingLayerID = SL("Default");
            sr.sortingOrder = 44;
            sr.color = c;
        }

        // ───────────── 맵별 ─────────────
        private static void BeautyForest0(Transform vfx) {
            PrepareBeautyMaterials();
            // 흩날리는 나뭇잎: 연두~노란 초록, 천천히 회전하며 왼쪽 아래로
            Drifters(vfx, "FallingLeaves", new Rect(-1955f, 10f, 690f, 40f), -1.5f, _matLeaf, C(0.62f, 0.8f, 0.36f, 0.95f), C(0.95f, 0.8f, 0.35f, 0.9f),
                22f, 0.18f, 0.34f, new Vector2(-0.6f, -0.9f), 160f, 16f, "Player", 45);
            // 숲 위 햇살 줄기(나뭇잎 사이로 비스듬히)
            foreach (var x in new[] { -1880f, -1790f, -1705f, -1610f, -1480f, -1390f, -1300f })
                GodRay(vfx, "CanopyRay", x, 0f, 38f, 6f, -22f, C(1f, 0.93f, 0.7f), 0.38f, x * 0.37f, "Default", 41, 3f, 0.35f, 5f, 0.6f);
            // 은빛 강 물비늘
            Twinkles(vfx, "RiverGlints", new Rect(-1520f, -2.5f, 88f, 2.5f), -0.4f, C(1f, 1f, 0.95f, 0.9f), C(0.8f, 0.95f, 1f, 0.8f), 14f, 0.25f, 0.55f, 1.4f, "Player", 46);
        }

        private static void BeautyForest1(Transform vfx) {
            PrepareBeautyMaterials();
            Drifters(vfx, "LushLeaves", new Rect(-1260f, 8f, 500f, 40f), -1.5f, _matLeaf, C(0.32f, 0.55f, 0.3f, 0.95f), C(0.5f, 0.65f, 0.28f, 0.9f),
                14f, 0.16f, 0.3f, new Vector2(-0.4f, -0.7f), 140f, 18f, "Player", 45);
            // 뿌리 근처 푸른 포자: 아래에서 천천히 떠오름
            Motes(vfx, "RootSpores", new Rect(-1250f, -8f, 490f, 16f), -0.8f, MatAddDot, C(0.5f, 1f, 0.9f, 0.8f), C(0.6f, 0.8f, 1f, 0.6f),
                18f, 0.05f, 0.12f, new Vector2(0.05f, 0.35f), 0.4f, 9f, "Player", 44);
            // 안개 골짜기: 크게 흘러가는 안개 덩어리
            var fog = Motes(vfx, "FogDrift", new Rect(-744f, -25f, 284f, 40f), 1.5f, MatAlphaFog, C(0.8f, 0.85f, 0.9f, 0.12f), C(0.7f, 0.78f, 0.85f, 0.08f),
                3f, 10f, 18f, new Vector2(0.35f, 0f), 0.2f, 30f, "Default", 46);
            var fm = fog.main; fm.maxParticles = 120;
        }

        private static void BeautyForest2(Transform vfx) {
            PrepareBeautyMaterials();
            // 몽환의 숲: 별가루 + 꽃잎
            Twinkles(vfx, "DreamStars", new Rect(-470f, -5f, 215f, 45f), -1f, C(1f, 0.85f, 1f, 1f), C(0.7f, 0.9f, 1f, 1f), 24f, 0.3f, 0.7f, 2.6f, "Player", 46);
            Drifters(vfx, "DreamPetals", new Rect(-470f, 10f, 215f, 40f), -1.2f, _matPetal, C(1f, 0.78f, 0.9f, 0.9f), C(0.95f, 0.92f, 1f, 0.85f),
                10f, 0.12f, 0.24f, new Vector2(0.3f, -0.5f), 200f, 18f, "Player", 45);
            // 바람의 산: 바람 줄기(가로로 빠르게) + 눈발
            var wind = Motes(vfx, "WindStreaks", new Rect(-252f, 25f, 175f, 90f), -1f, MatStreak, C(1f, 1f, 1f, 0.35f), C(0.85f, 0.9f, 1f, 0.2f),
                16f, 0.5f, 1.1f, new Vector2(-14f, 0.5f), 0.3f, 1.4f, "Player", 46);
            var wr = wind.GetComponent<ParticleSystemRenderer>();
            wr.renderMode = ParticleSystemRenderMode.Stretch;
            wr.velocityScale = 0.12f;
            wr.lengthScale = 1f;
            Motes(vfx, "MountainSnow", new Rect(-252f, 30f, 175f, 90f), -0.8f, MatAddDot, C(1f, 1f, 1f, 0.7f), C(0.85f, 0.9f, 1f, 0.5f),
                40f, 0.04f, 0.09f, new Vector2(-2.5f, -1.2f), 0.8f, 8f, "Player", 45);
        }

        private static void BeautyDeepSea(Transform vfx) {
            PrepareBeautyMaterials();
            // 물빛 일렁임: 착수 웅덩이~잠긴 통로(수면 가까움 → 뚜렷), 보스 구역(깊음 → 희미), 상승 통로(위로 갈수록 밝음)
            CausticsQuad(vfx, "Caustics_Shallow", new Rect(236f, -72f, 236f, 44f), 1.5f, C(0.55f, 0.9f, 1f, 0.55f));
            CausticsQuad(vfx, "Caustics_Boss", new Rect(506f, -104f, 505f, 34f), 3f, C(0.4f, 0.75f, 1f, 0.22f), 0.3f);
            CausticsQuad(vfx, "Caustics_Ascent", new Rect(1026f, -96f, 64f, 72f), 1.5f, C(0.6f, 0.95f, 1f, 0.6f));
            // 보스 구역 푸른 발광 플랑크톤: 반짝였다 사라짐
            Twinkles(vfx, "BioPlankton", new Rect(506f, -100f, 500f, 36f), -0.6f, C(0.4f, 1f, 0.95f, 0.8f), C(0.5f, 0.7f, 1f, 0.7f), 26f, 0.12f, 0.3f, 3.2f, "Player", 45);
        }

        private static void BeautyTwilight(Transform vfx) {
            PrepareBeautyMaterials();
            // 성역에 천천히 떨어지는 흰 깃털
            Drifters(vfx, "SanctumFeathers", new Rect(1640f, 10f, 245f, 40f), -1.2f, _matFeather, C(1f, 0.97f, 1f, 0.85f), C(1f, 0.9f, 0.9f, 0.7f),
                5f, 0.3f, 0.55f, new Vector2(-0.3f, -0.6f), 90f, 22f, "Player", 45);
            // 둥지: 땅에서 솟는 장밋빛 불티
            Motes(vfx, "NestEmbers", new Rect(1890f, 10f, 100f, 8f), -0.8f, MatAddDot, C(1f, 0.7f, 0.5f, 0.9f), C(1f, 0.5f, 0.55f, 0.7f),
                20f, 0.05f, 0.12f, new Vector2(0.1f, 1.2f), 0.6f, 6f, "Player", 45);
            // 백색 협곡: 흰 모래 먼지가 반짝이며 흩날림
            Twinkles(vfx, "CanyonSparkle", new Rect(1398f, 0f, 240f, 30f), -0.8f, C(1f, 0.92f, 0.9f, 0.8f), C(0.9f, 0.88f, 1f, 0.7f), 12f, 0.15f, 0.35f, 2.2f, "Player", 45);
        }
    }
}
#endif
