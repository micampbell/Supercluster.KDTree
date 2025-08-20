// <copyright file="BinaryTreeNavigator.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace SuperClusterKDTreeMedian.Utilities
{
    using System;
    using System.Collections.Generic;
    using static BinaryTreeNavigation;

    /// <summary>
    /// Allows one to navigate a binary tree stored in an <see cref="Array"/> using familiar
    /// tree navigation concepts.
    /// </summary>
    /// <typeparam name="TPoint">The type of the individual points.</typeparam>
    /// <typeparam name="TNode">The type of the individual nodes.</typeparam>
    internal class BinaryTreeNavigator<TPoint, TNode>
    {
        /// <summary>
        /// A reference to the pointArray in which the binary tree is stored in.
        /// </summary>
        private readonly IList<TPoint> pointArray;

        private readonly IList<TNode> nodeArray;

        /// <summary>
        /// The index in the pointArray that the current node resides in.
        /// </summary>
        internal int Index { get; }

        /// <summary>
        /// The left child of the current node.
        /// </summary>
        internal BinaryTreeNavigator<TPoint, TNode> Left
            =>
                LeftChildIndex(this.Index) < this.pointArray.Count - 1
                    ? new BinaryTreeNavigator<TPoint, TNode>(this.pointArray, this.nodeArray, LeftChildIndex(this.Index))
                    : null;

        /// <summary>
        /// The right child of the current node.
        /// </summary>
        internal BinaryTreeNavigator<TPoint, TNode> Right
               =>
                   RightChildIndex(this.Index) < this.pointArray.Count - 1
                       ? new BinaryTreeNavigator<TPoint, TNode>(this.pointArray, this.nodeArray, RightChildIndex(this.Index))
                       : null;

        /// <summary>
        /// The parent of the current node.
        /// </summary>
        internal BinaryTreeNavigator<TPoint, TNode> Parent => this.Index == 0 ? null : new BinaryTreeNavigator<TPoint, TNode>(this.pointArray, this.nodeArray, ParentIndex(this.Index));

        /// <summary>
        /// The current <typeparamref name="TPoint"/>.
        /// </summary>
        internal TPoint Point => this.pointArray[this.Index];

        /// <summary>
        /// The current <typeparamref name="TNode"/>
        /// </summary>
        internal TNode Node => this.nodeArray[this.Index];

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryTreeNavigator{TPoint, TNode}"/> class.
        /// </summary>
        /// <param name="pointArray">The point array backing the binary tree.</param>
        /// <param name="nodeArray">The node array corresponding to the point array.</param>
        /// <param name="index">The index of the node of interest in the pointArray. If not given, the node navigator start at the 0 index (the root of the tree).</param>
        internal BinaryTreeNavigator(IList<TPoint> pointArray, IList<TNode> nodeArray, int index = 0)
        {
            this.Index = index;
            this.pointArray = pointArray;
            this.nodeArray = nodeArray;
        }
    }
}
