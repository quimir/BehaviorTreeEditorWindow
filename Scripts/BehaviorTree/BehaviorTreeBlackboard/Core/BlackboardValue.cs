using System;
using System.Globalization;
using UnityEngine;

namespace BehaviorTree.BehaviorTreeBlackboard.Core
{
    public enum BlackboardValueType : byte
    {
        kUndefine=0,
        kNull=1,
        kInt=2,
        kLong=3,
        kFloat=4,
        kDouble=5,
        kBool=6,
        kVector3=7,
        kVector2=8,
        kEnum=9,
        kString=10,
        kObject=15
    }

    /// <summary>
    /// Represents a value stored in a behavior tree blackboard.
    /// A blackboard value can support multiple data types such as int, long, float, double, bool, string, vector types, enums, or objects.
    /// </summary>
    public readonly struct BlackboardValue : IEquatable<BlackboardValue>
    {
        public readonly BlackboardValueType Type;
        public readonly int IntValue;
        public readonly long LongValue;
        public readonly float FloatValue;
        public readonly double DoubleValue;
        public readonly bool BoolValue;
        public readonly byte ByteValue;
        public readonly Vector3 Vector3Value;
        
        public readonly object ObjectValue;

        // 静态工厂方法
        public static BlackboardValue From(int value)
        {
            var new_value = new BlackboardValue(BlackboardValueType.kInt, intVal: value);
            return new_value;
        }
        public static BlackboardValue From(long value) => new BlackboardValue(BlackboardValueType.kLong, longVal: value);
        public static BlackboardValue From(float value) => new BlackboardValue(BlackboardValueType.kFloat, floatVal: value);
        public static BlackboardValue From(double value) => new BlackboardValue(BlackboardValueType.kDouble, doubleVal: value);
        public static BlackboardValue From(bool value) => new BlackboardValue(BlackboardValueType.kBool, boolVal: value);
        public static BlackboardValue From(string value) => new BlackboardValue(BlackboardValueType.kString, objectVal: value);

        public static BlackboardValue From(Vector3 value) =>
            new BlackboardValue(BlackboardValueType.kVector3, vector3Val: value);
        public static BlackboardValue From<T>(T value) => new BlackboardValue(BlackboardValueType.kObject, objectVal: value);
        
        private BlackboardValue(BlackboardValueType type, int intVal = 0, long longVal = 0, float floatVal = 0f, 
            double doubleVal = 0d, bool boolVal = false, object objectVal = null,Vector3 vector3Val=default)
        {
            Type = type;
            IntValue = intVal;
            LongValue = longVal;
            FloatValue = floatVal;
            DoubleValue = doubleVal;
            BoolValue = boolVal;
            ByteValue = 0;
            ObjectValue = objectVal;
            Vector3Value=vector3Val;
        }

        public T Get<T>()
        {
            return Type switch
            {
                BlackboardValueType.kInt when typeof(T) == typeof(int) => (T)(object)IntValue,
                BlackboardValueType.kLong when typeof(T) == typeof(long) => (T)(object)LongValue,
                BlackboardValueType.kFloat when typeof(T) == typeof(float) => (T)(object)FloatValue,
                BlackboardValueType.kDouble when typeof(T) == typeof(double) => (T)(object)DoubleValue,
                BlackboardValueType.kBool when typeof(T) == typeof(bool) => (T)(object)BoolValue,
                BlackboardValueType.kVector3 when typeof(T) == typeof(Vector3) => (T)(object)Vector3Value,
                BlackboardValueType.kString when typeof(T) == typeof(string) => (T)(object)ObjectValue,
                BlackboardValueType.kObject when ObjectValue is T value => value,
                _ => throw new InvalidCastException($"Cannot cast {Type} to {typeof(T)}")
            };
        }

        public bool TryGet<T>(out T value)
        {
            try
            {
                value = Get<T>();
                return true;
            }
            catch (Exception)
            {
                value = default(T);
                return false;
            }
        }
        
        public bool Equals(BlackboardValue other)
        {
            if (Type!=other.Type)
            {
                return false;
            }

            return Type switch
            {
                BlackboardValueType.kInt => IntValue == other.IntValue,
                BlackboardValueType.kLong => LongValue == other.LongValue,
                BlackboardValueType.kFloat => FloatValue.Equals(other.FloatValue),
                BlackboardValueType.kDouble => DoubleValue.Equals(other.DoubleValue),
                BlackboardValueType.kBool => BoolValue == other.BoolValue,
                BlackboardValueType.kVector3 => Vector3Value.Equals(other.Vector3Value),
                BlackboardValueType.kString => ReferenceEquals(ObjectValue, other.ObjectValue) ||
                                               (ObjectValue?.Equals(other.ObjectValue) ?? false),
                BlackboardValueType.kObject => ReferenceEquals(ObjectValue, other.ObjectValue) ||
                                               (ObjectValue?.Equals(other.ObjectValue) ?? false),
                _ => false
            };
        }

        public override bool Equals(object obj) => obj is BlackboardValue other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Type, IntValue, LongValue, ObjectValue);

        public override string ToString()
        {
            return Type switch
            {
                BlackboardValueType.kInt => IntValue.ToString(),
                BlackboardValueType.kLong => LongValue.ToString(),
                BlackboardValueType.kFloat => FloatValue.ToString(CultureInfo.InvariantCulture),
                BlackboardValueType.kDouble => DoubleValue.ToString(CultureInfo.InvariantCulture),
                BlackboardValueType.kBool => BoolValue.ToString(),
                BlackboardValueType.kVector3 => Vector3Value.ToString(),
                BlackboardValueType.kString => ObjectValue?.ToString() ?? "null",
                BlackboardValueType.kObject => ObjectValue?.ToString() ?? "null",
                _ => "Unknown"
            };
        }
    }
}