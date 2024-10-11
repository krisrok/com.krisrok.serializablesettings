using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Assembly = System.Reflection.Assembly;

namespace SerializableSettings.Editor
{
    /// <summary>
    /// Automatically scans the domain for settings classes,
    /// creates <see cref="SettingsProvider"/>s and registers them in bulk.
    /// </summary>
    public static class SettingsProviderGroupService
    {
        [SettingsProviderGroup]
        public static SettingsProvider[] CreateProviders()
        {
            var result = new List<SettingsProvider>();

            var types = TypeCache.GetTypesWithAttribute<SettingsAttributeBase>();
            foreach (var type in types)
            {
                if (type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any(mi => mi.GetCustomAttribute<SettingsProviderAttribute>() != null))
                {
                    VerboseLog($"Skipping type (has SettingsProvider): {type}");
                    continue;
                }

                var attribute = type.GetCustomAttribute<SettingsAttributeBase>(inherit: true);
                if (typeof(ISettingsInternals).IsAssignableFrom(type) == false)
                {
                    Debug.LogError($"{type} is decorated with {attribute.GetType()} but does not inherit from Settings<>!\nPlease remove the attribute or fix the inheritance to e.g. Settings<{type}>.");
                    continue;
                }

                var instanceProp = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                var displayPathProp = type.GetProperty("DisplayPath", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                
                VerboseLog($"Adding type: {type}");

                result.Add(new ScriptableObjectSettingsProvider(() => (ScriptableObject)instanceProp.GetValue(null),
                    attribute is EditorUserSettingsAttribute ?
                    SettingsScope.User : SettingsScope.Project,
                    (string)displayPathProp.GetValue(null)));
            }

            return result.ToArray();
        }


        private static void VerboseLog(string msg)
        {
#if SETTINGSPROVIDERGROUPSERVICE_DEBUG
            Debug.Log($"{nameof(SettingsProviderGroupService)}: {msg}");
#endif
        }
    }

}
