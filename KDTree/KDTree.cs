// <copyright file="KDTree.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace SuperClusterKDTree
{
    using SuperClusterKDTree.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using static Utilities.BinaryTreeNavigation;

    /// <summary>
    /// Represents a KD-Tree. KD-Trees are used for fast spatial searches. Searching in a
    /// balanced KD-Tree is O(log n) where linear search is O(n). Points in the KD-Tree are
    /// equi-length arrays of type <typeparamref name="TDimension"/>. The node objects associated
    /// with the points is an array of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <remarks>
    /// KDTrees can be fairly difficult to understand at first. The following references helped me
    /// understand what exactly a KDTree is doing and the contain the best descriptions of searches in a KDTree.
    /// Samet's book is the best reference of multidimensional data structures I have ever seen. Wikipedia is also a good starting place.
    /// References:
    /// <ul style="list-style-type:none">
    /// <li> <a href="http://store.elsevier.com/product.jsp?isbn=9780123694461">Foundations of Multidimensional and Metric Data Structures, 1st Edition, by Hanan Samet. ISBN: 9780123694461</a> </li>
    /// <li> <a href="https://en.wikipedia.org/wiki/K-d_tree"> https://en.wikipedia.org/wiki/K-d_tree</a> </li>
    /// </ul>
    /// </remarks>
    /// <typeparam name="TDimension">The type of the dimension.</typeparam>
    /// <typeparam name="TNode">The type representing the actual node objects.</typeparam>
    [Serializable]
    public class KDTree<TDimension, TPriority, TNode>
        where TDimension : IComparable<TDimension>, IMinMaxValue<TDimension>
        where TPriority : INumber<TPriority>, IMinMaxValue<TPriority>
    {
        /// <summary>
        /// The number of points in the KDTree
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// The numbers of dimensions that the tree has.
        /// </summary>
        public int Dimensions { get; }

        /// <summary>
        /// The array in which the binary tree is stored. Enumerating this array is a level-order traversal of the tree.
        /// </summary>
        public IList<IReadOnlyList<TDimension>> InternalPointArray { get; }

        /// <summary>
        /// The array in which the node objects are stored. There is a one-to-one correspondence with this array and the <see cref="InternalPointArray"/>.
        /// </summary>
        public IList<TNode> InternalNodeArray { get; }

        /// <summary>
        /// The metric function used to calculate distance between points.
        /// </summary>
        public Func<IReadOnlyList<TDimension>, IReadOnlyList<TDimension>, TPriority> Metric { get; set; }

        /// <summary>
        /// Gets a <see cref="BinaryTreeNavigator{TPoint,TNode}"/> that allows for manual tree navigation,
        /// </summary>
        internal BinaryTreeNavigator<IReadOnlyList<TDimension>, TNode> Navigator
            => new BinaryTreeNavigator<IReadOnlyList<TDimension>, TNode>(this.InternalPointArray, this.InternalNodeArray);

        /// <summary>
        /// The maximum value along any dimension.
        /// </summary>
        private TDimension MaxValue { get; init; }

        /// <summary>
        /// The minimum value along any dimension.
        /// </summary>
        private TDimension MinValue { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KDTree{TDimension,TNode}". /> class.
        /// It is unlikely that this constructor will be used directly, as it is more common to use the
        /// Create method to create a KDTree from a set of points and nodes. This can be used and
        /// is left here for created more complex KD-Trees where the points have unique distance metrics.
        /// or the type of the distance metric is different from the type of the points.
        /// </summary>
        /// <param name="dimensions">The number of dimensions in the data set.</param>
        /// <param name="points">The points to be constructed into a <see cref="KDTree{TDimension,TNode}"/></param>
        /// <param name="nodes">The nodes associated with each point.</param>
        /// <param name="metric">The metric function which implicitly defines the metric space in which the KDTree operates in. This should satisfy the triangle inequality.</param>
        /// <param name="searchWindowMinValue">The minimum value to be used in node searches. If null, we assume that <typeparamref name="TDimension"/> has a static field named "MinValue". All numeric structs have this field.</param>
        /// <param name="searchWindowMaxValue">The maximum value to be used in node searches. If null, we assume that <typeparamref name="TDimension"/> has a static field named "MaxValue". All numeric structs have this field.</param>
        public KDTree(
            int dimensions,
            IEnumerable<IReadOnlyList<TDimension>> points, int pointsCount,
            IEnumerable<TNode> nodes,
            Func<IReadOnlyList<TDimension>, IReadOnlyList<TDimension>, TPriority> metric,
            TDimension searchWindowMinValue = default,
            TDimension searchWindowMaxValue = default)
        {
            // Attempt find the Min/Max value if null.
            if (searchWindowMinValue.Equals(default(TDimension)))
                this.MinValue = TDimension.MinValue;
            else
                this.MinValue = searchWindowMinValue;

            if (searchWindowMaxValue.Equals(default(TDimension)))
                this.MaxValue = TDimension.MaxValue;
            else
                this.MaxValue = searchWindowMaxValue;

            // Calculate the number of nodes needed to contain the binary tree.
            // This is equivalent to finding the power of 2 greater than the number of points
            var elementCount = (int)Math.Pow(2, (int)(Math.Log(pointsCount) / Math.Log(2)) + 1);
            this.Dimensions = dimensions;
            this.InternalPointArray = Enumerable.Repeat(default(IReadOnlyList<TDimension>), elementCount).ToArray();
            this.InternalNodeArray = Enumerable.Repeat(default(TNode), elementCount).ToArray();
            this.Metric = metric;
            this.Count = pointsCount;
            this.GenerateTree(0, 0, points, pointsCount, nodes);
        }


        public KDTree(int dimensions, ICollection<IReadOnlyList<TDimension>> points, IEnumerable<TNode> nodes, Func<IReadOnlyList<TDimension>,
            IReadOnlyList<TDimension>, TPriority> metric, TDimension searchWindowMinValue = default(TDimension), TDimension searchWindowMaxValue = default(TDimension))
            : this(dimensions, points, points.Count, nodes, metric, searchWindowMinValue, searchWindowMaxValue)
        {
        }




        /// <summary>
        /// Finds the nearest numNeighbors in the <see cref="KDTree{TDimension,TNode}"/> of the given <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The point whose numNeighbors we search for.</param>
        /// <param name="numNeighbors">The number of numNeighbors to look for.</param>
        /// <returns>The</returns>
        public IEnumerable<(IReadOnlyList<TDimension>, TNode)> NearestNeighbors(IReadOnlyList<TDimension> point, int numNeighbors)
        {
            var nearestNeighborList = new BoundedPriorityList<int, TPriority>(numNeighbors, true);
            var rect = HyperRect<TDimension>.Infinite(this.Dimensions, this.MaxValue, this.MinValue);
            this.SearchForNearestNeighbors(0, point, rect, 0, nearestNeighborList, TPriority.MaxValue);

            return ToResultSet(nearestNeighborList);
        }

        /// <summary>
        /// Searches for the closest points in a hyper-sphere around the given center.
        /// </summary>
        /// <param name="center">The center of the hyper-sphere</param>
        /// <param name="radius">The radius of the hyper-sphere</param>
        /// <param name="numNeighbors">The number of numNeighbors to return.</param>
        /// <returns>The specified number of closest points in the hyper-sphere</returns>
        public IEnumerable<(IReadOnlyList<TDimension>, TNode)> RadialSearch(IReadOnlyList<TDimension> center, TPriority radius, int numNeighbors = -1)
        {
            var nearestNeighbors = (numNeighbors == -1) ? new BoundedPriorityList<int, TPriority>(this.Count)
                                                        : new BoundedPriorityList<int, TPriority>(numNeighbors, true);
            this.SearchForNearestNeighbors(
                0,
                center,
                HyperRect<TDimension>.Infinite(this.Dimensions, this.MaxValue, this.MinValue),
                0,
                nearestNeighbors,
                radius);

            return ToResultSet(nearestNeighbors);
        }

        /// <summary>
        /// Grows a KD tree recursively via median splitting. We find the median by doing a full sort.
        /// </summary>
        /// <param name="index">The array index for the current node.</param>
        /// <param name="dim">The current splitting dimension.</param>
        /// <param name="points">The set of points remaining to be added to the kd-tree</param>
        /// <param name="nodes">The set of nodes RE</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateTree(
            int index,
            int dim,
            IEnumerable<IReadOnlyList<TDimension>> points, int pointsCount,
            IEnumerable<TNode> nodes)
        {
            var isEven = int.IsEvenInteger(pointsCount);
            var medianIndex = pointsCount / 2;
            var medianValue // this the 1st enumeration of points
                = CalcMedian.GetNthPosition(points.Select(p => p[dim]), medianIndex, pointsCount);

            // We now split the sorted points into 2 groups
            // 1st group: points before the median
            var leftPoints = new IReadOnlyList<TDimension>[medianIndex];
            var leftNodes = new TNode[medianIndex];
            // 2nd group: Points after the median
            var rightPoints = new IReadOnlyList<TDimension>[isEven ? medianIndex - 1 : medianIndex];
            var rightNodes = new TNode[isEven ? medianIndex - 1 : medianIndex];

            var leftSideFilled = false;
            var medianFilled = false;
            var leftIndex = 0;
            var rightIndex = 0;
            var nodeEnumerator = nodes.GetEnumerator();
            foreach (var point in points) // this the 2nd enumeration of points!
            {
                if (!nodeEnumerator.MoveNext())
                    throw new InvalidOperationException("The number of nodes is less than the number of points: the collections should be the same size.");
                var node = nodeEnumerator.Current;
                var pointValue = point[dim];
                var compare = pointValue.CompareTo(medianValue);
                if (!medianFilled && compare == 0)
                {
                    InternalPointArray[index] = point;
                    InternalNodeArray[index] = node;
                    medianFilled=true;
                }
                else if (!leftSideFilled && compare <= 0)
                {
                    // point is on the left side of the median
                    leftPoints[leftIndex] = point;
                    leftNodes[leftIndex] = node;
                    leftIndex++;
                    leftSideFilled = leftIndex == medianIndex;
                }
                else //if (compare > 0 || (!rightSideFilled && compare == 0))
                {
                    // point is on the right side of the median
                    rightPoints[rightIndex] = point;
                    rightNodes[rightIndex] = node;
                    rightIndex++;
                    //rightSideFilled = true;
                }
            }
            // We new recurse, passing the left and right arrays for arguments.
            // The current node's left and right values become the "roots" for
            // each recursion call. We also forward cycle to the next dimension.
            var nextDim = (dim + 1) % this.Dimensions; // select next dimension

            // We only need to recurse if the point array contains more than one point
            // If the array has no points then the node stay a null value
            if (leftPoints.Length <= 1)
            {
                if (leftPoints.Length == 1)
                {
                    this.InternalPointArray[LeftChildIndex(index)] = leftPoints[0];
                    this.InternalNodeArray[LeftChildIndex(index)] = leftNodes[0];
                }
            }
            else
            {
                this.GenerateTree(LeftChildIndex(index), nextDim, leftPoints, leftPoints.Length, leftNodes);
            }

            // Do the same for the right points
            if (rightPoints.Length <= 1)
            {
                if (rightPoints.Length == 1)
                {
                    this.InternalPointArray[RightChildIndex(index)] = rightPoints[0];
                    this.InternalNodeArray[RightChildIndex(index)] = rightNodes[0];
                }
            }
            else
            {
                this.GenerateTree(RightChildIndex(index), nextDim, rightPoints, rightPoints.Length, rightNodes);
            }
        }

        /// <summary>
        /// A top-down recursive method to find the nearest numNeighbors of a given point.
        /// </summary>
        /// <param name="nodeIndex">The index of the node for the current recursion branch.</param>
        /// <param name="target">The point whose numNeighbors we are trying to find.</param>
        /// <param name="rect">The <see cref="HyperRect{T}"/> containing the possible nearest numNeighbors.</param>
        /// <param name="dimension">The current splitting dimension for this recursion branch.</param>
        /// <param name="nearestNeighbors">The <see cref="BoundedPriorityList{TElement,TPriority}"/> containing the nearest numNeighbors already discovered.</param>
        /// <param name="maxSearchRadiusSquared">The squared radius of the current largest distance to search from the <paramref name="target"/></param>
        private void SearchForNearestNeighbors(
            int nodeIndex,
            IReadOnlyList<TDimension> target,
            HyperRect<TDimension> rect,
            int dimension,
            BoundedPriorityList<int, TPriority> nearestNeighbors,
            TPriority maxSearchRadiusSquared)
        {
            if (this.InternalPointArray.Count <= nodeIndex || nodeIndex < 0
                || this.InternalPointArray[nodeIndex] == null)
            {
                return;
            }

            // Work out the current dimension
            var dim = dimension % this.Dimensions;

            // Split our hyper-rectangle into 2 sub rectangles along the current
            // node's point on the current dimension
            var leftRect = rect.Clone();
            leftRect.MaxPoint[dim] = this.InternalPointArray[nodeIndex][dim];

            var rightRect = rect.Clone();
            rightRect.MinPoint[dim] = this.InternalPointArray[nodeIndex][dim];

            // Determine which side the target resides in
            var compare = target[dim].CompareTo(this.InternalPointArray[nodeIndex][dim]);

            var nearerRect = compare <= 0 ? leftRect : rightRect;
            var furtherRect = compare <= 0 ? rightRect : leftRect;

            var nearerNode = compare <= 0 ? LeftChildIndex(nodeIndex) : RightChildIndex(nodeIndex);
            var furtherNode = compare <= 0 ? RightChildIndex(nodeIndex) : LeftChildIndex(nodeIndex);

            // Move down into the nearer branch
            this.SearchForNearestNeighbors(
                nearerNode,
                target,
                nearerRect,
                dimension + 1,
                nearestNeighbors,
                maxSearchRadiusSquared);

            // Walk down into the further branch but only if our capacity hasn't been reached
            // OR if there's a region in the further rectangle that's closer to the target than our
            // current furtherest nearest neighbor
            var closestPointInFurtherRect = furtherRect.GetClosestPoint(target);
            var distanceSquaredToTarget = this.Metric(closestPointInFurtherRect, target);

            if (distanceSquaredToTarget.CompareTo(maxSearchRadiusSquared) <= 0)
            {
                if (nearestNeighbors.IsFull)
                {
                    if (distanceSquaredToTarget.CompareTo(nearestNeighbors.MaxPriority) < 0)
                    {
                        this.SearchForNearestNeighbors(
                            furtherNode,
                            target,
                            furtherRect,
                            dimension + 1,
                            nearestNeighbors,
                            maxSearchRadiusSquared);
                    }
                }
                else
                {
                    this.SearchForNearestNeighbors(
                        furtherNode,
                        target,
                        furtherRect,
                        dimension + 1,
                        nearestNeighbors,
                        maxSearchRadiusSquared);
                }
            }

            // Try to add the current node to our nearest numNeighbors list
            distanceSquaredToTarget = this.Metric(this.InternalPointArray[nodeIndex], target);
            if (distanceSquaredToTarget.CompareTo(maxSearchRadiusSquared) <= 0)
            {
                nearestNeighbors.Add(nodeIndex, distanceSquaredToTarget);
            }
        }

        /// <summary>
        /// Takes a <see cref="BoundedPriorityList{TElement,TPriority}"/> storing the indexes of the points and nodes of a KDTree
        /// and returns the points and nodes.
        /// </summary>
        /// <param name="list">The <see cref="BoundedPriorityList{TElement,TPriority}"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<(IReadOnlyList<TDimension>, TNode)> ToResultSet(BoundedPriorityList<int, TPriority> list)
        {
            for (var i = 0; i < list.Count; i++)
                yield return new(InternalPointArray[list[i]], InternalNodeArray[list[i]]);
        }
    }
}
