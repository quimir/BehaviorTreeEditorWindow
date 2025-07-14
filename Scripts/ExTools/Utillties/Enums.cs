namespace ExTools.Utillties
{
    /// <summary>
    /// Represents the state of a behavior in a behavior tree.
    /// </summary>
    public enum BehaviorState
    {
        /// <summary>
        /// 执行成功
        /// </summary>
        kSucceed,
        /// <summary>
        /// 执行失败
        /// </summary>
        kFailure,
        /// <summary>
        /// 没有在运行
        /// </summary>
        kNonExecuting,
        /// <summary>
        /// 运行中
        /// </summary>
        kExecuting
    }

    public enum SerializerType
    {
        kDefault=0,
        kJson,
        kBinary,
        kMessagePack,
        kCustom
    }

    /// <summary>
    /// 类型名称处理策略
    /// </summary>
    public enum SerializationTypeNameHandling
    {
        /// <summary>
        /// 不包含类型信息
        /// </summary>
        kNone,
        
        /// <summary>
        /// 包含对象的类型信息
        /// </summary>
        kObjects,
        
        /// <summary>
        /// 包含数组的类型信息
        /// </summary>
        kArrays,
        
        /// <summary>
        /// 自动处理类型信息
        /// </summary>
        kAuto,
        
        /// <summary>
        /// 对所有类型都包含类型信息
        /// </summary>
        kAll
    }

    /// <summary>
    /// 日期时间处理格式
    /// </summary>
    public enum SerializationDateTimeHandling
    {
        /// <summary>
        /// ISO 8601格式
        /// </summary>
        kIsoFormat,
        
        /// <summary>
        /// Unix时间戳(秒)
        /// </summary>
        kUnixTimestamp,
        
        /// <summary>
        /// 自定义格式
        /// </summary>
        kCustom
    }

    /// <summary>
    /// Specifies the type of property panel used to display and edit properties in the interface.
    /// </summary>
    public enum PropertyPanelType
    {
        /// <summary>
        /// Default property panel type.
        /// Used as the fallback or uninitialized state of the PropertyPanelType enum.
        /// </summary>
        kDefault,

        /// <summary>
        /// Represents the properties specific to a node in the property panel type.
        /// </summary>
        kNodeProperties = 1,

        /// <summary>
        /// Represents the panel type for displaying child nodes within a property panel.
        /// </summary>
        kChildNodes,

        /// <summary>
        /// Represents the style settings or configurations for a specific node
        /// in the property panel.
        /// </summary>
        kNodeStyle
    }

    /// <summary>
    /// Represents the state of an edge in a behavior tree or node-based visualization system.
    /// </summary>
    public enum EdgeState
    {
        kNormal,
        kSuccess,
        kFailure,
        kRunning
    }
}