using System;
using System.Collections.Generic;
using ExTools.Utillties;
using MessagePack;
using Save.Serialization;
using Save.Serialization.Storage;
using Script.Save.Serialization;
using Script.Save.Serialization.Storage;
using Script.Utillties;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Test.Serializer
{
    public class TestPoco
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    // 用於測試複雜場景的資料類別
    [CustomSerialize]
    public class ComplexData
    {
        public string Description { get; set; }
        public Dictionary<string, int> StringIntMap { get; set; }
        public List<TestPoco> PocoList { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Color ObjectColor { get; set; }
        public LayerMask PhysicsLayer { get; set; }
    }

// 用於測試多態性的基底類別和衍生類別
    [Union(0, typeof(Dog))]
    [Union(1, typeof(Cat))]
    public abstract class Animal
    {
        public abstract string MakeSound();
    }

    public class Dog : Animal
    {
        public string Breed { get; set; }
        public override string MakeSound() => "Woof!";
    }

    public class Cat : Animal
    {
        public int Lives { get; set; }
        public override string MakeSound() => "Meow!";
    }

    public class Zoo
    {
        public List<Animal> Animals { get; set; }
    }

    // 用於測試循環引用的類別
    public class Parent
    {
        public string Name { get; set; }
        public Child Child { get; set; }
    }

    public class Child
    {
        public string Name { get; set; }
        [IgnoreMember] // 如果沒有 PreserveReferences，這個標記是必須的，以避免無限循環
        public Parent MyParent { get; set; }
    }
    
    public class MessagePackUsageExample : MonoBehaviour
    {
        [Button]
        public void StartTest()
        {
            Debug.Log("========== Running MessagePackSerializer Tests ==========");

            TestComplexObjectSerialization();
            TestPolymorphismSerialization();
            TestCircularReferenceSerialization();

            Debug.Log("========== MessagePackSerializer Tests Finished ==========");
        }

        private void RegisterConverters(ISerializer serializer)
        {
            var converterManager = serializer.GetTypeConverterManager();
            converterManager.AddConverter(new Vector2TypeConverter());
            converterManager.AddConverter(new Vector3TypeConverter());
            converterManager.AddConverter(new QuaternionTypeConverter());
            converterManager.AddConverter(new ColorTypeConverter());
            converterManager.AddConverter(new LayerMaskTypeConverter());
            converterManager.AddConverter(new Matrix4x4TypeConverter());
            converterManager.AddConverter(new GameObjectTypeConverter());
        }

        private void TestComplexObjectSerialization()
        {
            Debug.Log("--- Test: Complex Object with Unity Types ---");

            // 1. 設定
            var settings = new SerializationSettings(); // 使用預設設定
            var serializer = new MessagePackSerializerWithStorage(settings);
            RegisterConverters(serializer);

            // 2. 建立原始物件
            var original = new ComplexData
            {
                Description = "Test data with Unity structs",
                StringIntMap = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } },
                PocoList = new List<TestPoco>
                {
                    new TestPoco { Id = 101, Name = "First" },
                    new TestPoco { Id = 102, Name = "Second" }
                },
                Position = new Vector3(1.1f, 2.2f, 3.3f),
                Rotation = Quaternion.Euler(30, 60, 90),
                ObjectColor = Color.red,
                PhysicsLayer = LayerMask.GetMask("Default", "Water")
            };

            // 3. 序列化與反序列化
            var serializedData = serializer.Serialize(original);
            var deserialized = serializer.Deserialize<ComplexData>(serializedData);

            // 4. 驗證
            var success = deserialized != null &&
                          deserialized.Description == original.Description &&
                          deserialized.PocoList.Count == 2 &&
                          deserialized.PocoList[0].Name == "First" &&
                          Math.Abs(deserialized.Position.x - 1.1f) < 0.001f &&
                          Math.Abs(deserialized.ObjectColor.r - 1.0f) < 0.001f &&
                          deserialized.PhysicsLayer.value == original.PhysicsLayer.value;

            Debug.Log(success ? "Complex Object Test: SUCCESS" : "Complex Object Test: FAILED");
            Debug.Assert(success, "Complex Object Test Failed!");
        }

        private void TestPolymorphismSerialization()
        {
            Debug.Log("--- Test: Polymorphism (TypeNameHandling) ---");

            // 1. 設定 - 必須啟用 TypeNameHandling
            var settings = new SerializationSettings
            {
                TypeNameHandling = SerializationTypeNameHandling.kObjects
            };
            var serializer = new MessagePackSerializerWithStorage(settings);
            RegisterConverters(serializer);

            // 2. 建立原始物件
            var original = new Zoo
            {
                Animals = new List<Animal>
                {
                    new Dog { Breed = "Golden Retriever" },
                    new Cat { Lives = 9 }
                }
            };

            // 3. 序列化與反序列化
            var serializedData = serializer.Serialize(original);
            var deserialized = serializer.Deserialize<Zoo>(serializedData);

            // 4. 驗證
            var success = deserialized != null &&
                          deserialized.Animals.Count == 2 &&
                          deserialized.Animals[0] is Dog &&
                          (deserialized.Animals[0] as Dog)?.Breed == "Golden Retriever" &&
                          deserialized.Animals[1] is Cat &&
                          (deserialized.Animals[1] as Cat)?.Lives == 9;

            Debug.Log(success ? "Polymorphism Test: SUCCESS" : "Polymorphism Test: FAILED");
            Debug.Assert(success, "Polymorphism Test Failed!");
        }

        private void TestCircularReferenceSerialization()
        {
            Debug.Log("--- Test: Circular References (PreserveReferences) ---");

            // 1. 設定 - 必須啟用 PreserveReferences
            var settings = new SerializationSettings
            {
                PreserveReferences = true
            };
            var serializer = new MessagePackSerializerWithStorage(settings);
            RegisterConverters(serializer);

            // 2. 建立原始物件
            var parent = new Parent { Name = "John" };
            var child = new Child { Name = "Jane", MyParent = parent };
            parent.Child = child;

            // 3. 序列化與反序列化
            var serializedData = serializer.Serialize(parent);
            var deserialized = serializer.Deserialize<Parent>(serializedData);

            // 4. 驗證
            var success = deserialized != null &&
                          deserialized.Name == "John" &&
                          deserialized.Child.Name == "Jane" &&
                          // 最關鍵的驗證：孩子的父級引用是否指向反序列化後的父級物件
                          ReferenceEquals(deserialized, deserialized.Child.MyParent);

            Debug.Log(success ? "Circular Reference Test: SUCCESS" : "Circular Reference Test: FAILED");
            Debug.Assert(success, "Circular Reference Test Failed!");
        }
    }
}