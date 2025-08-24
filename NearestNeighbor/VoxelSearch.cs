using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NearestNeighborSearch
{
    public static class VoxelSearch
    {
        public static VoxelSearch<TDimension, TNode> Create<TDimension, TNode>(
            ICollection<IReadOnlyList<TDimension>> points,
            IEnumerable<TNode> nodes,
            DistanceMetrics metricType, TDimension minValue = default, TDimension maxValue = default)
            where TDimension : IComparable<TDimension>, IMinMaxValue<TDimension>, INumber<TDimension>
        => new VoxelSearch<TDimension, TNode>(points, nodes, metricType, minValue, maxValue);

    }

    public class VoxelSearch<TDimension, TNode> : SearchMethod<TDimension, TDimension, TNode>
        where TDimension : IComparable<TDimension>, IMinMaxValue<TDimension>, INumber<TDimension>
    {
        const int MaxNumberOfVoxels = 1000000;

        private TDimension[] minima;
        private TDimension[] maxima;
        private double pixelSideLength;
        private TDimension inversePixelSideLength;
        private int[][] Indices;
        private int[] NumVoxelsInDim;
        private int[] IndexMultipliers;
        /// <summary>
        /// The array in which the binary tree is stored. Enumerating this array is a level-order traversal of the tree.
        /// </summary>
        private IReadOnlyList<TDimension>[] Points { get; }

        /// <summary>
        /// The array in which the node objects are stored. There is a one-to-one correspondence with this array and the <see cref="Points"/>.
        /// </summary>
        private TNode[] Nodes { get; }
        public DistanceMetrics MetricType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelSearch{TDimension,TNode}". /> class.
        /// It is unlikely that this constructor will be used directly, as it is more common to use the
        /// Create method to create a VoxelSearch from a set of points and nodes. This can be used and
        /// is left here for created more complex KD-Trees where the points have unique distance metrics.
        /// or the type of the distance metric is different from the type of the points.
        /// </summary>
        /// <param name="dimensions">The number of dimensions in the data set.</param>
        /// <param name="points">The points to be constructed into a <see cref="VoxelSearch{TDimension,TNode}"/></param>
        /// <param name="nodes">The nodes associated with each point.</param>
        /// <param name="metric">The metric function which implicitly defines the metric space in which the VoxelSearch operates in. This should satisfy the triangle inequality.</param>
        /// <param name="searchWindowMinValue">The minimum value to be used in node searches. If null, we assume that <typeparamref name="TDimension"/> has a static field named "MinValue". All numeric structs have this field.</param>
        /// <param name="searchWindowMaxValue">The maximum value to be used in node searches. If null, we assume that <typeparamref name="TDimension"/> has a static field named "MaxValue". All numeric structs have this field.</param>
        public VoxelSearch(ICollection<IReadOnlyList<TDimension>> points, IEnumerable<TNode> nodes, DistanceMetrics metricType,
            TDimension searchWindowMinValue = default, TDimension searchWindowMaxValue = default)
            : base(points, CommonDistanceMetrics.GetDistanceMetric<TDimension>(metricType), searchWindowMinValue, searchWindowMaxValue)
        {
            this.Points = new IReadOnlyList<TDimension>[Count];
            this.Nodes = new TNode[Count];
            this.MetricType = metricType;
            IEnumerator<IReadOnlyList<TDimension>> pointEnumerator = points.GetEnumerator();
            var nodeEnumerator = nodes.GetEnumerator();
            minima = Enumerable.Repeat(TDimension.MaxValue, Dimensions).ToArray();
            maxima = Enumerable.Repeat(TDimension.MinValue, Dimensions).ToArray();
            for (int i = 0; i < Count; i++)
            {
                pointEnumerator.MoveNext();
                var pt = pointEnumerator.Current;
                Points[i] = pt;
                nodeEnumerator.MoveNext();
                Nodes[i] = nodeEnumerator.Current;
                for (int j = 0; j < Dimensions; j++)
                {
                    if (pt[j] < minima[j])
                        minima[j] = pt[j];
                    if (pt[j] > maxima[j])
                        maxima[j] = pt[j];
                }
            }
            var numVoxels = Math.Min(Count, MaxNumberOfVoxels);
            var volume = TDimension.One;
            for (int i = 0; i < Dimensions; i++)
                volume *= (maxima[i] - minima[i]);
            var pixelVolume = volume / TDimension.CreateChecked(numVoxels);
            pixelSideLength = Math.Pow(double.CreateChecked(pixelVolume), 1.0 / Dimensions);
            inversePixelSideLength = TDimension.CreateChecked(1.0 / pixelSideLength);
            numVoxels = 1;
            NumVoxelsInDim = new int[Dimensions];
            for (int i = 0; i < Dimensions; i++)
            {
                var dimVoxels = 1 + int.CreateChecked((maxima[i] - minima[i]) * inversePixelSideLength);
                NumVoxelsInDim[i] = dimVoxels;
                numVoxels *= dimVoxels;
            }
            IndexMultipliers = new int[Dimensions];
            IndexMultipliers[0] = 1;
            for (int i = 1; i < Dimensions; i++)
                IndexMultipliers[i] = IndexMultipliers[i - 1] * NumVoxelsInDim[i - 1];
            Indices = new int[numVoxels][];
            for (int i = 0; i < Count; i++)
            {
                var pt = Points[i];
                int index = GetIndex(pt);
                if (Indices[index] == null)
                    Indices[index] = new int[] { i };
                else
                {
                    var oldArray = Indices[index];
                    Array.Resize(ref oldArray, oldArray.Length + 1);
                    oldArray[oldArray.Length - 1] = i;
                    Indices[index] = oldArray;
                }
            }
        }


        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetAllData()
        {
            for (int i = 0; i < Points.Length; i++)
            {
                if (Points[i] != null)
                    yield return new(Points[i], Nodes[i]);
            }
        }

        /// <inheritdoc/>
        public override (IReadOnlyList<TDimension>, TNode) GetNearestNeighbor(IReadOnlyList<TDimension> target)
        {
            var closestDistance = TDimension.MaxValue;
            (IReadOnlyList<TDimension>, TNode) closestPoint = default;
            var cutOffDist = TDimension.MaxValue;
            Func<int, IEnumerable<int[]>> combiner = MetricType == DistanceMetrics.ManhattanDistance
                ? GetAllCombinationsTotalingN
                : MetricType == DistanceMetrics.EuclideanDistance ? GetAllCombinationsTotalingNSquared
                : GetAllCombinationsTotalingNMax;

            var targetLocation = GetIndices(target);
            var layer = 0;
            var maxSet = false;
            var maxLayer = NumVoxelsInDim.Max();
            while (layer <= maxLayer)
            {
                if (!maxSet && closestDistance < TDimension.MaxValue)
                {
                    maxLayer = MetricType == DistanceMetrics.ManhattanDistance || MetricType == DistanceMetrics.ChebyshevDistance
                        ? layer + 1
                        : ((int)Math.Pow(((int)Math.Sqrt(layer - 1)) + 2, 2)); // - 1;
                    maxSet = true;
                }
                foreach (var voxelIndex in GetIndicesAtLayer(targetLocation, layer, combiner))
                {
                    if (Indices[voxelIndex] != null)
                    {
                        foreach (var pointIndex in Indices[voxelIndex])
                        {
                            var p = Points[pointIndex];
                            var currentDist = Metric(p, target);
                            if (currentDist < closestDistance)
                            {
                                closestDistance = currentDist;
                                closestPoint = (p, Nodes[pointIndex]);
                            }
                        }
                    }
                }
                layer++; // expand search outward one layer
            }
            return closestPoint;
        }

        private IEnumerable<int> GetIndicesAtLayer(int[] targetLocation, int layer, Func<int, IEnumerable<int[]>> combiner)
        {
            foreach (var delta in combiner(layer))
            {
                var location = (int[])targetLocation.Clone();
                var valid = true;
                for (int i = 0; i < Dimensions; i++)
                {
                    location[i] += delta[i];
                    if (location[i] < 0 || location[i] >= NumVoxelsInDim[i])
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                    yield return GetIndex(location);
            }
        }

        private IEnumerable<int[]> GetAllCombinationsTotalingNSquared(int layer)
        {
            // Return all integer vectors v (length = Dimensions) such that Sum(v[i]^2) == layer (L2 shell)
            if (layer == 0)
            {
                yield return new int[Dimensions];
                yield break;
            }

            var values = new int[Dimensions];

            IEnumerable<int[]> Recurse(int dim, int remainingSquared)
            {
                if (dim == Dimensions - 1)
                {
                    // Last dimension must satisfy remainingSquared = values[dim]^2
                    var root = (int)Math.Sqrt(remainingSquared);
                    if (root * root == remainingSquared)
                    {
                        values[dim] = root;
                        foreach (var v in EmitWithSigns())
                            yield return v;
                    }
                    yield break;
                }

                // For current dimension choose a non-negative value whose square <= remainingSquared
                int maxVal = (int)Math.Sqrt(remainingSquared);
                for (int a = 0; a <= maxVal; a++)
                {
                    values[dim] = a;
                    foreach (var v in Recurse(dim + 1, remainingSquared - a * a))
                        yield return v;
                }
            }

            IEnumerable<int[]> EmitWithSigns()
            {
                // Determine indices with non-zero values
                var nonZero = new List<int>(Dimensions);
                for (int i = 0; i < Dimensions; i++)
                    if (values[i] != 0) nonZero.Add(i);
                int nz = nonZero.Count;
                int totalMasks = 1 << nz;
                for (int mask = 0; mask < totalMasks; mask++)
                {
                    var vec = new int[Dimensions];
                    for (int i = 0; i < Dimensions; i++) vec[i] = values[i];
                    for (int bit = 0; bit < nz; bit++)
                        if ((mask & (1 << bit)) != 0)
                            vec[nonZero[bit]] = -vec[nonZero[bit]];
                    yield return vec;
                }
            }

            foreach (var v in Recurse(0, layer))
                yield return v;
        }


        private IEnumerable<int[]> GetAllCombinationsTotalingNMax(int layer)
        {
            // Return all integer vectors v (length = Dimensions) such that Max(abs(v[i])) == layer (L-inf shell)
            if (layer == 0)
            {
                yield return new int[Dimensions];
                yield break;
            }

            var values = new int[Dimensions];

            IEnumerable<int[]> Recurse(int dim, int remainingSquared)
            {
                if (dim == Dimensions - 1)
                {
                    // Last dimension must satisfy remainingSquared = values[dim]^2
                    var root = (int)Math.Sqrt(remainingSquared);
                    if (root * root == remainingSquared)
                    {
                        values[dim] = root;
                        foreach (var v in EmitWithSigns())
                            yield return v;
                    }
                    yield break;
                }

                // For current dimension choose a non-negative value whose square <= remainingSquared
                int maxVal = (int)Math.Sqrt(remainingSquared);
                for (int a = 0; a <= maxVal; a++)
                {
                    values[dim] = a;
                    foreach (var v in Recurse(dim + 1, remainingSquared - a * a))
                        yield return v;
                }
            }

            IEnumerable<int[]> EmitWithSigns()
            {
                // Determine indices with non-zero values
                var nonZero = new List<int>(Dimensions);
                for (int i = 0; i < Dimensions; i++)
                    if (values[i] != 0) nonZero.Add(i);
                int nz = nonZero.Count;
                int totalMasks = 1 << nz;
                for (int mask = 0; mask < totalMasks; mask++)
                {
                    var vec = new int[Dimensions];
                    for (int i = 0; i < Dimensions; i++) vec[i] = values[i];
                    for (int bit = 0; bit < nz; bit++)
                        if ((mask & (1 << bit)) != 0)
                            vec[nonZero[bit]] = -vec[nonZero[bit]];
                    yield return vec;
                }
            }

            foreach (var v in Recurse(0, layer))
                yield return v;
        }

        private IEnumerable<int[]> GetAllCombinationsTotalingN(int n)
        {
            if (n == 0)
            {
                yield return new int[Dimensions];
                yield break;
            }

            var absValues = new int[Dimensions];
            IEnumerable<int[]> Recurse(int dim, int remaining)
            {
                if (dim == Dimensions - 1)
                {
                    absValues[dim] = remaining;
                    foreach (var v in EmitWithSigns())
                        yield return v;
                    yield break;
                }
                for (int a = 0; a <= remaining; a++)
                {
                    absValues[dim] = a;
                    foreach (var v in Recurse(dim + 1, remaining - a))
                        yield return v;
                }
            }

            IEnumerable<int[]> EmitWithSigns()
            {
                // Collect indices of non-zero components
                var nonZeroIndices = new List<int>(Dimensions);
                for (int i = 0; i < Dimensions; i++)
                    if (absValues[i] != 0)
                        nonZeroIndices.Add(i);

                int nz = nonZeroIndices.Count;
                var totalMasks = 1 << nz;
                for (int mask = 0; mask < totalMasks; mask++)
                {
                    var vec = new int[Dimensions];
                    for (int i = 0; i < Dimensions; i++)
                        vec[i] = absValues[i];
                    for (int bit = 0; bit < nz; bit++)
                        if ((mask & (1 << bit)) != 0)
                            vec[nonZeroIndices[bit]] = -vec[nonZeroIndices[bit]];
                    yield return vec;
                }
            }

            foreach (var v in Recurse(0, n))
                yield return v;
        }

        /// <inheritdoc/>
        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNearestNeighbors(IReadOnlyList<TDimension> target, int numNeighbors)
        {
            if (numNeighbors <= 0 || numNeighbors >= Count)
                return GetAllData();
            if (numNeighbors == 1)
                return [GetNearestNeighbor(target)];
            var closestDistances = new TDimension[numNeighbors];
            var closestPoints = new (IReadOnlyList<TDimension>, TNode)[numNeighbors];
            var cutOffDist = TDimension.MaxValue;
            var locationOfMax = -1;
            var pointsSaved = 0;
            Func<int, IEnumerable<int[]>> combiner = MetricType == DistanceMetrics.ManhattanDistance
                ? GetAllCombinationsTotalingN
                : MetricType == DistanceMetrics.EuclideanDistance ? GetAllCombinationsTotalingNSquared
                : GetAllCombinationsTotalingNMax;

            var targetLocation = GetIndices(target);
            var layer = 0;
            var maxSet = false;
            var maxLayer = NumVoxelsInDim.Max();
            while (!maxSet || layer <= maxLayer)
            {
                if (!maxSet && pointsSaved == numNeighbors)
                {
                    maxLayer = MetricType == DistanceMetrics.ManhattanDistance || MetricType == DistanceMetrics.ChebyshevDistance
                        ? layer + 1
                        : ((int)Math.Pow(((int)Math.Sqrt(layer - 1)) + 2, 2)) - 1;
                    maxSet = true;
                }
                foreach (var voxelIndex in GetIndicesAtLayer(targetLocation, layer, combiner))
                {
                    if (Indices[voxelIndex] != null)
                    {
                        foreach (var pointIndex in Indices[voxelIndex])
                        {
                            var p = Points[pointIndex];
                            var currentDist = Metric(p, target);
                            if (pointsSaved < numNeighbors)
                            {
                                closestDistances[pointsSaved] = currentDist;
                                closestPoints[pointsSaved] = (p, Nodes[pointIndex]);
                                pointsSaved++;
                            }
                            else if (currentDist.CompareTo(cutOffDist) < 0)
                            {
                                closestDistances[locationOfMax] = currentDist;
                                closestPoints[locationOfMax] = (p, Nodes[pointIndex]);
                            }
                            if (pointsSaved == numNeighbors)
                            {
                                // recalc max
                                cutOffDist = TDimension.MinValue;
                                for (int j = 0; j < numNeighbors; j++)
                                {
                                    if (closestDistances[j].CompareTo(cutOffDist) > 0)
                                    {
                                        cutOffDist = closestDistances[j];
                                        locationOfMax = j;
                                    }
                                }
                            }

                        }
                    }
                }
                layer++; // expand search outward one layer
            }
            if (pointsSaved < numNeighbors)
                Array.Resize(ref closestPoints, pointsSaved);
            return closestPoints;
        }


        /// <inheritdoc/>
        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNeighborsInRadius(IReadOnlyList<TDimension> target, TDimension radius,
            int numNeighbors = -1)
        {
            var maxLayer = 1 + (int)Math.Ceiling(double.CreateChecked(radius * inversePixelSideLength));
            if (maxLayer > NumVoxelsInDim.Max())
            {
                if (numNeighbors <= 0) return GetAllData();
                else return GetNearestNeighbors(target, numNeighbors);
            }
            maxLayer *= maxLayer; // radius squared
            maxLayer -= 1;
            if (Metric.GetMethodInfo().Name == nameof(CommonDistanceMetrics.EuclideanDistance))
                radius *= radius; // we are using squared Euclidean distance, so square the radius.
            if (numNeighbors <= 0 || numNeighbors >= Count) return UnlimitedRadialSearch(target, radius, maxLayer);

            var closestDistances = new TDimension[numNeighbors];
            var closestPoints = new (IReadOnlyList<TDimension>, TNode)[numNeighbors];
            var cutOffDist = TDimension.MaxValue;
            var locationOfMax = -1;
            var pointsSaved = 0;
            Func<int, IEnumerable<int[]>> combiner = MetricType == DistanceMetrics.ManhattanDistance
                ? GetAllCombinationsTotalingN
                : MetricType == DistanceMetrics.EuclideanDistance ? GetAllCombinationsTotalingNSquared
                : GetAllCombinationsTotalingNMax;

            var targetLocation = GetIndices(target);
            var layer = 0;
            var maxSet = false;
            while (!maxSet || layer <= maxLayer)
            {
                if (!maxSet && pointsSaved == numNeighbors)
                {
                    maxLayer = MetricType == DistanceMetrics.ManhattanDistance || MetricType == DistanceMetrics.ChebyshevDistance
                        ? layer + 1
                        : ((int)Math.Pow(((int)Math.Sqrt(layer - 1)) + 2, 2)) - 1;
                    maxSet = true;
                }
                foreach (var voxelIndex in GetIndicesAtLayer(targetLocation, layer, combiner))
                {
                    if (Indices[voxelIndex] != null)
                    {
                        foreach (var pointIndex in Indices[voxelIndex])
                        {
                            var p = Points[pointIndex];
                            var currentDist = Metric(p, target);
                            if (currentDist > radius)
                                continue;
                            if (pointsSaved < numNeighbors)
                            {
                                closestDistances[pointsSaved] = currentDist;
                                closestPoints[pointsSaved] = (p, Nodes[pointIndex]);
                                pointsSaved++;
                            }
                            else if (currentDist.CompareTo(cutOffDist) < 0)
                            {
                                closestDistances[locationOfMax] = currentDist;
                                closestPoints[locationOfMax] = (p, Nodes[pointIndex]);
                            }
                            if (pointsSaved == numNeighbors)
                            {
                                // recalc max
                                cutOffDist = TDimension.MinValue;
                                for (int j = 0; j < numNeighbors; j++)
                                {
                                    if (closestDistances[j].CompareTo(cutOffDist) > 0)
                                    {
                                        cutOffDist = closestDistances[j];
                                        locationOfMax = j;
                                    }
                                }
                            }

                        }
                    }
                }
                layer++; // expand search outward one layer
            }
            if (pointsSaved < numNeighbors)
                Array.Resize(ref closestPoints, pointsSaved);
            return closestPoints;
        }

        private IEnumerable<(IReadOnlyList<TDimension>, TNode)> UnlimitedRadialSearch(IReadOnlyList<TDimension> target,
            TDimension radius, int maxLayer)
        {
            Func<int, IEnumerable<int[]>> combiner = MetricType == DistanceMetrics.ManhattanDistance
    ? GetAllCombinationsTotalingN
    : MetricType == DistanceMetrics.EuclideanDistance ? GetAllCombinationsTotalingNSquared
    : GetAllCombinationsTotalingNMax;

            var targetLocation = GetIndices(target);
            for (var layer = 0; layer <= maxLayer; layer++)
            {
                foreach (var voxelIndex in GetIndicesAtLayer(targetLocation, layer, combiner))
                {
                    if (Indices[voxelIndex] != null)
                    {
                        foreach (var pointIndex in Indices[voxelIndex])
                        {
                            var p = Points[pointIndex];
                            var currentDist = Metric(p, target);
                            if (currentDist <= radius)
                                yield return (p, Nodes[pointIndex]);
                        }
                    }
                }
            }
        }

        #region Indices and Enumerations

        /// <summary>
        /// Gets the voxelIndex of the x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.Int32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int[] GetIndices(IReadOnlyList<TDimension> pt)
        {
            var indices = new int[Dimensions];
            for (int j = 0; j < Dimensions; j++)
                indices[j] = (int.CreateChecked((pt[j] - minima[j]) * inversePixelSideLength));
            return indices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetIndex(IReadOnlyList<TDimension> pt)
        {
            var index = 0;
            for (int j = 0; j < Dimensions; j++)
                index += (int.CreateChecked((pt[j] - minima[j]) * inversePixelSideLength)) * IndexMultipliers[j];
            return index;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetIndex(int[] indices)
        {
            var index = 0;
            for (int j = 0; j < Dimensions; j++)
                index += indices[j] * IndexMultipliers[j];
            return index;
        }
        #endregion
    }
}