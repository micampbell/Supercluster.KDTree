
namespace NearestNeighborSearchTests
{
    using NearestNeighborSearch;
    using NUnit.Framework;
    using System.Linq;
    using System.Reflection;

    [TestFixture]
    public class AccuracyTest
    {


        [Test]
        public void FindNearestNeighborTest_KDTree()
        => FindNearestNeighborTest(typeof(KDTree));


        [Test]
        public void FindNearestNeighborTest_Linear()
        => FindNearestNeighborTest(typeof(LinearSearch));

        public void FindNearestNeighborTest(Type searchType)
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
                var treeNodes = treeData.Select(d => d.GetHashCode().ToString()).ToArray();
                var testData = Utilities.GenerateDoubles(testDataSize, range, dim);
                var searchMethod = GetSearchMethod(searchType, treeData, treeNodes);

                for (int j = 0; j < testDataSize; j++)
                {
                    var testPoint = testData[j];
                    (IReadOnlyList<double>, string)[] nearest = searchMethod.GetNearestNeighbors(testPoint, 10).OrderBy(g => Utilities.L2Norm_Squared_Double(g.Item1, testPoint)).ToArray();
                    var linearBaseline = Utilities.LinearSearch(treeData, treeNodes, testPoint, Utilities.L2Norm_Squared_Double, 10);

                    for (int k = 0; k < nearest.Length; k++)
                    {
                        Assert.That(nearest[k].Item1, Is.EqualTo(linearBaseline[k].Item1));
                        Assert.That(nearest[k].Item2, Is.EqualTo(linearBaseline[k].Item2));
                    }

                }
            }
        }

        private static SearchMethod<double, double, string>? GetSearchMethod(Type searchType, double[][] treeData, string[] treeNodes)
        {
            // Use the 3-argument Create<TDimension,TNode>(points, nodes, DistanceMetrics) overload.
            MethodInfo createMethod = searchType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m =>
                    m.Name == "Create" &&
                    m.GetGenericArguments().Length == 2 &&
                    m.GetParameters().Length >= 3 &&
                    m.GetParameters()[2].ParameterType == typeof(DistanceMetrics));

            var genericCreate = createMethod.MakeGenericMethod(typeof(double), typeof(string));
            var tree = (SearchMethod<double, double, string>)genericCreate.Invoke(
                null, [treeData, treeNodes, DistanceMetrics.EuclideanDistance, double.MinValue, double.MaxValue]);
            return tree;
        }

        [Test]
        public void RadialSearchTest_KDTree()
        => RadialSearchTest(typeof(KDTree));


        [Test]
        public void RadialSearchTest_Linear()
        => RadialSearchTest(typeof(LinearSearch));

        public void RadialSearchTest(Type searchType)
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
                var treeNodes = treeData.Select(d => d.GetHashCode().ToString()).ToArray();
                var testData = Utilities.GenerateDoubles(testDataSize, range, dim);
                var searchMethod = GetSearchMethod(searchType, treeData, treeNodes);

                for (int j = 0; j < testDataSize; j++)
                {
                    var testPoint = testData[j];
                    var nearest = searchMethod.GetNeighborsInRadius(testPoint, radius).OrderBy(g => Utilities.L2Norm_Squared_Double(g.Item1, testPoint)).ToArray();
                    var linearBaseline = Utilities.LinearRadialSearch(treeData, treeNodes, testPoint, Utilities.L2Norm_Squared_Double,
                        radius);
                    for (int k = 0; k < nearest.Length; k++)
                    {
                        Assert.That(nearest[k].Item1, Is.EqualTo(linearBaseline[k].Item1));
                        Assert.That(nearest[k].Item2, Is.EqualTo(linearBaseline[k].Item2));
                    }

                }
            }
        }


        [Test]
        public void LimitedRadialSearchTest_KDTree()
        => LimitedRadialSearchTest(typeof(KDTree));


        [Test]
        public void LimitedRadialSearchTest_Linear()
        => LimitedRadialSearchTest(typeof(LinearSearch));

        public void LimitedRadialSearchTest(Type searchType)
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
                var treeNodes = treeData.Select(d => d.GetHashCode().ToString()).ToArray();
                var testData = Utilities.GenerateDoubles(testDataSize, range, dim);
                var searchMethod = GetSearchMethod(searchType, treeData, treeNodes);

                for (int j = 0; j < testDataSize; j++)
                {
                    var testPoint = testData[j];
                    var nearest = searchMethod.GetNeighborsInRadius(testPoint, radius, 100).OrderBy(g => Utilities.L2Norm_Squared_Double(g.Item1, testPoint)).ToArray();
                    var linearBaseline = Utilities.LinearRadialSearch(treeData, treeNodes, testPoint, Utilities.L2Norm_Squared_Double,
                        radius, 100);
                    for (int k = 0; k < nearest.Length; k++)
                    {
                        Assert.That(nearest[k].Item1, Is.EqualTo(linearBaseline[k].Item1));
                        Assert.That(nearest[k].Item2, Is.EqualTo(linearBaseline[k].Item2));
                    }

                }
            }
        }
    }
}
