using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _VFX_Lib._03._Scripts.Editor.HierarchyStyler
{
    internal sealed class HierarchyStylerWindow
        : EditorWindow
    {
        private enum Page
        {
            SelectedObject,
            TreeLines
        }

        private Page currentPage;

        private bool isDivider;

        private Color objectColor =
            HierarchyStylerDefaults.ObjectColor;

        private string dividerText =
            HierarchyStylerDefaults.DividerText;

        private Color dividerBackgroundColor =
            HierarchyStylerDefaults.DividerBackgroundColor;

        private Color dividerTextColor =
            HierarchyStylerDefaults.DividerTextColor;

        [MenuItem(
            "Tools/Hierarchy Styler")]
        private static void OpenWindow()
        {
            HierarchyStylerWindow window =
                GetWindow<HierarchyStylerWindow>();

            window.titleContent =
                new GUIContent(
                    "Hierarchy Styler");

            window.minSize =
                new Vector2(
                    320f,
                    300f);
        }

        [MenuItem(
            "GameObject/Hierarchy Styler/Create Divider",
            false,
            0)]
        private static void CreateDividerMenu()
        {
            CreateDivider();
        }

        [MenuItem(
            "GameObject/Hierarchy Styler/Edit Selected",
            false,
            1)]
        private static void EditSelectedMenu()
        {
            OpenWindow();
        }

        private void OnEnable()
        {
            Selection.selectionChanged
                += HandleSelectionChanged;

            LoadSelectedStyle();
        }

        private void OnDisable()
        {
            Selection.selectionChanged
                -= HandleSelectionChanged;
        }


        private void HandleSelectionChanged()
        {
            LoadSelectedStyle();

            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);

            currentPage =
                (Page)GUILayout.Toolbar(
                    (int)currentPage,
                    new[]
                    {
                        "Selected",
                        "Tree Lines"
                    });

            EditorGUILayout.Space(10f);

            switch (currentPage)
            {
                case Page.SelectedObject:

                    DrawSelectedObjectPage();

                    break;

                case Page.TreeLines:

                    DrawTreeLinePage();

                    break;
            }
        }

        private void DrawSelectedObjectPage()
        {
            GameObject selected =
                Selection.activeGameObject;

            EditorGUILayout.LabelField(
                "Hierarchy Item",
                EditorStyles.boldLabel);

            EditorGUILayout.Space(5f);

            if (selected == null)
            {
                EditorGUILayout.HelpBox(
                    "Hierarchy에서 GameObject를 선택하세요.",
                    MessageType.Info);

                EditorGUILayout.Space(8f);

                if (GUILayout.Button(
                        "Create Divider",
                        GUILayout.Height(28f)))
                {
                    CreateDivider();
                }

                DrawMaintenanceSection();

                return;
            }

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.ObjectField(
                "Selected",
                selected,
                typeof(GameObject),
                true);

            EditorGUI.EndDisabledGroup();

            if (Selection.gameObjects.Length > 1)
            {
                EditorGUILayout.LabelField(
                    $"Selected Objects: {Selection.gameObjects.Length}");
            }

            if (string.IsNullOrEmpty(
                    selected.scene.path))
            {
                EditorGUILayout.HelpBox(
                    "Hierarchy 스타일을 저장하려면 Scene을 먼저 저장해야 합니다.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10f);

            isDivider =
                EditorGUILayout.Toggle(
                    "Divider",
                    isDivider);

            EditorGUILayout.Space(5f);

            if (isDivider)
            {
                DrawDividerSettings();
            }
            else
            {
                DrawObjectSettings();
            }

            EditorGUILayout.Space(12f);

            using (new EditorGUILayout
                       .HorizontalScope())
            {
                if (GUILayout.Button(
                        "Apply",
                        GUILayout.Height(26f)))
                {
                    ApplyToSelection();
                }

                if (GUILayout.Button(
                        "Clear",
                        GUILayout.Height(26f)))
                {
                    ClearSelection();
                }
            }

            EditorGUILayout.Space(15f);

            EditorGUILayout.LabelField(
                "Divider",
                EditorStyles.boldLabel);

            if (GUILayout.Button(
                    "Create Divider",
                    GUILayout.Height(28f)))
            {
                CreateDivider();
            }

            DrawMaintenanceSection();
        }

        private void DrawMaintenanceSection()
        {
            EditorGUILayout.Space(15f);

            EditorGUILayout.LabelField(
                "Maintenance",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "삭제된 오브젝트에 남아있는 스타일 정보를 정리합니다. ",
                MessageType.None);

            if (GUILayout.Button(
                    "Clean Up Unused Styles",
                    GUILayout.Height(26f)))
            {
                CleanUpUnusedStyles();
            }
        }

        private void DrawObjectSettings()
        {
            EditorGUILayout.LabelField(
                "Object Highlight",
                EditorStyles.boldLabel);

            objectColor =
                EditorGUILayout.ColorField(
                    "Row Color",
                    objectColor);

            EditorGUILayout.HelpBox(
                "Row Color의 Alpha를 낮게 두면 Unity 기본 글자와 아이콘을 유지하면서 색만 자연스럽게 표시됩니다.",
                MessageType.None);
        }

        private void DrawDividerSettings()
        {
            EditorGUILayout.LabelField(
                "Divider Settings",
                EditorStyles.boldLabel);

            dividerText =
                EditorGUILayout.TextField(
                    "Text",
                    dividerText);

            dividerBackgroundColor =
                EditorGUILayout.ColorField(
                    "Background",
                    dividerBackgroundColor);

            dividerTextColor =
                EditorGUILayout.ColorField(
                    "Text Color",
                    dividerTextColor);
        }

        private void DrawTreeLinePage()
        {
            HierarchyStylerStore store =
                HierarchyStylerStore.instance;

            HierarchyTreeLineSettings settings =
                store.TreeLines;

            EditorGUILayout.LabelField(
                "Parent / Child Lines",
                EditorStyles.boldLabel);

            EditorGUILayout.Space(5f);

            EditorGUI.BeginChangeCheck();

            settings.Enabled =
                EditorGUILayout.Toggle(
                    "Show Tree Lines",
                    settings.Enabled);

            using (new EditorGUI
                       .DisabledScope(
                           !settings.Enabled))
            {
                settings.Color =
                    EditorGUILayout.ColorField(
                        "Line Color",
                        settings.Color);

                settings.Thickness =
                    EditorGUILayout.Slider(
                        "Thickness",
                        settings.Thickness,
                        1f,
                        4f);

                settings.IndentWidth =
                    EditorGUILayout.Slider(
                        "Indent Width",
                        settings.IndentWidth,
                        8f,
                        30f);

                settings.BranchOffset =
                    EditorGUILayout.Slider(
                        "Branch Position",
                        settings.BranchOffset,
                        2f,
                        30f);

                settings.BranchEndPadding =
                    EditorGUILayout.Slider(
                        "End Padding",
                        settings.BranchEndPadding,
                        0f,
                        12f);
            }

            if (EditorGUI.EndChangeCheck())
            {
                store.SaveChanges();
            }

            EditorGUILayout.Space(10f);

            EditorGUILayout.HelpBox(
                "만들고나서 보니까 선",
                MessageType.Info);

            EditorGUILayout.Space(8f);

            if (GUILayout.Button(
                    "Reset Tree Line Settings",
                    GUILayout.Height(26f)))
            {
                store.ResetTreeLineSettings();
            }
        }

        private void ApplyToSelection()
        {
            GameObject[] selectedObjects =
                Selection.gameObjects;

            if (selectedObjects.Length == 0)
                return;

            HierarchyStylerStore store =
                HierarchyStylerStore.instance;

            int failedCount = 0;

            foreach (GameObject gameObject
                     in selectedObjects)
            {
                HierarchyItemStyle style =
                    store.GetOrCreateStyle(
                        gameObject);

                if (style == null)
                {
                    failedCount++;
                    continue;
                }

                style.IsDivider =
                    isDivider;

                style.ObjectColor =
                    objectColor;

                style.DividerText =
                    dividerText;

                style.DividerBackgroundColor =
                    dividerBackgroundColor;

                style.DividerTextColor =
                    dividerTextColor;
            }

            store.SaveChanges();

            if (failedCount > 0)
            {
                ShowNotification(
                    new GUIContent(
                        "Scene을 먼저 저장하세요."));
            }
        }

        private void ClearSelection()
        {
            GameObject[] selectedObjects =
                Selection.gameObjects;

            if (selectedObjects.Length == 0)
                return;

            HierarchyStylerStore store =
                HierarchyStylerStore.instance;

            foreach (GameObject gameObject
                     in selectedObjects)
            {
                store.RemoveStyle(
                    gameObject);
            }

            store.SaveChanges();

            LoadSelectedStyle();
        }

        private void LoadSelectedStyle()
        {
            GameObject selected =
                Selection.activeGameObject;

            if (selected == null)
                return;

            if (!HierarchyStylerStore
                    .instance
                    .TryGetStyle(
                        selected,
                        out HierarchyItemStyle style))
            {
                ResetLocalStyle();

                return;
            }

            isDivider =
                style.IsDivider;

            objectColor =
                style.ObjectColor;

            dividerText =
                style.DividerText;

            dividerBackgroundColor =
                style.DividerBackgroundColor;

            dividerTextColor =
                style.DividerTextColor;
        }

        private void ResetLocalStyle()
        {
            isDivider = false;

            objectColor =
                HierarchyStylerDefaults.ObjectColor;

            dividerText =
                HierarchyStylerDefaults.DividerText;

            dividerBackgroundColor =
                HierarchyStylerDefaults.DividerBackgroundColor;

            dividerTextColor =
                HierarchyStylerDefaults.DividerTextColor;
        }

        private static void CleanUpUnusedStyles()
        {
            bool confirmed =
                EditorUtility.DisplayDialog(
                    "Hierarchy Styler",
                    "삭제된 오브젝트의 스타일 정보를 정리합니다.\n" +
                    "닫혀있는 Scene의 오브젝트가 있다면 먼저 열어주세요.\n\n" +
                    "계속하시겠습니까?",
                    "정리",
                    "취소");

            if (!confirmed)
                return;

            int removedCount =
                HierarchyStylerStore
                    .instance
                    .CleanUpUnresolvedStyles();

            EditorUtility.DisplayDialog(
                "Hierarchy Styler",
                removedCount > 0
                    ? $"{removedCount}개의 사용하지 않는 스타일을 정리했습니다."
                    : "정리할 항목이 없습니다.",
                "확인");
        }

        private static void CreateDivider()
        {
            Scene activeScene =
                SceneManager.GetActiveScene();

            if (!activeScene.IsValid() ||
                string.IsNullOrEmpty(
                    activeScene.path))
            {
                EditorUtility.DisplayDialog(
                    "Hierarchy Styler",
                    "Divider를 만들기 전에 Scene을 먼저 저장해주세요.",
                    "확인");

                return;
            }

            Transform selectedTransform =
                Selection.activeTransform;

            GameObject divider =
                new GameObject(
                    "Hierarchy Divider");

            Undo.RegisterCreatedObjectUndo(
                divider,
                "Create Hierarchy Divider");

            // 선택한 오브젝트 바로 아래에 생성함
            if (selectedTransform != null &&
                selectedTransform.gameObject.scene
                == divider.scene)
            {
                Transform parent =
                    selectedTransform.parent;

                if (parent != null)
                {
                    Undo.SetTransformParent(
                        divider.transform,
                        parent,
                        "Create Hierarchy Divider");
                }

                divider.transform.SetSiblingIndex(
                    selectedTransform
                        .GetSiblingIndex() + 1);
            }

            HierarchyStylerStore store =
                HierarchyStylerStore.instance;

            HierarchyItemStyle style =
                store.GetOrCreateStyle(
                    divider);

            if (style != null)
            {
                style.IsDivider = true;

                style.DividerText =
                    "NEW SECTION";

                style.DividerBackgroundColor =
                    HierarchyStylerDefaults.DividerBackgroundColor;

                style.DividerTextColor =
                    HierarchyStylerDefaults.DividerTextColor;

                store.SaveChanges();
            }

            EditorSceneManager.MarkSceneDirty(
                divider.scene);

            Selection.activeGameObject =
                divider;
        }
    }
}