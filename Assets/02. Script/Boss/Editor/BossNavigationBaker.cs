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

public static class BossNavigationBaker
{
    private const string ScenePath = "Assets/00. Member/tyu/SecondMap.unity";
    private const string AssetPath = "Assets/00. Member/tyu/SecondMapBossNavigation.asset";
    private const string RequestPath = "Temp/BossNavigationBake.request";
    private const string ResultPath = "Temp/BossNavigationBake.result.txt";

    [InitializeOnLoadMethod]
    private static void RegisterRequestedBake()
    {
        if (File.Exists(RequestPath)) EditorApplication.update += RunRequestedBake;
    }

    private static void RunRequestedBake()
    {
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
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            Debug.LogError("플레이를 정지한 뒤 Bake하세요.");
            return;
        }
        try {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var room = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BossRoom>(true)).Single();
            var roomFields = new SerializedObject(room);
            var area = (BoxCollider2D)roomFields.FindProperty("zones").FindPropertyRelative("area").objectReferenceValue;
            var boss = (Boss)roomFields.FindProperty("boss").objectReferenceValue;
            var layers = roomFields.FindProperty("navigationObstacles").intValue;
            var navigation = (BossNavigation)roomFields.FindProperty("navigation").objectReferenceValue;
            if (navigation == null) {
                var go = new GameObject("BossNavigation", typeof(NavMeshSurface));
                SceneManager.MoveGameObjectToScene(go, scene);
                navigation = go.AddComponent<BossNavigation>();
            }

            var data = navigation.Bake(area, boss.GetComponent<Collider2D>(), layers);
            var fields = new SerializedObject(navigation);
            int agentType = fields.FindProperty("agentType").intValue;
            var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
            filter.SetAreaCost(0, 1f);
            int samples = 0;
            var bounds = fields.FindProperty("flightBounds").boundsValue;
            for (int x = 0; x < 24; x++)
            for (int y = 0; y < 12; y++) {
                var p = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, (x + 0.5f) / 24f),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, (y + 0.5f) / 12f), 0f);
                if (NavMesh.SamplePosition(p, out _, 1f, filter)) samples++;
            }
            if (samples == 0) throw new InvalidOperationException("Bake 결과에서 이동 가능한 표면을 찾지 못했습니다.");

            var surface = navigation.GetComponent<NavMeshSurface>();
            surface.RemoveData();
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(AssetPath);
            if (existing != null) {
                EditorUtility.CopySerialized(data, existing);
                UnityEngine.Object.DestroyImmediate(data);
                data = existing;
                EditorUtility.SetDirty(data);
            }
            else AssetDatabase.CreateAsset(data, AssetPath);
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
            string result = $"Bake saved: {AssetPath}; walkable samples={samples}/288; agent={agentType}; bounds={bounds.min}..{bounds.max}";
            File.WriteAllText(ResultPath, result);
            Debug.Log(result, navigation);
        }
        catch (Exception exception) {
            File.WriteAllText(ResultPath, exception.ToString());
            Debug.LogException(exception);
        }
    }
}
