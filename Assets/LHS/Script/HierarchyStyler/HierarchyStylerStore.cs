using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _VFX_Lib._03._Scripts.Editor.HierarchyStyler
{
    [Serializable]
    internal sealed class HierarchyItemStyle
    {
        [SerializeField]
        private string globalObjectId;

        [SerializeField]
        private bool isDivider;

        [SerializeField]
        private Color objectColor =
            HierarchyStylerDefaults.ObjectColor;

        [SerializeField]
        private string dividerText =
            HierarchyStylerDefaults.DividerText;

        [SerializeField]
        private Color dividerBackgroundColor =
            HierarchyStylerDefaults.DividerBackgroundColor;

        [SerializeField]
        private Color dividerTextColor =
            HierarchyStylerDefaults.DividerTextColor;

        internal string GlobalObjectId
        {
            get => globalObjectId;
            set => globalObjectId = value;
        }

        internal bool IsDivider
        {
            get => isDivider;
            set => isDivider = value;
        }

        internal Color ObjectColor
        {
            get => objectColor;
            set => objectColor = value;
        }

        internal string DividerText
        {
            get => dividerText;
            set => dividerText = value;
        }

        internal Color DividerBackgroundColor
        {
            get => dividerBackgroundColor;
            set => dividerBackgroundColor = value;
        }

        internal Color DividerTextColor
        {
            get => dividerTextColor;
            set => dividerTextColor = value;
        }
    }

    [Serializable]
    internal sealed class HierarchyTreeLineSettings
    {
        [SerializeField]
        private bool enabled = true;

        [SerializeField]
        private Color color =
            new Color(0.7f, 0.7f, 0.7f, 0.45f);

        [SerializeField]
        private float thickness = 1f;

        [SerializeField]
        private float indentWidth = 14f;

        [SerializeField]
        private float branchOffset = 11f;

        [SerializeField]
        private float branchEndPadding = 2f;

        internal bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        internal Color Color
        {
            get => color;
            set => color = value;
        }

        internal float Thickness
        {
            get => thickness;
            set => thickness = Mathf.Clamp(value, 1f, 4f);
        }

        internal float IndentWidth
        {
            get => indentWidth;
            set => indentWidth = Mathf.Clamp(value, 8f, 30f);
        }

        internal float BranchOffset
        {
            get => branchOffset;
            set => branchOffset = Mathf.Clamp(value, 2f, 30f);
        }

        internal float BranchEndPadding
        {
            get => branchEndPadding;
            set => branchEndPadding = Mathf.Clamp(value, 0f, 12f);
        }

        internal void ResetToDefaults()
        {
            enabled = true;

            color =
                new Color(
                    0.7f,
                    0.7f,
                    0.7f,
                    0.45f);

            thickness = 1f;
            indentWidth = 14f;
            branchOffset = 11f;
            branchEndPadding = 2f;
        }
    }

    [FilePath(
        "ProjectSettings/HierarchyStyler.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class HierarchyStylerStore
        : ScriptableSingleton<HierarchyStylerStore>
    {
        [SerializeField]
        private List<HierarchyItemStyle> itemStyles =
            new List<HierarchyItemStyle>();

        [SerializeField]
        private HierarchyTreeLineSettings treeLines =
            new HierarchyTreeLineSettings();

        [NonSerialized]
        private Dictionary<EntityId, HierarchyItemStyle>
            styleByInstanceId;

        [NonSerialized]
        private bool cacheDirty = true;

        internal HierarchyTreeLineSettings TreeLines
        {
            get
            {
                treeLines ??=
                    new HierarchyTreeLineSettings();

                return treeLines;
            }
        }

        internal bool TryGetStyle(
            EntityId entityId,
            out HierarchyItemStyle style)
        {
            EnsureCache();

            return styleByInstanceId.TryGetValue(
                entityId,
                out style);
        }

        internal bool TryGetStyle(
            GameObject gameObject,
            out HierarchyItemStyle style)
        {
            style = null;

            if (gameObject == null)
                return false;

            return TryGetStyle(
                
                gameObject.GetEntityId(),
                out style);
        }

        internal HierarchyItemStyle GetOrCreateStyle(
            GameObject gameObject)
        {
            if (!TryGetPersistentId(
                    gameObject,
                    out string objectId))
            {
                return null;
            }

            HierarchyItemStyle style =
                itemStyles.Find(
                    item =>
                        item.GlobalObjectId == objectId);

            if (style == null)
            {
                style =
                    new HierarchyItemStyle
                    {
                        GlobalObjectId = objectId
                    };

                itemStyles.Add(style);
            }

            EnsureCache();

            styleByInstanceId[
                gameObject.GetEntityId()] = style;

            return style;
        }

        internal bool RemoveStyle(
            GameObject gameObject)
        {
            if (!TryGetPersistentId(
                    gameObject,
                    out string objectId))
            {
                return false;
            }

            int removedCount =
                itemStyles.RemoveAll(
                    item =>
                        item.GlobalObjectId == objectId);

            if (styleByInstanceId != null)
            {
                styleByInstanceId.Remove(
                    gameObject.GetEntityId());
            }

            return removedCount > 0;
        }

        internal int CleanUpUnresolvedStyles()
        {
            if (itemStyles == null ||
                itemStyles.Count == 0)
            {
                return 0;
            }

            List<GlobalObjectId> ids =
                new List<GlobalObjectId>();

            List<HierarchyItemStyle> candidates =
                new List<HierarchyItemStyle>();

            foreach (HierarchyItemStyle style
                     in itemStyles)
            {
                if (style == null ||
                    string.IsNullOrEmpty(
                        style.GlobalObjectId))
                {
                    continue;
                }

                if (!GlobalObjectId.TryParse(
                        style.GlobalObjectId,
                        out GlobalObjectId id))
                {
                    continue;
                }

                ids.Add(id);
                candidates.Add(style);
            }

            if (ids.Count == 0)
                return 0;

            GlobalObjectId[] idArray =
                ids.ToArray();

            Object[] objects =
                new Object[idArray.Length];

            GlobalObjectId
                .GlobalObjectIdentifiersToObjectsSlow(
                    idArray,
                    objects);

            int removedCount = 0;

            for (int i = 0;
                 i < objects.Length;
                 i++)
            {
                if (objects[i] is GameObject)
                    continue;

                itemStyles.Remove(candidates[i]);
                removedCount++;
            }

            if (removedCount > 0)
            {
                InvalidateCache();
                SaveChanges();
            }

            return removedCount;
        }

        internal void SaveChanges()
        {
            Save(true);

            EditorApplication
                .RepaintHierarchyWindow();
        }

        internal void InvalidateCache()
        {
            cacheDirty = true;
        }

        internal void ResetTreeLineSettings()
        {
            TreeLines.ResetToDefaults();

            SaveChanges();
        }

        private void EnsureCache()
        {
            styleByInstanceId ??=
                new Dictionary<EntityId, HierarchyItemStyle>();

            if (!cacheDirty)
                return;

            cacheDirty = false;

            styleByInstanceId.Clear();

            if (itemStyles == null ||
                itemStyles.Count == 0)
            {
                return;
            }

            List<GlobalObjectId> ids =
                new List<GlobalObjectId>();

            List<HierarchyItemStyle> validStyles =
                new List<HierarchyItemStyle>();

            foreach (HierarchyItemStyle style
                     in itemStyles)
            {
                if (style == null)
                    continue;

                if (string.IsNullOrEmpty(
                        style.GlobalObjectId))
                {
                    continue;
                }

                if (!GlobalObjectId.TryParse(
                        style.GlobalObjectId,
                        out GlobalObjectId id))
                {
                    continue;
                }

                ids.Add(id);
                validStyles.Add(style);
            }

            if (ids.Count == 0)
                return;

            GlobalObjectId[] idArray =
                ids.ToArray();

            Object[] objects =
                new Object[idArray.Length];

            GlobalObjectId
                .GlobalObjectIdentifiersToObjectsSlow(
                    idArray,
                    objects);

            for (int i = 0;
                 i < objects.Length;
                 i++)
            {
                if (objects[i]
                    is not GameObject gameObject)
                {
                    continue;
                }

                styleByInstanceId[
                    gameObject.GetEntityId()]
                    = validStyles[i];
            }
        }

        private static bool TryGetPersistentId(
            GameObject gameObject,
            out string objectId)
        {
            objectId = null;

            if (gameObject == null)
                return false;

            if (!gameObject.scene.IsValid())
                return false;

            if (string.IsNullOrEmpty(
                    gameObject.scene.path))
            {
                return false;
            }

            GlobalObjectId id =
                GlobalObjectId
                    .GetGlobalObjectIdSlow(
                        gameObject);

            objectId = id.ToString();

            return !string.IsNullOrEmpty(
                objectId);
        }
    }
}