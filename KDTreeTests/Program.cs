
namespace KDTreeTests
{
    internal class Program
    {
       static Random rng = new Random(0);
      static  double r10 => rng.NextDouble() * 20 - 10;
        static void Main(string[] args)
        {
            //var points = new double[13][];
            //var nodes = new List<string>();
            //for (int i = 0; i < 13; i++)
            //{
            //    points[i] = [r10, r10, r10];
            //    nodes.Add($"{i}");
            //}
            //var rf = new SuperClusterKDTree.KDTree<double, double, string>(3, points, nodes, SuperClusterKDTree.KDTree.EuclideanDistance);
            //var m = new SuperClusterKDTreeMedian.KDTree<double, double, string>(3, points, nodes,
            //    SuperClusterKDTreeMedian.KDTree.EuclideanDistance);
            var test = new AccuracyTest();
            test.FindNearestNeighborTest();
            test.RadialSearchTest();
        }
    }
}
