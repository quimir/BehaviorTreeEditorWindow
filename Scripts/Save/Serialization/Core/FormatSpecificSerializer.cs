using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using ExTools;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Core.TypeConverter.Manager;
using Script.Save.Serialization;

namespace Save.Serialization.Core
{
    /// <summary>
    /// Represents an abstract base class for format-specific serializers that handle serialization and deserialization.
    /// </summary>
    /// <typeparam name="TConverter">The type of the converter used for type transformations during serialization.</typeparam>
    /// <typeparam name="TOptions">The type of options used to configure the serializer.</typeparam>
    public abstract class FormatSpecificSerializer<TConverter, TOptions> : SerializerBase
    {
        /// <summary>
        /// Represents configuration or customization settings used by the serializer when processing data.
        /// This variable typically holds options specific to the serialization format or framework being utilized.
        /// </summary>
        protected TOptions Options;

        /// <summary>
        /// Represents the serialization context used by the serializer to manage state, settings, and type conversion
        /// during serialization or deserialization processes. This variable is typically initialized internally based
        /// on the specific serialization implementation.
        /// </summary>
        protected ISerializationContext Context;

        /// <summary>
        /// Manages the collection of type converters and provides functionality
        /// to retrieve and adapt converters for serialization or deserialization processes.
        /// </summary>
        protected readonly BaseTypeConverterManager<TConverter> ConvertersManager;

        /// <summary>
        /// A protected variable that holds shared serialization settings.
        /// These settings are utilized across different serializer implementations
        /// to ensure consistent behavior and configuration during serialization processes.
        /// </summary>
        protected SerializationSettings shared_settings_;

        protected FormatSpecificSerializer(SerializationSettings shared_settings = null)
        {
            shared_settings_ = shared_settings ?? new SerializationSettings
            {
                PreserveReferences = true
            };

            InitializeContextInternal();

            ConvertersManager = new BaseTypeConverterManager<TConverter>(CreateSpecificAdapter);

            ConvertersManager.OnRegistryChanged += OnConvertersManagedChanged;
        }

        /// <summary>
        /// Creates a format-specific adapter for serialization and deserialization processes.
        /// </summary>
        /// <param name="typeConverter">The type converter responsible for converting objects during serialization.</param>
        /// <param name="supportedType">The type that the adapter supports for serialization and deserialization.</param>
        /// <param name="context">The serialization context providing additional information and configuration.</param>
        /// <returns>A format-specific adapter used for handling serialization and deserialization of the specified type.</returns>
        protected abstract TConverter CreateSpecificAdapter(ITypeConverter typeConverter, Type supportedType,
            ISerializationContext context);

        /// <summary>
        /// Handles changes to the converter registry managed by the serializer.
        /// </summary>
        /// <remarks>
        /// This method is invoked whenever the converter registry is updated.
        /// It triggers a rebuild of the library-specific options to reflect the updated converters.
        /// </remarks>
        private void OnConvertersManagedChanged()
        {
            // When converters change, rebuild the library-specific options
            RebuildOptions();
        }

        /// <summary>
        /// Initializes the serialization context with format-specific configurations and settings specific to the
        /// derived serializer.This method is invoked during the serializer setup process and is expected to define all
        /// necessary initialization logic required for the serializer's functioning, including context configuration,
        /// dependency registration, or preconditions validation.
        /// </summary>
        protected abstract void InitializeContextInternal();

        public override ITypeConverterMessage GetTypeConverterManager()
        {
            return ConvertersManager;
        }

        protected override bool ValidateConverterType(object converter)
        {
            return converter is TConverter or ITypeConverter;
        }
        
        /// <summary>
        /// Applies shared serialization settings to the format-specific options used during serialization and deserialization.
        /// </summary>
        /// <remarks>
        /// This method is responsible for configuring the format-specific options based on shared settings.
        /// It ensures consistency across different serializer instances by applying common configuration values.
        /// </remarks>
        protected abstract void ApplySharedSettingsToOptions();

        public virtual void UpdateSettings(SerializationSettings settings)
        {
            shared_settings_ = settings ?? new SerializationSettings();
            Context?.UpdateSettings(shared_settings_); // Update context if it holds settings

            // Applying settings might change how options are built, so rebuild.
            RebuildOptions();
        }

        /// <summary>
        /// Rebuilds the configuration options for the serializer specific to its format and type.
        /// </summary>
        /// <remarks>
        /// This method is responsible for reconstructing serializer options to align with updated settings
        /// or context changes. Implementations must ensure that the options reflect the current state
        /// of shared settings, context, and other serializer-specific requirements.
        /// </remarks>
        protected abstract void RebuildOptions();
    }

    /// <summary>
    /// Provides an abstract base class for asynchronous serializers, supporting serialization and deserialization
    /// operations that can run asynchronously.
    /// </summary>
    /// <typeparam name="TConverter">The type of the converter used for processing objects during serialization.</typeparam>
    /// <typeparam name="TOptions">The type of configuration options utilized by the serializer.</typeparam>
    public abstract class AsyncSerializerBase<TConverter, TOptions> : FormatSpecificSerializer<TConverter, TOptions>,
        IAsyncSerializer
    {
        protected AsyncSerializerBase(SerializationSettings shared_settings = null) : base(shared_settings)
        {
        }

        public virtual UniTask<object> SerializeAsync<T>(T obj, CancellationToken cancellation_token = default)
        {
            return UniTask.RunOnThreadPool(() => Serialize(obj), cancellationToken: cancellation_token);
        }

        public virtual UniTask<T> DeserializeAsync<T>(object data, CancellationToken cancellation_token = default)
        {
            return UniTask.RunOnThreadPool(() => DeserializeAsync<T>(data), cancellationToken: cancellation_token);
        }
    }
}