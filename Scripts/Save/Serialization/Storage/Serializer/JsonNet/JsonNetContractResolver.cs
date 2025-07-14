using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;

namespace Save.Serialization.Storage.Serializer.JsonNet
{
    /// <summary>
    /// Provides custom logic for serialization and deserialization of property values by utilizing a specified
    /// <see cref="JsonConverter"/>.
    /// </summary>
    /// <remarks>
    /// This class implements the <see cref="IValueProvider"/> interface to define custom behavior for getting
    /// and setting property values during JSON serialization and deserialization. It allows a specific
    /// converter to handle the transformation of property values to and from JSON tokens.
    /// </remarks>
    /// <threadsafety>
    /// Instances of this class are not guaranteed to be thread-safe.
    /// </threadsafety>
    public class CustomPropertyValueProvider : IValueProvider
    {
        private readonly PropertyInfo property_;
        private readonly JsonConverter converter_;

        public CustomPropertyValueProvider(PropertyInfo property, JsonConverter converter)
        {
            property_ = property;
            converter_ = converter;
        }

        public void SetValue(object target, object value)
        {
            // 使用转换器进行自定义反序列化
            if (converter_ != null && value is JToken token)
            {
                using var reader = token.CreateReader();
                var context = JsonSerializer.CreateDefault().Context;
                var serializer = new JsonSerializer { Context = context };
                var converted_value = converter_.ReadJson(reader, property_.PropertyType,
                    property_.GetValue(target), serializer);
                property_.SetValue(target, converted_value);
                return;
            }

            // 默认设置
            property_.SetValue(target, value);
        }

        public object GetValue(object target)
        {
            var value = property_.GetValue(target);

            // 使用转换器进行自定义序列化
            if (converter_ != null && converter_.CanConvert(property_.PropertyType))
            {
                // 需要在这里使用间接方法，因为GetValue需要返回值而非写入
                var token_writer = new JTokenWriter();
                using var writer = token_writer;
                converter_.WriteJson(writer, value, new JsonSerializer());
                return token_writer.Token;
            }

            return value;
        }
    }

    /// <summary>
    /// A custom implementation of <see cref="DefaultContractResolver"/> that allows for applying
    /// tailored serialization logic to JSON properties within a .NET application.
    /// </summary>
    /// <remarks>
    /// This class overrides the property creation process by customizing the behavior of the
    /// <see cref="CreateProperties"/> method. It enables advanced control over the serialization
    /// and deserialization of properties, such as modifying their inclusion, visibility, or naming.
    /// The custom serialization logic is applied to JSON properties based on the specified type
    /// and member serialization strategy.
    /// </remarks>
    /// <threadsafety>
    /// This class inherits from <see cref="DefaultContractResolver"/>, which is thread-safe when
    /// the default instance is used. Custom instances of this class are not guaranteed to be thread-safe
    /// and should be accessed with proper synchronization if shared across multiple threads.
    /// </threadsafety>
    public class JsonNetContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties=base.CreateProperties(type, memberSerialization);

