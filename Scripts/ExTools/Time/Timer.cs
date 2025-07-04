using UnityEngine.Events;

namespace Script.Tool.Timer
{
    public struct Timer
    {
        public float Time;

        public int Uuid;

        public UnityAction Action;
        
        private static int _uuid;
        
        public Timer(float time, UnityAction action)
        {
            Time = time;
            Action = action;
            Uuid = _uuid++;
        }
    }
}
