using System;
using System.Collections.Generic;
using MapTools;
using MapTools.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace _00._Member.LHS.Script.MapTools.Editor {
    internal readonly struct PropLineRequest {
        public readonly MapPropLineProfile Profile;
        public readonly Transform Parent;

        public readonly PropPlacementMode PlacementMode;
        public readonly Vector3 Start;
        public readonly Vector3 End;
        public readonly Vector3 StartControlPoint;
        public readonly Vector3 EndControlPoint;

        public readonly int Count;
        public readonly float MinimumDistance;

        public readonly Vector2 ZOffsetRange;
        public readonly Vector2 ScaleRange;
        public readonly bool RandomFlipX;

        public readonly int Seed;

        public readonly bool OverrideSortingLayer;
        public readonly int SortingLayerId;
        public readonly int BaseSortingOrder;

        public PropLineRequest(
            MapPropLineProfile profile,
            Transform parent,
            PropPlacementMode placementMode,
            Vector3 start,
            Vector3 end,
            Vector3 startControlPoint,
            Vector3 endControlPoint,
            int count,
            float minimumDistance,
            Vector2 zOffsetRange,
            Vector2 scaleRange,
            bool randomFlipX,
            int seed,
            bool overrideSortingLayer,
            int sortingLayerId,
            int baseSortingOrder) {
            Profile = profile;
            Parent = parent;

            PlacementMode = placementMode;
            Start = start;
            End = end;
            StartControlPoint = startControlPoint;
            EndControlPoint = endControlPoint;

            Count = count;
            MinimumDistance = minimumDistance;

            ZOffsetRange = zOffsetRange;
            ScaleRange = scaleRange;
            RandomFlipX = randomFlipX;

            Seed = seed;

            OverrideSortingLayer = overrideSortingLayer;
            SortingLayerId = sortingLayerId;
            BaseSortingOrder = baseSortingOrder;
        }
    }

    internal static class PropLineGenerator {
        private const int MinSortingOrder = -32768;
        private const int MaxSortingOrder = 32767;

        public static GameObject Generate(
            in PropLineRequest request) {
            Validate(request);

            Undo.IncrementCurrentGroup();

            var undoGroup =
                Undo.GetCurrentGroup();

            Undo.SetCurrentGroupName(
                "Generate Prop Line");

            var scene =
                request.Parent != null
                    ? request.Parent.gameObject.scene
                    : SceneManager.GetActiveScene();

            var container =
                new GameObject(
                    $"{request.Profile.name}_Generated");

            SceneManager.MoveGameObjectToScene(
                container,
                scene);

            Undo.RegisterCreatedObjectUndo(
                container,
                "Create Prop Line Container");

            if (request.Parent != null)
                Undo.SetTransformParent(
                    container.transform,
                    request.Parent,
                    "Parent Prop Line Container");

            var positions =
                PropPathSampler.BuildPositions(
                    request.PlacementMode,
                    request.Start,
                    request.End,
                    request.StartControlPoint,
                    request.EndControlPoint,
                    request.Count,
                    request.MinimumDistance);

            if (positions.Count < request.Count)
                Debug.LogWarning(
                    $"Requested {request.Count} props, but only {positions.Count} fit within the minimum distance.");

            var random =
                new Random(request.Seed);

            var variantPools =
                BuildVariantPools(request.Profile);

            var selectors =
                CreateSelectors(
                    request.Profile.SpriteVariantSets.Count);

            var instances =
                new List<GameObject>(positions.Count);

            var minZ =
                Mathf.Min(
                    request.ZOffsetRange.x,
                    request.ZOffsetRange.y);

            var maxZ =
                Mathf.Max(
                    request.ZOffsetRange.x,
                    request.ZOffsetRange.y);

            var minScale =
                Mathf.Max(
                    0.01f,
                    Mathf.Min(
                        request.ScaleRange.x,
                        request.ScaleRange.y));

            var maxScale =
                Mathf.Max(
                    minScale,
                    Mathf.Max(
                        request.ScaleRange.x,
                        request.ScaleRange.y));

            foreach (var basePosition in positions) {
                var position =
                    basePosition;

                position.z += Mathf.Lerp(
                    minZ,
                    maxZ,
                    (float)random.NextDouble());

                var instance =
                    PrefabUtility.InstantiatePrefab(
                        request.Profile.Prefab,
                        scene) as GameObject;

                if (instance == null)
                    continue;

                Undo.RegisterCreatedObjectUndo(
                    instance,
                    "Create Map Prop");

                Undo.SetTransformParent(
                    instance.transform,
                    container.transform,
                    "Parent Map Prop");

                Undo.RecordObject(
                    instance.transform,
                    "Configure Map Prop");

                instance.transform.position =
                    position;

                ApplyRandomTransform(
                    instance.transform,
                    random,
                    minScale,
                    maxScale,
                    request.RandomFlipX);

                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        instance.transform);

                ApplyVariants(
                    instance,
                    request.Profile,
                    variantPools,
                    selectors,
                    random);

                instances.Add(instance);
            }

            if (request.OverrideSortingLayer)
                ApplyDepthSorting(
                    instances,
                    request.SortingLayerId,
                    request.BaseSortingOrder);

            Undo.CollapseUndoOperations(
                undoGroup);

            Selection.activeGameObject =
                container;

            return container;
        }

        private static void ApplyRandomTransform(
            Transform target,
            Random random,
            float minScale,
            float maxScale,
            bool randomFlipX) {
            var scale =
                Mathf.Lerp(
                    minScale,
                    maxScale,
                    (float)random.NextDouble());

            var localScale =
                target.localScale * scale;

            if (
                randomFlipX &&
                random.NextDouble() < 0.5)
                localScale.x *= -1f;

            target.localScale =
                localScale;
        }

        private static void ApplyDepthSorting(
            List<GameObject> instances,
            int sortingLayerId,
            int baseSortingOrder) {
            instances.Sort((a, b) =>
                a.transform.position.z.CompareTo(
                    b.transform.position.z));

            var currentOrder =
                Mathf.Clamp(
                    baseSortingOrder,
                    MinSortingOrder,
                    MaxSortingOrder);

            foreach (var instance in instances) {
                var renderers =
                    instance.GetComponentsInChildren<SpriteRenderer>(
                        true);

                if (renderers.Length == 0)
                    continue;

                var minOrder =
                    int.MaxValue;

                var maxOrder =
                    int.MinValue;

                foreach (var renderer in renderers) {
                    minOrder =
                        Mathf.Min(
                            minOrder,
                            renderer.sortingOrder);

                    maxOrder =
                        Mathf.Max(
                            maxOrder,
                            renderer.sortingOrder);
                }

                foreach (var renderer in renderers) {
                    var relativeOrder =
                        renderer.sortingOrder -
                        maxOrder;

                    var sortingOrder =
                        Mathf.Clamp(
                            currentOrder + relativeOrder,
                            MinSortingOrder,
                            MaxSortingOrder);

                    Undo.RecordObject(
                        renderer,
                        "Change Sprite Sorting");

                    renderer.sortingLayerID =
                        sortingLayerId;

                    renderer.sortingOrder =
                        sortingOrder;

                    PrefabUtility
                        .RecordPrefabInstancePropertyModifications(
                            renderer);
                }

                var orderRange =
                    maxOrder -
                    minOrder +
                    1;

                currentOrder =
                    Mathf.Max(
                        MinSortingOrder,
                        currentOrder - orderRange);
            }
        }

        private static void Validate(
            in PropLineRequest request) {
            if (request.Profile == null)
                throw new InvalidOperationException(
                    "Profile is required.");

            if (request.Profile.Prefab == null)
                throw new InvalidOperationException(
                    "Profile prefab is required.");

            if (!PrefabUtility.IsPartOfPrefabAsset(
                    request.Profile.Prefab))
                throw new InvalidOperationException(
                    "Profile prefab must be a Prefab Asset.");

            if (request.Count < 1)
                throw new InvalidOperationException(
                    "Count must be at least 1.");

            if (request.MinimumDistance < 0f)
                throw new InvalidOperationException(
                    "Minimum distance cannot be negative.");

            if (request.Parent == null)
                return;

            var scene =
                request.Parent.gameObject.scene;

            if (
                !scene.IsValid() ||
                !scene.isLoaded)
                throw new InvalidOperationException(
                    "Parent must belong to a loaded Scene.");
        }

        private static List<Sprite>[] BuildVariantPools(
            MapPropLineProfile profile) {
            var sets =
                profile.SpriteVariantSets;

            var pools =
                new List<Sprite>[sets.Count];

            for (var i = 0; i < sets.Count; i++) {
                var source =
                    sets[i].Variants;

                var pool =
                    new List<Sprite>(source.Count);

                for (var j = 0; j < source.Count; j++)
                    if (source[j] != null)
                        pool.Add(source[j]);

                pools[i] =
                    pool;
            }

            return pools;
        }

        private static PropVariantSelector[] CreateSelectors(
            int count) {
            var selectors =
                new PropVariantSelector[count];

            for (var i = 0; i < count; i++)
                selectors[i] =
                    new PropVariantSelector();

            return selectors;
        }

        private static void ApplyVariants(
            GameObject instance,
            MapPropLineProfile profile,
            IReadOnlyList<List<Sprite>> pools,
            IReadOnlyList<PropVariantSelector> selectors,
            Random random) {
            var sets =
                profile.SpriteVariantSets;

            for (var i = 0; i < sets.Count; i++) {
                var pool =
                    pools[i];

                if (pool.Count == 0)
                    continue;

                var set =
                    sets[i];

                var target =
                    FindTarget(
                        instance.transform,
                        set.RendererPath);

                if (
                    target == null ||
                    !target.TryGetComponent(
                        out SpriteRenderer renderer)) {
                    Debug.LogWarning(
                        $"SpriteRenderer not found. Prefab: {instance.name}, Path: '{set.RendererPath}'",
                        instance);

                    continue;
                }

                var index =
                    selectors[i].Next(
                        random,
                        pool.Count,
                        set.MaxConsecutiveSame);

                Undo.RecordObject(
                    renderer,
                    "Apply Sprite Variant");

                renderer.sprite =
                    pool[index];

                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        renderer);
            }
        }

        private static Transform FindTarget(
            Transform root,
            string path) {
            return string.IsNullOrWhiteSpace(path)
                ? root
                : root.Find(path);
        }
    }
}