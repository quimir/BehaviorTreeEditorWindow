using System.Collections.Generic;
using BehaviorTree.Nodes;
using Script.BehaviorTree.Save;
using UnityEngine;

namespace Editor.EditorToolExs.BtNodeWindows
{
    /// <summary>
    /// Manages styles for behavior tree nodes, providing functionality to retrieve, apply, reset,
    /// and clear styles associated with nodes. This class ensures that each node has a style,
    /// generating a default style if one does not exist.
    /// </summary>
    public class BtNodeStyleManager
    {
        private readonly Dictionary<string, BtNodeStyle> node_styles_ = new();

        /// <summary>
        /// Provides access to the dictionary containing styles for behavior tree nodes.
        /// The <c>NodeStyles</c> property stores mappings of node identifiers to their respective
        /// styles, enabling customization and retrieval of visual properties for each node.
        /// This property can also be set, allowing external updates to the style collection.
        /// </summary>
        public Dictionary<string, BtNodeStyle> NodeStyles => node_styles_;

        // 默认样式
        private readonly BtNodeStyle DefaultCompositeStyle = new BtNodeStyle
        {
            //BackgroundColor = new Color(0.3f, 0.5f, 0.8f),
            TextColor = Color.green,
            FontSize = 12
        };

        private readonly BtNodeStyle DefaultPreconditionStyle = new BtNodeStyle
        {
            //BackgroundColor = new Color(0.8f, 0.6f, 0.2f),
            TextColor = Color.yellow,
            FontSize = 12
        };

        private readonly BtNodeStyle DefaultActionStyle = new BtNodeStyle
        {
            //BackgroundColor = new Color(0.2f, 0.7f, 0.3f),
            TextColor = Color.white,
            FontSize = 12
        };

        public BtNodeStyleManager()
        {
        }

        public BtNodeStyleManager(Dictionary<string, BtNodeStyle> node_styles)
        {
            if (node_styles.Count>0)
            {
                node_styles_ = node_styles;
            }
        }
        
        /// <summary>
        /// 尝试获取节点的风格，如果该节点没有在节点管理器注册的话，会注册一个默认风格
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns>已经在节点管理器注册的风格/节点的默认风格</returns>
        public BtNodeStyle TryGetNodeStyle(BtNodeBase node)
        {
            if (node==null)
            {
                return null;
            }
            
            // 尝试从缓存获取
            if (node_styles_.TryGetValue(node.Guild,out var style))
            {
                return style;
            }

            style = CreateDefaultStyle(node);

            if (string.IsNullOrEmpty(style.NodeGuid))
            {
                style.NodeGuid = node.Guild;
            }
            node_styles_[node.Guild] = style;
            return style;
        }

        /// <summary>
        /// Creates a default style for the specified node based on its type.
        /// </summary>
        /// <param name="node">The node for which the default style is to be created.</param>
        /// <returns>A new default style configured according to the type of the node.</returns>
        public BtNodeStyle CreateDefaultStyle(BtNodeBase node)
        {
            BtNodeStyle new_style = new BtNodeStyle
            {
                NodeGuid = node.Guild
            };

            // 根据节点类型设置默认样式
            if (node is BtComposite)
            {
                new_style = CloneStyle(DefaultCompositeStyle);
            }
            else if (node is BtPrecondition)
            {
                new_style = CloneStyle(DefaultPreconditionStyle);
            }
            else if (node is BtActionNode)
            {
                new_style = CloneStyle(DefaultActionStyle);
            }
            
            return new_style;
        }

        /// <summary>
        /// Applies the specified style to a behavior tree node. If the node or style is null, the method will not apply any changes.
        /// </summary>
        /// <param name="node">The behavior tree node to which the style will be applied.</param>
        /// <param name="style">The style to be applied to the behavior tree node.</param>
        public void ApplyStyle(BtNodeBase node, BtNodeStyle style)
        {
            if (node == null || style == null)
            {
                return;
            }

            node_styles_[node.Guild] = style;
        }

        /// <summary>
        /// Resets the style of the specified behavior tree node to its default style.
        /// If the node is not null, a default style is generated and applied to the node.
        /// </summary>
        /// <param name="node">The behavior tree node whose style is to be reset.</param>
        public void ResetNodeStyle(BtNodeBase node)
        {
            if (node == null)
                return;

            var new_style = CreateDefaultStyle(node);
            node_styles_[node.Guild] = new_style;
        }

        /// <summary>
        /// Creates a copy of the specified node style.
        /// </summary>
        /// <param name="source">The source node style to clone.</param>
        /// <returns>A new node style object that is a copy of the provided source style.</returns>
        private BtNodeStyle CloneStyle(BtNodeStyle source)
        {
            return EditorExTools.Instance.CloneBtNodeStyle(source);
        }
    }
}