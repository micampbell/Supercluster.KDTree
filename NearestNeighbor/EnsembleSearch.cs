using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks; // added for threading constructs

namespace NearestNeighborSearch
{
    public static class EnsembleSearch
    {
        public static EnsembleSearch<TDimension, TNode> Create<TDimension, TNode>(
            ICollection<IReadOnlyList<TDimension>> points,
            IEnumerable<TNode> nodes,
            DistanceMetrics metricType, TDimension minValue = default, TDimension maxValue = default)
            where TDimension : IComparable<TDimension>, IMinMaxValue<TDimension>, INumber<TDimension>
        => new EnsembleSearch<TDimension, TNode>(points, nodes, metricType, minValue, maxValue);

    }

    public class EnsembleSearch<TDimension, TNode> : SearchMethod<TDimension, TDimension, TNode>
        where TDimension : IComparable<TDimension>, IMinMaxValue<TDimension>, INumber<TDimension>
    {

        /// <summary>
        /// The array in which the binary tree is stored. Enumerating this array is a level-order traversal of the tree.
        /// </summary>
        private IReadOnlyList<TDimension>[] Points { get; }

        /// <summary>
        /// The array in which the node objects are stored. There is a one-to-one correspondence with this array and the <see cref="Points"/>.
        /// </summary>
        private TNode[] Nodes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnsembleSearch{TDimension,TNode}". /> class.
        /// It is unlikely that this constructor will be used directly, as it is more common to use the
        /// Create method to create a EnsembleSearch from a set of points and nodes. This can be used and
        /// is left here for created more complex KD-Trees where the points have unique distance metrics.
        /// or the type of the distance metric is different from the type of the points.
        /// </summary>
        /// <param name="dimensions">The number of dimensions in the data set.</param>
        /// <param name="points">The points to be constructed into a <see cref="EnsembleSearch{TDimension,TNode}"/></param>
        /// <param name="nodes">The nodes associated with each point.</param>
        /// <param name="metric">The metric function which implicitly defines the metric space in which the EnsembleSearch operates in. This should satisfy the triangle inequality.</param>
        /// <param name="searchWindowMinValue">The minimum value to be used in node searches. If null, we assume that <typeparamref name="TDimension"/> has a static field named "MinValue". All numeric structs have this field.</param>
        /// <param name="searchWindowMaxValue">The maximum value to be used in node searches. If null, we assume that <typeparamref name="TDimension"/> has a static field named "MaxValue". All numeric structs have this field.</param>
        public EnsembleSearch(ICollection<IReadOnlyList<TDimension>> points, IEnumerable<TNode> nodes, DistanceMetrics metricType,
            TDimension searchWindowMinValue = default, TDimension searchWindowMaxValue = default)
            : base(points, CommonDistanceMetrics.GetDistanceMetric<TDimension>(metricType), searchWindowMinValue, searchWindowMaxValue)
        {
            this.Points = points.ToArray();
            this.Nodes = nodes.ToArray();
            kdTreeSearch = KDTree.Create(points, nodes, metricType, searchWindowMinValue, searchWindowMaxValue);
            linearSearch = LinearSearch.Create(points, nodes, metricType, searchWindowMinValue, searchWindowMaxValue);
            voxelSearch = VoxelSearch.Create(points, nodes, metricType);
        }
        VoxelSearch<TDimension, TNode> voxelSearch;
        KDTree<TDimension, TDimension, TNode> kdTreeSearch;
        LinearSearch<TDimension, TDimension, TNode> linearSearch;


        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetAllData()
        {
            for (int i = 0; i < Points.Length; i++)
            {
                if (Points[i] != null)
                    yield return new(Points[i], Nodes[i]);
            }
        }

        /// <inheritdoc/>
        public override (IReadOnlyList<TDimension>, TNode) GetNearestNeighbor(IReadOnlyList<TDimension> target)
        {
            Task<(IReadOnlyList<TDimension>, TNode)> kdTask = Task.Run(() => kdTreeSearch.GetNearestNeighbor(target));
            //Task<(IReadOnlyList<TDimension>, TNode)> linearTask = Task.Run(() => linearSearch.GetNearestNeighbor(target));
            Task<(IReadOnlyList<TDimension>, TNode)> voxelTask = Task.Run(() => voxelSearch.GetNearestNeighbor(target));

            //int index = Task.WaitAny(kdTask, linearTask, voxelTask);
            //var result = index == 0 ? kdTask.Result : index == 1 ? linearTask.Result : voxelTask.Result;
            int index = Task.WaitAny(kdTask, voxelTask);
            var result = index == 0 ? kdTask.Result : voxelTask.Result;

            return result;
        }
        /// <inheritdoc/>
        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNearestNeighbors(IReadOnlyList<TDimension> target, int numNeighbors)
        {
            if (numNeighbors <= 0 || numNeighbors >= Count)
                return GetAllData();
            if (numNeighbors == 1)
                return [GetNearestNeighbor(target)];
            Task<(IReadOnlyList<TDimension>, TNode)[]> kdTask = Task.Run(() => kdTreeSearch.GetNearestNeighbors(target, numNeighbors).ToArray());
            //Task<(IReadOnlyList<TDimension>, TNode)[]> linearTask = Task.Run(() => linearSearch.GetNearestNeighbors(target, numNeighbors).ToArray());
            Task<(IReadOnlyList<TDimension>, TNode)[]> voxelTask = Task.Run(() => voxelSearch.GetNearestNeighbors(target, numNeighbors).ToArray());

            //int index = Task.WaitAny(kdTask, linearTask, voxelTask);
            //var result = index == 0 ? kdTask.Result : index == 1 ? linearTask.Result : voxelTask.Result;
            int index = Task.WaitAny(kdTask, voxelTask);
            var result = index == 0 ? kdTask.Result : voxelTask.Result;

            return result;
        }


        /// <inheritdoc/>
        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNeighborsInRadius(IReadOnlyList<TDimension> target, TDimension radius,
            int numNeighbors = -1)
        {
            Task<(IReadOnlyList<TDimension>, TNode)[]> kdTask = Task.Run(() => kdTreeSearch.GetNeighborsInRadius(target,radius, numNeighbors).ToArray());
            //Task<(IReadOnlyList<TDimension>, TNode)[]> linearTask = Task.Run(() => linearSearch.GetNeighborsInRadius(target, radius, numNeighbors).ToArray());
            Task<(IReadOnlyList<TDimension>, TNode)[]> voxelTask = Task.Run(() => voxelSearch.GetNeighborsInRadius(target, radius, numNeighbors).ToArray());

            //int index = Task.WaitAny(kdTask, linearTask, voxelTask);
            //var result = index == 0 ? kdTask.Result : index == 1 ? linearTask.Result : voxelTask.Result;
            int index = Task.WaitAny(kdTask,  voxelTask);
            var result = index == 0 ? kdTask.Result : voxelTask.Result;

            return result;
        }
    }
}