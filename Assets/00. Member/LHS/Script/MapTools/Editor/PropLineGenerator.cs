using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace MapTools.Editor {
    internal readonly struct PropLineRequest {
        public readonly MapPropLineProfile Profile;
        public readonly Transform Parent;
        public readonly Vector3 Start;
        public readonly Vector3 End;
        public readonly int Count;
        public readonly Vector2 ZOffsetRange;
        public readonly int Seed;

        public PropLineRequest(
            MapPropLineProfile profile,
            Transform parent,
            Vector3 start,
            Vector3 end,
            int count,
            Vector2 zOffsetRange,
            int seed) {
            Profile = profile;
            Parent = parent;
            Start = start;
            End = end;
            Count = count;
            ZOffsetRange = zOffsetRange;
            Seed = seed;
        }
    }

    internal static class PropLineGenerator {
        public static GameObject Generate(in PropLineRequest request) {
            Validate(request);

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate Prop Line");

            var scene = request.Parent != null
                ? request.Parent.gameObject.scene
                : SceneManager.GetActiveScene();

            GameObject container = new($"{request.Profile.name}_Generated");
            SceneManager.MoveGameObjectToScene(container, scene);
            Undo.RegisterCreatedObjectUndo(container, "Create Prop Line Container");

            if (request.Parent != null)
                Undo.SetTransformParent(container.transform, request.Parent, "Parent Prop Line Container");

            Random random = new(request.Seed);
            var variantPools = BuildVariantPools(request.Profile);
            var selectors = CreateSelectors(request.Profile.SpriteVariantSets.Count);

            var minZ = Mathf.Min(request.ZOffsetRange.x, request.ZOffsetRange.y);
            var maxZ = Mathf.Max(request.ZOffsetRange.x, request.ZOffsetRange.y);

            for (var i = 0; i < request.Count; i++) {
                var t = request.Count == 1 ? 0f : i / (request.Count - 1f);
                var position = Vector3.Lerp(request.Start, request.End, t);
                position.z += Mathf.Lerp(minZ, maxZ, (float)random.NextDouble());

                var instance =
                    PrefabUtility.InstantiatePrefab(request.Profile.Prefab, scene) as GameObject;

                if (instance == null)
                    continue;

                Undo.RegisterCreatedObjectUndo(instance, "Create Map Prop");
                Undo.SetTransformParent(instance.transform, container.transform, "Parent Map Prop");
                Undo.RegisterFullObjectHierarchyUndo(instance, "Configure Map Prop");

                instance.transform.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);

                ApplyVariants(instance, request.Profile, variantPools, selectors, random);
            }

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = container;

            return container;
        }

        private static void Validate(in PropLineRequest request) {
            if (request.Profile == null)
                throw new InvalidOperationException("Profile is required.");

            if (request.Profile.Prefab == null)
                throw new InvalidOperationException("Profile prefab is required.");

            if (!PrefabUtility.IsPartOfPrefabAsset(request.Profile.Prefab))
                throw new InvalidOperationException("Profile prefab must be a Prefab Asset.");

            if (request.Count < 1)
                throw new InvalidOperationException("Count must be at least 1.");

            if (request.Parent != null) {
                var scene = request.Parent.gameObject.scene;

                if (!scene.IsValid() || !scene.isLoaded)
                    throw new InvalidOperationException("Parent must belong to a loaded Scene.");
            }
        }

        private static List<Sprite>[] BuildVariantPools(MapPropLineProfile profile) {
            var sets = profile.SpriteVariantSets;
            var pools = new List<Sprite>[sets.Count];

            for (var i = 0; i < sets.Count; i++) {
                var source = sets[i].Variants;
                List<Sprite> pool = new(source.Count);

                for (var j = 0; j < source.Count; j++)
                    if (source[j] != null)
                        pool.Add(source[j]);

                pools[i] = pool;
            }

            return pools;
        }

        private static PropVariantSelector[] CreateSelectors(int count) {
            var selectors = new PropVariantSelector[count];

            for (var i = 0; i < count; i++)
                selectors[i] = new PropVariantSelector();

            return selectors;
        }

        private static void ApplyVariants(
            GameObject instance,
            MapPropLineProfile profile,
            IReadOnlyList<List<Sprite>> pools,
            IReadOnlyList<PropVariantSelector> selectors,
            Random random) {
            var sets = profile.SpriteVariantSets;

            for (var i = 0; i < sets.Count; i++) {
                var pool = pools[i];

                if (pool.Count == 0)
                    continue;

                var set = sets[i];
                var target = FindTarget(instance.transform, set.RendererPath);

                if (target == null || !target.TryGetComponent(out SpriteRenderer renderer)) {
                    Debug.LogWarning(
                        $"SpriteRenderer not found. Prefab: {instance.name}, Path: '{set.RendererPath}'",
                        instance);
                    continue;
                }

                var index = selectors[i].Next(
                    random,
                    pool.Count,
                    set.MaxConsecutiveSame);

                renderer.sprite = pool[index];
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static Transform FindTarget(Transform root, string path) {
            return string.IsNullOrWhiteSpace(path)
                ? root
                : root.Find(path);
        }
    }
}