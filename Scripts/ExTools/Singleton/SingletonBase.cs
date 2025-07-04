namespace ExTools.Singleton
{
    /// <summary>
    /// A generic base class that implements the Singleton design pattern.
    /// Guarantees a single, globally accessible instance of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the derived singleton class. Must have a parameterless constructor.
    /// </typeparam>
    public abstract class SingletonBase<T> where T : new()
    {
        private static T instance_;

        private static readonly object locker_ = new object();

        public static T Instance
        {
            get
            {
                if (instance_==null)
                {
                    lock (locker_)
                    {
                        if (instance_==null)
                        {
                            instance_ = new T();
                        }
                    }
                }

                return instance_;
            }
        }

        protected SingletonBase()
        {
            Initialization();
        }

        protected abstract void Initialization();
    }
}
