using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Save.Serialization.Core.TypeConverter.Manager
{
    /// <summary>
    /// Manages a collection of type converters, providing methods for adding, removing,
    /// and querying converters, as well as notifying changes in the registry.
    /// </summary>
    /// <typeparam name="TConverter">
    /// The primary type of converters managed by this class.
    /// </typeparam>
    public class BaseTypeConverterManager<TConverter> : ITypeConverterMessage<TConverter>
    {
        /// <summary>
        /// A private collection that stores the primary converters managed by the system.
        /// These converters are responsible for performing type-specific serialization or
        /// deserialization tasks.
        /// </summary>
        private readonly List<TConverter> primary_converters_ = new();

        /// <summary>
        /// A collection of secondary type converters used to handle conversion of types
        /// that are not managed as primary converters. This list ensures extensibility by
        /// supporting additional conversion rules through secondary mechanisms.
        /// </summary>
        private readonly List<ITypeConverter> secondary_converters_ = new();

        /// <summary>
        /// Factory function provided by the specific serializer to create library-specific adapters from ITypeConverter.
        /// </summary>
        private readonly Func<ITypeConverter, Type, ISerializationContext, TConverter> adapter_factory_;

        /// <summary>
        /// Event triggered when a converter is added or removed.
        /// </summary>
        public event Action OnRegistryChanged;

        public BaseTypeConverterManager(Func<ITypeConverter, Type, ISerializationContext, TConverter> adapterFactory)
        {
            adapter_factory_ = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        }

        public virtual void AddConverter(object converter)
        {
            if (converter == null) return;

            var changed = false;
            if (converter is TConverter primary)
            {
                // Use ReferenceEquals for potentially stateless formatters/converters
                if (!primary_converters_.Any(c => ReferenceEquals(c, primary) || c.Equals(primary)))
                {
                    primary_converters_.Add(primary);
                    changed = true;
                }
            }
            else if (converter is ITypeConverter secondary)
            {
                if (!secondary_converters_.Any(c => ReferenceEquals(c, secondary) || c.Equals(secondary)))
                {
                    secondary_converters_.Add(secondary);
                    changed = true; // Adding a secondary converter also requires rebuild
                }
            }
            else
            {
                throw new ArgumentException(
                    $"Converter must be of type {typeof(TConverter).Name} or {nameof(ITypeConverter)}. Got: {converter.GetType().Name}");
            }

            if (changed) NotifyChange();
        }

        /// <summary>
        /// Invokes the OnRegistryChanged event.
        /// </summary>
        protected virtual void NotifyChange()
        {
            OnRegistryChanged?.Invoke();
        }

        public virtual bool RemoveConverter(object converter)
        {
            if (converter == null) return false;

            var changed = false;
            if (converter is TConverter primary)
            {
                // Find based on reference or equality
                var existing =
                    primary_converters_.FirstOrDefault(c => ReferenceEquals(c, primary) || c.Equals(primary));
                if (existing != null)
                {
                    primary_converters_.Remove(existing);
                    changed = true;
                }
            }
            else if (converter is ITypeConverter secondary)
            {
                var existing =
                    secondary_converters_.FirstOrDefault(c => ReferenceEquals(c, secondary) || c.Equals(secondary));
                if (existing != null)
                {
                    secondary_converters_.Remove(existing);
                    changed = true;
                }
            }
            else
            {
                // Optionally log a warning or ignore if type doesn't match
                Debug.LogWarning(
                    $"Attempted to remove an object that is neither {typeof(TConverter).Name} nor " +
                    $"{nameof(ITypeConverter)}: {converter.GetType().Name}");
                return false;
            }

            if (changed) NotifyChange();

            return true;
        }

        public virtual bool ContainsConverter(object converter)
        {
            if (converter == null) return false;

            if (converter is TConverter primary)
                return primary_converters_.Any(c => ReferenceEquals(c, primary) || c.Equals(primary));

            if (converter is ITypeConverter secondary)
                return secondary_converters_.Any(c => ReferenceEquals(c, secondary) || c.Equals(secondary));

            return false;
        }

        public virtual IEnumerable<object> GetAllConverterObjects()
        {
            return primary_converters_.Cast<object>().Concat(secondary_converters_.Cast<object>());
        }

        public IEnumerable<TConverter> GetPrimaryConverters()
        {
            return primary_converters_.AsReadOnly();
        }

        public IEnumerable<ITypeConverter> GetSecondaryConverters()
        {
            return secondary_converters_.AsReadOnly();
        }

        /// <summary>
        /// Retrieves the adapted converters from the secondary converters based on the provided serialization context.
        /// </summary>
        /// <param name="context">The serialization context used to adapt and filter the converters.</param>
        /// <returns>An enumerable collection of distinct adapted converters.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided context is null.</exception>
        public virtual IEnumerable<TConverter> GetAdaptedConverters(ISerializationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var adapters = new List<TConverter>();
            foreach (var type_converter in secondary_converters_)
            {
                if (type_converter.SupportedTypes == null) continue;

                foreach (var supported_type in type_converter.SupportedTypes)
                    try
                    {
                        // Use the factory passed in constructor
                        var adapter = adapter_factory_(type_converter, supported_type, context);
                        if (adapter != null) adapters.Add(adapter);
                    }
                    catch (Exception e)
                    {
                        // Log error during adapter creation for a specific type
                        Debug.LogError($"Error creating adapter for {type_converter.GetType().Name} targeting " +
                                       $"{supported_type.Name}: {e}");
                    }
            }

            // Return distinct adapters. Equality check depends on TConverter's implementation. Using a comparer might
            // be necessary if default equality is not sufficient.
            return adapters.Distinct();
        }
    }
}