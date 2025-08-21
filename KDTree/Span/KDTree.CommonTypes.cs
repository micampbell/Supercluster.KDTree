// <copyright file="KDTree.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace SuperClusterKDTreeSpan
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
            else if (distanceMetric == DistanceMetrics.EuclideanDistance)
                metric = (x, y) => EuclideanDistance(x, y);
            else if (distanceMetric == DistanceMetrics.ChebyshevDistance)
                metric = (x, y) => ChebyshevDistance(x, y);
            else //if (distanceMetric == DistanceMetrics.CosineDistance)
                metric = (x, y) => CosineDistance(x, y);

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
            var distance = TDimension.Zero;
            for (int i = 0; i < x.Count; i++)
                distance += (x[i] - y[i]) * (x[i] - y[i]);

            return distance;
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
            var distance = TDimension.Zero;
            for (int i = 0; i < x.Count; i++)
                distance += Abs(x[i] - y[i]);

            return distance;
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
            var distance = TDimension.Zero;
            for (int i = 0; i < x.Count; i++)
            {
                var d = Abs(x[i] - y[i]);
                if (d > distance)
                    distance = d;
            }
            return distance;
        }

        /// <summary>
        /// Calculates the cosine similarity between two points represented as TDimension arrays.
        /// Cosine similarity measures the cosine of the angle between two vectors, providing a value between -1 and 1.
        /// A value of 1 indicates identical orientation, 0 indicates orthogonality, and -1 indicates opposite orientation.
        /// </summary>
        /// <typeparam name="TDimension">The numeric type of the vector dimensions.</typeparam>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The cosine similarity between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDimension CosineDistance<TDimension>(IReadOnlyList<TDimension> x, IReadOnlyList<TDimension> y)
            where TDimension : INumber<TDimension>, IMinMaxValue<TDimension>
        {
            var dot = TDimension.Zero; //dot product between the two vectors: x and y
            var xMag = TDimension.Zero; //squared magnitude of x
            var yMag = TDimension.Zero; //squared magnitude of y
            for (int i = 0; i < x.Count; i++)
            {
                xMag += x[i] * x[i];
                dot += x[i] * y[i];
                yMag += y[i] * y[i];
            }
            if (dot == TDimension.Zero) return TDimension.One; //if the dot product is zero, the vectors are orthogonal, so similarity is zero
            if (xMag == TDimension.Zero || yMag == TDimension.Zero)
                return (TDimension.One + TDimension.One); //if either vector has zero magnitude, return 2
                                                          // which is from 1 - (-1) or opposite directions
            var xySqrDouble = double.Sqrt(double.CreateChecked(xMag * yMag));
            var xySqr = TDimension.CreateChecked(xySqrDouble);
            return TDimension.One - dot / xySqr; //return the 1 - cosine similarity
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
