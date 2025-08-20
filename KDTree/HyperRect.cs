// <copyright file="HyperRect.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace SuperClusterKDTree
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents a hyper-rectangle. An N-Dimensional rectangle.
    /// </summary>
    /// <typeparam name="T">The type of "dimension" in the metric space in which the hyper-rectangle lives.</typeparam>
    internal readonly ref struct HyperRect<T>
        where T : IComparable<T>
    {
        private readonly Memory<T> minPoint;
        private readonly Memory<T> maxPoint;

        public Span<T> MinPoint => minPoint.Span;
        public Span<T> MaxPoint => maxPoint.Span;

        private HyperRect(Memory<T> minPoint, Memory<T> maxPoint)
        {
            this.minPoint = minPoint;
            this.maxPoint = maxPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HyperRect<T> Create(Memory<T> minPoint, Memory<T> maxPoint)
        {
            return new HyperRect<T>(minPoint, maxPoint);
        }

        public static HyperRect<T> Infinite(int dimensions, T positiveInfinity, T negativeInfinity)
        {
            var minPoint = new T[dimensions];
            var maxPoint = new T[dimensions];

            var minSpan = minPoint.AsSpan();
            var maxSpan = maxPoint.AsSpan();

            minSpan.Fill(negativeInfinity);
            maxSpan.Fill(positiveInfinity);

            return new HyperRect<T>(minPoint, maxPoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<T> GetClosestPoint(IReadOnlyList<T> toPoint)
        {
            var closest = new T[toPoint.Count];
            var closestSpan = closest.AsSpan();
            var minSpan = MinPoint;
            var maxSpan = MaxPoint;

            for (var dimension = 0; dimension < toPoint.Count; dimension++)
            {
                var current = toPoint[dimension];
                if (minSpan[dimension].CompareTo(current) > 0)
                {
                    closestSpan[dimension] = minSpan[dimension];
                }
                else if (maxSpan[dimension].CompareTo(current) < 0)
                {
                    closestSpan[dimension] = maxSpan[dimension];
                }
                else
                {
                    closestSpan[dimension] = current;
                }
            }

            return closest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HyperRect<T> Clone()
        {
            var newMin = new T[minPoint.Length];
            var newMax = new T[maxPoint.Length];
            
            MinPoint.CopyTo(newMin);
            MaxPoint.CopyTo(newMax);
            
            return new HyperRect<T>(newMin, newMax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMinPoint(int dimension, T value)
        {
            MinPoint[dimension] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMaxPoint(int dimension, T value)
        {
            MaxPoint[dimension] = value;
        }
    }
}
