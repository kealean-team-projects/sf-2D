using System;
using System.Collections;
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
            StartCoroutine(SwitchScene("MainMenu"));
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

        private IEnumerator SwitchScene(string sceneName) {
            if (isLoading) yield break;
            isLoading = true;

            if (CurrentScene.IsValid())
                yield return SceneManager.UnloadSceneAsync(CurrentScene);

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            CurrentScene = SceneManager.GetSceneByName(sceneName);

            isLoading = false;
        }

        public IEnumerator LoadChapter(int chapterNumber) {
            if (chapterNumber <= 0 || chapterNumber > chapterSceneNames.Length) yield break;
            yield return SwitchScene(chapterSceneNames[chapterNumber - 1]);
        }
    }
}