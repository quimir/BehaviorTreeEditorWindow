# 简介
在使用Unity开发中出现了一个核心痛点：不同序列化器之前的差异性导致的重复代码编写问题。为了解决这个问题，设计了该序列化框架，框架的核心目的是通过统一抽象和适配器模式，实现“写一套代码，适配所有序列化器”的目标。

##  设计理念
框架基于两个关键观察：1.文件处理统一性：序列化器都关心“这个文件的内部格式是否可以进行处理”以及“能否转换为内部格式”。2.类型转换统一性：序列化器一般都关心“这个类型是否可以序列化/反序列化”以及“如何转换为内部格式”

## 架构设计
### 1.核心接口层
**ISerializer-序列化器最小单元**
```c#
public interface ISerializer
{
    object Serialize<T>(T obj);
    T Deserialize<T>(object data);
    string SerializerName { get; }
    Type GetSerializedDataType();
    ITypeConverterMessage GetTypeConverterManager();
}
```
设计要点：

- 定义序列化和反序列化的基本契约
- 提供唯一标识符用于区分不同序列化器
- 通过 `GetSerializedDataType()` 明确数据类型（如文本、二进制）
- 集成类型转换器管理 

**ITypeConverter-类型转换适配器**

```c#
public interface ITypeConverter<T> : ITypeConverter
{
    object Serialize(T value, ISerializationContext context);
    T Deserialize(object data, ISerializationContext context);
}
```

设计要点:

- 允许自定义类型按照特定规则进行序列化
- 支持上下文传递，提供灵活的序列化环境
- 推荐使用key-value格式，便于处理和适配

### 2.专用接口扩展

**文本序列化器**

```c#
public interface ITextSerializer : ISerializer
{
    string SerializeToText<T>(T obj);
    T DeserializeFromText<T>(string text);
}
```

**二进制序列化器**

```c#
public interface IBinarySerializer : ISerializer
{
    byte[] SerializeToBinary<T>(T obj);
    T DeserializeFromBinary<T>(byte[] data);
}
```

两者主要是为顶层的序列化器所考虑的，其主要专注于当数据序列化之后其会变为文本格式还是二进制格式。并为其提供相应的接口

