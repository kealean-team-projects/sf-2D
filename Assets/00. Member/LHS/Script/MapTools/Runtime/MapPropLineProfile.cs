using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapTools {
    [CreateAssetMenu(fileName = "MapPropLineProfile", menuName = "Map Tools/Prop Line Profile")]
    public sealed class MapPropLineProfile : ScriptableObject {
        [SerializeField] private GameObject prefab;
        [SerializeField] private List<SpriteVariantSet> spriteVariantSets = new();

        public GameObject Prefab => prefab;
        public IReadOnlyList<SpriteVariantSet> SpriteVariantSets => spriteVariantSets;
    }

    [Serializable]
    public sealed class SpriteVariantSet {
        [SerializeField] private string rendererPath;
        [SerializeField] private List<Sprite> variants = new();
        [SerializeField] [Min(1)] private int maxConsecutiveSame = 1;

        public string RendererPath => rendererPath;
        public IReadOnlyList<Sprite> Variants => variants;
        public int MaxConsecutiveSame => Mathf.Max(1, maxConsecutiveSame);
    }
}