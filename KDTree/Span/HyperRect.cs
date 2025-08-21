// <copyright file="HyperRect.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace SuperClusterKDTreeSpan
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a hyper-rectangle. An N-Dimensional rectangle.
    /// </summary>
    /// <typeparam name="T">The type of "dimension" in the metric space in which the hyper-rectangle lives.</typeparam>
    internal struct HyperRect<T>
        where T : IComparable<T>
    {
        /// <summary>
        /// Backing field for the <see cref="MinPoint"/> property.
        /// </summary>
        private T[] minPoint;

        /// <summary>
        /// Backing field for the <see cref="MaxPoint"/> property.
        /// </summary>
        private T[] maxPoint;

        /// <summary>
        /// The minimum point of the hyper-rectangle. One can think of this point as the
        /// bottom-left point of a 2-Dimensional rectangle.
        /// </summary>
        public T[] MinPoint
        {
            get
            {
                return this.minPoint;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this.minPoint = new T[value.Length];
                // Using spans for potentially more optimized copy
                value.AsSpan().CopyTo(this.minPoint);
            }
        }

        /// <summary>
        /// The maximum point of the hyper-rectangle. One can think of this point as the
        /// top-right point of a 2-Dimensional rectangle.
        /// </summary>
        public T[] MaxPoint
        {
            get
            {
                return this.maxPoint;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this.maxPoint = new T[value.Length];
                // Using spans for potentially more optimized copy
                value.AsSpan().CopyTo(this.maxPoint);
            }
        }

        /// <summary>
        /// Get a hyper rectangle which spans the entire implicit metric space.
        /// </summary>
        /// <param name="dimensions">The number of dimensions in the hyper-rectangle's metric space.</param>
        /// <param name="positiveInfinity">The smallest possible values in any given dimension.</param>
        /// <param name="negativeInfinity">The largest possible values in any given dimension.</param>
        /// <returns>The hyper-rectangle which spans the entire metric space.</returns>
        public static HyperRect<T> Infinite(int dimensions, T positiveInfinity, T negativeInfinity)
        {
            var rect = default(HyperRect<T>);

            // Initialize backing fields directly to avoid double allocation through setters
            rect.minPoint = new T[dimensions];
            rect.maxPoint = new T[dimensions];

            // Fill using spans/Array.Fill for efficiency
            rect.minPoint.AsSpan().Fill(negativeInfinity);
            rect.maxPoint.AsSpan().Fill(positiveInfinity);

            return rect;
        }

        /// <summary>
        /// Gets the point on the rectangle that is closest to the given point.
        /// If the point is within the rectangle, then the input point is the same as the
        /// output point.f the point is outside the rectangle then the point on the rectangle
        /// that is closest to the given point is returned.
        /// </summary>
        /// <param name="toPoint">We try to find a point in or on the rectangle closest to this point.</param>
        /// <returns>The point on or in the rectangle that is closest to the given point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<T> GetClosestPoint(IReadOnlyList<T> toPoint)
        {
            var closest = new T[toPoint.Count];

            var min = this.minPoint;
            var max = this.maxPoint;

            if (toPoint is T[] arr)
            {
                var toSpan = arr.AsSpan();
                var closestSpan = closest.AsSpan();
                var minSpan = min.AsSpan();
                var maxSpan = max.AsSpan();

                for (int dimension = 0; dimension < toSpan.Length; dimension++)
                {
                    var value = toSpan[dimension];
                    if (minSpan[dimension].CompareTo(value) > 0)
                    {
                        closestSpan[dimension] = minSpan[dimension];
                    }
                    else if (maxSpan[dimension].CompareTo(value) < 0)
                    {
                        closestSpan[dimension] = maxSpan[dimension];
                    }
                    else
                    {
                        // Point is within rectangle, at least on this dimension
                        closestSpan[dimension] = value;
                    }
                }

                return closest;
            }
            else if (toPoint is List<T> list)
            {
                var toSpan = CollectionsMarshal.AsSpan(list);
                var closestSpan = closest.AsSpan();
                var minSpan = min.AsSpan();
                var maxSpan = max.AsSpan();

                for (int dimension = 0; dimension < toSpan.Length; dimension++)
                {
                    var value = toSpan[dimension];
                    if (minSpan[dimension].CompareTo(value) > 0)
                    {
                        closestSpan[dimension] = minSpan[dimension];
                    }
                    else if (maxSpan[dimension].CompareTo(value) < 0)
                    {
                        closestSpan[dimension] = maxSpan[dimension];
                    }
                    else
                    {
                        closestSpan[dimension] = value;
                    }
                }

                return closest;
            }
            else
            {
                for (var dimension = 0; dimension < toPoint.Count; dimension++)
                {
                    var value = toPoint[dimension];
                    if (min[dimension].CompareTo(value) > 0)
                    {
                        closest[dimension] = min[dimension];
                    }
                    else if (max[dimension].CompareTo(value) < 0)
                    {
                        closest[dimension] = max[dimension];
                    }
                    else
                    {
                        // Point is within rectangle, at least on this dimension
                        closest[dimension] = value;
                    }
                }

                return closest;
            }
        }

        /// <summary>
        /// Clones the <see cref="HyperRect{T}"/>.
        /// </summary>
        /// <returns>A clone of the <see cref="HyperRect{T}"/></returns>
        public HyperRect<T> Clone()
        {
            // For a discussion of why we don't implement ICloneable
            // see http://stackoverflow.com/questions/536349/why-no-icloneablet
            var rect = default(HyperRect<T>);

            // Copy backing arrays directly to avoid extra allocations via property setters
            var min = this.minPoint;
            var max = this.maxPoint;

            rect.minPoint = new T[min.Length];
            rect.maxPoint = new T[max.Length];

            min.AsSpan().CopyTo(rect.minPoint);
            max.AsSpan().CopyTo(rect.maxPoint);

            return rect;
        }
    }
}
