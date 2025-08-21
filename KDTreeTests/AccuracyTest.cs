
namespace KDTreeTests
{
    using System.Linq;
    using NUnit.Framework;
    using SuperClusterKDTreeSpan;

    [TestFixture]
    public class AccuracyTest
    {


        [Test]
        public void FindNearestNeighborTest()
        {
            var range = 1000;
            var dataSizeConst = 10000;
            var testDataSizeConst = 100;
            for (int i = 0; i < 4; i++)
            {
                var dim = 1 + (int)Math.Exp(i); // 2, 3, 8, 21
                var dataSize = dataSizeConst / (i + 1);
                var testDataSize = testDataSizeConst / (i + 1);
                var treeData = Utilities.GenerateDoubles(dataSize, range, dim);
                var treeNodes = treeData.Select(d => d.GetHashCode()).ToArray();
                var testData = Utilities.GenerateDoubles(testDataSize, range, dim);

                var tree = KDTree.Create(treeData, treeNodes, DistanceMetrics.EuclideanDistance);

                for (int j = 0; j < testDataSize; j++)
                {
                    var testPoint = testData[j];
                    var kdtree = tree.NearestNeighbors(testPoint, 10).ToArray();
                    var linearBaseline = Utilities.LinearSearch(treeData, treeNodes, testPoint, Utilities.L2Norm_Squared_Double, 10);

                    for (int k = 0; k < kdtree.Length; k++)
                    {
                        Assert.That(kdtree[k].Item1, Is.EqualTo(linearBaseline[k].Item1));
                        Assert.That(kdtree[k].Item2, Is.EqualTo(linearBaseline[k].Item2));
                    }

                }
            }
        }

        [Test]
        public void RadialSearchTest()
        {
            var range = 1000;
            var dataSizeConst = 10000;
            var testDataSizeConst = 100;
            for (int i = 0; i < 4; i++)
            {
                var dim = 1 + (int)Math.Exp(i); // 2, 3, 8, 21
                var radius = dim * range * range;
                var dataSize = dataSizeConst / (i + 1);
                var testDataSize = testDataSizeConst / (i + 1);
                var treeData = Utilities.GenerateDoubles(dataSize, range, dim);
                var treeNodes = treeData.Select(d => d.GetHashCode()).ToArray();
                var testData = Utilities.GenerateDoubles(testDataSize, range, dim);

                var tree = KDTree.Create(treeData, treeNodes, DistanceMetrics.EuclideanDistance);

                for (int j = 0; j < testDataSize; j++)
                {
                    var testPoint = testData[j];
                    var kdtree = tree.RadialSearch(testPoint, radius).ToArray();
                    var linearBaseline = Utilities.LinearRadialSearch(treeData, treeNodes, testPoint, Utilities.L2Norm_Squared_Double,
                        radius);
                    for (int k = 0; k < kdtree.Length; k++)
                    {
                        Assert.That(kdtree[k].Item1, Is.EqualTo(linearBaseline[k].Item1));
                        Assert.That(kdtree[k].Item2, Is.EqualTo(linearBaseline[k].Item2));
                    }

                }
            }
        }

        [Test]
        public void LimitedRadialSearchTest()
        {
            var range = 1000;
            var dataSizeConst = 10000;
            var testDataSizeConst = 100;
            for (int i = 0; i < 4; i++)
            {
                var dim = 1 + (int)Math.Exp(i); // 2, 3, 8, 21
                var radius = dim * range * range;
                var dataSize = dataSizeConst / (i + 1);
                var testDataSize = testDataSizeConst / (i + 1);
                var treeData = Utilities.GenerateDoubles(dataSize, range, dim);
                var treeNodes = treeData.Select(d => d.GetHashCode()).ToArray();
                var testData = Utilities.GenerateDoubles(testDataSize, range, dim);

                var tree = KDTree.Create(treeData, treeNodes, DistanceMetrics.EuclideanDistance);

                for (int j = 0; j < testDataSize; j++)
                {
                    var testPoint = testData[j];
                    var kdtree = tree.RadialSearch(testPoint, radius, 100).ToArray();
                    var linearBaseline = Utilities.LinearRadialSearch(treeData, treeNodes, testPoint, Utilities.L2Norm_Squared_Double,
                        radius, 100);
                    for (int k = 0; k < kdtree.Length; k++)
                    {
                        Assert.That(kdtree[k].Item1, Is.EqualTo(linearBaseline[k].Item1));
                        Assert.That(kdtree[k].Item2, Is.EqualTo(linearBaseline[k].Item2));
                    }

                }
            }
        }
    }
}
