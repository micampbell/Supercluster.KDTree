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
                metric = L1NormDistanceDouble;
            else if (distanceMetric == DistanceMetrics.EuclideanDistance)
                metric = L2NormDistanceDouble;
            else //if (proximityType == ProximityTypes.ChebyshevDistance)
                metric = LInfNormDistanceDouble;

            return new KDTree<double, TNode>(points[0].Length, points, nodes, metric, default, default);
        }


        private static readonly Func<double[], double[], double> L2NormDistanceDouble = L2NormDistanceDoubleImpl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double L2NormDistanceDoubleImpl(double[] x, double[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += (x[i] - y[i]) * (x[i] - y[i]);

            return dist;
        }

        private static readonly Func<double[], double[], double> L1NormDistanceDouble = L1NormDistanceDoubleImpl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double L1NormDistanceDoubleImpl(double[] x, double[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += Math.Abs(x[i] - y[i]);

            return dist;
        }

        private static readonly Func<double[], double[], double> LInfNormDistanceDouble = LInfNormDistanceDoubleImpl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double LInfNormDistanceDoubleImpl(double[] x, double[] y)
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
                metric = L1NormDistanceFloat;
            else if (distanceMetric == DistanceMetrics.EuclideanDistance)
                metric = L2NormDistanceFloat;
            else //if (proximityType == ProximityTypes.ChebyshevDistance)
                metric = LInfNormDistanceFloat;

            return new KDTree<float, TNode>(points[0].Length, points, nodes, metric, default, default);
        }

        private static readonly Func<float[], float[], double> L2NormDistanceFloat = L2NormDistanceFloatImpl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double L2NormDistanceFloatImpl(float[] x, float[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += (x[i] - y[i]) * (x[i] - y[i]);

            return dist;
        }

        private static readonly Func<float[], float[], double> L1NormDistanceFloat = L1NormDistanceFloatImpl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double L1NormDistanceFloatImpl(float[] x, float[] y)
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += Math.Abs(x[i] - y[i]);

            return dist;
        }

        private static readonly Func<float[], float[], double> LInfNormDistanceFloat = LInfNormDistanceFloatImpl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double LInfNormDistanceFloatImpl(float[] x, float[] y)
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
                metric = L1NormDistanceInt;
            else if (distanceMetric == DistanceMetrics.EuclideanDistance)
                metric = L2NormDistanceInt;
            else //if (proximityType == ProximityTypes.ChebyshevDistance)
                metric = LInfNormDistanceInt;

            return new KDTree<int, TNode>(points[0].Length, points, nodes, metric, default, default);
        }


        private static readonly Func<int[], int[], double> L2NormDistanceInt = L2NormDistanceIntImpl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double L2NormDistanceIntImpl(int[] x, int[] y)
        {
            int dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += (x[i] - y[i]) * (x[i] - y[i]);

            return dist;
        }

        private static readonly Func<int[], int[], double> L1NormDistanceInt = L1NormDistanceIntImpl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double L1NormDistanceIntImpl(int[] x, int[] y)
        {
            int dist = 0;
            for (int i = 0; i < x.Length; i++)
                dist += Math.Abs(x[i] - y[i]);

            return dist;
        }

        private static readonly Func<int[], int[], double> LInfNormDistanceInt = LInfNormDistanceIntImpl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double LInfNormDistanceIntImpl(int[] x, int[] y)
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
