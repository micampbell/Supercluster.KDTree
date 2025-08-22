namespace NearestNeighborSearch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    /// <summary>
    /// Provides point search operations (nearest neighbor and radial search) over an indexed point set.
    /// </summary>
    /// <typeparam name="TDimension">The dimension value type.</typeparam>
    /// <typeparam name="TPriority">The priority / distance numeric type.</typeparam>
    /// <typeparam name="TNode">The associated node payload type.</typeparam>
    public abstract class SearchMethod<TDimension, TPriority, TNode>
        where TDimension : IComparable<TDimension>, IMinMaxValue<TDimension>, INumber<TDimension>
        where TPriority : INumber<TPriority>, IMinMaxValue<TPriority>
    {
        protected SearchMethod(ICollection<IReadOnlyList<TDimension>> points, Func<IReadOnlyList<TDimension>, IReadOnlyList<TDimension>, TPriority> metric,
            TDimension searchWindowMinValue, TDimension searchWindowMaxValue)
        {
            // Attempt find the Min/Max value if null.
            if (searchWindowMinValue.Equals(default))
                this.MinValue = TDimension.MinValue;
            else
                this.MinValue = searchWindowMinValue;

            if (searchWindowMaxValue.Equals(default))
                this.MaxValue = TDimension.MaxValue;
            else
                this.MaxValue = searchWindowMaxValue;
            this.Dimensions = points.First().Count;
            this.Metric = metric;
            this.Count = points.Count;
        }

        /// <summary>
        /// Gets the number of points.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the number of dimensions.
        /// </summary>
        public int Dimensions { get; }
        /// <summary>
        /// The metric function used to calculate distance between points.
        /// </summary>
        protected Func<IReadOnlyList<TDimension>, IReadOnlyList<TDimension>, TPriority> Metric { get; set; }

        /// <summary>
        /// The maximum value along any dimension.
        /// </summary>
        protected TDimension MaxValue { get; init; }

        /// <summary>
        /// The minimum value along any dimension.
        /// </summary>
        protected TDimension MinValue { get; init; }

        /// <summary>
        /// Gets all the data points and their associated nodes in no particular order.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetAllData();

        /// <summary>
        /// Finds the single nearest neighbor to the specified <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The query point.</param>
        /// <returns>An enumerable of point/node tuples ordered by increasing distance.</returns>
        public abstract (IReadOnlyList<TDimension>, TNode) GetNearestNeighbor(IReadOnlyList<TDimension> point);

        /// <summary>
        /// Finds the nearest neighbors to the specified <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The query point.</param>
        /// <param name="numNeighbors">The number of neighbors to return.</param>
        /// <returns>An enumerable of point/node tuples ordered by increasing distance.</returns>
        public abstract IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNearestNeighbors(IReadOnlyList<TDimension> point, int numNeighbors);

        /// <summary>
        /// Performs a radial search centered at <paramref name="center"/> returning up to <paramref name="numNeighbors"/> closest points within <paramref name="radius"/>.
        /// </summary>
        /// <param name="center">The center point.</param>
        /// <param name="radius">The search radius.</param>
        /// <param name="numNeighbors">Maximum number of neighbors to return (-1 for all within radius).</param>
        /// <returns>An enumerable of point/node tuples ordered by increasing distance.</returns>
        public abstract IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNeighborsInRadius(IReadOnlyList<TDimension> center, TPriority radius, int numNeighbors = -1);


    }
}
