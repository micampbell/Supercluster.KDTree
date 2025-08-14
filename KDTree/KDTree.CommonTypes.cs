// <copyright file="KDTree.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace SuperClusterKDTree
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public class KDTree
    {
        #region when points are TDimensions
        /// <summary>
        /// Initializes a new instance of the <see cref="KDTree{TDimension, TNode}"/>.
        /// This is simpler than the KDTree constructor as the most common use case is
        /// to have an n-dimension point that is comprised of TDimensions and the
        /// distance metric is likey the L1, L2 or L∞ norm.
        /// </summary>
        /// <param name="points">The points to be constructed into a <see cref="KDTree{TDimension,TNode}"/></param>
        /// <param name="nodes">The nodes associated with each point.</param>
        /// <param name="distanceMetric">The nodes associated with each point.</param>
        public static KDTree<TDimension, TDimension, TNode> Create<TDimension, TNode>(IList<IReadOnlyList<TDimension>> points,
            IList<TNode> nodes, DistanceMetrics distanceMetric)
            where TDimension : INumber<TDimension>, IMinMaxValue<TDimension>
        {
            Func<IReadOnlyList<TDimension>, IReadOnlyList<TDimension>, TDimension> metric;
            if (distanceMetric == DistanceMetrics.ManhattanDistance)
                metric = (x, y) => ManhattanDistance(x, y);
            //metric = ManhattanDistanceFuncTDimension;
            else if (distanceMetric == DistanceMetrics.EuclideanDistance)
                metric = (x, y) => EuclideanDistance(x, y);
            else //if (proximityType == ProximityTypes.ChebyshevDistance)
                metric = (x, y) => ChebyshevDistance(x, y);

            return new KDTree<TDimension, TDimension, TNode>(points[0].Count, points, nodes, metric, default, default);
        }

        /// <summary>
        /// Calculates the squared Euclidean (L2 norm) distance between two points represented as TDimension arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The squared Euclidean distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDimension EuclideanDistance<TDimension>(IReadOnlyList<TDimension> x, IReadOnlyList<TDimension> y)
            where TDimension : INumber<TDimension>
        {
            TDimension dist = TDimension.Zero;
            for (int i = 0; i < x.Count; i++)
                dist += (x[i] - y[i]) * (x[i] - y[i]);

            return dist;
        }


        /// <summary>
        /// Calculates the Manhattan (L1 norm) distance between two points represented as TDimension arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The Manhattan distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDimension ManhattanDistance<TDimension>(IReadOnlyList<TDimension> x, IReadOnlyList<TDimension> y)
            where TDimension : INumber<TDimension>
        {
            TDimension dist = TDimension.Zero;
            for (int i = 0; i < x.Count; i++)
                dist += Abs(x[i] - y[i]);

            return dist;
        }


        /// <summary>
        /// Calculates the Chebyshev (L∞ norm) distance between two points represented as TDimension arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The Chebyshev distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDimension ChebyshevDistance<TDimension>(IReadOnlyList<TDimension> x, IReadOnlyList<TDimension> y)
            where TDimension : INumber<TDimension>
        {
            TDimension dist = TDimension.Zero;
            for (int i = 0; i < x.Count; i++)
            {
                var d = Abs(x[i] - y[i]);
                if (d > dist)
                    dist = d;
            }
            return dist;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TDimension Abs<TDimension>(TDimension x)
            where TDimension : INumber<TDimension>
        {
            if (x < TDimension.Zero)
                return -x;
            return x;
        }
        #endregion
    }

}
