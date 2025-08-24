using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading; // added for threading constructs

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
            // Run all three strategies in parallel and return the first one that completes.
            // Requirement: use WaitOne to obtain the earliest result.
            var done = new ManualResetEvent(false);
            (IReadOnlyList<TDimension>, TNode) result = default;
            int resultSet = 0; // 0 = not set

            void TrySetResult((IReadOnlyList<TDimension>, TNode) r)
            {
                if (Interlocked.CompareExchange(ref resultSet, 1, 0) == 0)
                {
                    result = r;
                    done.Set();
                }
            }

            // Start each search on the ThreadPool
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { TrySetResult(kdTreeSearch.GetNearestNeighbor(target)); } catch { /* ignore */ }
            });
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { TrySetResult(linearSearch.GetNearestNeighbor(target)); } catch { /* ignore */ }
            });
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { TrySetResult(voxelSearch.GetNearestNeighbor(target)); } catch { /* ignore */ }
            });

            // Wait for the first completed result.
            done.WaitOne();
            return result;
        }
        /// <inheritdoc/>
        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNearestNeighbors(IReadOnlyList<TDimension> target, int numNeighbors)
        {
            // Run searches concurrently, streaming the full result set of whichever finishes first, then the others.
            var events = new[] { new ManualResetEvent(false), new ManualResetEvent(false), new ManualResetEvent(false) };
            var results = new List<(IReadOnlyList<TDimension>, TNode)>[3];

            ThreadPool.QueueUserWorkItem(_ => { try { results[0] = kdTreeSearch.GetNearestNeighbors(target, numNeighbors).ToList(); } catch { results[0] = new(); } finally { events[0].Set(); } });
            ThreadPool.QueueUserWorkItem(_ => { try { results[1] = linearSearch.GetNearestNeighbors(target, numNeighbors).ToList(); } catch { results[1] = new(); } finally { events[1].Set(); } });
            ThreadPool.QueueUserWorkItem(_ => { try { results[2] = voxelSearch.GetNearestNeighbors(target, numNeighbors).ToList(); } catch { results[2] = new(); } finally { events[2].Set(); } });

            var remainingEventHandles = new List<WaitHandle>(events);
            var processed = new bool[3];
            var comparer = new PointComparer();
            var seen = new HashSet<IReadOnlyList<TDimension>>(comparer);

            while (remainingEventHandles.Count > 0)
            {
                int signaledIndex = WaitHandle.WaitAny(remainingEventHandles.ToArray());
                var handle = remainingEventHandles[signaledIndex];
                int originalIndex = Array.IndexOf(events, handle);
                if (originalIndex >= 0 && !processed[originalIndex])
                {
                    processed[originalIndex] = true;
                    if (results[originalIndex] != null)
                    {
                        foreach (var tuple in results[originalIndex])
                        {
                            if (tuple.Item1 != null && seen.Add(tuple.Item1))
                                yield return tuple;
                        }
                    }
                }
                // Remove this handle so future WaitAny ignores it.
                remainingEventHandles.RemoveAt(signaledIndex);
            }
        }


        /// <inheritdoc/>
        public override IEnumerable<(IReadOnlyList<TDimension>, TNode)> GetNeighborsInRadius(IReadOnlyList<TDimension> target, TDimension radius,
            int numNeighbors = -1)
        {
            var events = new[] { new ManualResetEvent(false), new ManualResetEvent(false), new ManualResetEvent(false) };
            var results = new List<(IReadOnlyList<TDimension>, TNode)>[3];

            ThreadPool.QueueUserWorkItem(_ => { try { results[0] = kdTreeSearch.GetNeighborsInRadius(target, radius, numNeighbors).ToList(); } catch { results[0] = new(); } finally { events[0].Set(); } });
            ThreadPool.QueueUserWorkItem(_ => { try { results[1] = linearSearch.GetNeighborsInRadius(target, radius, numNeighbors).ToList(); } catch { results[1] = new(); } finally { events[1].Set(); } });
            ThreadPool.QueueUserWorkItem(_ => { try { results[2] = voxelSearch.GetNeighborsInRadius(target, radius, numNeighbors).ToList(); } catch { results[2] = new(); } finally { events[2].Set(); } });

            var remainingEventHandles = new List<WaitHandle>(events);
            var processed = new bool[3];
            var comparer = new PointComparer();
            var seen = new HashSet<IReadOnlyList<TDimension>>(comparer);

            while (remainingEventHandles.Count > 0)
            {
                int signaledIndex = WaitHandle.WaitAny(remainingEventHandles.ToArray());
                var handle = remainingEventHandles[signaledIndex];
                int originalIndex = Array.IndexOf(events, handle);
                if (originalIndex >= 0 && !processed[originalIndex])
                {
                    processed[originalIndex] = true;
                    if (results[originalIndex] != null)
                    {
                        foreach (var tuple in results[originalIndex])
                        {
                            if (tuple.Item1 != null && seen.Add(tuple.Item1))
                                yield return tuple;
                        }
                    }
                }
                remainingEventHandles.RemoveAt(signaledIndex);
            }
        }

        // Equality comparer for point coordinate lists to avoid duplicate yields.
        private sealed class PointComparer : IEqualityComparer<IReadOnlyList<TDimension>>
        {
            public bool Equals(IReadOnlyList<TDimension>? x, IReadOnlyList<TDimension>? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null || x.Count != y.Count) return false;
                for (int i = 0; i < x.Count; i++)
                {
                    if (!x[i].Equals(y[i])) return false;
                }
                return true;
            }
            public int GetHashCode(IReadOnlyList<TDimension> obj)
            {
                unchecked
                {
                    int hash = 17;
                    for (int i = 0; i < obj.Count; i++)
                        hash = hash * 31 + obj[i].GetHashCode();
                    return hash;
                }
            }
        }

    }
}