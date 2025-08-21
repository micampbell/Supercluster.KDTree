namespace SuperClusterKDTreeSpan
{
    /// <summary>
    /// Some default and common proximity types.
    /// </summary>
    public enum DistanceMetrics
    {
        /// <summary>
        /// AKA "L1 Distance", "Taxicab Distance", "RectilinearDistance. 
        /// The sum of the absolute differences of the coordinates.
        /// </summary>
        ManhattanDistance,
        /// <summary>
        /// AKA "L2 Distance", "Straightline Distance Squared"
        /// This is the sum of the squared differences of the coordinates.
        /// It does NOT take the square root (in order to be faster).
        /// But if you do, then you get the straight-line distance between two points in Euclidean space.
        /// </summary>
        EuclideanDistance,
        /// <summary>
        /// Chebyshev Distance, also known as "L∞ Distance" or "Maximum Metric". This is the maximum 
        /// absolute difference between the coordinates.
        ChebyshevDistance,
        /// <summary>
        /// Cosine Distance is 1 - S_c where S_c is the cosine similarity
        /// see: https://en.wikipedia.org/wiki/Cosine_similarity
        CosineDistance
    }
}
