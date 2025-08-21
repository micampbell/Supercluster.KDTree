using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SuperClusterKDTreeSpan.Utilities
{
    internal static class CalcMedian
    {
        /// <summary>
        /// Gets the median of the collection using a clever linear algorithm. The array is not actually sorted
        /// which would require an O(nlog(index)) operation.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>System.Double.</returns>
        /// <value>The median.</value>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDimension GetNthPosition<TDimension>(IEnumerable<TDimension> numbers, int index, int numCount)
        where TDimension : IComparable<TDimension>
        {
            var array = new TDimension[numCount];
            var i = 0;
            foreach (var num in numbers)
                array[i++] = num;
            var start = 0;
            var end = numCount - 1;

            while (true)
            {
                var pivot = array[end];
                var lastLow = start - 1;
                for (i = start; i < end; i++)
                {
                    if (array[i].CompareTo(pivot) <= 0)
                        swap(array, i, ++lastLow);
                }
                swap(array, end, ++lastLow);
                var pivotIndex = lastLow;
                if (pivotIndex == index)
                    return array[pivotIndex];
                if (index < pivotIndex)
                    end = pivotIndex - 1;
                else
                    start = pivotIndex + 1;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void swap<TDimension>(TDimension[] array, int i, int j)
        where TDimension : IComparable<TDimension>
        {
            var temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}
