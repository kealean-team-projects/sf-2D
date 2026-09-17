#if UNITY_EDITOR
using UnityEngine;

namespace _VFX_Lib._03._Scripts.Editor.HierarchyStyler {
    internal static class HierarchyStylerDefaults {
        internal const string DividerText = "SECTION";

        internal static readonly Color ObjectColor = new(0.25f, 0.5f, 0.9f, 0.3f);

        internal static readonly Color DividerBackgroundColor = new(0.16f, 0.18f, 0.22f, 1f);

        internal static readonly Color DividerTextColor = Color.white;
    }
}
#endif