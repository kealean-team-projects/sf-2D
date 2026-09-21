using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00._Member.LHS.Script {
    public class ChapterLoader : MonoBehaviour {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset[] chapterSceneLists;
#endif

        private string[] chapterSceneNames;

        private bool isLoading;
        public static ChapterLoader Instance { get; private set; }

        public Scene CurrentScene { get; private set; }

        private void Awake() {
            if (Instance == null)
                Instance = this;
            else Destroy(gameObject);
        }

        private void Start() {
            SwitchScene("MainMenu").Forget();
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
            isLoading = true;

            if (CurrentScene.IsValid())
                await SceneManager.UnloadSceneAsync(CurrentScene);

            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            CurrentScene = SceneManager.GetSceneByName(sceneName);

            isLoading = false;
        }

        public async UniTaskVoid LoadChapter(int chapterNumber) {
            if (chapterNumber <= 0 || chapterNumber > chapterSceneNames.Length) return;
            await SwitchScene(chapterSceneNames[chapterNumber - 1]);
        }
    }
}