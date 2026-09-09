using System;

namespace MapTools.Editor
{
    internal sealed class PropVariantSelector
    {
        private int _lastIndex = -1;
        private int _consecutiveCount;

        public int Next(Random random, int variantCount, int maxConsecutiveSame)
        {
            if (variantCount <= 1)
            {
                _lastIndex = 0;
                _consecutiveCount = variantCount == 1 ? _consecutiveCount + 1 : 0;
                return 0;
            }

            int index;

            if (_lastIndex >= 0 && _consecutiveCount >= maxConsecutiveSame)
            {
                int candidate = random.Next(variantCount - 1);
                index = candidate >= _lastIndex ? candidate + 1 : candidate;
            }
            else
            {
                index = random.Next(variantCount);
            }

            if (index == _lastIndex)
            {
                _consecutiveCount++;
            }
            else
            {
                _lastIndex = index;
                _consecutiveCount = 1;
            }

            return index;
        }
    }
}
