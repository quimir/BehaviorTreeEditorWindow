using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BehaviorTree.Nodes;
using ExTools;
using ExTools.Utillties;
using Script.BehaviorTree;
using Script.Tool;
using Script.Utillties;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEngine;

// 创建一个特性处理器，用于在不同面板中组织属性
namespace Editor.View.BTWindows.InspectorUI
{
    /// <summary>
    /// A custom attribute processor used to dynamically group properties into organized panels
    /// for improved UI layout in the inspector.
    /// </summary>
    /// <typeparam name="T">
    /// The type of object being processed, which must inherit from BtNodeBase.
    /// </typeparam>
    public class PanelGroupAttributeProcessor<T> : OdinAttributeProcessor<T> where T : BtNodeBase
    {
        public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member)
        {
            return true;
        }

        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            // 获取PanelDelegatedPropertyAttribute
            var panelAttr = member.GetCustomAttribute<PanelDelegatedPropertyAttribute>();
    
            if (panelAttr != null)
            {
                // 如果是被委派到特定面板的属性，先移除所有组相关的特性
                attributes.RemoveAll(attr => attr is TabGroupAttribute or 
                    FoldoutGroupAttribute or BoxGroupAttribute or TitleGroupAttribute);
        
                // 确定属性应该在哪个分组
                PropertyPanelType panelType = panelAttr.PanelType;
                string tabName = PropertyPanelTypeToString(panelType);
        
                // 仅添加TabGroup，不添加其他Group
                attributes.Add(new TabGroupAttribute("PanelTabs", tabName));
            }
            else
            {
                // 如果不是委派属性，确保移除任何可能的TabGroup，保留其他组特性
                attributes.RemoveAll(attr => attr is TabGroupAttribute or FoldoutGroupAttribute);
        
                // 可以选择添加到默认标签页
                attributes.Add(new TabGroupAttribute("PanelTabs", PropertyPanelTypeToString(PropertyPanelType.kNodeProperties)));
            }
        }

        private string PropertyPanelTypeToString(PropertyPanelType panelType)
        {
            switch (panelType)
            {
                case PropertyPanelType.kDefault:
                    return "默认值";
                case PropertyPanelType.kNodeProperties:
                    return "节点属性";
                case PropertyPanelType.kChildNodes:
                    return "子节点";
                case PropertyPanelType.kNodeStyle:
                    return "节点风格";
                default:
                    throw new ArgumentOutOfRangeException(nameof(panelType), panelType, null);
            }
        }
    }
}
