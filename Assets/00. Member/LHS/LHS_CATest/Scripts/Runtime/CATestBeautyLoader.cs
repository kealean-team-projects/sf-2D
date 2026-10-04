using UnityEngine;
using UnityEngine.SceneManagement;

namespace LHS_CATest {
    /// <summary>
    /// 맵 씬이 로드되면 Resources/CATestBeauty/씬이름 프리팹(파티클·셰이더 추가 효과)을 꺼내 그 씬 안에 넣는다.
    /// 맵 씬 파일 자체는 바뀌지 않고, 씬이 내려가면 효과도 같이 사라진다.
    /// 효과를 빼고 싶으면 프리팹을 지우면 된다(없으면 아무것도 하지 않음).
    /// </summary>
    public static class CATestBeautyLoader {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode) {
            if (!scene.name.StartsWith("CATest_")) return;
            var prefab = Resources.Load<GameObject>("CATestBeauty/" + scene.name);
            if (prefab == null) return;
            var go = Object.Instantiate(prefab);
            go.name = prefab.name;
            SceneManager.MoveGameObjectToScene(go, scene);
        }
    }
}
