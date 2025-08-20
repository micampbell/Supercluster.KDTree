using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;


namespace KDTree.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, iterationCount: 5)]
    public class Benchmarks
    {
        // Test parameters - varying dimensions from 2D to 12D
        [Params(2, 3, 6, 9, 12, 15)]
        public int Dimensions { get; set; }

        // Data sizes for different scenarios
        [Params(100, 10000,1000000)]
        public int DataSize { get; set; }

        // Number of neighbors to search for
        [Params(1, 5,25)]
        public int NeighborCount { get; set; }

        // Test data
        private double[][] _points;
        private string[] _nodes;
        private double[][] _queryPoints;
        private SuperClusterKDTree.KDTree<double, double, string> _kdTree;
        private SuperclusterKDTreeSpan.KDTreePQ<double, string> _kdTreePQ;
        
        // Metrics
        private static readonly Func<double[], double[], double> L2Metric = (x, y) =>
        {
            double dist = 0.0;
            for (int i = 0; i < x.Length; i++)
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
                    _points[i][j] = random.NextDouble() * 1000.0; // Range 0-1000
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
                    _queryPoints[i][j] = random.NextDouble() * 1000.0;
                }
            }

            // Pre-build trees for search benchmarks
            _kdTree = new SuperClusterKDTree.KDTree<double, double, string>(
                Dimensions, 
                _points, 
                _nodes, 
                L2Metric);

            _kdTreePQ = new SuperclusterKDTreeSpan.KDTree<double, double, string>(
                Dimensions, 
                _points, 
                _nodes, 
                L2Metric);
        }

        #region Tree Construction Benchmarks

        [Benchmark]
        [BenchmarkCategory("Construction")]
        public SuperClusterKDTree.KDTree<double, string> KDTree_Construction()
        {
            return new SuperClusterKDTree.KDTree<double, string>(
                Dimensions,
                _points,
                _nodes,
                L2Metric);
        }

        [Benchmark]
        [BenchmarkCategory("Construction")]
        public SuperClusterKDTree.KDTreePQ<double, string> KDTreePQ_Construction()
        {
            return new SuperClusterKDTree.KDTreePQ<double, string>(
                Dimensions,
                _points,
                _nodes,
                L2Metric);
        }

        #endregion

        #region Nearest Neighbor Search Benchmarks

        [Benchmark]
        [BenchmarkCategory("NearestNeighbors")]
        public (double[], string)[][] KDTree_NearestNeighbors()
        {
            var results = new (double[], string)[_queryPoints.Length][];
            for (int i = 0; i < _queryPoints.Length; i++)
            {
                results[i] = _kdTree.NearestNeighbors(_queryPoints[i], NeighborCount);
            }
            return results;
        }

        [Benchmark]
        [BenchmarkCategory("NearestNeighbors")]
        public (double[], string)[][] KDTreePQ_NearestNeighbors()
        {
            var results = new (double[], string)[_queryPoints.Length][];
            for (int i = 0; i < _queryPoints.Length; i++)
            {
                results[i] = _kdTreePQ.NearestNeighbors(_queryPoints[i], NeighborCount);
            }
            return results;
        }

        #endregion

        #region Single Query Benchmarks (for detailed analysis)

        [Benchmark]
        [BenchmarkCategory("SingleQuery")]
        public (double[], string)[] KDTree_SingleNearestNeighbor()
        {
            return _kdTree.NearestNeighbors(_queryPoints[0], NeighborCount);
        }

        [Benchmark]
        [BenchmarkCategory("SingleQuery")]
        public (double[], string)[] KDTreePQ_SingleNearestNeighbor()
        {
            return _kdTreePQ.NearestNeighbors(_queryPoints[0], NeighborCount);
        }

        #endregion

        #region Radial Search Benchmarks

        [Benchmark]
        [BenchmarkCategory("RadialSearch")]
        public (double[], string)[][] KDTree_RadialSearch()
        {
            double radius = 50.0; // Fixed radius for comparison
            var results = new (double[], string)[Math.Min(50, _queryPoints.Length)][];
            
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _kdTree.RadialSearch(_queryPoints[i], radius);
            }
            return results;
        }

        [Benchmark]
        [BenchmarkCategory("RadialSearch")]
        public (double[], string)[][] KDTreePQ_RadialSearch()
        {
            double radius = 50.0; // Fixed radius for comparison
            var results = new (double[], string)[Math.Min(50, _queryPoints.Length)][];
            
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _kdTreePQ.RadialSearch(_queryPoints[i], radius);
            }
            return results;
        }

        #endregion

        #region High-Dimensional Stress Tests

        [Benchmark]
        [BenchmarkCategory("StressTest")]
        public long KDTree_MassiveQueries()
        {
            // Perform many small queries to stress test the data structures
            long totalResults = 0;
            int iterations = Math.Min(1000, _queryPoints.Length);
            
            for (int i = 0; i < iterations; i++)
            {
                var results = _kdTree.NearestNeighbors(_queryPoints[i % _queryPoints.Length], 1);
                totalResults += results.Length;
            }
            return totalResults;
        }

        [Benchmark]
        [BenchmarkCategory("StressTest")]
        public long KDTreePQ_MassiveQueries()
        {
            // Perform many small queries to stress test the data structures
            long totalResults = 0;
            int iterations = Math.Min(1000, _queryPoints.Length);
            
            for (int i = 0; i < iterations; i++)
            {
                var results = _kdTreePQ.NearestNeighbors(_queryPoints[i % _queryPoints.Length], 1);
                totalResults += results.Length;
            }
            return totalResults;
        }

        #endregion

        #region Memory Usage Scenarios

        [Benchmark]
        [BenchmarkCategory("Memory")]
        public int KDTree_MemoryFootprint()
        {
            // This benchmark helps measure memory allocation patterns
            var tree = new Supercluster.KDTree.KDTree<double, string>(Dimensions, _points, _nodes, L2Metric);
            
            // Perform some operations to trigger any lazy allocations
            var result = tree.NearestNeighbors(_queryPoints[0], 1);
            
            return tree.Count + result.Length;
        }

        [Benchmark]
        [BenchmarkCategory("Memory")]
        public int KDTreePQ_MemoryFootprint()
        {
            // This benchmark helps measure memory allocation patterns
            var tree = new Supercluster.KDTree.KDTreePQ<double, string>(Dimensions, _points, _nodes, L2Metric);
            
            // Perform some operations to trigger any lazy allocations
            var result = tree.NearestNeighbors(_queryPoints[0], 1);
            
            return tree.Count + result.Length;
        }

        #endregion
    }

    /// <summary>
    /// Specialized benchmarks for very high dimensional data (8D to 12D)
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, iterationCount: 3)]
    public class HighDimensionalBenchmarks
    {
        [Params(8, 10, 12)]
        public int Dimensions { get; set; }

        [Params(50000)]
        public int DataSize { get; set; }

        [Params(10, 100)]
        public int QueryCount { get; set; }

        private double[][] _points;
        private string[] _nodes;
        private double[][] _queryPoints;
        private SuperClusterKDTree.KDTree<double, double, string> _kdTree;
        private SuperclusterKDTreeSpan.KDTree<double, double, string> _kdTreePQ;

        private static readonly Func<double[], double[], double> L2Metric = (x, y) =>
        {
            double dist = 0.0;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }
            return dist;
        };

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            
            _points = new double[DataSize][];
            _nodes = new string[DataSize];
            
            for (int i = 0; i < DataSize; i++)
            {
                _points[i] = new double[Dimensions];
                for (int j = 0; j < Dimensions; j++)
                {
                    _points[i][j] = random.NextDouble() * 1000.0;
                }
                _nodes[i] = $"Node_{i}";
            }

            _queryPoints = new double[QueryCount][];
            for (int i = 0; i < QueryCount; i++)
            {
                _queryPoints[i] = new double[Dimensions];
                for (int j = 0; j < Dimensions; j++)
                {
                    _queryPoints[i][j] = random.NextDouble() * 1000.0;
                }
            }

            _kdTree = new SuperClusterKDTree.KDTree<double, double, string>(Dimensions, _points, _nodes, L2Metric);
            _kdTreePQ = new SuperclusterKDTreeSpan.KDTree<double, double, string>(Dimensions, _points, _nodes, L2Metric);
        }

        [Benchmark]
        [BenchmarkCategory("HighDim")]
        public int KDTree_HighDim_Construction_And_Search()
        {
            var tree = new SuperClusterKDTree.KDTree<double, double, string>(Dimensions, _points, _nodes, L2Metric);
            int totalResults = 0;
            
            foreach (var query in _queryPoints)
            {
                var results = tree.NearestNeighbors(query, 5);
                totalResults += results.Length;
            }
            
            return totalResults;
        }

        [Benchmark]
        [BenchmarkCategory("HighDim")]
        public int KDTreePQ_HighDim_Construction_And_Search()
        {
            var tree = new SuperClusterKDTreeSpan.KDTree<double, double, string>(Dimensions, _points, _nodes, L2Metric);
            int totalResults = 0;
            
            foreach (var query in _queryPoints)
            {
                var results = tree.NearestNeighbors(query, 5);
                totalResults += results.Length;
            }
            
            return totalResults;
        }
    }

    /// <summary>
    /// Float precision benchmarks for performance comparison
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, iterationCount: 5)]
    public class FloatPrecisionBenchmarks
    {
        [Params(3, 6, 12)]
        public int Dimensions { get; set; }

        [Params(10000)]
        public int DataSize { get; set; }

        private float[][] _floatPoints;
        private double[][] _doublePoints;
        private string[] _nodes;
        private float[][] _floatQueryPoints;
        private double[][] _doubleQueryPoints;

        private static readonly Func<float[], float[], double> FloatL2Metric = (x, y) =>
        {
            float dist = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }
            return dist;
        };

        private static readonly Func<double[], double[], double> DoubleL2Metric = (x, y) =>
        {
            double dist = 0.0;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }
            return dist;
        };

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            
            _floatPoints = new float[DataSize][];
            _doublePoints = new double[DataSize][];
            _nodes = new string[DataSize];
            
            for (int i = 0; i < DataSize; i++)
            {
                _floatPoints[i] = new float[Dimensions];
                _doublePoints[i] = new double[Dimensions];
                
                for (int j = 0; j < Dimensions; j++)
                {
                    var value = random.NextDouble() * 1000.0;
                    _floatPoints[i][j] = (float)value;
                    _doublePoints[i][j] = value;
                }
                _nodes[i] = $"Node_{i}";
            }

            int queryCount = 1000;
            _floatQueryPoints = new float[queryCount][];
            _doubleQueryPoints = new double[queryCount][];
            
            for (int i = 0; i < queryCount; i++)
            {
                _floatQueryPoints[i] = new float[Dimensions];
                _doubleQueryPoints[i] = new double[Dimensions];
                
                for (int j = 0; j < Dimensions; j++)
                {
                    var value = random.NextDouble() * 1000.0;
                    _floatQueryPoints[i][j] = (float)value;
                    _doubleQueryPoints[i][j] = value;
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory("Precision")]
        public int KDTree_Float_Performance()
        {
            var tree = new SuperClusterKDTree.KDTree<float,float, string>(Dimensions, _floatPoints, _nodes, FloatL2Metric);
            int totalResults = 0;
            
            for (int i = 0; i < Math.Min(100, _floatQueryPoints.Length); i++)
            {
                var results = tree.NearestNeighbors(_floatQueryPoints[i], 5);
                totalResults += results.Length;
            }
            
            return totalResults;
        }

        [Benchmark]
        [BenchmarkCategory("Precision")]
        public int KDTree_Double_Performance()
        {
            var tree = new SuperClusterKDTree.KDTree<double, double, string>(Dimensions, _doublePoints, _nodes, DoubleL2Metric);
            int totalResults = 0;
            
            for (int i = 0; i < Math.Min(100, _doubleQueryPoints.Length); i++)
            {
                var results = tree.NearestNeighbors(_doubleQueryPoints[i], 5);
                totalResults += results.Length;
            }
            
            return totalResults;
        }

        [Benchmark]
        [BenchmarkCategory("Precision")]
        public int KDTreePQ_Float_Performance()
        {
            var tree = new SuperClusterKDTree.KDTreePQ<float, string>(Dimensions, _floatPoints, _nodes, FloatL2Metric);
            int totalResults = 0;
            
            for (int i = 0; i < Math.Min(100, _floatQueryPoints.Length); i++)
            {
                var results = tree.NearestNeighbors(_floatQueryPoints[i], 5);
                totalResults += results.Length;
            }
            
            return totalResults;
        }

        [Benchmark]
        [BenchmarkCategory("Precision")]
        public int KDTreePQ_Double_Performance()
        {
            var tree = new SuperClusterKDTreeSpan.KDTree<double, double, string>(Dimensions, _doublePoints, _nodes, DoubleL2Metric);
            int totalResults = 0;
            
            for (int i = 0; i < Math.Min(100, _doubleQueryPoints.Length); i++)
            {
                var results = tree.NearestNeighbors(_doubleQueryPoints[i], 5);
                totalResults += results.Length;
            }
            
            return totalResults;
        }
    }
}
