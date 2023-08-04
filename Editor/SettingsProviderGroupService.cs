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
        private class ScannedAssemblyInfo
        {
            public Assembly Assembly;
            public ScannedTypeInfo[] Types;
        }

        private class ScannedTypeInfo
        {
            public Type Type;
            public Attribute Attribute;
            public PropertyInfo InstanceProp;
            public PropertyInfo DisplayPathProp;

            public ScannedTypeInfo(Type type)
            {
                Type = type;
                Attribute = type.GetCustomAttribute<SettingsAttributeBase>(inherit: true);
                InstanceProp = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                DisplayPathProp = type.GetProperty("DisplayPath", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            }
        }

        private static ScannedAssemblyInfo[] _assemblyInfos;

        private static ScannedAssemblyInfo[] GetAssemblyInfos()
        {
            if (_assemblyInfos != null)
                return _assemblyInfos;

#if SETTINGSPROVIDERGROUPSERVICE_DEBUG
            VerboseLog($"{nameof(SettingsProviderGroupService)}.{nameof(GetAssemblyInfos)}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var assemblyInfos = PerformFullScan();

            _assemblyInfos = assemblyInfos.ToArray();

#if SETTINGSPROVIDERGROUPSERVICE_DEBUG
            VerboseLog($"{nameof(SettingsProviderGroupService)}.{nameof(GetAssemblyInfos)} ran in {stopwatch.Elapsed.TotalSeconds}s");
#endif

            return _assemblyInfos;
        }

        private static Assembly[] GetReferencingAssemblies()
        {
            var attributeAssemblyName = typeof(SettingsAttributeBase).Assembly.FullName;

            // todo: most likely, we can filter out lots of those
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.IsDynamic == false)
                .Where(a => a.GetReferencedAssemblies().Any(ra => ra.FullName == attributeAssemblyName))
                .ToArray();
        }

        private static ScannedAssemblyInfo[] PerformFullScan()
        {
            VerboseLog("Performing a full scan");

            var referencingAssemblies = GetReferencingAssemblies();

            var scannedAssemblyInfos = referencingAssemblies
                .Select(a => ScanAssembly(a))
                .ToArray();

            return scannedAssemblyInfos;
        }

        private static ScannedAssemblyInfo ScanAssembly(Assembly assembly)
        {
            VerboseLog($"Scanning {assembly.FullName}");

            var typeAttributeTuples = assembly.DefinedTypes
                .Select(t => (Type: t, Attribute: t.GetCustomAttribute<SettingsAttributeBase>(inherit: true)))
                .Where(ta => ta.Attribute != null);

            var types = new List<ScannedTypeInfo>();

            foreach (var ta in typeAttributeTuples)
            {
                if (ta.Type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any(mi => mi.GetCustomAttribute<SettingsProviderAttribute>() != null))
                {
                    VerboseLog($"Skipping type (has SettingsProvider): {ta.Type}");
                    continue;
                }

                if (typeof(ISettingsInternals).IsAssignableFrom(ta.Type) == false)
                {
                    Debug.LogError($"{ta.Type} is decorated with {ta.Attribute.GetType()} but does not inherit from Settings<>!\nPlease remove the attribute or fix the inheritance to e.g. Settings<{ta.Type}>.");
                    continue;
                }

                VerboseLog($"Adding type: {ta.Type}");

                types.Add(new ScannedTypeInfo(ta.Type));
            }

            var result = new ScannedAssemblyInfo
            {
                Assembly = assembly,
                Types = types.ToArray()
            };

            return result;
        }

        [SettingsProviderGroup]
        public static SettingsProvider[] CreateProviders()
        {
            var assemblyInfos = GetAssemblyInfos();

            var result = new List<SettingsProvider>();

            foreach (var at in assemblyInfos.SelectMany(ai => ai.Types))
            {
                result.Add(new ScriptableObjectSettingsProvider(() => (ScriptableObject)at.InstanceProp.GetValue(null),
                    at.Attribute is EditorUserSettingsAttribute ?
                    SettingsScope.User : SettingsScope.Project,
                    (string)at.DisplayPathProp.GetValue(null)));
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
