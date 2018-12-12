﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Model.Scene
    {
        /// <summary>
        /// 
        /// </summary>
        public abstract class GroupNodeBase : SceneNode
        {
            public enum Operation
            {
                Add, Remove, Clear
            }

            public sealed class OnChildNodeChangedArgs : EventArgs
            {
                /// <summary>
                /// Gets or sets the node.
                /// </summary>
                /// <value>
                /// The node.
                /// </value>
                public SceneNode Node { private set; get; }
                /// <summary>
                /// Gets or sets a value indicating whether [add =true or remove = false].
                /// </summary>
                /// <value>
                ///   <c>true</c> if [add or remove]; otherwise, <c>false</c>.
                /// </value>
                public Operation Operation { private set; get; }
                /// <summary>
                /// Initializes a new instance of the <see cref="OnChildNodeChangedArgs"/> class.
                /// </summary>
                /// <param name="node">The node.</param>
                /// <param name="operation">if set to <c>true</c> [add or remove].</param>
                public OnChildNodeChangedArgs(SceneNode node, Operation operation)
                {
                    Node = node;
                    Operation = operation;
                }

                public static implicit operator SceneNode(OnChildNodeChangedArgs args) { return args.Node; }
            }
            protected readonly Dictionary<Guid, SceneNode> itemHashSet = new Dictionary<Guid, SceneNode>();

            public event EventHandler<OnChildNodeChangedArgs> OnAddChildNode;
            public event EventHandler<OnChildNodeChangedArgs> OnRemoveChildNode;
            public event EventHandler<OnChildNodeChangedArgs> OnClear;
            /// <summary>
            /// Initializes a new instance of the <see cref="GroupNodeBase"/> class.
            /// </summary>
            public GroupNodeBase()
            {
                ItemsInternal = new System.Collections.ObjectModel.ObservableCollection<SceneNode>();
                Items = new System.Collections.ObjectModel.ReadOnlyObservableCollection<SceneNode>(ItemsInternal);
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="GroupNodeBase"/> class.
            /// </summary>
            /// <param name="name">The name.</param>
            public GroupNodeBase(string name) : this()
            {
                Name = name;
            }

            public bool AddChildNode(SceneNode node)
            {
                if (node != null && !itemHashSet.ContainsKey(node.GUID))
                {
                    itemHashSet.Add(node.GUID, node);
                    ItemsInternal.Add(node);
                    if(node.Parent != NullSceneNode.NullNode && node.Parent != this)
                    {
                        throw new ArgumentException("SceneNode already attach to a different node");
                    }
                    node.Parent = this;
                    if (IsAttached)
                    {
                        node.Attach(RenderHost);
                    }
                    OnAddChildNode?.Invoke(this, new OnChildNodeChangedArgs(node, Operation.Add));
                    return true;
                }
                else { return false; }
            }
            /// <summary>
            /// Clears this instance.
            /// </summary>
            public void Clear()
            {
                for (int i = 0; i < ItemsInternal.Count; ++i)
                {
                    ItemsInternal[i].Detach();
                    ItemsInternal[i].Parent = null;
                }
                ItemsInternal.Clear();
                itemHashSet.Clear();
                OnClear?.Invoke(this, new OnChildNodeChangedArgs(null, Operation.Clear));
            }
            /// <summary>
            /// Removes the child node.
            /// </summary>
            /// <param name="node">The node.</param>
            /// <returns></returns>
            public bool RemoveChildNode(SceneNode node)
            {
                if (node != null && itemHashSet.Remove(node.GUID))
                {
                    node.Detach();             
                    ItemsInternal.Remove(node);
                    node.Parent = null;
                    OnRemoveChildNode?.Invoke(this, new OnChildNodeChangedArgs(node, Operation.Remove));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            /// <summary>
            /// Tries the get node.
            /// </summary>
            /// <param name="guid">The unique identifier.</param>
            /// <param name="node">The node.</param>
            /// <returns></returns>
            public bool TryGetNode(Guid guid, out SceneNode node)
            {
                return itemHashSet.TryGetValue(guid, out node);
            }
            /// <summary>
            /// Called when [attach].
            /// </summary>
            /// <param name="host">The host.</param>
            /// <returns></returns>
            protected override bool OnAttach(IRenderHost host)
            {
                if (base.OnAttach(host))
                {
                    for (int i = 0; i < ItemsInternal.Count; ++i)
                    {
                        ItemsInternal[i].Attach(host);
                    }
                    return true;
                }
                else { return false; }
            }
            /// <summary>
            /// Called when [detach].
            /// </summary>
            protected override void OnDetach()
            {
                for (int i = 0; i < ItemsInternal.Count; ++i)
                {
                    ItemsInternal[i].Detach();
                }
                base.OnDetach();
            }

            /// <summary>
            /// Called when [hit test].
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="totalModelMatrix">The total model matrix.</param>
            /// <param name="ray">The ray.</param>
            /// <param name="hits">The hits.</param>
            /// <returns></returns>
            protected override bool OnHitTest(RenderContext context, Matrix totalModelMatrix, ref Ray ray, ref List<HitTestResult> hits)
            {
                bool hit = false;
                foreach (var c in this.ItemsInternal)
                {
                    if (c.HitTest(context, ray, ref hits))
                    {
                        hit = true;
                    }
                }
                return hit;
            }

            /// <summary>
            /// Called when [dispose].
            /// </summary>
            /// <param name="disposeManagedResources">if set to <c>true</c> [dispose managed resources].</param>
            protected override void OnDispose(bool disposeManagedResources)
            {
                Clear();
                OnClear = null;
                base.OnDispose(disposeManagedResources);
            }
        }
    }

}