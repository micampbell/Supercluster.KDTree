using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System;
using System.Collections.Generic;
using System.Linq;


namespace NearestNeighborSearch.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, iterationCount: 5)]
    public class Benchmarks
    {
        // Test parameters - varying dimensions from 2D to 12D
        [Params(2, 3,4)]//  9, 12, 15)]
        public int Dimensions { get; set; }

        // Data sizes for different scenarios
        [Params(8000, 100000)] //, 1000000)]
        public int DataSize { get; set; }

        // Number of neighbors to search for
        [Params(10,100)] //, 300)] 
        public int NeighborCount { get; set; }

        // Test data
        private IReadOnlyList<double>[] _points;
        private string[] _nodes;
        private IReadOnlyList<double>[] _queryPoints;
        private KDTree<double, double, string> _kdTree;
        private LinearSearch<double, double, string> _linear;
        private VoxelSearch<double, string> _voxel;

        // Metrics
        private static readonly Func<IReadOnlyList<double>, IReadOnlyList<double>, double> L2Metric = (x, y) =>
        {
            double dist = 0.0;
            for (int i = 0; i < x.Count; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }
            return dist;
        };

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(11); // Fixed seed for reproducible results

            // Generate test points
            _points = new double[DataSize][];
            _nodes = new string[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _points[i] = new double[Dimensions];
                for (int j = 0; j < Dimensions; j++)
                {
                    ((double[])_points[i])[j] = random.NextDouble() * 1000.0; // Range 0-1000
                }
                _nodes[i] = $"Node_{i}";
            }

            // Generate query points for searches (10% of data size)
            int queryCount = Math.Max(100, DataSize / 10);
            _queryPoints = new double[queryCount][];
            for (int i = 0; i < queryCount; i++)
            {
                _queryPoints[i] = new double[Dimensions];
                for (int j = 0; j < Dimensions; j++)
                {
                    ((double[])_queryPoints[i])[j] = random.NextDouble() * 1000.0;
                }
            }

            // Pre-build trees for search benchmarks
            _kdTree = KDTree.Create(_points, _nodes, DistanceMetrics.EuclideanDistance);
            _voxel = VoxelSearch.Create(_points, _nodes, DistanceMetrics.EuclideanDistance);
            _linear = LinearSearch.Create(_points, _nodes, DistanceMetrics.EuclideanDistance);
        }

        #region Nearest Neighbor Search Benchmarks

        [Benchmark]
        [BenchmarkCategory("GetNearestNeighbors")]
        public (IReadOnlyList<double>, string)[][] KDTree_NearestNeighbors()
        {
            var results = new (IReadOnlyList<double>, string)[_queryPoints.Length][];
            for (int i = 0; i < _queryPoints.Length; i++)
            {
                results[i] = _kdTree.GetNearestNeighbors(_queryPoints[i], NeighborCount).ToArray();
            }
            return results;
        }

        //[Benchmark]
        //[BenchmarkCategory("GetNearestNeighbors")]
        //public (IReadOnlyList<double>, string)[][] Linear_NearestNeighbors()
        //{
        //    var results = new (IReadOnlyList<double>, string)[_queryPoints.Length][];
        //    for (int i = 0; i < _queryPoints.Length; i++)
        //    {
        //        results[i] = _linear.GetNearestNeighbors(_queryPoints[i], NeighborCount).ToArray();
        //    }
        //    return results;
        //}

        [Benchmark]
        [BenchmarkCategory("GetNearestNeighbors")]
        public (IReadOnlyList<double>, string)[][] Voxel_NearestNeighbors()
        {
            var results = new (IReadOnlyList<double>, string)[_queryPoints.Length][];
            for (int i = 0; i < _queryPoints.Length; i++)
            {
                results[i] = _voxel.GetNearestNeighbors(_queryPoints[i], NeighborCount).ToArray();
            }
            return results;
        }

        #endregion

        #region Radial Search Benchmarks

        [Benchmark]
        [BenchmarkCategory("RadialSearch")]
        public (IReadOnlyList<double>, string)[][] KDTree_RadialSearch()
        {
            double radius = 150.0; // Fixed radius for comparison
            var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)][];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _kdTree.GetNeighborsInRadius(_queryPoints[i], radius).ToArray();
            }
            return results;
        }

        //[Benchmark]
        //[BenchmarkCategory("RadialSearch")]
        //public (IReadOnlyList<double>, string)[][] Linear_RadialSearch()
        //{
        //    double radius = 150.0; // Fixed radius for comparison
        //    var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)][];

        //    for (int i = 0; i < results.Length; i++)
        //    {
        //        results[i] = _linear.GetNeighborsInRadius(_queryPoints[i], radius).ToArray();
        //    }
        //    return results;
        //}

        [Benchmark]
        [BenchmarkCategory("RadialSearch")]
        public (IReadOnlyList<double>, string)[][] Voxel_RadialSearch()
        {
            double radius = 150.0; // Fixed radius for comparison
            var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)][];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _voxel.GetNeighborsInRadius(_queryPoints[i], radius).ToArray();
            }
            return results;
        }

        #endregion
        #region Both Radial and Number Search Benchmarks

        //[Benchmark]
        //[BenchmarkCategory("BothLimitsSearch")]
        //public (IReadOnlyList<double>, string)[][] KDTree_BothSearch()
        //{
        //    double radius = 150.0; // Fixed radius for comparison
        //    var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)][];

        //    for (int i = 0; i < results.Length; i++)
        //    {
        //        results[i] = _kdTree.GetNeighborsInRadius(_queryPoints[i], radius, NeighborCount).ToArray();
        //    }
        //    return results;
        //}

        //[Benchmark]
        //[BenchmarkCategory("BothLimitsSearch")]
        //public (IReadOnlyList<double>, string)[][] Linear_BothSearch()
        //{
        //    double radius = 150.0; // Fixed radius for comparison
        //    var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)][];

        //    for (int i = 0; i < results.Length; i++)
        //    {
        //        results[i] = _linear.GetNeighborsInRadius(_queryPoints[i], radius, NeighborCount).ToArray();
        //    }
        //    return results;
        //}
        //[Benchmark]
        //[BenchmarkCategory("BothLimitsSearch")]
        //public (IReadOnlyList<double>, string)[][] Voxel_BothSearch()
        //{
        //    double radius = 150.0; // Fixed radius for comparison
        //    var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)][];

        //    for (int i = 0; i < results.Length; i++)
        //    {
        //        results[i] = _voxel.GetNeighborsInRadius(_queryPoints[i], radius, NeighborCount).ToArray();
        //    }
        //    return results;
        //}

        #endregion
        #region Single Search Benchmarks

        [Benchmark]
        [BenchmarkCategory("SingleBestSearch")]
        public (IReadOnlyList<double>, string)[] KDTree_SingleSearch()
        {
            var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _kdTree.GetNearestNeighbor(_queryPoints[i]);
            }
            return results;
        }

        //[Benchmark]
        //[BenchmarkCategory("SingleBestSearch")]
        //public (IReadOnlyList<double>, string)[] Linear_SingleSearch()
        //{
        //    var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)];

        //    for (int i = 0; i < results.Length; i++)
        //    {
        //        results[i] = _linear.GetNearestNeighbor(_queryPoints[i]);
        //    }
        //    return results;
        //}

        [Benchmark]
        [BenchmarkCategory("SingleBestSearch")]
        public (IReadOnlyList<double>, string)[] Voxel_SingleSearch()
        {
            var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _voxel.GetNearestNeighbor(_queryPoints[i]);
            }
            return results;
        }

        #endregion

    }
}