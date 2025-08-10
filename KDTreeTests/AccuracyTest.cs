
namespace KDTreeTests
{
    using System.Linq;
    using NUnit.Framework;
    using SuperClusterKDTree;

    [TestFixture]
    public class AccuracyTest
    {


        [Test]
        public void FindNearestNeighborTest()
        {
            var dataSize = 10000;
            var testDataSize = 100;
            var range = 1000;

            var treePoints = Utilities.GenerateDoubles(dataSize, range);
            var treeNodes = Utilities.GenerateDoubles(dataSize, range).Select(d => d.ToString()).ToArray();
            var testData = Utilities.GenerateDoubles(testDataSize, range);


            var tree = KDTree.Create(treePoints, treeNodes, DistanceMetrics.EuclideanDistance);

            for (int i = 0; i < testDataSize; i++)
            {
                var treeNearest = tree.NearestNeighbors(testData[i], 1);
                var linearNearest = Utilities.LinearSearch(treePoints, treeNodes, testData[i], Utilities.L2Norm_Squared_Double);

                Assert.That(Utilities.L2Norm_Squared_Double(testData[i], linearNearest.Item1), Is.EqualTo(Utilities.L2Norm_Squared_Double(testData[i], treeNearest[0].Item1)));

                // TODO: wrote linear search for both node and point arrays
                Assert.That(treeNearest[0].Item2, Is.EqualTo(linearNearest.Item2));
            }
        }

        [Test]
        public void RadialSearchTest()
        {
            var dataSize = 10000;
            var testDataSize = 100;
            var range = 1000;
            var radius = 100;

            var treeData = Utilities.GenerateDoubles(dataSize, range);
            var treeNodes = Utilities.GenerateDoubles(dataSize, range).Select(d => string.Join(',', d[0], d[1])).ToArray();
            var testData = Utilities.GenerateDoubles(testDataSize, range);
            var tree = KDTree.Create(treeData, treeNodes, DistanceMetrics.EuclideanDistance);

            for (int i = 0; i < testDataSize; i++)
            {
                var treeRadial = tree.RadialSearch(testData[i], radius);
                var linearRadial = Utilities.LinearRadialSearch(
                    treeData,
                    treeNodes,
                    testData[i],
                    Utilities.L2Norm_Squared_Double,
                    radius);

                for (int j = 0; j < treeRadial.Length; j++)
                {
                    Assert.That(treeRadial[j].Item1, Is.EqualTo(linearRadial[j].Item1));
                    Assert.That(treeRadial[j].Item2, Is.EqualTo(linearRadial[j].Item2));
                }


            }
        }
    }
}
