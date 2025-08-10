// <copyright file="KDTree.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace SuperClusterKDTree
{
    using System;
    using System.Runtime.CompilerServices;

    public class KDTree
    {
        #region when points are doubles
        /// <summary>
        /// Initializes a new instance of the <see cref="KDTree{double, TNode}"/>.
        /// This is simpler than the KDTree constructor as the most common use case is
        /// to have an n-dimension point that is comprised of doubles and the
        /// distance metric is likey the L1, L2 or L∞ norm.
        /// </summary>
        /// <param name="points">The points to be constructed into a <see cref="KDTree{TDimension,TNode}"/></param>
        /// <param name="nodes">The nodes associated with each point.</param>
        /// <param name="distanceMetric">The nodes associated with each point.</param>
        public static KDTree<double, TNode> Create<TNode>(double[][] points, TNode[] nodes, DistanceMetrics distanceMetric)
        {
            Func<double[], double[], double> metric;
            if (distanceMetric == DistanceMetrics.ManhattanDistance)
                metric = ManhattanDistanceFuncDouble;
            else if (distanceMetric == DistanceMetrics.EuclideanDistance)
                metric = EuclideanDistanceFuncDouble;
            else //if (proximityType == ProximityTypes.ChebyshevDistance)
                metric = ChebyshevDistanceFuncDouble;

            return new KDTree<double, TNode>(points[0].Length, points, nodes, metric, default, default);
        }


        private static readonly Func<double[], double[], double> EuclideanDistanceFuncDouble = EuclideanDistance;

        /// <summary>
        /// Calculates the squared Euclidean (L2 norm) distance between two points represented as double arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The squared Euclidean distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double EuclideanDistance(double[] x, double[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += (x[i] - y[i]) * (x[i] - y[i]);

            return dist;
        }

        private static readonly Func<double[], double[], double> ManhattanDistanceFuncDouble = ManhattanDistance;

        /// <summary>
        /// Calculates the Manhattan (L1 norm) distance between two points represented as double arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The Manhattan distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ManhattanDistance(double[] x, double[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += Math.Abs(x[i] - y[i]);

            return dist;
        }

        private static readonly Func<double[], double[], double> ChebyshevDistanceFuncDouble = ChebyshevDistance;

        /// <summary>
        /// Calculates the Chebyshev (L∞ norm) distance between two points represented as double arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The Chebyshev distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ChebyshevDistance(double[] x, double[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
            {
                var d = Math.Abs(x[i] - y[i]);
                if (d > dist)
                    dist = d;
            }
            return dist;
        }
        #endregion

        #region when points are floats
        /// <summary>
        /// Initializes a new instance of the <see cref="KDTree{float, TNode}"/>.
        /// This is simpler than the KDTree constructor as the most common use case is
        /// to have an n-dimension point that is comprised of floats and the
        /// distance metric is likey the L1, L2 or L∞ norm.
        /// </summary>
        /// <param name="points">The points to be constructed into a <see cref="KDTree{TDimension,TNode}"/></param>
        /// <param name="nodes">The nodes associated with each point.</param>
        /// <param name="distanceMetric">The nodes associated with each point.</param>
        public static KDTree<float, TNode> Create<TNode>(float[][] points, TNode[] nodes, DistanceMetrics distanceMetric)
        {
            Func<float[], float[], double> metric;
            if (distanceMetric == DistanceMetrics.ManhattanDistance)
                metric = ManhattanDistanceFuncFloat;
            else if (distanceMetric == DistanceMetrics.EuclideanDistance)
                metric = EuclideanDistanceFuncFloat;
            else //if (proximityType == ProximityTypes.ChebyshevDistance)
                metric = ChebyshevDistanceFuncFloat;

            return new KDTree<float, TNode>(points[0].Length, points, nodes, metric, default, default);
        }

        private static readonly Func<float[], float[], double> EuclideanDistanceFuncFloat = EuclideanDistance;

        /// <summary>
        /// Calculates the squared Euclidean (L2 norm) distance between two points represented as double arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The squared Euclidean distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double EuclideanDistance(float[] x, float[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += (x[i] - y[i]) * (x[i] - y[i]);

            return dist;
        }

        private static readonly Func<float[], float[], double> ManhattanDistanceFuncFloat = ManhattanDistance;

        /// <summary>
        /// Calculates the Manhattan (L1 norm) distance between two points represented as double arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The Manhattan distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ManhattanDistance(float[] x, float[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += Math.Abs(x[i] - y[i]);

            return dist;
        }

        private static readonly Func<float[], float[], double> ChebyshevDistanceFuncFloat = ChebyshevDistance;

        /// <summary>
        /// Calculates the Chebyshev (L∞ norm) distance between two points represented as double arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The Chebyshev distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ChebyshevDistance(float[] x, float[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
            {
                var d = Math.Abs(x[i] - y[i]);
                if (d > dist)
                    dist = d;
            }
            return dist;
        }
        #endregion

        #region when points are ints
        /// <summary>
        /// Initializes a new instance of the <see cref="KDTree{int, TNode}"/>.
        /// This is simpler than the KDTree constructor as the most common use case is
        /// to have an n-dimension point that is comprised of ints and the
        /// distance metric is likey the L1, L2 or L∞ norm.
        /// </summary>
        /// <param name="points">The points to be constructed into a <see cref="KDTree{TDimension,TNode}"/></param>
        /// <param name="nodes">The nodes associated with each point.</param>
        /// <param name="distanceMetric">The nodes associated with each point.</param>
        public static KDTree<int, TNode> Create<TNode>(int[][] points, TNode[] nodes, DistanceMetrics distanceMetric)
        {
            Func<int[], int[], double> metric;
            if (distanceMetric == DistanceMetrics.ManhattanDistance)
                metric = ManhattanDistanceFuncInt;
            else if (distanceMetric == DistanceMetrics.EuclideanDistance)
                metric = EuclideanDistanceFuncInt;
            else //if (proximityType == ProximityTypes.ChebyshevDistance)
                metric = ChebyshevDistanceFuncInt;

            return new KDTree<int, TNode>(points[0].Length, points, nodes, metric, default, default);
        }


        private static readonly Func<int[], int[], double> EuclideanDistanceFuncInt = EuclideanDistance;

        /// <summary>
        /// Calculates the squared Euclidean (L2 norm) distance between two points represented as double arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The squared Euclidean distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double EuclideanDistance(int[] x, int[] y)
        {
            int dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += (x[i] - y[i]) * (x[i] - y[i]);

            return dist;
        }

        private static readonly Func<int[], int[], double> ManhattanDistanceFuncInt = ManhattanDistance;

        /// <summary>
        /// Calculates the Manhattan (L1 norm) distance between two points represented as double arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The Manhattan distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ManhattanDistance(int[] x, int[] y)
        {
            int dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += Math.Abs(x[i] - y[i]);

            return dist;
        }

        private static readonly Func<int[], int[], double> ChebyshevDistanceFuncInt = ChebyshevDistance;

        /// <summary>
        /// Calculates the Chebyshev (L∞ norm) distance between two points represented as double arrays.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The Chebyshev distance between <paramref name="x"/> and <paramref name="y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ChebyshevDistance(int[] x, int[] y)
        {
            int dist = 0;
            for (int i = 0; i < x.Length; i++)
            {
                var d = Math.Abs(x[i] - y[i]);
                if (d > dist)
                    dist = d;
            }
            return dist;
        }
        #endregion
    }

}
