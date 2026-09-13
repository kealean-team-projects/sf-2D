using System;
using MapTools;
using UnityEditor;
using UnityEngine;

namespace _00._Member.LHS.Script.MapTools.Editor
{
    public sealed class MapPropLineWindow : EditorWindow
    {
        [SerializeField]
        private MapPropLineProfile profile;

        [SerializeField]
        private Transform parent;

        [SerializeField]
        private PropPlacementMode placementMode;

        [SerializeField]
        private Vector3 startPosition;

        [SerializeField]
        private Vector3 endPosition =
            new Vector3(10f, 0f, 0f);

        [SerializeField]
        private Vector3 startControlPoint =
            new Vector3(3.33f, 0f, 0f);

        [SerializeField]
        private Vector3 endControlPoint =
            new Vector3(6.66f, 0f, 0f);

        [SerializeField]
        private int count = 10;

        [SerializeField]
        private float minimumDistance = 1f;

        [SerializeField]
        private Vector2 zOffsetRange =
            new Vector2(-1f, 1f);

        [SerializeField]
        private Vector2 scaleRange =
            new Vector2(0.9f, 1.1f);

        [SerializeField]
        private bool randomFlipX = true;

        [SerializeField]
        private int seed = 12345;

        [SerializeField]
        private bool overrideSortingLayer;

        [SerializeField]
        private int sortingLayerId;

        [SerializeField]
        private int baseSortingOrder = 100;

        [MenuItem(
            "Tools/Map Tools/Prop Line Generator")]
        private static void Open()
        {
            GetWindow<MapPropLineWindow>(
                "Prop Line Generator");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui +=
                OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -=
                OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);

            profile =
                (MapPropLineProfile)
                EditorGUILayout.ObjectField(
                    "Profile",
                    profile,
                    typeof(MapPropLineProfile),
                    false);

            parent =
                (Transform)
                EditorGUILayout.ObjectField(
                    "Parent",
                    parent,
                    typeof(Transform),
                    true);

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField(
                "Path",
                EditorStyles.boldLabel);

            placementMode =
                (PropPlacementMode)
                EditorGUILayout.EnumPopup(
                    "Placement Mode",
                    placementMode);

            startPosition =
                EditorGUILayout.Vector3Field(
                    "Start",
                    startPosition);

            endPosition =
                EditorGUILayout.Vector3Field(
                    "End",
                    endPosition);

            if (
                placementMode ==
                PropPlacementMode.Bezier)
            {
                startControlPoint =
                    EditorGUILayout.Vector3Field(
                        "Start Control",
                        startControlPoint);

                endControlPoint =
                    EditorGUILayout.Vector3Field(
                        "End Control",
                        endControlPoint);

                if (GUILayout.Button(
                        "Reset Curve Handles"))
                {
                    ResetCurveHandles();
                }
            }

            count =
                Mathf.Max(
                    1,
                    EditorGUILayout.IntField(
                        "Count",
                        count));

            minimumDistance =
                Mathf.Max(
                    0f,
                    EditorGUILayout.FloatField(
                        "Minimum Distance",
                        minimumDistance));

            EditorGUILayout.Space(8f);

            using (
                new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        "Start From Selection"))
                {
                    SetFromSelection(true);
                }

