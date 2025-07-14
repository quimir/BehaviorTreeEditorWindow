using System;

namespace Save.Serialization.Core.TypeConverter.SerializerAttribute
{
    /// <summary>
    /// 自定义序列化特性，用于标记需要特殊处理的字段或属性.一般来说其不会处理NonSerialize标记的属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false)]
    public class CustomSerializeAttribute : Attribute
    {
        public Type ConverterType { get; } = null;
    }
}
