using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SerializableSettings
{
    public abstract partial class SerializableSettings<T> : Settings<T>, ISerializableSettings, IOverridableSettings
        where T : SerializableSettings<T>
    {
        internal static void LoadRuntimeCommandlineOverrides(ref T runtimeInstance)
        {
            var args = CommandlineHelper.SettingsArgs;

            if (args == null)
                return;

            foreach (var arg in args)
            {
                var propertyPathParts = arg.PropertyPathParts;
                try
                {
                    var propertyPathSettingsName = propertyPathParts[0];
                    if (propertyPathSettingsName != typeof(T).Name)
                        continue;

                    var root = new JObject();
                    var current = root;
                    for (var i = 1; i < propertyPathParts.Length; i++)
                    {
                        if (propertyPathParts.Length - 1 == i)
                        {
                            current.Add(propertyPathParts[i], JValue.CreateString(arg.Value));
                        }
                        else
                        {
                            current.Add(propertyPathParts[i], current = new JObject());
                        }
                    }

                    PopulateFromJToken(ref runtimeInstance, root);

                    AddCommandlineOverrideOrigin(runtimeInstance, arg.OriginalArgument);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading overrides from commandline argument '{arg}' for {typeof(T).Name}\n{ex}");
                }
            }
        }

        private static void AddCommandlineOverrideOrigin(T instance, string argument)
        {
            if (instance._overrideOrigins == null)
            {
                instance._overrideOrigins = new List<IOverrideOrigin>();
            }

            instance._overrideOrigins.Add(new CommandlineOverrideOrgin(argument));
        }
    }
}
