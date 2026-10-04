#if UNITY_EDITOR
using System.Linq;
using _02._Script._03_TrapAndEnemy.Enemies;
using _02._Script._03_TrapAndEnemy.Traps;
using _02._Script._04_Interaction;
using ClamTrapArt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace LHS_CATest.EditorTools {
    public static partial class CATestBuilder {
        // 생성된 프리팹 참조 (씬 빌드에서 사용)
        public static GameObject PSaveForest, PSaveSea, PRelicForest, PRelicSea, PCrumble, PPushRock, PBreakWall, PBloomPad,
            PLamp, PWisp, PThornsForest, PUrchinSea, PClam, PJelly, PEyeTrap, PWindColumn, PCurrentColumn, PSeaweed,
            PKelpCover, PDeadZone, PCrate, PPlate, PLever, PThornSeed, PLampForest, PBloomPadForest;

        // 원본 프리팹 경로
        private const string SrcWisp = "Assets/03. Prefabs/Trap/Wisp.prefab";
        private const string SrcUrchin = "Assets/03. Prefabs/Trap/UrchinTrap.prefab";
        private const string SrcClamp = "Assets/03. Prefabs/Trap/Clamp.prefab";
        private const string SrcFan = "Assets/03. Prefabs/Trap/Fan.prefab";
        private const string SrcElectronic = "Assets/03. Prefabs/Enemies/Electronic.prefab";
        private const string SrcWhileEye = "Assets/03. Prefabs/Enemies/WhileEye.prefab";
        private const string SrcLightObj = "Assets/03. Prefabs/Interact/LightObj.prefab";
        private const string SrcDeadZone = "Assets/03. Prefabs/DeadZone.prefab";
        private const string SrcClamArt = "Assets/ClamTrap_AI/Prefabs/ClamTrap.prefab";
        private const string SrcFire = "Assets/00. Member/LHS/_VFX_Lib/01. Prefabs/FireParticle.prefab";
        private const string SrcWind = "Assets/00. Member/LHS/_VFX_Lib/01. Prefabs/WindParticle.prefab";
        private const string SrcBubbles = FSB + "/Underwater area pack/Prefabs/Particle Bubbles.prefab";
        private const string SrcFireflies = FSB + "/Old Forest pack/Prefabs/Firefly particle.prefab";
        private const string SrcLeaves = FSB + "/Old Forest pack/Prefabs/LEAFS Particle.prefab";

        // ───────────────────────── 에셋 준비 ─────────────────────────
        public static void PrepareAssets() {
            SpriteCache.Clear();
            foreach (var d in new[] { ScenesDir, PrefabDir, MatDir, ArtDir, SettingsDir }) EnsureFolder(d);
            AssetDatabase.Refresh();

            // 새로 만든 PNG를 스프라이트로 임포트
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir })) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                var changed = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single ||
                              ti.mipmapEnabled || !Mathf.Approximately(ti.spritePixelsPerUnit, 100f) || !ti.alphaIsTransparency;
                if (!changed) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.SaveAndReimport();
            }

            var shader = Shader.Find("LHS_CATest/SpriteBlend");
            if (shader == null) Debug.LogError("[LHS_CATest] LHS_CATest/SpriteBlend 셰이더를 찾을 수 없습니다.");
            MatAdd = Mat("M_CATest_Additive", shader, null, true);
            MatAlpha = Mat("M_CATest_Alpha", shader, null, false);
            MatAddDot = Mat("M_CATest_AddDot", shader, Tex(ArtDir + "/CATest_SoftDot.png"), true);
            MatAlphaDot = Mat("M_CATest_AlphaDot", shader, Tex(ArtDir + "/CATest_SoftDot.png"), false);
            MatAlphaFog = Mat("M_CATest_AlphaFog", shader, Tex(ArtDir + "/CATest_Fog.png"), false);
            MatStreak = Mat("M_CATest_Streak", shader, Tex(ArtDir + "/CATest_Streak.png"), true);
            MatBeam = Mat("M_CATest_Beam", shader, Texture2D.whiteTexture, true);
            MatBubble = Mat("M_CATest_Bubble", shader, Tex(UW + "Bubble particle.png"), false);
            // 햇살 전용 절차적 셰이더(Shaders/CATest_GodRay.shader). 흰 사각 스프라이트 위에 빛줄기를 계산해서 그린다.
            var godRay = Shader.Find("LHS_CATest/GodRay2D");
            if (godRay == null) Debug.LogError("[LHS_CATest] LHS_CATest/GodRay2D 셰이더를 찾을 수 없습니다.");
            MatGodRay = Mat("M_CATest_GodRay", godRay, Tex(ArtDir + "/CATest_White.png"), true);
            // 마지막 챕터: 일식 하늘 / 거울 물 (Shaders/CATest_EclipseSky, CATest_MirrorWater)
            var skyShader = Shader.Find("LHS_CATest/EclipseSky");
            var mirrorShader = Shader.Find("LHS_CATest/MirrorWater");
            if (skyShader == null || mirrorShader == null) Debug.LogError("[LHS_CATest] EclipseSky / MirrorWater 셰이더를 찾을 수 없습니다.");
            MatSky = Mat("M_CATest_EclipseSky", skyShader, Tex(ArtDir + "/CATest_White.png"), false);
            ConfigureSkyMaterial(MatSky);
            MatMirror = Mat("M_CATest_MirrorWater", mirrorShader, Tex(ArtDir + "/CATest_White.png"), false);
            PrepareUISprites();
            AssetDatabase.SaveAssets();
        }

        private static Texture2D Tex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        private static Material Mat(string name, Shader shader, Texture tex, bool additive) {
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            if (tex != null) m.mainTexture = tex;
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Sprite Glow => Sp(ArtDir + "/CATest_Glow.png");
        public static Sprite WhiteSprite => Sp(ArtDir + "/CATest_White.png");

        // ───────────────────────── 프리팹 ─────────────────────────
        public static void BuildPrefabs() {
            var temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var holder = new GameObject("__PrefabBuild").transform;

            PSaveForest = SavePrefab(BuildSavePoint(holder, false), "P_CATest_SavePoint_Forest");
            PSaveSea = SavePrefab(BuildSavePoint(holder, true), "P_CATest_SavePoint_Sea");
            PRelicForest = SavePrefab(BuildRelic(holder, false), "P_CATest_Relic_Forest");
            PRelicSea = SavePrefab(BuildRelic(holder, true), "P_CATest_Relic_Sea");
            PCrumble = SavePrefab(BuildCrumble(holder), "P_CATest_CrumbleSlab");
            PPushRock = SavePrefab(BuildPushRock(holder), "P_CATest_PushRock");
            PBreakWall = SavePrefab(BuildBreakWall(holder), "P_CATest_BreakableWall");
            PBloomPad = SavePrefab(BuildBloomPad(holder), "P_CATest_LightBloomPad");
            PSeaweed = SavePrefab(BuildSeaweed(holder), "P_CATest_SeaweedTrap");
            PKelpCover = SavePrefab(BuildKelpCover(holder), "P_CATest_KelpCover");
            PCrate = SavePrefab(BuildCrate(holder), "P_CATest_Crate");
            PPlate = SavePrefab(BuildPlate(holder), "P_CATest_PressurePlate");
            PLever = SavePrefab(BuildLever(holder), "P_CATest_Lever");
            PBloomPadForest = SavePrefab(BuildBloomPadForest(holder), "P_CATest_LightBloomPad_Forest");

            // 기존 프리팹의 Variant (원본은 그대로, 비주얼/연결만 추가)
            PLamp = Variant(SrcLightObj, "PV_CATest_LampFlower", DecorateLamp);
            PWisp = Variant(SrcWisp, "PV_CATest_Wisp", DecorateWisp);
            PThornsForest = Variant(SrcUrchin, "PV_CATest_Thorns_Forest", go => HidePlaceholder(go));
            PUrchinSea = Variant(SrcUrchin, "PV_CATest_Urchin_Sea", go => HidePlaceholder(go));
            PClam = Variant(SrcClamp, "PV_CATest_Clam", DecorateClam);
            PJelly = Variant(SrcElectronic, "PV_CATest_ElectricJelly", DecorateJelly);
            PEyeTrap = Variant(SrcWhileEye, "PV_CATest_WallEye", DecorateEye);
            PWindColumn = Variant(SrcFan, "PV_CATest_WindColumn", go => DecorateColumn(go, false));
            PCurrentColumn = Variant(SrcFan, "PV_CATest_CurrentColumn", go => DecorateColumn(go, true));
            PThornSeed = Variant(SrcElectronic, "PV_CATest_ThornSeed", DecorateThornSeed);
            PLampForest = Variant(SrcLightObj, "PV_CATest_LampMushroom", DecorateLampForest);
            PDeadZone = Load<GameObject>(SrcDeadZone);
            BuildTwilightPrefabs(holder);

            Object.DestroyImmediate(holder.gameObject);
            AssetDatabase.SaveAssets();
        }

        private static GameObject SavePrefab(GameObject go, string name) {
            var path = $"{PrefabDir}/{name}.prefab";
            var p = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return p;
        }

        private static GameObject Variant(string srcPath, string name, System.Action<GameObject> decorate) {
            var src = Load<GameObject>(srcPath);
            if (src == null) return null;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            inst.name = name;
            decorate(inst);
            var path = $"{PrefabDir}/{name}.prefab";
            var p = PrefabUtility.SaveAsPrefabAsset(inst, path);
            Object.DestroyImmediate(inst);
            return p;
        }

        /// <summary>원본 프리팹의 흰 사각형/원(임시 스프라이트)을 숨긴다.</summary>
        private static void HidePlaceholder(GameObject go) {
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) {
                var n = sr.sprite != null ? sr.sprite.name : "";
                if (n == "Square" || n == "Circle" || n.StartsWith("Knob") || sr.sprite == null) sr.enabled = false;
            }
        }

        private static GameObject NewRoot(Transform holder, string name, int layer = 0) {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(holder, false);
            return go;
        }

        // 세이브 포인트: 숲 = 작은 돌기둥 위의 따뜻한 등불 / 심해 = 조개 받침 위의 푸른 수정
        private static GameObject BuildSavePoint(Transform holder, bool sea) {
            var root = NewRoot(holder, sea ? "SavePoint_Sea" : "SavePoint_Forest");
            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3f, 3f);
            box.offset = new Vector2(0f, 1.2f);
            var sp = root.AddComponent<CATestSavePoint>();
            var tint = new System.Collections.Generic.List<SpriteRenderer>();
            Transform bob;
            if (!sea) {
                var baseS = Put(root.transform, Sp(OF + "Old stounes.png", "Old stounes_14"), new Vector3(0, 0.85f, 0.1f), 0.55f, "Item", 5, C(0.8f, 0.8f, 0.75f));
                var cap = Put(root.transform, Sp(OF + "Old stounes.png", "Old stounes_6"), new Vector3(0, 1.9f, 0.05f), 0.32f, "Item", 6, C(0.8f, 0.8f, 0.75f));
                tint.Add(baseS); tint.Add(cap);
                var moss = Put(root.transform, Sp(OF + "Old greens.png", "Old greens_1"), new Vector3(0.1f, 0.15f, -0.05f), 0.8f, "Item", 7, Color.white);
                bob = new GameObject("Orb").transform;
                bob.SetParent(root.transform, false);
                bob.localPosition = new Vector3(0, 2.55f, 0);
                Put(bob, Glow, bob.position, 0.45f, "Item", 8, C(1f, 0.75f, 0.4f, 0.9f), mat: MatAdd);
                Put(bob, Glow, bob.position, 0.18f, "Item", 9, C(1f, 0.95f, 0.8f, 1f), mat: MatAdd);
                var fire = Load<GameObject>(SrcFire);
                if (fire != null) {
                    var f = (GameObject)PrefabUtility.InstantiatePrefab(fire);
                    f.transform.SetParent(bob, false);
                    f.transform.localPosition = new Vector3(0, -0.1f, -0.05f);
                    f.transform.localScale = Vector3.one * 0.25f;
                }
                if (moss != null) moss.transform.localScale = Vector3.one * 0.8f;
            }
            else {
                var shell = Put(root.transform, Sp(UW + "Seaweed.png", "Seaweed_0"), new Vector3(0, 0.6f, 0.1f), 0.32f, "Item", 5, C(0.7f, 0.8f, 0.85f));
                tint.Add(shell);
                bob = new GameObject("Crystal").transform;
                bob.SetParent(root.transform, false);
                bob.localPosition = new Vector3(0, 1.9f, 0);
                var cr = Put(bob, Sp(DG + "cristals blue.png", "cristals blue_1"), bob.position, 0.55f, "Item", 7, Color.white);
                tint.Add(cr);
                Put(bob, Glow, bob.position, 0.55f, "Item", 8, C(0.35f, 0.85f, 1f, 0.7f), mat: MatAdd);
            }
            var light = Light(root.transform, "Glow", new Vector3(0, 2.3f, 0), sea ? C(0.4f, 0.85f, 1f) : C(1f, 0.75f, 0.45f), 0.4f, 7f, 0.5f);
            var burst = Burst(root.transform, "ActivateBurst", new Vector3(0, 2.2f, -0.2f), MatAddDot,
                sea ? C(0.5f, 0.95f, 1f) : C(1f, 0.85f, 0.5f), 45, 6f, 0.35f, 1.4f, "Player", 20, -0.2f);
            var loop = Motes(root.transform, "Embers", new Rect(-0.8f, 1.8f, 1.6f, 1.2f), -0.2f, MatAddDot,
                sea ? C(0.5f, 0.9f, 1f, 0.8f) : C(1f, 0.8f, 0.45f, 0.8f), sea ? C(0.8f, 1f, 1f, 0.6f) : C(1f, 0.6f, 0.3f, 0.6f),
                5f, 0.05f, 0.14f, new Vector2(0f, 0.6f), 0.3f, 2.5f, "Player", 19);
            loop.transform.localPosition = new Vector3(0, 2.3f, -0.2f);
            Set(sp, "glow", light);
            Set(sp, "tintTargets", tint.Where(t => t != null).Cast<Object>().ToArray());
            Set(sp, "activateBurst", burst);
            Set(sp, "loopParticles", loop);
            Set(sp, "bobTarget", bob);
            Set(sp, "idleColor", sea ? C(0.35f, 0.55f, 0.7f) : C(0.55f, 0.6f, 0.7f));
            Set(sp, "activeColor", sea ? C(0.45f, 0.95f, 1f) : C(1f, 0.78f, 0.45f));
            return root;
        }

        private static GameObject BuildRelic(Transform holder, bool sea) {
            var root = NewRoot(holder, "Relic");
            var c = root.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.8f;
            var col = root.AddComponent<CATestCollectible>();
            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var am = vis.AddComponent<CATestAmbientMotion>();
            am.bobHeight = 0.18f; am.bobSpeed = 1.8f; am.swayAngle = 6f; am.swaySpeed = 0.8f;
            Put(vis.transform, Glow, Vector3.zero, 0.5f, "Player", 3, sea ? C(0.3f, 0.9f, 1f, 0.8f) : C(1f, 0.85f, 0.45f, 0.8f), mat: MatAdd);
            var s = sea ? Sp(DG + "cristals blue.png", "cristals blue_3") : Sp(OF + "Old stounes.png", "Old stounes_7");
            Put(vis.transform, s, Vector3.zero, sea ? 0.8f : 0.3f, "Player", 4, Color.white);
            var l = Light(vis.transform, "Light", Vector3.zero, sea ? C(0.4f, 0.9f, 1f) : C(1f, 0.8f, 0.45f), 0.9f, 4f, 0.2f);
            am.flickerLight = l; am.flickerAmount = 0.15f;
            var burst = Burst(root.transform, "Pickup", Vector3.back * 0.2f, MatAddDot, sea ? C(0.5f, 1f, 1f) : C(1f, 0.9f, 0.5f), 60, 8f, 0.3f, 1.2f, "Player", 25, -0.1f);
            Set(col, "visualRoot", vis);
            Set(col, "pickupBurst", burst);
            Set(col, "id", sea ? "심해 유물" : "숲 유물");
            return root;
        }

        private static GameObject BuildCrumble(Transform holder) {
            var root = NewRoot(holder, "CrumbleSlab", LayerGround);
            var b = root.AddComponent<BoxCollider2D>();
            b.size = new Vector2(3f, 0.7f);
            b.offset = new Vector2(0f, -0.35f);
            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var slab = Put(vis.transform, Sp(OF + "Stone.png", "Stone_3"), new Vector3(0, -0.45f, 0), 1f, "Ground", 20, C(0.85f, 0.85f, 0.8f));
            if (slab != null) slab.transform.localScale = new Vector3(1.65f, 0.42f, 1f);
            Put(vis.transform, Sp(OF + "Old greens.png", "Old greens_1"), new Vector3(-0.5f, 0.05f, -0.05f), 0.7f, "Ground", 21, C(0.9f, 0.9f, 0.9f));
            var crumble = root.AddComponent<CATestCrumblingPlatform>();
            var dust = Burst(root.transform, "Dust", new Vector3(0, -0.5f, -0.3f), MatAlphaDot, C(0.75f, 0.7f, 0.62f, 0.8f), 20, 1.5f, 0.35f, 1.2f, "Player", 12, 0.4f);
            var sh = dust.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(3f, 0.3f, 0.1f);
            Set(crumble, "visual", vis.transform);
            Set(crumble, "dust", dust);
            return root;
        }

        private static GameObject BuildPushRock(Transform holder) {
            var root = NewRoot(holder, "PushRock", 0); // Default: 지형(Ground)과 충돌하도록 (Crate 주석 참고)
            var rb = root.AddComponent<Rigidbody2D>();
            rb.mass = 2.5f;
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var b = root.AddComponent<BoxCollider2D>();
            b.size = new Vector2(2.5f, 2.5f);
            var mat = new PhysicsMaterial2D("CATest_RockFriction") { friction = 0.35f, bounciness = 0f };
            var matPath = $"{MatDir}/PM_CATest_Rock.physicsMaterial2D";
            AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(mat, matPath);
            b.sharedMaterial = mat;
            GroundTop(root.transform, new Vector2(2.4f, 2.5f));
            Put(root.transform, Sp(OF + "Stone.png", "Stone_2"), new Vector3(0, 0, 0), 1.38f, "Ground", 25, C(0.9f, 0.88f, 0.82f));
            Put(root.transform, Sp(OF + "Old greens.png", "Old greens_4"), new Vector3(0.4f, 1.3f, -0.05f), 0.9f, "Ground", 26, Color.white);
            return root;
        }

        private static GameObject BuildBreakWall(Transform holder) {
            var root = NewRoot(holder, "CrackedWall", LayerInteractable);
            var b = root.AddComponent<BoxCollider2D>();
            b.size = new Vector2(1.6f, 4f);
            b.offset = new Vector2(0f, 2f);
            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var s1 = Put(vis.transform, Sp(OF + "Stone.png", "Stone_1"), new Vector3(0, 1.05f, 0), 0.95f, "Ground", 30, C(0.75f, 0.72f, 0.68f));
            var s2 = Put(vis.transform, Sp(OF + "Stone.png", "Stone_0"), new Vector3(0.05f, 3.0f, 0), 0.9f, "Ground", 30, C(0.7f, 0.68f, 0.64f), true);
            Put(vis.transform, Sp(OF + "old wall leafs.png", "old wall leafs_1"), new Vector3(0.3f, 2.2f, -0.05f), 0.9f, "Ground", 31, C(0.8f, 0.85f, 0.8f));
            var wall = root.AddComponent<CATestBreakableWall>();
            var debris = Burst(root.transform, "Debris", new Vector3(0, 2f, -0.3f), MatAlphaDot, C(0.7f, 0.65f, 0.58f), 1, 5f, 0.35f, 1.4f, "Player", 15, 1.2f);
            var sh = debris.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.4f, 3.6f, 0.2f);
            Set(wall, "targetRenderer", s1);
            var lightObj = Load<GameObject>(SrcLightObj);
            var il = lightObj != null ? lightObj.GetComponent<InteractLight>() : null;
            if (il != null) {
                var so = new SerializedObject(il);
                Set(wall, "outlineMaterial", so.FindProperty("outlineMaterial").objectReferenceValue);
            }
            // Interactable(8) 레이어는 플레이어와 충돌하지 않으므로(Physics2D 충돌 매트릭스), 실제로 길을 막는 판정은
            // Ground 레이어의 별도 자식 콜라이더로 둔다. 부서지면 둘 다 끈다.
            var solid = new GameObject("Solid") { layer = LayerGround };
            solid.transform.SetParent(root.transform, false);
            var sb = solid.AddComponent<BoxCollider2D>();
            sb.size = b.size;
            sb.offset = b.offset;
            Set(wall, "hideOnBreak", new Object[] { vis });
            Set(wall, "debris", debris);
            Set(wall, "blockers", new Object[] { b, sb });
            return root;
        }

        private static GameObject BuildBloomPad(Transform holder) {
            var root = NewRoot(holder, "LightBloomPad");
            var solid = new GameObject("Solid") { layer = LayerGround };
            solid.transform.SetParent(root.transform, false);
            var b = solid.AddComponent<BoxCollider2D>();
            b.size = new Vector2(3.2f, 0.6f);
            b.offset = new Vector2(0, -0.3f);
            var v1 = Put(root.transform, Sp(UW + "Seaweed.png", "Seaweed_7"), new Vector3(0, -0.55f, 0), 0.45f, "Ground", 30, C(0.55f, 1f, 0.9f), rot: 0f);
            if (v1 != null) v1.transform.localScale = new Vector3(0.62f, 0.22f, 1f);
            var v2 = Put(root.transform, Glow, new Vector3(0, -0.25f, -0.05f), 0.9f, "Ground", 31, C(0.4f, 1f, 0.85f, 0.55f), mat: MatAdd);
            if (v2 != null) v2.transform.localScale = new Vector3(1.4f, 0.35f, 1f);
            var l = Light(root.transform, "Glow", new Vector3(0, 0, 0), C(0.4f, 1f, 0.85f), 0.2f, 4.5f, 0.3f);
            var pad = root.AddComponent<CATestLightBloomPlatform>();
            Set(pad, "solid", b);
            Set(pad, "visuals", new Object[] { v1, v2 });
            Set(pad, "glow", l);
            return root;
        }

        private static GameObject BuildSeaweed(Transform holder) {
            var root = NewRoot(holder, "SeaweedTrap");
            var b = root.AddComponent<BoxCollider2D>();
            b.isTrigger = true;
            b.size = new Vector2(5f, 2.6f);
            b.offset = new Vector2(0f, 1.3f);
            var trap = root.AddComponent<CATestSeaweed>();
            var stems = new System.Collections.Generic.List<Object>();
            var ids = new[] { 2, 3, 4, 5, 3 };
            for (var i = 0; i < 5; i++) {
                var pivot = new GameObject("Stem" + i).transform;
                pivot.SetParent(root.transform, false);
                pivot.localPosition = new Vector3(-2f + i, 0f, i % 2 == 0 ? -0.2f : 0.2f);
                var s = Sp(UW + "Seaweed.png", "Seaweed_" + ids[i]);
                var sr = Put(pivot, s, pivot.position + new Vector3(0, 1.5f, 0), 0.26f + (i % 3) * 0.03f, i % 2 == 0 ? "Architecture" : "Item", 5 + i,
                    C(0.55f, 0.9f, 0.7f), i % 2 == 1);
                stems.Add(sr);
            }
            Set(trap, "stems", stems.ToArray());
            return root;
        }

        private static GameObject BuildKelpCover(Transform holder) {
            var root = NewRoot(holder, "KelpCover");
            var b = root.AddComponent<BoxCollider2D>();
            b.isTrigger = true;
            b.size = new Vector2(5f, 3f);
            b.offset = new Vector2(0f, 1.5f);
            var cover = root.AddComponent<CATestCover>();
            var list = new System.Collections.Generic.List<Object>();
            var ids = new[] { 4, 2, 5, 3, 2, 4 };
            for (var i = 0; i < ids.Length; i++) {
                var x = -2.4f + i * 0.95f;
                var sr = Put(root.transform, Sp(UW + "Seaweed.png", "Seaweed_" + ids[i]), new Vector3(x, 1.9f, -0.6f - i * 0.05f),
                    0.3f + (i % 2) * 0.04f, "Architecture", 12 + i, C(0.18f, 0.42f, 0.4f), i % 2 == 0);
                list.Add(sr);
            }
            Set(cover, "rustleTargets", list.ToArray());
            return root;
        }

        // 밀 수 있는 나무 상자 (숲 팩 danger_0). 무게 2, 회전 고정. 죽었다 살아나면 원위치(CATestRespawnReset).
        private static GameObject BuildCrate(Transform holder) {
            // 레이어 주의: Ground(6)끼리는 충돌하지 않으므로(충돌 매트릭스) 상자 본체는 Default(0)로 둬야 지형 위에 선다.
            // 대신 플레이어의 접지 판정(Mover.whatIsGround = Ground)을 위해 같은 크기의 Ground 레이어 자식 콜라이더를 붙인다.
            var root = NewRoot(holder, "Crate", 0);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.mass = 2f;
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var b = root.AddComponent<BoxCollider2D>();
            b.size = new Vector2(1.8f, 1.8f);
            b.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>($"{MatDir}/PM_CATest_Rock.physicsMaterial2D");
            GroundTop(root.transform, new Vector2(1.7f, 1.8f));
            // 지형 윗면 그림(풀 가장자리)이 충돌선보다 약간 위에 그려지므로, 상자 그림도 0.3m 올려 "파묻혀" 보이지 않게 한다.
            Put(root.transform, Sp(FF + "danger.png", "danger_0"), new Vector3(0f, 0.3f, 0f), 1.26f, "Ground", 25, C(0.92f, 0.9f, 0.85f));
            Put(root.transform, Sp(FF + "greens..png", "greens._23"), new Vector3(0.55f, 1.25f, -0.05f), 0.55f, "Ground", 26, Color.white);
            root.AddComponent<CATestRespawnReset>();
            return root;
        }

        private static void GroundTop(Transform parent, Vector2 size) {
            var go = new GameObject("GroundTop") { layer = LayerGround };
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<BoxCollider2D>();
            c.size = size;
        }

        // 압력판: 이끼 낀 돌판 + 상태 표시등(빨강=꺼짐, 초록=눌림)
        private static GameObject BuildPlate(Transform holder) {
            var root = NewRoot(holder, "PressurePlate");
            var plate = root.AddComponent<CATestPressurePlate>();
            var vis = new GameObject("PlateVisual").transform;
            vis.SetParent(root.transform, false);
            var slab = Put(vis, Sp(DG + "metal detail for dungeon.png", "metal detail for dungeon_6"), new Vector3(0, 0.05f, 0.1f), 1f, "Ground", 22, C(0.75f, 0.8f, 0.72f));
            if (slab != null) slab.transform.localScale = new Vector3(1.25f, 0.2f, 1f);
            Put(vis, Sp(OF + "Old greens.png", "Old greens_1"), new Vector3(-0.8f, 0.1f, -0.05f), 0.5f, "Ground", 23, Color.white);
            var l = Light(root.transform, "Indicator", new Vector3(0, 0.6f, -0.2f), C(1f, 0.45f, 0.3f), 0.5f, 3.2f, 0.2f);
            Put(root.transform, Glow, new Vector3(0, 0.15f, -0.1f), 0.5f, "Ground", 24, C(0.8f, 1f, 0.8f, 0.35f), mat: MatAdd);
            Set(plate, "plateVisual", vis);
            Set(plate, "indicator", l);
            Set(plate, "detectMask", (1 << LayerGround) | 1 | (1 << LayerPlayer));
            return root;
        }

        // 레버: 바위 받침 + 톱니 달린 금속 손잡이(던전 팩). Interactable 레이어의 실제(비트리거) 콜라이더여야
        // 플레이어 Interactor(ContactFilter: useTriggers=false, layer 8)가 찾는다.
        private static GameObject BuildLever(Transform holder) {
            var root = NewRoot(holder, "Lever", LayerInteractable);
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.4f, 2f);
            box.offset = new Vector2(0f, 1f);
            var lever = root.AddComponent<CATestLever>();
            Put(root.transform, Sp(FF + "greens..png", "greens._11"), new Vector3(0, 0.25f, 0.1f), 0.9f, "Item", 10, C(0.8f, 0.8f, 0.75f));
            var handle = new GameObject("Handle").transform;
            handle.SetParent(root.transform, false);
            handle.localPosition = new Vector3(0, 0.35f, 0.05f);
            var h = Put(handle, Sp(DG + "metal detail for dungeon.png", "metal detail for dungeon_8"), handle.position + new Vector3(0, 0.75f, 0), 0.5f, "Item", 11, C(0.85f, 0.82f, 0.75f));
            var lamp = Light(root.transform, "Lamp", new Vector3(0, 1.6f, -0.2f), C(0.6f, 0.75f, 1f), 0.35f, 3.5f, 0.2f);
            Put(handle, Glow, handle.position + new Vector3(0, 1.45f, -0.05f), 0.3f, "Item", 12, C(1f, 0.9f, 0.6f, 0.6f), mat: MatAdd);
            var burst = Burst(root.transform, "PullSparks", new Vector3(0, 1.2f, -0.3f), MatAddDot, C(1f, 0.85f, 0.5f), 18, 4f, 0.2f, 0.7f, "Player", 20, 0.5f);
            Set(lever, "targetRenderer", h);
            Set(lever, "handle", handle);
            Set(lever, "lamp", lamp);
            Set(lever, "pullBurst", burst);
            var lightObj = Load<GameObject>(SrcLightObj);
            var il = lightObj != null ? lightObj.GetComponent<InteractLight>() : null;
            if (il != null) Set(lever, "outlineMaterial", new SerializedObject(il).FindProperty("outlineMaterial").objectReferenceValue);
            return root;
        }

        // 숲용 빛 발판: 등불(버섯)을 켜면 실체화되는 발광 이끼 선반
        private static GameObject BuildBloomPadForest(Transform holder) {
            var root = NewRoot(holder, "LightBloomPad_Forest");
            var solid = new GameObject("Solid") { layer = LayerGround };
            solid.transform.SetParent(root.transform, false);
            var b = solid.AddComponent<BoxCollider2D>();
            b.size = new Vector2(3.2f, 0.6f);
            b.offset = new Vector2(0, -0.3f);
            var v1 = Put(root.transform, Sp(DG + "dungeon items.png", "dungeon items_2"), new Vector3(0, -0.35f, 0), 1f, "Ground", 30, C(0.6f, 1f, 0.9f));
            if (v1 != null) v1.transform.localScale = new Vector3(1.5f, 0.9f, 1f);
            var v2 = Put(root.transform, Glow, new Vector3(0, -0.2f, -0.05f), 0.9f, "Ground", 31, C(0.45f, 1f, 0.85f, 0.55f), mat: MatAdd);
            if (v2 != null) v2.transform.localScale = new Vector3(1.4f, 0.35f, 1f);
            var l = Light(root.transform, "Glow", new Vector3(0, 0, 0), C(0.45f, 1f, 0.85f), 0.2f, 4.5f, 0.3f);
            var pad = root.AddComponent<CATestLightBloomPlatform>();
            Set(pad, "solid", b);
            Set(pad, "visuals", new Object[] { v1, v2 });
            Set(pad, "glow", l);
            return root;
        }

        // 가시 씨앗: 기존 Electronic(좌우 왕복 + 닿으면 사망) 로직을 그대로 쓰고, 머리 높이로 날아다니는 숲 가시 뭉치로 꾸민다.
        // 원본의 비트리거 박스(1x1)는 플레이어를 몸으로 밀어내므로 트리거로 바꾸고, 판정 원을 작게(0.4) 해서
        // "웅크리면 머리 위로 지나가는" 퍼즐이 되게 한다.
        private static void DecorateThornSeed(GameObject go) {
            foreach (var bc in go.GetComponents<BoxCollider2D>()) {
                bc.isTrigger = true;
                bc.size = new Vector2(0.6f, 0.6f);
            }
            foreach (var cc in go.GetComponents<CircleCollider2D>()) cc.radius = 0.4f;
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.gravityScale = 0f;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) {
                sr.sprite = Sp(DG + "dungeon items.png", "dungeon items_20");
                sr.sharedMaterial = LitMat;
                sr.sortingLayerID = SL("Player");
                sr.sortingOrder = 25;
                sr.color = C(0.75f, 0.6f, 0.55f);
                sr.transform.localScale = Vector3.one * 0.32f;
                sr.transform.localPosition = new Vector3(0, -0.05f, 0);
                var am = sr.gameObject.AddComponent<CATestAmbientMotion>();
                am.swayAngle = 25f; am.swaySpeed = 2.5f;
            }
            Put(go.transform, Glow, go.transform.position + Vector3.back * 0.1f, 0.55f, "Player", 24, C(1f, 0.45f, 0.3f, 0.6f), mat: MatAdd);
            Light(go.transform, "SeedLight", go.transform.position, C(1f, 0.55f, 0.35f), 1.4f, 3.6f, 0.4f);
        }

        // 숲용 등불: 발광 버섯. 기존 LightObj(InteractLight)를 그대로 사용 → E 로 켜고 끔.
        private static void DecorateLampForest(GameObject go) {
            var il = go.GetComponent<InteractLight>();
            var sr = go.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
            if (sr != null) {
                sr.sprite = Sp(DG + "dungeon items.png", "dungeon items_14");
                sr.transform.localScale = Vector3.one * 0.85f;
                sr.transform.localPosition = new Vector3(0, 0.95f, 0);
                sr.sortingLayerID = SL("Item");
                sr.sortingOrder = 10;
            }
            var box = go.GetComponent<BoxCollider2D>();
            if (box != null) { box.size = new Vector2(1.2f, 2f); box.offset = new Vector2(0, 1f); }
            foreach (var l in go.GetComponentsInChildren<Light2D>(true)) {
                l.lightType = Light2D.LightType.Point;
                l.pointLightOuterRadius = 12f;
                l.pointLightInnerRadius = 1.5f;
                l.pointLightInnerAngle = 360f;
                l.pointLightOuterAngle = 360f;
                l.color = C(0.55f, 1f, 0.85f);
                l.intensity = 1.5f;
                l.transform.localPosition = new Vector3(0, 1.4f, 0);
                ApplyAllSortingLayers(l);
            }
            var glow = Put(go.transform, Glow, go.transform.position + new Vector3(0, 1.5f, -0.1f), 0.7f, "Item", 11, C(0.5f, 1f, 0.85f, 0.55f), mat: MatAdd);
            if (glow != null && il != null) {
                var so = new SerializedObject(il);
                var lp = so.FindProperty("lightPrefab").objectReferenceValue as GameObject;
                if (lp != null) glow.transform.SetParent(lp.transform, true);
            }
        }

        // ── Variant decorators ──
        private static void DecorateLamp(GameObject go) {
            var il = go.GetComponent<InteractLight>();
            var sr = go.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
            if (sr != null) {
                sr.sprite = Sp(UW + "Seaweed.png", "Seaweed_16");
                sr.transform.localScale = Vector3.one * 0.32f;
                sr.transform.localPosition = new Vector3(0, 0.75f, 0);
                sr.sortingLayerID = SL("Item");
                sr.sortingOrder = 10;
            }
            var box = go.GetComponent<BoxCollider2D>();
            if (box != null) { box.size = new Vector2(0.8f, 2.4f); box.offset = new Vector2(0, 1.1f); }
            foreach (var l in go.GetComponentsInChildren<Light2D>(true)) {
                l.lightType = Light2D.LightType.Point;
                l.pointLightOuterRadius = 11f;
                l.pointLightInnerRadius = 1f;
                l.pointLightInnerAngle = 360f;
                l.pointLightOuterAngle = 360f;
                l.color = C(1f, 0.85f, 0.45f);
                l.intensity = 1.3f;
                l.transform.localPosition = new Vector3(0, 1.5f, 0);
                ApplyAllSortingLayers(l);
            }
            var glow = Put(go.transform, Glow, go.transform.position + new Vector3(0, 1.55f, -0.1f), 0.5f, "Item", 11, C(1f, 0.85f, 0.4f, 0.55f), mat: MatAdd);
            if (glow != null && il != null) {
                // 등불이 꺼지면 발광도 같이 꺼지도록 lightPrefab(켜고 끄는 대상) 아래로 넣는다.
                var so = new SerializedObject(il);
                var lp = so.FindProperty("lightPrefab").objectReferenceValue as GameObject;
                if (lp != null) glow.transform.SetParent(lp.transform, true);
            }
        }

        private static void DecorateWisp(GameObject go) {
            var wisp = go.GetComponent<Wisp>();
            var so = new SerializedObject(wisp);
            var ghost = so.FindProperty("ghostTrm").objectReferenceValue as Transform;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) {
                sr.sprite = Glow;
                sr.sharedMaterial = MatAdd;
                sr.sortingLayerID = SL("Player");
                sr.sortingOrder = 30;
                sr.color = sr.transform == ghost ? C(0.5f, 1f, 0.9f, 0.85f) : C(0.55f, 1f, 0.9f, 0.9f);
                if (sr.transform != ghost) sr.transform.localScale = Vector3.one * 0.55f;
            }
            if (so.FindProperty("size").floatValue < 1f) Set(wisp, "size", 3.2f);
            var l = Light(go.transform, "WispLight", go.transform.position, C(0.45f, 1f, 0.85f), 0.9f, 4.5f, 0.2f);
            var am = go.AddComponent<CATestAmbientMotion>();
            am.bobHeight = 0.35f; am.bobSpeed = 1.3f; am.flickerLight = l; am.flickerAmount = 0.25f;
            Motes(go.transform, "WispDust", new Rect(-0.6f, -0.6f, 1.2f, 1.2f), -0.1f, MatAddDot, C(0.5f, 1f, 0.9f, 0.9f),
                C(0.8f, 1f, 1f, 0.6f), 8f, 0.04f, 0.12f, new Vector2(0f, 0.4f), 0.4f, 1.8f, "Player", 29).transform.localPosition = Vector3.zero;
            var bridge = go.AddComponent<CATestTrapBridge>();
            Set(bridge, "wisp", wisp);
            Set(bridge, "wispGhost", ghost);
        }

        private static void DecorateClam(GameObject go) {
            HidePlaceholder(go);
            var art = Load<GameObject>(SrcClamArt);
            if (art == null) return;
            var a = (GameObject)PrefabUtility.InstantiatePrefab(art);
            a.transform.SetParent(go.transform, false);
            a.transform.localPosition = new Vector3(0, -0.5f, 0.05f);
            a.transform.localScale = Vector3.one * 0.55f;
            var cp = a.GetComponent<ClamTrapPlayer>();
            if (cp != null) {
                cp.autoReopen = false;
                cp.closeOnContact = false; // 닫기는 CATestTrapBridge 가 함정 판정 상자 기준으로 직접 호출
                cp.activationLayers = 1 << LayerPlayer;
            }
            foreach (var sr in a.GetComponentsInChildren<SpriteRenderer>(true)) {
                sr.sortingLayerID = SL("Player");
                sr.sortingOrder += 5;
            }
            var bridge = go.AddComponent<CATestTrapBridge>();
            Set(bridge, "clamArt", cp);
            var box = go.GetComponent<BoxCollider2D>();
            if (box != null) { box.size = new Vector2(1.4f, 1f); box.offset = Vector2.zero; }
        }

        private static void DecorateJelly(GameObject go) {
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) {
                sr.sprite = Sp(ArtDir + "/CATest_Jelly.png");
                sr.sharedMaterial = MatAlpha;
                sr.sortingLayerID = SL("Player");
                sr.sortingOrder = 25;
                sr.color = C(0.75f, 1f, 1f, 0.9f);
                sr.transform.localScale = Vector3.one * 0.6f;
                sr.transform.localPosition = new Vector3(0, -0.45f, 0);
            }
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.gravityScale = 0f;
            var l = Light(go.transform, "JellyLight", go.transform.position, C(0.35f, 0.95f, 1f), 1.1f, 5f, 0.3f);
            var glow = Put(go.transform, Glow, go.transform.position + Vector3.back * 0.1f, 0.4f, "Player", 26, C(0.4f, 0.95f, 1f, 0.45f), mat: MatAdd);
            var am = glow != null ? glow.gameObject.AddComponent<CATestAmbientMotion>() : null;
            if (am != null) { am.flickerLight = l; am.flickerAmount = 0.35f; am.pulseSprite = glow; am.pulseAmount = 0.4f; }
        }

        private static void DecorateEye(GameObject go) {
            HidePlaceholder(go);
            var eye = go.GetComponent<WhaleEye>();
            var root = new GameObject("EyeVisual").transform;
            root.SetParent(go.transform, false);
            root.localPosition = new Vector3(0, 0, 0.3f);
            Put(root, Sp(ArtDir + "/CATest_EyeSocketRock.png"), root.position, 0.9f, "Default", 40, Color.white);
            var open = new GameObject("Open").transform;
            open.SetParent(root, false);
            Put(open, Sp(ArtDir + "/CATest_Eyeball.png"), open.position + Vector3.forward * 0.05f, 0.42f, "Default", 38, C(0.8f, 0.78f, 0.7f));
            var iris = Put(open, Sp(ArtDir + "/CATest_Iris.png"), open.position, 0.55f, "Default", 39, Color.white);
            var l = Light(root, "EyeLight", root.position + Vector3.back * 0.5f, C(1f, 0.4f, 0.25f), 0f, 9f, 0.5f);
            var bridge = go.AddComponent<CATestTrapBridge>();
            Set(bridge, "whaleEye", eye);
            Set(bridge, "eyeLid", open);
            Set(bridge, "eyeLight", l);
            Set(bridge, "eyeIris", iris);
        }

        private static void DecorateColumn(GameObject go, bool sea) {
            HidePlaceholder(go);
            go.transform.localScale = Vector3.one;
            var box = go.GetComponent<BoxCollider2D>();
            if (box != null) { box.size = new Vector2(8f, 30f); box.offset = new Vector2(0, 15f); }
            Set(go.GetComponent<WaterCurrent>(), "pushSpeed", new Vector2(0f, sea ? 7f : 9f));
            var mat = sea ? MatBubble : MatAddDot;
            var ps = Motes(go.transform, "Rising", new Rect(-3.5f, 0f, 7f, 1f), -0.3f, mat,
                sea ? C(0.7f, 0.95f, 1f, 0.7f) : C(1f, 1f, 0.9f, 0.5f), sea ? C(0.9f, 1f, 1f, 0.45f) : C(0.9f, 0.95f, 1f, 0.25f),
                sea ? 28f : 40f, sea ? 0.12f : 0.05f, sea ? 0.35f : 0.12f, new Vector2(0f, sea ? 6f : 11f), 0.6f, 3f, "Player", 12);
            ps.transform.localPosition = new Vector3(0, 0.5f, -0.3f);
            if (!sea) {
                var wind = Load<GameObject>(SrcWind);
                if (wind != null) {
                    var w = (GameObject)PrefabUtility.InstantiatePrefab(wind);
                    w.transform.SetParent(go.transform, false);
                    w.transform.localPosition = new Vector3(0, 6f, -0.5f);
                    w.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                }
                var leaves = Load<GameObject>(SrcLeaves);
                if (leaves != null) {
                    var lv = (GameObject)PrefabUtility.InstantiatePrefab(leaves);
                    lv.transform.SetParent(go.transform, false);
                    lv.transform.localPosition = new Vector3(0, 3f, -0.4f);
                }
            }
        }
    }
}
#endif
