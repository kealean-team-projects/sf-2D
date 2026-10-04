using System.Reflection;
using _02._Script;
using _02._Script._01_Players;
using _02._Script._05_Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// CATest 에서 기력 UI 를 원본 StaminaUI 프리팹(Assets/03. Prefabs/StaminaUI.prefab)으로 바꾼다. 씬 파일은 건드리지 않는다.
    ///
    /// ■ 흐름
    ///  1) CATest_ 씬이 로드되면(sceneLoaded) UIManager 가 있는지 본다.
    ///  2) StaminaUI 프리팹을 전용 오버레이 캔버스 아래에 하나 만든다(DontDestroyOnLoad).
    ///  3) UIManager 의 private 필드 staminaUI 에 그 인스턴스를 넣는다(리플렉션).
    ///     → 원본 UIManager.RegisterPlayer 가 "staminaUI 가 있으면 그걸 쓰고, 없으면 글자 HUD" 로 되어 있으므로
    ///       이후 플레이어가 등록될 때 자동으로 게이지 쪽 경로를 탄다(기력 변화 이벤트 구독 + 머리 위 따라다니기).
    ///  4) 이미 플레이어가 글자 HUD 로 등록된 뒤라면, HUD 를 끄고 플레이어를 다시 등록한다.
    ///  5) UIManager 가 사라지면(타이틀로 돌아갈 때) 이 캔버스도 같이 지운다.
    /// </summary>
    public sealed class CATestStaminaUIHook : MonoBehaviour {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo StaminaUIField = typeof(UIManager).GetField("staminaUI", Flags);
        private static readonly FieldInfo StaminaHUDField = typeof(UIManager).GetField("staminaHUD", Flags);
        private static readonly FieldInfo CurrentPlayerField = typeof(UIManager).GetField("currentPlayer", Flags);

        private UIManager _owner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode) {
            if (!scene.name.StartsWith("CATest_")) return;
            Apply();
        }

        private static void Apply() {
            var ui = UIManager.Instance;
            if (ui == null || StaminaUIField == null) return;

            // 이미 이 훅이 만든 게이지가 연결돼 있으면 끝(스트리밍으로 맵 씬이 여러 번 로드되어도 한 번만)
            var current = StaminaUIField.GetValue(ui) as StaminaUI;
            if (current != null && current.gameObject.scene.IsValid() && current.GetComponentInParent<CATestStaminaUIHook>(true) != null) return;

            var refs = CATestUIRefs.Get();
            if (refs != null && !refs.useStaminaUI) return;

            // 원본으로 쓸 것 찾기: ① 씬에 직접 놓은 StaminaUI → ② CATest_UIRefs 의 프리팹 → ③ (에디터) 03. Prefabs/StaminaUI.prefab
            // ※ UIManager 의 staminaUI 칸에 "프리팹 에셋"이 들어가 있으면 화면에 없는 에셋을 갱신하게 돼서 아무것도 안 보인다 → 씬 오브젝트로 교체
            GameObject template = null;
            var fromScene = false;
            foreach (var s in FindObjectsByType<StaminaUI>(FindObjectsInactive.Include)) {
                if (s == null || !s.gameObject.scene.IsValid()) continue;
                if (s.GetComponentInParent<CATestStaminaUIHook>(true) != null) continue;
                template = s.gameObject;
                fromScene = true;
                break;
            }
            if (template == null && refs != null) template = refs.staminaUIPrefab;
#if UNITY_EDITOR
            if (template == null)
                template = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/StaminaUI.prefab");
#endif
            if (template == null) {
                Debug.LogWarning("[CATest] StaminaUI 를 찾지 못했습니다. CoreScene 에 놓거나 Resources/CATestUI/CATest_UIRefs 에 연결하세요.");
                return;
            }

            // 전용 캔버스: Screen Space - Overlay.
            // StaminaUI 는 LateUpdate 에서 transform.position 에 "화면 좌표"를 넣어 머리 위를 따라간다.
            // 그런데 씬 최상위(루트)에 놓인 캔버스는 위치를 Unity 가 화면 크기에 고정해서 움직일 수 없다
            // → 반드시 다른 오버레이 캔버스의 "자식"이어야 따라다닌다. 그래서 이 캔버스 아래로 옮긴다.
            var canvasGo = new GameObject("CATest_StaminaCanvas", typeof(RectTransform));
            DontDestroyOnLoad(canvasGo);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = refs != null ? refs.staminaSortingOrder : 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var hook = canvasGo.AddComponent<CATestStaminaUIHook>();
            hook._owner = ui;

            // 비활성 상태로 복제 → 크기/기준점을 자식용으로 고친 뒤 켠다(켤 때 StaminaUI.Awake 가 고친 크기를 기준으로 기억)
            if (fromScene) template.SetActive(false);
            var go = Instantiate(template, canvasGo.transform, false);
            if (!fromScene && go.activeSelf) go.SetActive(false);
            go.name = "StaminaUI";
            if (fromScene) Destroy(template); // 씬에 놓은 원래 것은 제거(복제본이 대신함)

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            var nested = go.GetComponent<Canvas>();
            if (nested != null) nested.overrideSorting = false;

            var staminaUI = go.GetComponent<StaminaUI>();
            if (staminaUI == null) {
                Debug.LogWarning("[CATest] StaminaUI 오브젝트에 StaminaUI 컴포넌트가 없습니다.");
                Destroy(canvasGo);
                return;
            }
            // 게이지가 클릭을 가로막지 않게
            var group = go.GetComponent<CanvasGroup>();
            if (group != null) { group.blocksRaycasts = false; group.interactable = false; }
            go.SetActive(true); // Awake 실행(카메라, 원래 크기 기억). 처음엔 투명(기력 가득)

            StaminaUIField.SetValue(ui, staminaUI);

            // 기존 글자 HUD 끄기
            var hud = StaminaHUDField?.GetValue(ui) as _02._Script.UI.StaminaHUD;
            if (hud != null) hud.gameObject.SetActive(false);

            // 플레이어가 이미 등록돼 있었다면 다시 등록 → 새 게이지에 기력 이벤트 연결 + 따라갈 대상 지정
            var player = CurrentPlayerField?.GetValue(ui) as Player;
            if (player != null) {
                player.SetHUD(null);
                ui.RegisterPlayer(player);
            }
            Debug.Log($"[CATest] 기력 게이지 연결됨 ({(fromScene ? "씬에 놓은 StaminaUI" : "StaminaUI 프리팹")} 사용)");
        }

        private void Update() {
            // UIManager 가 파괴되면(타이틀 복귀) 같이 정리 → 다음 게임 시작 때 새 UIManager 에 다시 붙는다
            if (_owner == null) Destroy(gameObject);
        }
    }
}
