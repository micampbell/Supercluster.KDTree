namespace NearestNeighborSearchTests
{
    using System.Linq;

    public static class Utilities
    {
        #region Metrics
        public static Func<IReadOnlyList<double>, IReadOnlyList<double>, double> L2Norm_Squared_Double = (x, y) =>
        {
            double dist = 0f;
            for (int i = 0; i < x.Count; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }
            return dist;
        };
        #endregion

        #region Data Generation

        public static double[][] GenerateDoubles(int points, double range, int dimensions)
        {
            var data = new List<double[]>();
            var random = new Random();

            for (var i = 0; i < points; i++)
            {
                var array = new double[dimensions];
                for (var j = 0; j < dimensions; j++)
                {
                    array[j] = 2 * random.NextDouble() * range - range;
                }
                data.Add(array);
            }

            return data.ToArray();
        }
        #endregion


        #region Searches

        /// <summary>
        /// Performs a linear search on a given points set to find a nodes that is closest to the given nodes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="point"></param>
        /// <param name="metric"></param>
        /// <returns></returns>

        public static (TPoint[], TNode)[] LinearSearch<TPoint, TNode>(TPoint[][] points, TNode[] nodes, TPoint[] target,
            Func<TPoint[], TPoint[], double> metric, int numToKeep)
        {
            var closestPoints = new SortedList<double, (TPoint[], TNode)>(numToKeep);
            var cutOffDist = double.MaxValue;

            for (int i = 0; i < points.Length; i++)
            {
                var currentDist = metric(points[i], target);
                if (currentDist <= cutOffDist)
                {
                    if (closestPoints.Count == numToKeep)
                    {
                        closestPoints.RemoveAt(closestPoints.Count - 1);
                        cutOffDist = Math.Max(closestPoints.Keys.Last(), currentDist);
                    }
                    closestPoints.Add(currentDist, (points[i], nodes[i]));
                }
            }
            return closestPoints.Values.ToArray();
        }



        public static (TPoint[], TNode)[] LinearRadialSearch<TPoint, TNode>(TPoint[][] points, TNode[] nodes, TPoint[] target,
            Func<TPoint[], TPoint[], double> metric, double radius, int numToKeep = -1)
        {
            var pointsInRadius = numToKeep < 0 ? new SortedList<double, (TPoint[], TNode)>() :
                new SortedList<double, (TPoint[], TNode)>(numToKeep + 1);
            for (int i = 0; i < points.Length; i++)
            {
                var currentDist = metric(target, points[i]);
                if (radius >= currentDist)
                {
                    pointsInRadius.Add(currentDist, (points[i], nodes[i]));
                    if (numToKeep >= 0 && pointsInRadius.Count > numToKeep)
                        pointsInRadius.RemoveAt(pointsInRadius.Count - 1);
                }
            }
            return pointsInRadius.Values.ToArray();
        }

        #endregion
    }
}
