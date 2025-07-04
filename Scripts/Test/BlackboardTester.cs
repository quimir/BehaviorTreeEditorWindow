using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree.BehaviorTreeBlackboard;
using Sirenix.OdinInspector;
using UnityEngine;

// 自定义类型示例
public class MyCustomClass
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; } // 新增一个属性

    public MyCustomClass(int id, string name)
    {
        Id = id;
        Name = name;
        IsActive = true;
    }

    public override bool Equals(object obj)
    {
        return obj is MyCustomClass other && Id == other.Id && Name == other.Name && IsActive == other.IsActive;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, IsActive);
    }

    public override string ToString()
    {
        return $"MyCustomClass[Id: {Id}, Name: {Name}, Active: {IsActive}]";
    }
}

public enum TestEnum
{
    ValueA,
    ValueB,
    ValueC
}

public class BlackboardTester : MonoBehaviour
{
    // 使用 Odin 的 [ShowInInspector] 和 [ReadOnly] 特性来显示内部状态
    [ShowInInspector]
    [ReadOnly]
    private BlackboardStorage _sharedGlobalBlackboard; // 作为所有 LayeredBlackboard 实例共享的黑板

    [ShowInInspector]
    [ReadOnly]
    private LayeredBlackboard _myLayeredBlackboard;

    [ShowInInspector]
    [ReadOnly]
    private LayeredBlackboard _anotherLayeredBlackboard; // 另一个 LayeredBlackboard 实例，用于测试共享

    // 用于在 Inspector 中设置和获取值的键
    [FoldoutGroup("Keys")] public IntBlackboardKey HealthKey = new("Health", BlackboardKeyModifiers.kNone);
    [FoldoutGroup("Keys")] public FloatBlackboardKey SpeedKey = new("Speed", BlackboardKeyModifiers.kNone);
    [FoldoutGroup("Keys")] public BoolBlackboardKey IsAliveKey = new("IsAlive", BlackboardKeyModifiers.kNone);
    [FoldoutGroup("Keys")] public DefaultBlackboardKey<string> PlayerNameKey = new("PlayerName", BlackboardKeyModifiers.kNone);
    [FoldoutGroup("Keys")] public DefaultBlackboardKey<Vector2> TargetPos2DKey = new("TargetPos2D", BlackboardKeyModifiers.kNone);
    [FoldoutGroup("Keys")] public Vector3BlackboardKey TargetPos3DKey = new("TargetPos3D", BlackboardKeyModifiers.kNone); // 假设已扩展 BlackboardValue
    [FoldoutGroup("Keys")] public DefaultBlackboardKey<MyCustomClass> CustomObjectKey = new("MyCustomData", BlackboardKeyModifiers.kNone);
    [FoldoutGroup("Keys")] public DefaultBlackboardKey<TestEnum> EnumStateKey = new("State", BlackboardKeyModifiers.kNone);

    // 具有特定修饰符的键
    [FoldoutGroup("Keys")] public DefaultBlackboardKey<int> PrivateValueKey = new("PrivateValue", BlackboardKeyModifiers.kPrivate);
    [FoldoutGroup("Keys")] public IntBlackboardKey ReadOnlyValueKey = new("ReadOnlyValue", BlackboardKeyModifiers.kReadonly);
    [FoldoutGroup("Keys")] public IntBlackboardKey VolatileValueKey = new("VolatileValue", BlackboardKeyModifiers.kVolatile | BlackboardKeyModifiers.kEnableEquals);
    [FoldoutGroup("Keys")] public IntBlackboardKey LocalCacheValueKey = new("LocalCacheValue", BlackboardKeyModifiers.kLocalCache);


    // 用于在 Inspector 中输入值的字段
    [PropertySpace(SpaceBefore = 20)]
    [FoldoutGroup("Set Values")] public int SetHealth;
    [FoldoutGroup("Set Values")] public float SetSpeed;
    [FoldoutGroup("Set Values")] public bool SetIsAlive;
    [FoldoutGroup("Set Values")] public string SetPlayerName;
    [FoldoutGroup("Set Values")] public Vector2 SetTargetPos2D;
    [FoldoutGroup("Set Values")] public Vector3 SetTargetPos3D;
    [FoldoutGroup("Set Values")] public int SetCustomObjectId;
    [FoldoutGroup("Set Values")] public string SetCustomObjectName;
    [FoldoutGroup("Set Values")] public TestEnum SetEnumState;
    [FoldoutGroup("Set Values")] public int SetPrivateValue;
    [FoldoutGroup("Set Values")] public int SetReadOnlyValue; // 只能设置一次
    [FoldoutGroup("Set Values")] public int SetVolatileValue;
    [FoldoutGroup("Set Values")] public int SetLocalCacheValue;

    // 用于在 Inspector 中显示获取到的值
    [PropertySpace(SpaceBefore = 20)]
    [FoldoutGroup("Get Values")]
    [ReadOnly] public int GetHealth;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public float GetSpeed;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public bool GetIsAlive;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public string GetPlayerName;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public Vector2 GetTargetPos2D;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public Vector3 GetTargetPos3D;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public MyCustomClass GetCustomObject;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public TestEnum GetEnumState;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public int GetPrivateValue;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public int GetReadOnlyValue;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public int GetVolatileValue;
    [FoldoutGroup("Get Values")]
    [ReadOnly] public int GetLocalCacheValue;

