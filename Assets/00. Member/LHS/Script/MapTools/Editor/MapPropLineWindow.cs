using System;
using UnityEditor;
using UnityEngine;

namespace MapTools.Editor {
    public sealed class MapPropLineWindow : EditorWindow {
        [SerializeField] private MapPropLineProfile profile;
        [SerializeField] private Transform parent;
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 endPosition = new(10f, 0f, 0f);
        [SerializeField] private int count = 10;
        [SerializeField] private Vector2 zOffsetRange = new(-1f, 1f);
        [SerializeField] private int seed = 12345;

        private void OnGUI() {
            EditorGUILayout.Space(6f);

            profile = (MapPropLineProfile)EditorGUILayout.ObjectField(
                "Profile",
                profile,
                typeof(MapPropLineProfile),
                false);

            parent = (Transform)EditorGUILayout.ObjectField(
                "Parent",
                parent,
                typeof(Transform),
                true);

            EditorGUILayout.Space(6f);

            startPosition = EditorGUILayout.Vector3Field("Start", startPosition);
            endPosition = EditorGUILayout.Vector3Field("End", endPosition);
            count = Mathf.Max(1, EditorGUILayout.IntField("Count", count));
            zOffsetRange = EditorGUILayout.Vector2Field("Z Offset Range", zOffsetRange);
            seed = EditorGUILayout.IntField("Seed", seed);

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Start From Selection"))
                    SetFromSelection(true);

                if (GUILayout.Button("End From Selection"))
                    SetFromSelection(false);
            }

            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(profile == null || profile.Prefab == null)) {
                if (GUILayout.Button("Generate", GUILayout.Height(32f)))
                    Generate();
            }
        }

        [MenuItem("Tools/Map Tools/Prop Line Generator")]
        private static void Open() {
            GetWindow<MapPropLineWindow>("Prop Line Generator");
        }

        private void SetFromSelection(bool start) {
            if (Selection.activeTransform == null)
                return;

            if (start)
                startPosition = Selection.activeTransform.position;
            else
                endPosition = Selection.activeTransform.position;

            Repaint();
        }

        private void Generate() {
            try {
                PropLineRequest request = new(
                    profile,
                    parent,
                    startPosition,
                    endPosition,
                    count,
                    zOffsetRange,
                    seed);

                PropLineGenerator.Generate(request);
            }
            catch (Exception exception) {
                Debug.LogException(exception);
            }
        }
    }
}