using _02._Script._03_TrapAndEnemy.Enemies;
using _02._Script._03_TrapAndEnemy.Traps;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LHS_CATest {
    /// <summary>
    /// 맵 씬이 로드될 때 원본 함정/적에 반복 효과음(CATestSfxLoop)을 자동으로 붙인다. 씬 파일은 바뀌지 않는다.
    ///  - WaterCurrent(바람 기둥 / 물살 기둥): 물속(y &lt; -20)이면 current_loop, 아니면 wind_column_loop
    ///  - ElectronicEnemy(전기 해파리 / 가시 씨앗): jelly_hum
    /// </summary>
    public static class CATestAudioHooks {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode) {
            if (!scene.name.StartsWith("CATest_")) return;
            foreach (var root in scene.GetRootGameObjects()) {
                foreach (var w in root.GetComponentsInChildren<WaterCurrent>(true))
                    Add(w.gameObject, w.transform.position.y < -20f ? "current_loop" : "wind_column_loop", 16f);
                foreach (var e in root.GetComponentsInChildren<ElectronicEnemy>(true))
                    Add(e.gameObject, "jelly_hum", 10f);
            }
        }

        private static void Add(GameObject go, string key, float range) {
            if (go.GetComponent<CATestSfxLoop>() != null) return;
            var l = go.AddComponent<CATestSfxLoop>();
            l.key = key;
            l.range = range;
        }
    }
}
