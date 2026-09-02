using UnityEditor;
using UnityEngine;

namespace _VFX_Lib._03._Scripts.Editor.HierarchyStyler {
    [InitializeOnLoad]
    internal static class HierarchyStylerDrawer {
        private static GUIStyle dividerLabelStyle;

        private static bool rebuildQueued;

        static HierarchyStylerDrawer() {
            EditorApplication
                    .hierarchyWindowItemByEntityIdOnGUI
                += HandleHierarchyItem;

            EditorApplication
                    .hierarchyChanged
                += HandleHierarchyChanged;
        }

        private static void HandleHierarchyChanged() {
            if (rebuildQueued)
                return;

            rebuildQueued = true;

            EditorApplication.delayCall
                += FlushPendingRebuild;
        }

        private static void FlushPendingRebuild() {
            rebuildQueued = false;

            HierarchyStylerStore
                .instance
                .InvalidateCache();

            EditorApplication
                .RepaintHierarchyWindow();
        }

        private static void HandleHierarchyItem(
            EntityId instanceId,
            Rect selectionRect) {
            if (Event.current.type
                != EventType.Repaint)
                return;

            var gameObject =
                EditorUtility
                        .EntityIdToObject(
                            instanceId)
                    as GameObject;

            if (gameObject == null)
                return;

            var store =
                HierarchyStylerStore.instance;

            var hasStyle =
                store.TryGetStyle(
                    instanceId,
                    out var style);

            if (hasStyle &&
                style.IsDivider) {
                DrawDivider(
                    gameObject,
                    selectionRect,
                    style);

                return;
            }

            if (hasStyle)
                DrawObjectTint(
                    gameObject,
                    selectionRect,
                    style.ObjectColor);

            if (store.TreeLines.Enabled)
                DrawTreeLines(
                    gameObject.transform,
                    selectionRect,
                    store.TreeLines);
        }

        private static void DrawObjectTint(
            GameObject gameObject,
            Rect selectionRect,
            Color color) {
            var backgroundRect =
                selectionRect;

            backgroundRect.xMin -= 2f;

            backgroundRect.xMax =
                EditorGUIUtility
                    .currentViewWidth;

            color.a =
                Mathf.Min(
                    color.a,
                    0.5f);

            if (Selection.Contains(gameObject)) color.a *= 0.45f;

            EditorGUI.DrawRect(
                backgroundRect,
                color);
        }

        private static void DrawDivider(
            GameObject gameObject,
            Rect selectionRect,
            HierarchyItemStyle style) {
            EnsureGUIStyles();

            var backgroundRect =
                selectionRect;

            backgroundRect.xMin = 0f;

            backgroundRect.xMax =
                EditorGUIUtility
                    .currentViewWidth;

            EditorGUI.DrawRect(
                backgroundRect,
                style.DividerBackgroundColor);

            // Divider 선택 상태 표시
            if (Selection.Contains(gameObject)) {
                var selectionIndicator =
                    new Rect(
                        0f,
                        selectionRect.y,
                        3f,
                        selectionRect.height);

                EditorGUI.DrawRect(
                    selectionIndicator,
                    new Color(
                        0.25f,
                        0.55f,
                        1f,
                        1f));
            }

            dividerLabelStyle
                    .normal
                    .textColor =
                style.DividerTextColor;

            var labelRect =
                backgroundRect;

            labelRect.xMin += 20f;
            labelRect.xMax -= 10f;

            var text =
                string.IsNullOrWhiteSpace(
                    style.DividerText)
                    ? "SECTION"
                    : style.DividerText;

            GUI.Label(
                labelRect,
                text,
                dividerLabelStyle);
        }

        private static void DrawTreeLines(
            Transform transform,
            Rect selectionRect,
            HierarchyTreeLineSettings settings) {
            var parent =
                transform.parent;

            if (parent == null)
                return;

            var branchX =
                selectionRect.x
                - settings.BranchOffset;

            var centerY =
                selectionRect.center.y;

            var isLastSibling =
                IsLastSibling(transform);

            // 현재 오브젝트의 세로선
            var verticalEnd =
                isLastSibling
                    ? centerY
                    : selectionRect.yMax;

            DrawVerticalLine(
                branchX,
                selectionRect.y,
                verticalEnd,
                settings);

            var horizontalEnd =
                selectionRect.x
                - settings.BranchEndPadding;

            DrawHorizontalLine(
                branchX,
                horizontalEnd,
                centerY,
                settings);

            var ancestor =
                parent;

            var ancestorX =
                branchX
                - settings.IndentWidth;

            while (ancestor != null &&
                   ancestor.parent != null) {
                if (!IsLastSibling(ancestor))
                    DrawVerticalLine(
                        ancestorX,
                        selectionRect.y,
                        selectionRect.yMax,
                        settings);

                ancestor =
                    ancestor.parent;

                ancestorX -=
                    settings.IndentWidth;
            }
        }

        private static bool IsLastSibling(
            Transform transform) {
            var parent =
                transform.parent;

            if (parent == null)
                return true;

            return transform
                       .GetSiblingIndex()
                   >= parent.childCount - 1;
        }

        private static void DrawVerticalLine(
            float x,
            float startY,
            float endY,
            HierarchyTreeLineSettings settings) {
            var height =
                endY - startY;

            if (height <= 0f)
                return;

            var rect =
                new Rect(
                    x - settings.Thickness * 0.5f,
                    startY,
                    settings.Thickness,
                    height);

            EditorGUI.DrawRect(
                rect,
                settings.Color);
        }

        private static void DrawHorizontalLine(
            float startX,
            float endX,
            float y,
            HierarchyTreeLineSettings settings) {
            var width =
                endX - startX;

            if (width <= 0f)
                return;

            var rect =
                new Rect(
                    startX,
                    y - settings.Thickness * 0.5f,
                    width,
                    settings.Thickness);

            EditorGUI.DrawRect(
                rect,
                settings.Color);
        }

        private static void EnsureGUIStyles() {
            if (dividerLabelStyle != null)
                return;

            dividerLabelStyle =
                new GUIStyle(
                    EditorStyles.boldLabel) {
                    alignment =
                        TextAnchor.MiddleCenter,

                    clipping =
                        TextClipping.Clip,

                    fontSize = 11
                };
        }
    }
}