using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System;
using System.Collections.Generic;
using System.Linq;


namespace KDTree.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, iterationCount: 5)]
    public class Benchmarks
    {
        // Test parameters - varying dimensions from 2D to 12D
        [Params(2, 3, 6, 9, 12)]
        public int Dimensions { get; set; }

        // Data sizes for different scenarios
        [Params(100, 3000, 100000)]
        public int DataSize { get; set; }

        // Number of neighbors to search for
        [Params(1, 5, 25)]
        public int NeighborCount { get; set; }

        // Test data
        private IReadOnlyList<double>[] _points;
        private string[] _nodes;
        private IReadOnlyList<double>[] _queryPoints;
        private SuperClusterKDTree.KDTree<double, double, string> _kdTree;
        private SuperClusterKDTreeSpan.KDTree<double, double, string> _kdTreeSpan;

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
            var random = new Random(42); // Fixed seed for reproducible results

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
            _kdTree = new SuperClusterKDTree.KDTree<double, double, string>(
                Dimensions,
                _points,
                _nodes,
                L2Metric);

            _kdTreeSpan = new SuperClusterKDTreeSpan.KDTree<double, double, string>(
                Dimensions,
                _points,
                _nodes,
                L2Metric);
        }

        #region Tree Construction Benchmarks

        [Benchmark]
        [BenchmarkCategory("Construction")]
        public SuperClusterKDTree.KDTree<double, double, string> KDTree_Construction()
        {
            return new SuperClusterKDTree.KDTree<double, double, string>(
                Dimensions,
                _points,
                _nodes,
                L2Metric);
        }

        [Benchmark]
        [BenchmarkCategory("Construction")]
        public SuperClusterKDTreeSpan.KDTree<double, double, string> KDTreeSpan_Construction()
        {
            return new SuperClusterKDTreeSpan.KDTree<double, double, string>(
                Dimensions,
                _points,
                _nodes,
                L2Metric);
        }

        #endregion

        #region Nearest Neighbor Search Benchmarks

        [Benchmark]
        [BenchmarkCategory("NearestNeighbors")]
        public (IReadOnlyList<double>, string)[][] KDTree_NearestNeighbors()
        {
            var results = new (IReadOnlyList<double>, string)[_queryPoints.Length][];
            for (int i = 0; i < _queryPoints.Length; i++)
            {
                results[i] = _kdTree.NearestNeighbors(_queryPoints[i], NeighborCount).ToArray();
            }
            return results;
        }

        [Benchmark]
        [BenchmarkCategory("NearestNeighbors")]
        public (IReadOnlyList<double>, string)[][] KDTreeSpan_NearestNeighbors()
        {
            var results = new (IReadOnlyList<double>, string)[_queryPoints.Length][];
            for (int i = 0; i < _queryPoints.Length; i++)
            {
                results[i] = _kdTreeSpan.NearestNeighbors(_queryPoints[i], NeighborCount).ToArray();
            }
            return results;
        }

        #endregion

        #region Single Query Benchmarks (for detailed analysis)

        [Benchmark]
        [BenchmarkCategory("SingleQuery")]
        public (IReadOnlyList<double>, string)[] KDTree_SingleNearestNeighbor()
        {
            return _kdTree.NearestNeighbors(_queryPoints[0], NeighborCount).ToArray();
        }

        [Benchmark]
        [BenchmarkCategory("SingleQuery")]
        public (IReadOnlyList<double>, string)[] KDTreeSpan_SingleNearestNeighbor()
        {
            return _kdTreeSpan.NearestNeighbors(_queryPoints[0], NeighborCount).ToArray();
        }

        #endregion

        #region Radial Search Benchmarks

        [Benchmark]
        [BenchmarkCategory("RadialSearch")]
        public (IReadOnlyList<double>, string)[][] KDTree_RadialSearch()
        {
            double radius = 50.0; // Fixed radius for comparison
            var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)][];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _kdTree.RadialSearch(_queryPoints[i], radius).ToArray();
            }
            return results;
        }

        [Benchmark]
        [BenchmarkCategory("RadialSearch")]
        public (IReadOnlyList<double>, string)[][] KDTreeSpan_RadialSearch()
        {
            double radius = 50.0; // Fixed radius for comparison
            var results = new (IReadOnlyList<double>, string)[Math.Min(50, _queryPoints.Length)][];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _kdTreeSpan.RadialSearch(_queryPoints[i], radius).ToArray();
            }
            return results;
        }

        #endregion



        #region Memory Usage Scenarios

        [Benchmark]
        [BenchmarkCategory("Memory")]
        public int KDTree_MemoryFootprint()
        {
            // This benchmark helps measure memory allocation patterns
            var tree = new SuperClusterKDTree.KDTree<double, double, string>(Dimensions, _points, _nodes, L2Metric);

            // Perform some operations to trigger any lazy allocations
            var result = tree.NearestNeighbors(_queryPoints[0], 1).ToArray();

            return tree.Count + result.Length;
        }

        [Benchmark]
        [BenchmarkCategory("Memory")]
        public int KDTreeSpan_MemoryFootprint()
        {
            // This benchmark helps measure memory allocation patterns
            var tree = new SuperClusterKDTreeSpan.KDTree<double, double, string>(Dimensions, _points, _nodes, L2Metric);

            // Perform some operations to trigger any lazy allocations
            var result = tree.NearestNeighbors(_queryPoints[0], 1).ToArray();

            return tree.Count + result.Length;
        }

        #endregion
    }

}