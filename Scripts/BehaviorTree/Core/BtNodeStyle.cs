using System;
using Script.Save.Serialization;
using Script.Tool;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Script.BehaviorTree.Save
{
    /// <summary>
    /// Represents the style settings for a node in the behavior tree editor.
    /// </summary>
    [Serializable]
    public class BtNodeStyle
    {
        /// <summary>
        /// 需要保存但不需要显示因为该属性为两者链接的属性
        /// </summary>
        [HideInInspector]
        public string NodeGuid;
        
        [LabelText("背景颜色"),FoldoutGroup("节点风格")]
        public Color BackgroundColor=new Color(255,255,255,0.4f);
        
        [LabelText("标题栏背景颜色"),FoldoutGroup("标题风格")]
        public Color TitleBackgroundColor = Color.clear;
        
        [LabelText("文本颜色"),FoldoutGroup("文本风格")]
        public Color TextColor=Color.white;
        
        [LabelText("文本大小"),FoldoutGroup("文本风格"),Range(1,30)]
        public int FontSize = 12;
        
        [LabelText("启用阴影"),ToggleLeft,FoldoutGroup("文本风格")]
        public bool EnableShadow = false;
        
        [LabelText("阴影颜色"),EnableIf("EnableShadow"),FoldoutGroup("文本风格")]
        public Color ShadowColor = new Color(0, 0, 0, 0.5f);
        
        [LabelText("阴影偏移"),EnableIf("EnableShadow"),FoldoutGroup("文本风格")]
        public Vector2 ShadowOffset = new Vector2(1, 1);
        
        [LabelText("阴影模糊半径"),EnableIf("EnableShadow"),FoldoutGroup("文本风格")]
        public int BlurRadius = 1;
    }
}