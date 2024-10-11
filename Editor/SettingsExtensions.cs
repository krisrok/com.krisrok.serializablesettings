// Copyright 2021 by Hextant Studios. https://HextantStudios.com
// This work is licensed under CC BY 4.0. http://creativecommons.org/licenses/by/4.0/
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

namespace SerializableSettings.Editor
{
    public static class SettingsExtensions
    {
        /// <summary></summary>
        /// <remarks>Settings classes are now collected automatically, no need to use this anymore.</remarks>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [Obsolete("Settings classes are now collected automatically. If you still want to use your own SettingsProvider consider using the overload with type inference instead: SettingsExtensions.GetSettingsProvider(() => instance)")]
        public static SettingsProvider GetSettingsProvider<T>() where T : Settings<T>
        {
            var instanceProp = typeof(Settings<T>).GetProperty(nameof(Settings<T>.Instance), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return new ScriptableObjectSettingsProvider(() => (ScriptableObject)instanceProp.GetValue(null),
                Settings<T>.Attribute is EditorUserSettingsAttribute ?
                SettingsScope.User : SettingsScope.Project,
                Settings<T>.DisplayPath);
        }

        /// <summary>
        /// Creates a SettingsProvider for the given settings instance.
        /// Easy to copy-paste usage: <code>SettingsExtensions.GetSettingsProvider(() => instance)</code>
        /// </summary>
        /// <remarks>Settings classes are now collected automatically, no need to use this anymore.</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="instanceGetter"></param>
        /// <returns></returns>
        public static SettingsProvider GetSettingsProvider<T>(Func<T> instanceGetter)
            where T : Settings<T>
        {
            return new ScriptableObjectSettingsProvider(instanceGetter,
                Settings<T>.Attribute is EditorUserSettingsAttribute ?
                SettingsScope.User : SettingsScope.Project,
                Settings<T>.DisplayPath);
        }
    }
}
