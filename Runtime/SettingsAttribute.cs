// Derivative of "https://github.com/hextantstudios/com.hextantstudios.utilities" 
//  Original Copyright 2021 by Hextant Studios. https://HextantStudios.com
//  Used under CC BY 4.0. http://creativecommons.org/licenses/by/4.0/
using System;

namespace SerializableSettings
{
    /// <summary>
    /// Abstract base class for Settings attributes.
    /// Please use its derivates <see cref="RuntimeProjectSettingsAttribute"/>, <see cref="EditorProjectSettingsAttribute"/> and <see cref="EditorUserSettingsAttribute"/> instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public abstract class SettingsAttributeBase : Attribute
    {
        internal SettingsAttributeBase(SettingsUsage usage, string displayPath = null)
        {
            Usage = usage;
            DisplayPath = displayPath;
        }

        /// <summary>
        /// Internal settings usage switch.
        /// </summary>
        internal readonly SettingsUsage Usage;

        /// <summary>
        /// Path to the setting in the Project Settings window. If null, the type's name is used.
        /// </summary>
        public readonly string DisplayPath;

        /// <summary>
        /// The filename used to store the settings. If null, the type's name is used.
        /// </summary>
        public readonly string Filename;
    }

    public class RuntimeProjectSettingsAttribute : SettingsAttributeBase, IRuntimeSettingsAttribute
    {
        /// <summary>
        /// Declares an <see cref="SerializableSettings{T}"/> class to be used as runtime settings
        /// which can be configured using the Project Settings window.
        /// </summary>
        /// <param name="displayPath">Path to the setting in the Project Settings window.</param>
        /// <param name="overrideOptions">Defines which override sources are permitted during runtime.</param>
        public RuntimeProjectSettingsAttribute(string displayPath, OverrideOptions overrideOptions = OverrideOptions.None)
            : base(SettingsUsage.RuntimeProject, displayPath)
        {
            OverrideOptions = overrideOptions;
        }

        /// <summary>
        /// Defines which override sources are permitted during runtime.
        /// </summary>
        public OverrideOptions OverrideOptions { get; set; }

        [Obsolete("Use " + nameof(OverrideOptions) + " instead.")]
        public bool allowRuntimeFileOverrides { get => this.AllowsFileOverrides(); set => OverrideOptions |= OverrideOptions.File; }

        [Obsolete("Use " + nameof(OverrideOptions) + " instead.")]
        public bool allowRuntimeFileWatchers { get => this.AllowsFileWatchers(); set => OverrideOptions |= OverrideOptions.FileWatcher; }

        [Obsolete("Use " + nameof(OverrideOptions) + " instead.")]
        public bool allowCommandlineArgsOverrides { get => this.AllowsCommandlineOverrides(); set => OverrideOptions |= OverrideOptions.Commandline; }
    }

    public class EditorProjectSettingsAttribute : SettingsAttributeBase
    {
        /// <summary>
        /// Declares an <see cref="Settings{T}"/> class to be used as editor-only settings
        /// which can be configured using the 'Project Settings' window.
        /// </summary>
        /// <param name="displayPath">Path to the setting in the Project Settings window. If omited, it will be generated using the class name.</param>
        public EditorProjectSettingsAttribute(string displayPath = null)
            : base(SettingsUsage.EditorProject, displayPath)
        { }
    }

    public class EditorUserSettingsAttribute : SettingsAttributeBase
    {
        /// <summary>
        /// Declares an <see cref="Settings{T}"/> class to be used as editor-only settings
        /// which can be configured using the 'Preferences' window.
        /// </summary>
        /// <param name="displayPath">Path to the setting in the Project Settings window. If omited, it will be generated using the class name.</param>
        public EditorUserSettingsAttribute(string displayPath = null)
            : base(SettingsUsage.EditorUser, displayPath)
        { }
    }

    public interface IRuntimeSettingsAttribute
    {
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("Use " + nameof(OverrideOptions) + " instead.")]
        bool allowRuntimeFileOverrides { get; set; }

        [Obsolete("Use " + nameof(OverrideOptions) + " instead.")]
        bool allowRuntimeFileWatchers { get; set; }

        [Obsolete("Use " + nameof(OverrideOptions) + " instead.")]
        bool allowCommandlineArgsOverrides { get; set; }
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Used to configure sources this settings object reads overrides from at runtime.
        /// Works in conjunction with <see cref="SerializableSettings{T}"/>
        /// </summary>
        OverrideOptions OverrideOptions { get; set; }
    }

    public static class RuntimeSettingsAttributeExtensions
    {
        public static bool AllowsFileOverrides(this IRuntimeSettingsAttribute attribute) => (attribute.OverrideOptions & OverrideOptions.File) != 0;
        public static bool AllowsFileWatchers(this IRuntimeSettingsAttribute attribute) => (attribute.OverrideOptions & OverrideOptions.FileWatcher) != 0;
        public static bool AllowsCommandlineOverrides(this IRuntimeSettingsAttribute attribute) => (attribute.OverrideOptions & OverrideOptions.Commandline) != 0;
    }

    [Flags]
    public enum OverrideOptions
    {
        /// <summary>
        /// No overrides will be applied.
        /// </summary>
        None = 0,
        /// <summary>
        /// Overrides will be applied from Settings.json and [ClassName].json at runtime.
        /// </summary>
        File = 1,
        /// <summary>
        /// Overrides will be applied from Settings.json and [ClassName].json at runtime.
        /// If any relevant json file exists, it will also be watched for changes during runtime.
        /// </summary>
        FileWatcher = File | 2,
        /// <summary>
        /// Overrides will be applied from commandline arguments.
        /// </summary>
        Commandline = 1 << 2,
        /// <summary>
        /// Overrides will be applied from in-memory json strings.
        /// See <see cref="InMemoryOverrides"/>.
        /// </summary>
        InMemory = 1 << 3,
        /// <summary>
        /// <inheritdoc cref="InMemory"/>
        /// Deferred changes will be applied during runtime, too.
        /// </summary>
        InMemoryDeferred = InMemory | 1 << 4,
        /// <summary>
        /// See <see cref="FileWatcher"/>, <see cref="Commandline"/> and <see cref="InMemory"/>.
        /// </summary>
        All = FileWatcher | Commandline | InMemoryDeferred
    }
}
