using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;

namespace Test.ScriptableSingletonTest
{
    [FilePath("UserSettings/ComplexDataManager.asset",FilePathAttribute.Location.ProjectFolder)]
    public class ComplexDataManager : ScriptableSingleton<ComplexDataManager>,ISerializationCallbackReceiver
    {
        // List<T> 可以被Unity直接序列化 (只要T是可序列化的)
        [SerializeField]
        public List<MyCustomData> myDataList = new List<MyCustomData>();
        
        // Dictionary 不能被Unity直接序列化，所以我们不加 [SerializeField]
        public Dictionary<string, MyCustomData> myDataDictionary = new Dictionary<string, MyCustomData>();

        // --- 字典序列化的标准解决方案 ---
        // 关键点2: 使用两个List作为字典的“代理”来进行序列化
        [SerializeField]
        private List<string> _dictionaryKeys = new List<string>();
        [SerializeField]
        private List<MyCustomData> _dictionaryValues = new List<MyCustomData>();
        // ---

        // 在Unity准备序列化（保存）这个对象之前调用
        public void OnBeforeSerialize()
        {
            // 将字典数据存入可序列化的List中
            _dictionaryKeys.Clear();
            _dictionaryValues.Clear();

            foreach (var kvp in myDataDictionary)
            {
                _dictionaryKeys.Add(kvp.Key);
                _dictionaryValues.Add(kvp.Value);
            }
        }

        // 在Unity完成反序列化（加载）这个对象之后调用
        public void OnAfterDeserialize()
        {
            // 从List重建字典
            myDataDictionary = new Dictionary<string, MyCustomData>();

            for (int i = 0; i < _dictionaryKeys.Count; i++)
            {
                if (i < _dictionaryValues.Count) // 确保数据完整性
                {
                    myDataDictionary.Add(_dictionaryKeys[i], _dictionaryValues[i]);
                }
            }
        }

        // 提供一个公共方法来保存更改
        public void SaveData()
        {
            Save(true);
        }
    }
}
