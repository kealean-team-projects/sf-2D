using System;
using System.IO;
using System.Linq;
using _02._Script.Boss;
using NavMeshPlus.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BossNavigationBaker {
    private const string ScenePath = "Assets/00. Member/tyu/SecondMap.unity";
    private const string AssetPath = "Assets/00. Member/tyu/SecondMapBossNavigation.asset";
    private const string WholeMapAssetPath = "Assets/00. Member/tyu/SecondMapWholeNavigation.asset";
    private const string RequestPath = "Temp/BossNavigationBake.request";
    private const string ResultPath = "Temp/BossNavigationBake.result.txt";

    [InitializeOnLoadMethod]
    private static void RegisterRequestedBake() {
        if (File.Exists(RequestPath)) EditorApplication.update += RunRequestedBake;
    }

    private static void RunRequestedBake() {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorApplication.isPlaying = false;
            return;
        }

        EditorApplication.update -= RunRequestedBake;
        File.Delete(RequestPath);
        Bake();
    }

    [MenuItem("Tools/Boss/Bake SecondMap Navigation")]
    public static void Bake() {
        Bake(false);
    }

    [MenuItem("Tools/Navigation/전체/Bake SecondMap")]
    public static void BakeWholeMap() {
        Bake(true);
    }

    [MenuItem("Tools/Navigation/사용 범위/보스방")]
    public static void UseBossRoom() {
        SelectNavigation(false);
    }

    [MenuItem("Tools/Navigation/사용 범위/전체")]
    public static void UseWholeMap() {
        SelectNavigation(true);
    }

    private static string NavigationName(bool wholeMap) {
        return wholeMap ? "WholeMapNavigation" : "BossNavigation";
    }

    private static Scene OpenScene() {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        return scene.isLoaded ? scene : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
    }

    private static void SelectNavigation(bool wholeMap) {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            Debug.LogError("플레이를 정지한 뒤 전환하세요.");
            return;
        }

        var scene = OpenScene();
        var navigations = scene.GetRootGameObjects()
            .SelectMany(x => x.GetComponentsInChildren<BossNavigation>(true)).ToArray();
        var selected = navigations.SingleOrDefault(x => x.name == NavigationName(wholeMap));
        if (selected == null || selected.GetComponent<NavMeshSurface>().navMeshData == null) {
            Debug.LogError("선택한 범위를 먼저 Bake하세요.");
            return;
        }

        var room = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BossRoom>(true)).Single();
        foreach (var navigation in navigations) {
            if (navigation.name != NavigationName(false) && navigation.name != NavigationName(true)) continue;
            if (navigation == selected) navigation.GetComponent<NavMeshSurface>().enabled = true;
            navigation.gameObject.SetActive(navigation == selected);
        }

        var fields = new SerializedObject(room);
        fields.FindProperty("navigation").objectReferenceValue = selected;
        fields.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = selected.gameObject;
        Debug.Log($"Navigation 사용 범위: {(wholeMap ? "전체" : "보스방")}", selected);
    }

    private static void Bake(bool wholeMap) {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            Debug.LogError("플레이를 정지한 뒤 Bake하세요.");
            return;
        }

        try {
            var scene = OpenScene();
            var assetPath = wholeMap ? WholeMapAssetPath : AssetPath;
            var room = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BossRoom>(true)).Single();
            var roomFields = new SerializedObject(room);
            var area = (BoxCollider2D)roomFields.FindProperty("zones").FindPropertyRelative("area")
                .objectReferenceValue;
            var boss = (Boss)roomFields.FindProperty("boss").objectReferenceValue;
            var layers = roomFields.FindProperty("navigationObstacles").intValue;
            var navigation = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<BossNavigation>(true))
                .SingleOrDefault(x => x.name == NavigationName(wholeMap));
            if (navigation == null) {
                var go = new GameObject(NavigationName(wholeMap), typeof(NavMeshSurface));
                SceneManager.MoveGameObjectToScene(go, scene);
                navigation = go.AddComponent<BossNavigation>();
            }

            navigation.gameObject.SetActive(true);
            navigation.GetComponent<NavMeshSurface>().enabled = true;
            var body = boss.GetComponents<CircleCollider2D>().Where(x => x.enabled)
                .OrderByDescending(x => x.radius).First();
            var data = navigation.Bake(area, body, layers, wholeMap);
            var fields = new SerializedObject(navigation);
            var agentType = fields.FindProperty("agentType").intValue;
            var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
            filter.SetAreaCost(0, 1f);
            var samples = 0;
            var bounds = fields.FindProperty("flightBounds").boundsValue;
            for (var x = 0; x < 24; x++)
            for (var y = 0; y < 12; y++) {
                var p = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, (x + 0.5f) / 24f),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, (y + 0.5f) / 12f), 0f);
                if (NavMesh.SamplePosition(p, out _, 1f, filter)) samples++;
            }

            if (samples == 0) throw new InvalidOperationException("Bake 결과에서 이동 가능한 표면을 찾지 못했습니다.");

            var surface = navigation.GetComponent<NavMeshSurface>();
            surface.RemoveData();
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);
            if (existing != null) {
                EditorUtility.CopySerialized(data, existing);
                Object.DestroyImmediate(data);
                data = existing;
                EditorUtility.SetDirty(data);
            }
            else {
                AssetDatabase.CreateAsset(data, assetPath);
            }

            surface.navMeshData = data;
            surface.AddData();
            roomFields.FindProperty("navigation").objectReferenceValue = navigation;
            roomFields.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(navigation);
            EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = navigation.gameObject;
            SelectNavigation(wholeMap);
            var result =
                $"Bake saved: {assetPath}; walkable samples={samples}/288; agent={agentType}; bounds={bounds.min}..{bounds.max}";
            File.WriteAllText(ResultPath, result);
            Debug.Log(result, navigation);
        }
        catch (Exception exception) {
            File.WriteAllText(ResultPath, exception.ToString());
            Debug.LogException(exception);
        }
    }
}