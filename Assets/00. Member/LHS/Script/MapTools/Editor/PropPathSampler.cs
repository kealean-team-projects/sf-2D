using System.Collections.Generic;
using UnityEngine;

namespace _00._Member.LHS.Script.MapTools.Editor {
    internal static class PropPathSampler {
        private const int MinSampleCount = 128;
        private const int SamplesPerProp = 16;

        public static List<Vector3> BuildPositions(
            PropPlacementMode mode,
            Vector3 start,
            Vector3 end,
            Vector3 startControlPoint,
            Vector3 endControlPoint,
            int desiredCount,
            float minimumDistance) {
            if (desiredCount <= 0)
                return new List<Vector3>();

            if (desiredCount == 1)
                return new List<Vector3> { start };

            var sampleCount = Mathf.Max(
                MinSampleCount,
                desiredCount * SamplesPerProp);

            var samples = new Vector3[sampleCount + 1];
            var cumulativeDistances = new float[sampleCount + 1];

            samples[0] = Evaluate(
                mode,
                start,
                end,
                startControlPoint,
                endControlPoint,
                0f);

            for (var i = 1; i <= sampleCount; i++) {
                var t = i / (float)sampleCount;

                samples[i] = Evaluate(
                    mode,
                    start,
                    end,
                    startControlPoint,
                    endControlPoint,
                    t);

                cumulativeDistances[i] =
                    cumulativeDistances[i - 1] +
                    Vector3.Distance(samples[i - 1], samples[i]);
            }

            var totalLength = cumulativeDistances[sampleCount];

            if (totalLength <= Mathf.Epsilon)
                return new List<Vector3> { start };

            var actualCount = desiredCount;

            if (minimumDistance > 0f) {
                var maximumCount =
                    Mathf.FloorToInt(totalLength / minimumDistance) + 1;

                actualCount = Mathf.Clamp(
                    Mathf.Min(desiredCount, maximumCount),
                    1,
                    desiredCount);
            }

            if (actualCount == 1)
                return new List<Vector3> { samples[0] };

            var positions =
                new List<Vector3>(actualCount);

            var segmentIndex = 1;

            for (var i = 0; i < actualCount; i++) {
                var targetDistance =
                    totalLength * i / (actualCount - 1f);

                while (
                    segmentIndex < cumulativeDistances.Length - 1 &&
                    cumulativeDistances[segmentIndex] < targetDistance)
                    segmentIndex++;

                var previousIndex =
                    Mathf.Max(0, segmentIndex - 1);

                var segmentStart =
                    cumulativeDistances[previousIndex];

                var segmentEnd =
                    cumulativeDistances[segmentIndex];

                var segmentLength =
                    segmentEnd - segmentStart;

                var segmentT =
                    segmentLength <= Mathf.Epsilon
                        ? 0f
                        : (targetDistance - segmentStart) / segmentLength;

                positions.Add(
                    Vector3.Lerp(
                        samples[previousIndex],
                        samples[segmentIndex],
                        segmentT));
            }

            return positions;
        }

        private static Vector3 Evaluate(
            PropPlacementMode mode,
            Vector3 start,
            Vector3 end,
            Vector3 startControlPoint,
            Vector3 endControlPoint,
            float t) {
            return mode == PropPlacementMode.Bezier
                ? EvaluateBezier(
                    start,
                    startControlPoint,
                    endControlPoint,
                    end,
                    t)
                : Vector3.Lerp(start, end, t);
        }

        private static Vector3 EvaluateBezier(
            Vector3 start,
            Vector3 startControlPoint,
            Vector3 endControlPoint,
            Vector3 end,
            float t) {
            var u = 1f - t;
            var uu = u * u;
            var tt = t * t;

            return
                uu * u * start +
                3f * uu * t * startControlPoint +
                3f * u * tt * endControlPoint +
                tt * t * end;
        }
    }
}