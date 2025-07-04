using UnityEngine;

namespace Test.ScriptableSingletonTest
{
    [System.Serializable]
    public class MyCustomData
    {
        public string description;
        public Vector3 position;
        public bool isEnabled;

        public MyCustomData(string desc)
        {
            this.description = desc;
            this.position = new Vector3(Random.Range(-10f, 10f), 0, 0);
            this.isEnabled = true;
        }
    }
}
