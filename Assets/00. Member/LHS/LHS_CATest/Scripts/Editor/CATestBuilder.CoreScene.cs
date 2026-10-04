#if UNITY_EDITOR
using _02._Script._05_Managers;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// CATest_CoreScene 생성.
    ///  - LHS의 최신 CoreScene(UIManager, EffectManager, SoundManager, UI 캔버스, 카메라)을 복제해서 시작한다.
    ///  - ChapterLoader는 끈다: Start에서 MainMenu를 로드하고, 챕터를 "언로드 후 로드"로 바꾸기 때문에 끊김 없는 스트리밍과 맞지 않음.
    ///  - 스트리밍 구조에서는 맵이 언로드되어도 살아 있어야 하는 것들을 모두 여기에 둔다:
    ///    Player / GameManager / SaveManager / Cinemachine 카메라 / 전역 Light2D / StageAudio / 스트리머 / 분위기 디렉터.
    /// </summary>
    public static partial class CATestBuilder {
        private const string PlayerPrefabPath = "Assets/03. Prefabs/Player.prefab";

        public static void BuildCoreScene() {
            EnsureFolder(ScenesDir);
            AssetDatabase.DeleteAsset(CoreScenePath);
            if (!AssetDatabase.CopyAsset(SourceCoreScene, CoreScenePath)) {
                Debug.LogError($"[LHS_CATest] {SourceCoreScene} 복제 실패");
                return;
            }
            var scene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);

            Camera cam = null;
            foreach (var go in scene.GetRootGameObjects()) {
                foreach (var loader in go.GetComponentsInChildren<_00._Member.LHS.Script.ChapterLoader>(true))
                    loader.gameObject.SetActive(false);
                if (cam == null) cam = go.GetComponentInChildren<Camera>(true);
                // 사망 연출: 화면 덮개(EffectManager.fade)를 빨강 → 검정, 시간 1 → 1.5초
                //  (CATestDeathPresenter 가 그 사이 플레이어를 투명하게 만든다. 원본 CoreScene 은 그대로, 복제본에서만 변경)
                foreach (var em in go.GetComponentsInChildren<EffectManager>(true)) {
                    var eso = new SerializedObject(em);
                    var fadeProp = eso.FindProperty("fade");
                    if (fadeProp != null && fadeProp.objectReferenceValue is UnityEngine.UI.Image img) {
                        img.color = new Color(0f, 0f, 0f, img.color.a);
                        EditorUtility.SetDirty(img);
                    }
                    var dur = eso.FindProperty("duration");
                    if (dur != null) dur.floatValue = 1.5f;
                    eso.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var systems = new GameObject("CATest_Systems");
            SceneManager.MoveGameObjectToScene(systems, scene);

            // 전역광 (맵마다 두지 않고 여기 하나)
            var gl = new GameObject("Global Light 2D");
            gl.transform.SetParent(systems.transform, false);
            var light = gl.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.8f;
            light.color = C(0.8f, 0.85f, 1f);
            ApplyAllSortingLayers(light);

            // Player (비활성으로 저장 → CATestBootstrap 이 매니저 초기화 후 켠다)
            var playerPrefab = Load<GameObject>(PlayerPrefabPath);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = new Vector3(CrevicePath[0].x, CrevicePath[0].y, 0f);
            player.SetActive(false);
            // CATest 전용 조정(프리팹 인스턴스 오버라이드 → 원본 Player.prefab 과 다른 맵에는 영향 없음)
            //  moveSpeed 10 → 7 : 피드백 "이속이 너무 빠르다". 걷기 점프 거리 ≈ 8, 달리기 ≈ 16 기준으로 레벨을 설계함.
            var playerComp = player.GetComponent<_02._Script._01_Players.Player>();
            Set(playerComp, "moveSpeed", 7f);

            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(systems.transform, false);
            var gm = gmGo.AddComponent<GameManager>();
            gm.player = player.transform;

            var smGo = new GameObject("SaveManager");
            SceneManager.MoveGameObjectToScene(smGo, scene); // DontDestroyOnLoad 대상이라 루트에 둔다
            smGo.AddComponent<SaveManager>();

            var audioGo = new GameObject("StageAudio");
            audioGo.transform.SetParent(systems.transform, false);
            var sa = audioGo.AddComponent<StageAudio>();
            Set(sa, "backgroundMusic", "BG_1");

            // 카메라: 원근 카메라 + CinemachinePositionComposer (기존 맵과 같은 FOV/거리)
            if (cam != null) {
                if (cam.GetComponent<CinemachineBrain>() == null) cam.gameObject.AddComponent<CinemachineBrain>();
                cam.orthographic = false;
                cam.fieldOfView = 12.9f;
                cam.farClipPlane = 5000f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = C(0.38f, 0.43f, 0.55f);
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }
            var vcamGo = new GameObject("CM_PlayerCamera");
            vcamGo.transform.SetParent(systems.transform, false);
            vcamGo.transform.position = new Vector3(CrevicePath[0].x, CrevicePath[0].y, -100f);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Target.TrackingTarget = player.transform;
            var lens = vcam.Lens;
            lens.FieldOfView = 12.9f;
            lens.NearClipPlane = 0.1f;
            lens.FarClipPlane = 5000f;
            vcam.Lens = lens;
            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 100f;
            composer.TargetOffset = new Vector3(0f, 1.5f, 0f);
            composer.Damping = new Vector3(1f, 1f, 1f);

            // 컷신 카메라: 평소 꺼져 있다가 켜지면 Cinemachine 이 부드럽게 블렌드 (CATestCutscene.Focus / Release)
            var cutGo = new GameObject("CM_Cutscene");
            cutGo.transform.SetParent(systems.transform, false);
            var cutTarget = new GameObject("CutsceneTarget").transform;
            cutTarget.SetParent(systems.transform, false);
            var cutVcam = cutGo.AddComponent<CinemachineCamera>();
            cutVcam.Target.TrackingTarget = cutTarget;
            cutVcam.Lens = lens;
            var cutComposer = cutGo.AddComponent<CinemachinePositionComposer>();
            cutComposer.CameraDistance = 110f;
            cutComposer.Damping = new Vector3(0.6f, 0.6f, 0.6f);
            var cutCtl = new GameObject("CutsceneCamera").AddComponent<CATestCutsceneCamera>();
            cutCtl.transform.SetParent(systems.transform, false);
            Set(cutCtl, "vcam", cutVcam);
            Set(cutCtl, "composer", cutComposer);
            Set(cutCtl, "target", cutTarget);
            cutGo.SetActive(false);

            // 사망 연출 (멈춤 → 투명 → 검정 → 체크포인트)
            new GameObject("DeathPresenter").AddComponent<CATestDeathPresenter>().transform.SetParent(systems.transform, false);

            // 스트리머
            var stGo = new GameObject("SceneStreamer");
            stGo.transform.SetParent(systems.transform, false);
            var streamer = stGo.AddComponent<CATestSceneStreamer>();
            var so = new SerializedObject(streamer);
            so.FindProperty("target").objectReferenceValue = player.transform;
            var zones = so.FindProperty("zones");
            zones.arraySize = 6;
            FillZone(zones.GetArrayElementAtIndex(0), "CATest_Forest0_Sunny", Forest0ScenePath, new Rect(-1965, -45, 705, 190), 40f, 80f);
            FillZone(zones.GetArrayElementAtIndex(1), "CATest_Forest1_LushFog", Forest1ScenePath, new Rect(-1290, -45, 830, 150), 40f, 80f);
            FillZone(zones.GetArrayElementAtIndex(2), "CATest_Forest2_DreamMountain", Forest2ScenePath, new Rect(-480, -35, 400, 190), 40f, 80f);
            FillZone(zones.GetArrayElementAtIndex(3), "CATest_DeepSea", DeepSeaScenePath, new Rect(225, -132, 875, 150), 52f, 95f);
            FillZone(zones.GetArrayElementAtIndex(4), "CATest_Twilight", TwilightScenePath, new Rect(1360, -40, 690, 150), 40f, 90f);
            FillZone(zones.GetArrayElementAtIndex(5), "CATest_Prologue", PrologueScenePath, new Rect(RoomX0 - 8f, -10f, RoomX1 - RoomX0 + 16f, 30f), 30f, 60f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // 분위기 디렉터
            var dirGo = new GameObject("AtmosphereDirector");
            dirGo.transform.SetParent(systems.transform, false);
            var dir = dirGo.AddComponent<CATestAtmosphereDirector>();
            Set(dir, "target", player.transform);
            Set(dir, "globalLight", light);
            Set(dir, "cam", cam);
            Set(dir, "composer", composer);

            // CATest 화면 UI (페이드 / 지역 이름 / 상호작용 E 안내 / 조작 힌트)
            var hudGo = new GameObject("CATest_HUD");
            hudGo.transform.SetParent(systems.transform, false);
            var hud = hudGo.AddComponent<CATestHUD>();
            Set(hud, "keySprite", Sp(ArtDir + "/CATest_KeyCap.png"));
            Set(hud, "softSprite", Sp(ArtDir + "/CATest_SoftPanel.png"));

            // 게임 UI: ESC 메뉴(설정/타이틀로) + 용 대화창/선택지 + 엔딩 화면 (UGUI)
            BuildGameUI(systems.transform);

            // 부트스트랩 + 테스트용 시작 지점
            var bsGo = new GameObject("CATest_Bootstrap");
            bsGo.transform.SetParent(systems.transform, false);
            var bs = bsGo.AddComponent<CATestBootstrap>();
            var bso = new SerializedObject(bs);
            bso.FindProperty("playerRoot").objectReferenceValue = player;
            bso.FindProperty("streamer").objectReferenceValue = streamer;
            bso.FindProperty("startIndex").intValue = 0;
            var sp = bso.FindProperty("startPoints");
            var points = new (string, Vector2)[] {
                ("F1  화창한 숲 (오프닝 컷신)", CrevicePath[0]),
                ("F2  개울 협곡", new Vector2(-1790f, 6.5f)),
                ("F3  굴뚝", new Vector2(-1702f, 18f)),
                ("F4  은빛 강", new Vector2(-1517f, 7.5f)),
                ("F5  응용 퍼즐", new Vector2(-1446f, 7.5f)),
                ("F6  울창한 숲 입구", new Vector2(-1250f, 6.5f)),
                ("F7  나무 굴뚝", new Vector2(-1012f, 18.5f)),
                ("F8  시간 문 레버", new Vector2(-856f, 13.5f)),
                ("F9  상자·압력판", new Vector2(-805f, 9.5f)),
                ("F10 안개 골짜기", new Vector2(-713f, -3.5f)),
                ("F11 안개 갈림길", new Vector2(-646f, -3.5f)),
                ("F12 몽환의 숲", new Vector2(-462f, 13.5f)),
                ("S+F1 잊힌 유적", new Vector2(-415f, 6.5f)),
                ("S+F2 상승 기류", new Vector2(-330f, 5.5f)),
                ("S+F3 산 기슭", new Vector2(-256f, 31f)),
                ("S+F4 산 굴뚝", new Vector2(-211.8f, 47.5f)),
                ("S+F5 산 정상", new Vector2(-160f, 86f)),
                ("S+F6 심해 착수", new Vector2(269f, -30f)),
                ("S+F7 잠긴 통로", new Vector2(398f, -58.5f)),
                ("S+F8 해구(등불)", new Vector2(474f, -86.8f)),
                ("S+F9 보스 입구", new Vector2(512f, -86.8f)),
                ("S+F10 보스 H3 절벽", new Vector2(700f, -84.5f)),
                ("S+F11 보스 H4 해구", new Vector2(776f, -88.5f)),
                ("S+F12 추격 직전", new Vector2(870f, -86.5f)),
                ("C+F1 굴 앞", new Vector2(1000f, -88.5f)),
                ("C+F2 상승 통로", new Vector2(1030f, -88.5f)),
                ("C+F3 빛이 드는 틈", new Vector2(1055f, -28.5f)),
                ("C+F4 백색 협곡 (마지막 챕터)", new Vector2(1418f, 2f)),
                ("C+F5 금 간 석판 다리", new Vector2(1494f, 14f)),
                ("C+F6 모래바람 평지", new Vector2(1582f, 3f)),
                ("C+F7 백색 성역", new Vector2(1646f, 3f)),
                ("C+F8 룬 순서 퍼즐", new Vector2(1694f, 10.5f)),
                ("C+F9 심판의 빛", new Vector2(1746f, 10.5f)),
                ("C+F10 떠다니는 석판", new Vector2(1797f, 10.5f)),
                ("C+F11 용의 둥지 앞 (엔딩 전 세이브)", new Vector2(TwPreDragonSave.x, 16f)),
                ("C+F12 용의 둥지 (엔딩 직전)", new Vector2(1905f, 16f)),
            };
            sp.arraySize = points.Length;
            for (var i = 0; i < points.Length; i++) {
                var e = sp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("label").stringValue = points[i].Item1;
                e.FindPropertyRelative("position").vector2Value = points[i].Item2;
            }
            bso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CoreScenePath);
        }

        private static void FillZone(SerializedProperty e, string name, string path, Rect bounds, float load, float unload) {
            e.FindPropertyRelative("sceneName").stringValue = name;
            e.FindPropertyRelative("scenePath").stringValue = path;
            e.FindPropertyRelative("bounds").rectValue = bounds;
            e.FindPropertyRelative("loadPadding").floatValue = load;
            e.FindPropertyRelative("unloadPadding").floatValue = unload;
        }
    }
}
#endif
