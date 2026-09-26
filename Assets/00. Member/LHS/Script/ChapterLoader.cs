using System;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00._Member.LHS.Script {
    public class ChapterLoader : MonoBehaviour {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset[] chapterSceneLists;
#endif

        [SerializeField, HideInInspector] private string[] chapterSceneNames = Array.Empty<string>();

        private bool isLoading;
        public bool IsLoading => isLoading;
        public static ChapterLoader Instance { get; private set; }

        public Scene CurrentScene { get; private set; }

        private void Awake() {
            if (Instance == null)
                Instance = this;
            else Destroy(gameObject);
        }

        private void Start() {
            if (Instance != this) return;
            SwitchScene("MainMenu").Forget();
        }

        private void OnDestroy() {
            if (Instance == this) Instance = null;
        }


#if UNITY_EDITOR
        private void OnValidate() {
            if (chapterSceneLists == null) return;

            Array.Resize(ref chapterSceneNames, chapterSceneLists.Length);

            for (var i = 0; i < chapterSceneLists.Length; i++)
                if (chapterSceneLists[i] != null) {
                    chapterSceneNames[i] = chapterSceneLists[i].name;
                }
                else {
                    chapterSceneNames[i] = string.Empty;
                    Debug.LogWarning($"{nameof(chapterSceneLists)}[{i}] hasn't any value.");
                }
        }
#endif

        private async UniTask SwitchScene(string sceneName) {
            if (isLoading) return;
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName)) {
                Debug.LogWarning($"챕터 씬 '{sceneName}'을 불러올 수 없습니다. Build Profiles의 Scene List를 확인해주세요.", this);
                return;
            }
            isLoading = true;
            try {
                if (CurrentScene.IsValid() && CurrentScene.isLoaded)
                    await SceneManager.UnloadSceneAsync(CurrentScene);

                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                CurrentScene = SceneManager.GetSceneByName(sceneName);
                SceneManager.SetActiveScene(CurrentScene);
            }
            finally {
                isLoading = false;
            }
        }

        public bool TryLoadChapter(int chapterNumber) {
            if (isLoading) return false;
            if (!TryGetChapterName(chapterNumber, out var sceneName)) return false;
            SwitchScene(sceneName).Forget();
            return true;
        }

        private bool TryGetChapterName(int chapterNumber, out string sceneName) {
            sceneName = null;
            if (chapterSceneNames == null || chapterNumber <= 0 || chapterNumber > chapterSceneNames.Length) {
                Debug.LogWarning($"등록되지 않은 챕터 번호입니다: {chapterNumber}", this);
                return false;
            }
            sceneName = chapterSceneNames[chapterNumber - 1];
            if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName)) return true;
            Debug.LogWarning($"챕터 {chapterNumber}의 씬이 비어 있거나 Scene List에 등록되지 않았습니다.", this);
            return false;
        }

        public async UniTaskVoid LoadChapter(int chapterNumber) {
            if (isLoading || !TryGetChapterName(chapterNumber, out var sceneName)) return;
            await SwitchScene(sceneName);
        }
    }
}
