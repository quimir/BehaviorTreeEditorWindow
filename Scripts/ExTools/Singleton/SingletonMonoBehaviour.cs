using System;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using UnityEngine;

namespace ExTools.Singleton
{
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour, IDisposable where T : SingletonMonoBehaviour<T>
    {
        private static T instance_;
        private static readonly object locker_ = new();
        private bool is_disposed_;

        public static T Instance
        {
            get
            {
                if (application_quitting_)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                        new LogSpaceNode("MonoBehaviour").AddChild("SingletonMonoBehaviour"), new LogEntry(
                            LogLevel.kWarning,
                            $"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null."),
                        true);
                    return null;
                }

                lock (locker_)
                {
                    if (instance_ == null)
                    {
                        // 尝试寻找场景中是否存在实例
                        instance_ = FindObjectOfType<T>();

                        if (instance_ == null)
                        {
                            // 如果不存在，动态建立一个新的GameObject并附加元件
                            var singleton_object = new GameObject();
                            instance_ = singleton_object.AddComponent<T>();
                            singleton_object.name = $"Singleton<{typeof(T).Name}>";
                            // 確保這個物件在場景切換時不會被銷毀
                            DontDestroyOnLoad(singleton_object);
                        }
                    }

                    return instance_;
                }
            }
        }

        private static bool application_quitting_ = false;

        private void Awake()
        {
            if (instance_ != null && instance_ != this)
            {
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(
                    new LogSpaceNode("MonoBehaviour").AddChild("SingletonMonoBehaviour"),
                    new LogEntry(LogLevel.kWarning,
                        $"[Singleton] Another instance of '{typeof(T)}' was found. Destroying this duplicate."), true);
                Destroy(gameObject);
                return;
            }
                
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit()
        {
            application_quitting_ = true;
        }

        protected virtual void OnDestroy()
        {
            if (application_quitting_)
            {
                return;
            }

            if (instance_==this)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (is_disposed_)
            {
                return;
            }

            is_disposed_ = true;
        }
    }
}