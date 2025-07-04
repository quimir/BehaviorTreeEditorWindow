using System;
using System.Threading;
using UnityEngine;

namespace ExTools.Singleton
{
    /// <summary>
    /// A thread-safe generic singleton class that uses lazy initialization to ensure that only one instance
    /// of the given type <typeparamref name="T"/> is created and managed throughout the application's lifecycle.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the singleton instance. It must be a reference type and have a parameterless constructor.
    /// </typeparam>
    public class SingletonWithLazy<T> where T : class
    {
        private static readonly Lazy<T> instance_lazy_ = new Lazy<T>(CreateAndInitializeInstance,
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of the type <typeparamref name="T"/>.
        /// This property ensures that only one instance of <typeparamref name="T"/> is created
        /// and provides a global point of access to it.
        /// The instance is created using lazy initialization with thread safety.
        /// </summary>
        public static T Instance => instance_lazy_.Value;

        /// <summary>
        /// Creates and initializes an instance of the singleton type <typeparamref name="T"/>.
        /// This method is responsible for creating a new instance of the type and invoking any necessary
        /// initialization logic defined in derived classes through the <see cref="InitializationInternal"/> method.
        /// </summary>
        /// <returns>
        /// A newly created and initialized instance of type <typeparamref name="T"/>.
        /// </returns>
        private static T CreateAndInitializeInstance()
        {
            T instance=Activator.CreateInstance<T>();
            (instance as SingletonWithLazy<T>)?.InitializationInternal();
            return instance;
        }

        /// <summary>
        /// Provides an opportunity for subclasses to perform custom initialization logic during the singleton instance creation.
        /// This method is called during the lazy initialization of the singleton instance and is intended to be overridden
        /// by derived classes to set up their specific state or dependencies.
        /// </summary>
        /// <remarks>
        /// This method is protected and virtual, allowing derived classes to override its behavior.
        /// By default, it is a no-op (does nothing) to ensure that not all subclasses are required to implement their own logic.
        /// </remarks>
        protected virtual void InitializationInternal()
        {
        }

#if UNITY_EDITOR
        static SingletonWithLazy()
        {
            // 注册域重载事件来重置单例
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ResetInstance;
        }

        private static void ResetInstance()
        {
            if (instance_lazy_.IsValueCreated)
            {
                // 如果实例实现了 IDisposable，先释放资源
                if (instance_lazy_.Value is IDisposable disposable)
                {
                    try
                    {
                        disposable?.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error disposing singleton instance: {e.Message} Type: {typeof(T).Name}");
                    }
                }
            }
        }
#endif
    }
}
