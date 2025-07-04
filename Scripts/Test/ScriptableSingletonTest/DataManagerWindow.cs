using UnityEditor;
using UnityEngine;

namespace Test.ScriptableSingletonTest
{
    public class DataManagerWindow : EditorWindow
    {
        [MenuItem("Tools/My Data Manager")]
        public static void ShowWindow()
        {
            GetWindow<DataManagerWindow>("My Data Manager");
        }

        void OnGUI()
        {
            // 获取 ScriptableSingleton 的实例
            ComplexDataManager manager = ComplexDataManager.instance;

            GUILayout.Label("数据管理器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // --- 操作和显示 List ---
            GUILayout.Label("列表数据 (List)", EditorStyles.centeredGreyMiniLabel);
            if (GUILayout.Button("向 List 添加一条新数据"))
            {
                string id = "ListItem_" + manager.myDataList.Count;
                manager.myDataList.Add(new MyCustomData(id));
                manager.SaveData(); // 保存更改
            }

            foreach (var item in manager.myDataList)
            {
                EditorGUILayout.LabelField(" - " + item.description, "Enabled: " + item.isEnabled);
            }

            EditorGUILayout.Space();

            // --- 操作和显示 Dictionary ---
            GUILayout.Label("字典数据 (Dictionary)", EditorStyles.centeredGreyMiniLabel);
            if (GUILayout.Button("向 Dictionary 添加一条新数据"))
            {
                string key = "DictKey_" + manager.myDataDictionary.Count;
                manager.myDataDictionary.Add(key, new MyCustomData("Data for " + key));
                manager.SaveData(); // 保存更改
            }

            foreach (var kvp in manager.myDataDictionary)
            {
                EditorGUILayout.LabelField(" - Key: " + kvp.Key, "Value Desc: " + kvp.Value.description);
            }

            EditorGUILayout.Space();

            // --- 清除数据 ---
            if (GUILayout.Button("清除所有数据并保存"))
            {
                manager.myDataList.Clear();
                manager.myDataDictionary.Clear();
                manager.SaveData();
            }
        }
    }
}