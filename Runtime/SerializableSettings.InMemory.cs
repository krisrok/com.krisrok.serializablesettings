using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SerializableSettings
{
    public abstract partial class SerializableSettings<T> : Settings<T>, ISerializableSettings, IOverridableSettings
        where T : SerializableSettings<T>
    {
        internal static void LoadInitialInMemoryOverrides(ref T runtimeInstance)
        {
            if (InMemoryOverrides.UntypedOverrides == null)
                return;

            foreach (var untypedOverrides in InMemoryOverrides.UntypedOverrides)
            {
                LoadInMemoryOverrides(ref runtimeInstance, untypedOverrides);
            }

            // Subscribe to deferred changes if needed
            if ((_instance as IOverridableSettings).OverrideOptions.HasFlag(OverrideOptions.InMemoryDeferred))
            {
                InMemoryOverrides.UntypedOverridesAdded -= InMemoryOverrides_UntypedOverridesAdded;
                InMemoryOverrides.UntypedOverridesAdded += InMemoryOverrides_UntypedOverridesAdded;
            }
        }

        private static bool LoadInMemoryOverrides(ref T runtimeInstance, InMemoryOverrides.Item untypedOverrides)
        {
            try
            {
                using var textReader = new StringReader(untypedOverrides.Json);
                if (PopulateFromReader(ref runtimeInstance, textReader, typeof(T).Name, throwWhenPathNotFound: false))
                {
                    AddInMemoryOverrideOrigin(runtimeInstance, untypedOverrides.Description);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading overrides from in-memory json ({untypedOverrides.Description}):\n{untypedOverrides.Json}\n{ex}");
            }

            return false;
        }

        private static void AddInMemoryOverrideOrigin(T instance, string description)
        {
            if (instance._overrideOrigins == null)
            {
                instance._overrideOrigins = new List<IOverrideOrigin>();
            }

            instance._overrideOrigins.Add(new InMemoryOverrideOrigin(description));
        }

        private static void InMemoryOverrides_UntypedOverridesAdded(InMemoryOverrides.Item item)
        {
            PostToMainThread(() =>
            {
                var changed = LoadInMemoryOverrides(ref _instance, item);

                if (changed)
                {
                    var overridesString = SetRuntimeInstanceName(_instance);

                    _instance._changed?.Invoke();

                    Debug.Log($"Updated {typeof(T).Name} runtime instance {overridesString}");
                }
            });
        }

        private static void ClearInMemoryEventListener()
        {
            InMemoryOverrides.UntypedOverridesAdded -= InMemoryOverrides_UntypedOverridesAdded;
        }
    }
}
