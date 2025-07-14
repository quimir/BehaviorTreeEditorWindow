using System;
using ExTools.Utillties;

namespace ExTools
{
    /// <summary>
    /// 名称标签，此标签可以自定义名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeLabelAttribute : Attribute
    {
        /// <summary>
        /// 菜单名称
        /// </summary>
        public readonly string menu_name_;

        /// <summary>
        /// 标题名称
        /// </summary>
        public readonly string label_;

        /// <summary>
        /// 显示自定义的名称
        /// </summary>
        /// <param name="menuName">选择目录当中的自定义名称</param>
        /// <param name="label">在名称当中显示的自定义名称</param>
        public NodeLabelAttribute(string menuName, string label = null)
        {
            menu_name_ = menuName;
            label_ = label;
        }
    }

    /// <summary>
    /// 自定义分类节点名称，使用此可以把相同的分类节点组合在一起
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeFoldoutGroup : Attribute
    {
        public readonly string foldout_group_;

        /// <summary>
        /// 显示自定义的分类，使用其可以在菜单当中显示当前类的分类
        /// </summary>
        /// <param name="foldoutGroup">分类名称</param>
        public NodeFoldoutGroup(string foldoutGroup)
        {
            foldout_group_ = foldoutGroup;
        }
    }

    /// <summary>
    /// Attribute used to designate a property or field for integration into a specific property panel type.
    /// </summary>
    /// <remarks>
    /// Applied to fields or properties to specify the relationship with a designated
    /// property panel type. Supports options for inheritance and child application.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false,
        Inherited = true)]
    public class PanelDelegatedPropertyAttribute : Attribute
    {
        /// <summary>
        /// Represents the type of property panel associated with the attribute.
        /// </summary>
        public PropertyPanelType PanelType { get; private set; }

        /// <summary>
        /// Indicates whether the attribute should also apply to child elements or derived members.
        /// </summary>
        /// <remarks>
        /// When set to true, the attribute will extend its effect to child elements or derived members,
        /// allowing inheritance of the specified property or behavior in the hierarchy.
        /// </remarks>
        public bool ApplyToChildren { get; set; }

        public PanelDelegatedPropertyAttribute(PropertyPanelType panelType, bool apply_to_children = false)
        {
            PanelType = panelType;
            ApplyToChildren = apply_to_children;
        }
    }

    /// <summary>
    /// Attribute that is used to specify that the annotated class should not be displayed
    /// or processed in derived classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class HideInDerivedClass : Attribute
    {
    }
}