    // 事件监听日志
    [PropertySpace(SpaceBefore = 20)]
    [InfoBox("Volatile Key Change Log")]
    public List<string> VolatileChangeLog = new List<string>();

    [Button("初始化")]
    private void Init()
    {
        // 确保只有一个全局共享黑板实例
        _sharedGlobalBlackboard = new BlackboardStorage();
        _sharedGlobalBlackboard.OnValueChanged += LogVolatileChange; // 监听共享黑板的事件

        _myLayeredBlackboard = new LayeredBlackboard(_sharedGlobalBlackboard);
        _anotherLayeredBlackboard = new LayeredBlackboard(_sharedGlobalBlackboard);

        // 设置只读值的初始值
        try
        {
            _myLayeredBlackboard.SetValue(ReadOnlyValueKey, 999);
            Debug.Log("ReadOnlyValueKey initialized with 999.");
        }
        catch (InvalidOperationException e)
        {
            Debug.LogError($"Error initializing ReadOnlyValueKey: {e.Message}");
        }
    }

    private void LogVolatileChange(IBlackboardKey key, BlackboardValue oldVal, BlackboardValue newVal)
    {
        VolatileChangeLog.Add($"[{DateTime.Now.ToShortTimeString()}] Key: {key.Name}, Old: {oldVal}, New: {newVal}");
        if (VolatileChangeLog.Count > 10) // 限制日志数量
        {
            VolatileChangeLog.RemoveAt(0);
        }
    }

    [Button("Set All Values (My Blackboard)")]
    public void SetAllValuesMyBlackboard()
    {
        _myLayeredBlackboard.SetValue(HealthKey, SetHealth);
        _myLayeredBlackboard.SetValue(SpeedKey, SetSpeed);
        _myLayeredBlackboard.SetValue(IsAliveKey, SetIsAlive);
        _myLayeredBlackboard.SetValue(PlayerNameKey, SetPlayerName);
        _myLayeredBlackboard.SetValue(TargetPos2DKey, SetTargetPos2D);
        _myLayeredBlackboard.SetValue(TargetPos3DKey, SetTargetPos3D);
        _myLayeredBlackboard.SetValue(CustomObjectKey, new MyCustomClass(SetCustomObjectId, SetCustomObjectName));
        _myLayeredBlackboard.SetValue(EnumStateKey, SetEnumState);
        _myLayeredBlackboard.SetValue(PrivateValueKey, SetPrivateValue);
        // _myLayeredBlackboard.SetValue(ReadOnlyValueKey, SetReadOnlyValue); // 此处会因只读特性抛出异常，不建议在按钮中直接调用
        _myLayeredBlackboard.SetValue(VolatileValueKey, SetVolatileValue);
        _myLayeredBlackboard.SetValue(LocalCacheValueKey, SetLocalCacheValue);

        Debug.Log("Values set on My Layered Blackboard.");
        GetAllValuesMyBlackboard(); // 设置后立即刷新显示
    }

    [Button("Get All Values (My Blackboard)")]
    public void GetAllValuesMyBlackboard()
    {
        _myLayeredBlackboard.TryGetValue(HealthKey, out GetHealth);
        _myLayeredBlackboard.TryGetValue(SpeedKey, out GetSpeed);
        _myLayeredBlackboard.TryGetValue(IsAliveKey, out GetIsAlive);
        _myLayeredBlackboard.TryGetValue(PlayerNameKey, out GetPlayerName);
        _myLayeredBlackboard.TryGetValue(TargetPos2DKey, out GetTargetPos2D);
        _myLayeredBlackboard.TryGetValue(TargetPos3DKey, out GetTargetPos3D);
        _myLayeredBlackboard.TryGetValue(CustomObjectKey, out GetCustomObject);
        _myLayeredBlackboard.TryGetValue(EnumStateKey, out GetEnumState);
        _myLayeredBlackboard.TryGetValue(PrivateValueKey, out GetPrivateValue);
        _myLayeredBlackboard.TryGetValue(ReadOnlyValueKey, out GetReadOnlyValue);
        _myLayeredBlackboard.TryGetValue(VolatileValueKey, out GetVolatileValue);
        _myLayeredBlackboard.TryGetValue(LocalCacheValueKey, out GetLocalCacheValue);

        Debug.Log("Values retrieved from My Layered Blackboard.");
    }