            return ApplyCustomSerializationLogic(properties, type);
        }

        /// <summary>
        /// Applies custom serialization logic to a list of JSON properties based on the specified type.
        /// This method customizes property behaviors during serialization and deserialization, ensuring
        /// compliance with custom attributes and serialization rules.
        /// </summary>
        /// <param name="properties">The list of JSON properties that will be modified or filtered according to
        /// custom rules.</param>
        /// <param name="type">The type containing members for which serialization logic is being applied.</param>
        /// <returns>A modified list of <see cref="JsonProperty"/> containing properties after applying custom
        /// serialization logic.</returns>
        private IList<JsonProperty> ApplyCustomSerializationLogic(IList<JsonProperty> properties, Type type)
        {
            var result = new List<JsonProperty>();

            var all_members = GetAllSerializableMembers(type);

            foreach (var member in all_members)
            {
                var property=properties.FirstOrDefault(p=>p.UnderlyingName==member.Name);

                if (property==null)
                {
                    // 如果原本不再序列化列表，但有PersistField标记，需要创建新的属性
                    if (member.GetCustomAttribute<PersistFieldAttribute>()!=null)
                    {
                        property=CreatePropertyFromMember(member,MemberSerialization.Fields);
                    }
                    else
                    {
                        continue;
                    }
                }
                
                // 应用序列化规则
                ApplySerializationRules(property, member);

                if (!property.Ignored)
                {
                    result.Add(property);
                }
            }

            return result;
        }

        /// <summary>
        /// Applies specific serialization rules to a property based on metadata from the associated member.
        /// This method modifies property behaviors during serialization and deserialization depending on
        /// custom attributes and field properties.
        /// </summary>
        /// <param name="property">The JSON property to which the serialization rules are being applied.</param>
        /// <param name="member">The member metadata used to evaluate custom serialization rules.</param>
        private void ApplySerializationRules(JsonProperty property, MemberInfo member)
        {
            if (property.Ignored)
            {
                return;
            }

            var has_non_serialized = member.GetCustomAttribute<NonSerializeAttribute>()!=null;
            
            // 检查字段的IsNotSerialized属性
            var is_field_not_serialized = member is FieldInfo { IsNotSerialized: true };

            if (has_non_serialized||is_field_not_serialized)
            {
                property.Ignored = true;
                property.ShouldSerialize = _ => false;
                property.ShouldDeserialize = _ => false;
                return;
            }
            
            // 检查PersistFieldAttribute
            var persist_field_attr=member.GetCustomAttribute<PersistFieldAttribute>();
            if (persist_field_attr!=null)
            {
                property.Ignored = false;
                property.Readable = true;
                property.Writable = true;
                property.ShouldSerialize = _ => true;
                property.ShouldDeserialize = _ => true;
                
                // 应用自定义属性名
                if (!string.IsNullOrEmpty(persist_field_attr.PropertyName))
                {
                    property.PropertyName = persist_field_attr.PropertyName;
                }
                
                // 应用必须性
                if (persist_field_attr.Required)
                {
                    property.Required = Required.Always;
                }
                
                // 应用默认值
                if (persist_field_attr.DefaultValue!=null)
                {
                    property.DefaultValue = persist_field_attr.DefaultValue;
                }
            }
            else
            {
                if (IsPrivateMember(member))
                {
                    property.Ignored = true;
                    property.ShouldSerialize = _ => false;
                    property.ShouldDeserialize = _ => false;
                    return;
                }
            }
            
            ProcessCustomSerializeAttribute(property, member);
        }

        /// <summary>
        /// Processes a custom serialization attribute applied to a member and applies the associated logic
        /// to the provided JSON property. This method allows customization of serialization behavior using
        /// a specified converter type from the <see cref="CustomSerializeAttribute"/> attribute.
        /// </summary>
        /// <param name="property">The JSON property to which the custom serialization logic will be applied.</param>
        /// <param name="member">The member containing the <see cref="CustomSerializeAttribute"/> that defines the
        /// custom serialization behavior.</param>
        private void ProcessCustomSerializeAttribute(JsonProperty property, MemberInfo member)
        {
            var customSerializeAttr = member.GetCustomAttribute<CustomSerializeAttribute>();
            if (customSerializeAttr?.ConverterType != null)
            {
                if (Activator.CreateInstance(customSerializeAttr.ConverterType) is JsonConverter converter)
                {
                    if (member is PropertyInfo propertyInfo)
                    {
                        property.ValueProvider = new CustomPropertyValueProvider(propertyInfo, converter);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether a given member is private, based on its accessibility level.
        /// This method checks if the member is a field or property and evaluates its
        /// access modifiers to identify if it is private.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> instance representing the member
        /// that needs to be checked for privacy.</param>
        /// <returns><c>true</c> if the member is private; otherwise, <c>false</c>.</returns>
        private bool IsPrivateMember(MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return field.IsPrivate;
            }

            if (member is PropertyInfo property)
            {
                var getter=property.GetGetMethod(true);
                var setter=property.GetSetMethod(true);
                
                return (getter?.IsPrivate ?? true)&&(setter?.IsPrivate ?? true);
            }

            return false;
        }

        /// <summary>
        /// Creates a JSON property based on the specified member and serialization options.
        /// This method generates a property representation for serialization/deserialization
        /// based on metadata from the provided member.
        /// </summary>
        /// <param name="member">The member information (such as a field or property) from which the JSON property is
        /// created.</param>
        /// <param name="optIn">Specifies the serialization options, such as whether member serialization
        /// is opt-in or other custom rules are applied.</param>
        /// <returns>A <see cref="JsonProperty"/> instance representing the JSON property
        /// created from the specified member.</returns>
        private JsonProperty CreatePropertyFromMember(MemberInfo member, MemberSerialization optIn)
        {
            var property = base.CreateProperty(member, optIn);
            return property;
        }

        /// <summary>
        /// Retrieves all serializable members (fields and properties) of the specified type, including both public
        /// and non-public members.
        /// This method filters out non-serializable members, such as readonly fields, constants, and indexed properties,
        /// ensuring that only applicable members are considered for serialization.
        /// </summary>
        /// <param name="type">The type whose serializable members are being retrieved.</param>
        /// <returns>An enumerable collection of <see cref="MemberInfo"/> representing the serializable fields and
        /// properties of the specified type.</returns>
        private IEnumerable<MemberInfo> GetAllSerializableMembers(Type type)
        {
            var binding_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var fields = type.GetFields(binding_flags).Where(f => !f.IsInitOnly && !f.IsLiteral).Cast<MemberInfo>();
            var properties = type.GetProperties(binding_flags)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0).Cast<MemberInfo>();
            
            return fields.Concat(properties);
        }
    }
}