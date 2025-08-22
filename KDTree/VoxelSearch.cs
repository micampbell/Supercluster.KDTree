
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NearestNeighborSearch
{
    public static class VoxelSearch
    {
        public static VoxelSearch<TDimension, TDimension, TNode> Create<TDimension, TNode>(
            ICollection<IReadOnlyList<TDimension>> points,
            IEnumerable<TNode> nodes,
            DistanceMetrics metricType, TDimension minValue = default, TDimension maxValue = default)
            where TDimension : IComparable<TDimension>, IMinMaxValue<TDimension>, INumber<TDimension>
        => new VoxelSearch<TDimension, TDimension, TNode>(points, nodes, CommonDistanceMetrics.GetDistanceMetric<TDimension>(metricType), minValue, maxValue);

        public static VoxelSearch<TDimension, TPriority, TNode> Create<TDimension, TPriority, TNode>(
            ICollection<IReadOnlyList<TDimension>> points,
            IEnumerable<TNode> nodes, Func<IReadOnlyList<TDimension>, IReadOnlyList<TDimension>, TPriority> metric,
            TDimension minValue = default, TDimension maxValue = default)
            where TDimension : IComparable<TDimension>, IMinMaxValue<TDimension>, INumber<TDimension>
            where TPriority : INumber<TPriority>, IMinMaxValue<TPriority>
        => new VoxelSearch<TDimension, TPriority, TNode>(points, nodes, metric, minValue, maxValue);

    }

    public class VoxelSearch<TDimension, TPriority, TNode> : SearchMethod<TDimension, TPriority, TNode>
        where TDimension : IComparable<TDimension>, IMinMaxValue<TDimension>, INumber<TDimension>
        where TPriority : INumber<TPriority>, IMinMaxValue<TPriority>
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
        internal VoxelSearch(
            ICollection<IReadOnlyList<TDimension>> points,
            IEnumerable<TNode> nodes,
            Func<IReadOnlyList<TDimension>, IReadOnlyList<TDimension>, TPriority> metric,
            TDimension searchWindowMinValue = default,
            TDimension searchWindowMaxValue = default) : base(points, metric, searchWindowMinValue, searchWindowMaxValue)
        {
            this.Points = new IReadOnlyList<TDimension>[Count];
            this.Nodes = new TNode[Count];
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
            var index = GetIndex(target);

        }

        /// <inheritdoc/>
        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNearestNeighbors(IReadOnlyList<TDimension> target, int numNeighbors)
        {
            var closestDistances = new TPriority[numNeighbors];
            var closestPoints = new (IReadOnlyList<TDimension>, TNode)[numNeighbors];
            var cutOffDist = TPriority.MaxValue;
            var locationOfMax = -1;
            var pointsSaved = 0;

            for (int i = 0; i < Points.Length; i++)
            {
                var currentDist = Metric(Points[i], target);
                if (pointsSaved < numNeighbors)
                {
                    closestDistances[pointsSaved] = currentDist;
                    closestPoints[pointsSaved] = (Points[i], Nodes[i]);
                    pointsSaved++;
                }
                else if (currentDist.CompareTo(cutOffDist) < 0)
                {
                    closestDistances[locationOfMax] = currentDist;
                    closestPoints[locationOfMax] = (Points[i], Nodes[i]);
                }
                if (pointsSaved == numNeighbors)
                {
                    // recalc max
                    cutOffDist = TPriority.MinValue;
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
            if (pointsSaved < numNeighbors)
                Array.Resize(ref closestPoints, pointsSaved);
            return closestPoints;
        }


        /// <inheritdoc/>
        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNeighborsInRadius(IReadOnlyList<TDimension> target, TPriority radius, int numNeighbors = -1)
        {
            if (numNeighbors <= 0) return UnlimitedRadialSearch(target, radius);
            var closestDistances = new TPriority[numNeighbors];
            var closestPoints = new (IReadOnlyList<TDimension>, TNode)[numNeighbors];
            var cutOffDist = TPriority.MaxValue;
            var locationOfMax = -1;
            var pointsSaved = 0;

            for (int i = 0; i < Points.Length; i++)
            {
                var currentDist = Metric(Points[i], target);
                if (currentDist.CompareTo(radius) > 0)
                    continue;
                if (pointsSaved < numNeighbors)
                {
                    closestDistances[pointsSaved] = currentDist;
                    closestPoints[pointsSaved] = (Points[i], Nodes[i]);
                    pointsSaved++;
                }
                else if (currentDist.CompareTo(cutOffDist) < 0)
                {
                    closestDistances[locationOfMax] = currentDist;
                    closestPoints[locationOfMax] = (Points[i], Nodes[i]);
                }
                if (pointsSaved == numNeighbors)
                {
                    // recalc max
                    cutOffDist = TPriority.MinValue;
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
            if (pointsSaved < numNeighbors)
                Array.Resize(ref closestPoints, pointsSaved);
            return closestPoints;
        }

        private IEnumerable<(IReadOnlyList<TDimension>, TNode)> UnlimitedRadialSearch(IReadOnlyList<TDimension> target, TPriority radius)
        {
            for (int i = 0; i < Points.Length; i++)
            {
                var currentDist = Metric(target, Points[i]);
                if (currentDist.CompareTo(radius) <= 0)
                    yield return (Points[i], Nodes[i]);
            }
        }
    }
}