    [Button("Clear My Blackboard Private Storage")]
    public void ClearMyPrivateBlackboard()
    {
        var keysToRemove = _myLayeredBlackboard.PrivateStorage.GetAllKeys().ToList();
        foreach (var key in keysToRemove)
        {
            // 这是一个通用移除方法，因为RemoveValue<T>需要T
            // 简单起见，这里假设我们知道类型或者根据key的类型调用RemoveValue
            // 更健壮的实现可能需要反射或多态方法
            if (key is IntBlackboardKey intKey) _myLayeredBlackboard.RemoveValue(intKey);
            else if (key is FloatBlackboardKey floatKey) _myLayeredBlackboard.RemoveValue(floatKey);
            else if (key is BoolBlackboardKey boolKey) _myLayeredBlackboard.RemoveValue(boolKey);
            else if (key is DefaultBlackboardKey<string> stringKey) _myLayeredBlackboard.RemoveValue(stringKey);
            else if (key is DefaultBlackboardKey<Vector2> vec2Key) _myLayeredBlackboard.RemoveValue(vec2Key);
            else if (key is Vector3BlackboardKey vec3Key) _myLayeredBlackboard.RemoveValue(vec3Key);
            else if (key is DefaultBlackboardKey<MyCustomClass> customKey) _myLayeredBlackboard.RemoveValue(customKey);
            else if (key is DefaultBlackboardKey<TestEnum> enumKey) _myLayeredBlackboard.RemoveValue(enumKey);
            // ... 根据需要添加更多类型
        }
        Debug.Log("My Private Blackboard cleared.");
        GetAllValuesMyBlackboard();
    }

    [Button("Remove Volatile Key from My Blackboard (and Shared)")]
    public void RemoveVolatileKey()
    {
        // 移除私有和共享中的 VolatileValueKey
        bool removed = (_myLayeredBlackboard as LayeredBlackboard).RemoveValue(VolatileValueKey, true);
        Debug.Log($"VolatileValueKey removed: {removed}");
        GetAllValuesMyBlackboard(); // 刷新显示
    }

    [Button("Test Shared Access (Another Blackboard)")]
    public void TestSharedAccess()
    {
        Debug.Log("--- Testing Shared Access from Another Layered Blackboard ---");

        // 设置 MyBlackboard 的健康值
        _myLayeredBlackboard.SetValue(HealthKey, 75);
        Debug.Log(
            $"My Blackboard Health set to 75. Shared Health: {_sharedGlobalBlackboard.TryGetValue(HealthKey, out int sh)}");

        // 尝试从 AnotherBlackboard 获取健康值
        _anotherLayeredBlackboard.TryGetValue(HealthKey, out int anotherHealth);
        Debug.Log($"Another Blackboard (shared) Health: {anotherHealth}");
        Debug.Assert(anotherHealth == 75, "Shared health should be 75.");

        // 尝试从 AnotherBlackboard 获取 MyBlackboard 的私有值 (应失败)
        _anotherLayeredBlackboard.TryGetValue(PrivateValueKey, out int anotherPrivateValue);
        Debug.Log($"Another Blackboard Private Value: {anotherPrivateValue} (Expected 0)");
        Debug.Assert(anotherPrivateValue == 0, "Private value should not be accessible from another blackboard.");

        // 尝试从 AnotherBlackboard 设置一个本地缓存值
        _anotherLayeredBlackboard.SetValue(LocalCacheValueKey, 500);
        _anotherLayeredBlackboard.TryGetValue(LocalCacheValueKey, out int anotherLocalCacheValue);
        Debug.Log($"Another Blackboard Local Cache Value: {anotherLocalCacheValue}");
        Debug.Assert(anotherLocalCacheValue == 500, "Another blackboard local cache should be 500.");

        // 验证 MyBlackboard 和 SharedGlobalBlackboard 中没有这个本地缓存值
        _myLayeredBlackboard.TryGetValue(LocalCacheValueKey, out int myLocalCacheValue);
        Debug.Log($"My Blackboard Local Cache Value: {myLocalCacheValue} (Expected 0)");
        Debug.Assert(myLocalCacheValue == 0, "My blackboard should not see another's local cache.");
        _sharedGlobalBlackboard.TryGetValue(LocalCacheValueKey, out int sharedLocalCacheValue);
        Debug.Log($"Shared Global Blackboard Local Cache Value: {sharedLocalCacheValue} (Expected 0)");
        Debug.Assert(sharedLocalCacheValue == 0, "Shared global blackboard should not see local cache.");

        Debug.Log("--- Shared Access Test Complete ---");
    }

    // 在 Inspector 中直接显示两个黑板的内部存储视图
    [PropertySpace(SpaceBefore = 20)]
    [Title("Internal Storage Views")]
    [ShowInInspector]
    [PropertyOrder(99)] // 放在最后
    private  IReadOnlyDictionary<IBlackboardKey,BlackboardValue> MyPrivateStorageView => (_myLayeredBlackboard?.PrivateStorage?.GetStorageView());

    [ShowInInspector]
    [PropertyOrder(100)] // 放在最后
    private IReadOnlyDictionary<IBlackboardKey,BlackboardValue> SharedGlobalStorageView => (_sharedGlobalBlackboard?.GetStorageView());

    [ShowInInspector]
    [PropertyOrder(101)] // 放在最后
    private IReadOnlyDictionary<IBlackboardKey,BlackboardValue> AnotherPrivateStorageView => (_anotherLayeredBlackboard?.PrivateStorage?.GetStorageView());
}
