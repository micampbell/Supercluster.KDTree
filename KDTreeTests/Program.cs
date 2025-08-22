
using NearestNeighborSearch;

namespace NearestNeighborSearchTests
{
    internal class Program
    {
       static Random rng = new Random(0);
      static  double r10 => rng.NextDouble() * 20 - 10;
        static void Main(string[] args)
        {
            var test = new AccuracyTest();
            test.LimitedRadialSearchTest(typeof(LinearSearch));
        }
    }
}
