using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NearestNeighborSearch
{
    internal static class VoxelHelpers<TDimension>
        where TDimension : System.Numerics.INumber<TDimension>
    {
        /// <summary>
        /// Gets the index of the x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.Int32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetIndex(TDimension x, TDimension minimum, TDimension inverseSideLength) 
            => int.CreateChecked((x - minimum) * inverseSideLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetIndex(IReadOnlyList<TDimension> pt, TDimension[] minima, int[] IndexMultipliers, 
            int n, TDimension inverseSideLength)
        {
            var index = 0;
            for (int j = 0; j < n; j++)
                index += (int.CreateChecked((pt[j] - minima[j]) * inverseSideLength)) * IndexMultipliers[j];
            return index;
        }
    }
}