                if (GUILayout.Button(
                        "End From Selection"))
                {
                    SetFromSelection(false);
                }
            }

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField(
                "Randomization",
                EditorStyles.boldLabel);

            zOffsetRange =
                EditorGUILayout.Vector2Field(
                    "Z Offset Range",
                    zOffsetRange);

            scaleRange =
                EditorGUILayout.Vector2Field(
                    "Scale Range",
                    scaleRange);

            scaleRange.x =
                Mathf.Max(
                    0.01f,
                    scaleRange.x);

            scaleRange.y =
                Mathf.Max(
                    0.01f,
                    scaleRange.y);

            randomFlipX =
                EditorGUILayout.Toggle(
                    "Random Flip X",
                    randomFlipX);

            seed =
                EditorGUILayout.IntField(
                    "Seed",
                    seed);

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField(
                "Sorting",
                EditorStyles.boldLabel);

            overrideSortingLayer =
                EditorGUILayout.Toggle(
                    "Override Sorting Layer",
                    overrideSortingLayer);

            if (overrideSortingLayer)
            {
                DrawSortingSettings();
            }

            EditorGUILayout.Space(12f);

            using (
                new EditorGUI.DisabledScope(
                    profile == null ||
                    profile.Prefab == null))
            {
                if (GUILayout.Button(
                        "Generate",
                        GUILayout.Height(34f)))
                {
                    Generate();
                }
            }

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI(
            SceneView sceneView)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 newStart =
                Handles.PositionHandle(
                    startPosition,
                    Quaternion.identity);

            Vector3 newEnd =
                Handles.PositionHandle(
                    endPosition,
                    Quaternion.identity);

            Vector3 newStartControl =
                startControlPoint;

            Vector3 newEndControl =
                endControlPoint;

            if (
                placementMode ==
                PropPlacementMode.Bezier)
            {
                newStartControl =
                    Handles.PositionHandle(
                        startControlPoint,
                        Quaternion.identity);

                newEndControl =
                    Handles.PositionHandle(
                        endControlPoint,
                        Quaternion.identity);

                Handles.DrawLine(
                    newStart,
                    newStartControl);

                Handles.DrawLine(
                    newEnd,
                    newEndControl);

                Handles.DrawBezier(
                    newStart,
                    newEnd,
                    newStartControl,
                    newEndControl,
                    Handles.color,
                    null,
                    3f);
            }
            else
            {
                Handles.DrawLine(
                    newStart,
                    newEnd);
            }

            Handles.Label(
                newStart,
                "Start");

            Handles.Label(
                newEnd,
                "End");

            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RecordObject(
                this,
                "Move Prop Path");

            startPosition =
                newStart;

            endPosition =
                newEnd;

            startControlPoint =
                newStartControl;

            endControlPoint =
                newEndControl;

            Repaint();
        }

        private void ResetCurveHandles()
        {
            Undo.RecordObject(
                this,
                "Reset Curve Handles");

            Vector3 delta =
                endPosition -
                startPosition;

            startControlPoint =
                startPosition +
                delta / 3f;

            endControlPoint =
                startPosition +
                delta * (2f / 3f);

            SceneView.RepaintAll();
        }

        private void DrawSortingSettings()
        {
            SortingLayer[] layers =
                SortingLayer.layers;

            if (layers.Length == 0)
                return;

            string[] layerNames =
                new string[layers.Length];

            int currentIndex = 0;

            for (int i = 0;
                 i < layers.Length;
                 i++)
            {
                layerNames[i] =
                    layers[i].name;

                if (
                    layers[i].id ==
                    sortingLayerId)
                {
                    currentIndex =
                        i;
                }
            }

            currentIndex =
                EditorGUILayout.Popup(
                    "Sorting Layer",
                    currentIndex,
                    layerNames);

            sortingLayerId =
                layers[currentIndex].id;

            baseSortingOrder =
                Mathf.Clamp(
                    EditorGUILayout.IntField(
                        "Base Sorting Order",
                        baseSortingOrder),
                    -32768,
                    32767);
        }

        private void SetFromSelection(
            bool start)
        {
            if (
                Selection.activeTransform ==
                null)
            {
                return;
            }

            Undo.RecordObject(
                this,
                "Set Prop Path Position");

            if (start)
            {
                startPosition =
                    Selection.activeTransform.position;
            }
            else
            {
                endPosition =
                    Selection.activeTransform.position;
            }

            Repaint();
            SceneView.RepaintAll();
        }

        private void Generate()
        {
            try
            {
                PropLineRequest request =
                    new PropLineRequest(
                        profile,
                        parent,
                        placementMode,
                        startPosition,
                        endPosition,
                        startControlPoint,
                        endControlPoint,
                        count,
                        minimumDistance,
                        zOffsetRange,
                        scaleRange,
                        randomFlipX,
                        seed,
                        overrideSortingLayer,
                        sortingLayerId,
                        baseSortingOrder);

                PropLineGenerator.Generate(
                    request);
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);
            }
        }
    }
}