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
        internal SettingsAttributeBase( SettingsUsage usage, string displayPath = null)
        {
            Usage = usage;
            DisplayPath = displayPath;
        }

        // The type of settings (how and when they are used).
        internal readonly SettingsUsage Usage;

        // The display name and optional path in the settings dialog.
        public readonly string DisplayPath;

        // The filename used to store the settings. If null, the type's name is used.
        public readonly string Filename;
    }

    public class RuntimeProjectSettingsAttribute : SettingsAttributeBase, IRuntimeSettingsAttribute
    {
        public RuntimeProjectSettingsAttribute(string displayPath = null )
            : base(SettingsUsage.RuntimeProject, displayPath )
        { }

        public OverrideOptions OverrideOptions { get; set; }

        [Obsolete( "Use " + nameof( OverrideOptions ) + " instead." )]
        public bool allowRuntimeFileOverrides { get => this.AllowsFileOverrides(); set => OverrideOptions |= OverrideOptions.File; }

        [Obsolete( "Use " + nameof( OverrideOptions ) + " instead." )]
        public bool allowRuntimeFileWatchers { get => this.AllowsFileWatchers(); set => OverrideOptions |= OverrideOptions.FileWatcher; }

        [Obsolete( "Use " + nameof( OverrideOptions ) + " instead." )]
        public bool allowCommandlineArgsOverrides { get => this.AllowsCommandlineOverrides(); set => OverrideOptions |= OverrideOptions.Commandline; }
    }

    public class EditorProjectSettingsAttribute : SettingsAttributeBase
    {
        public EditorProjectSettingsAttribute( string displayPath = null )
            : base( SettingsUsage.EditorProject, displayPath )
        { }
    }

    public class EditorUserSettingsAttribute : SettingsAttributeBase
    {
        public EditorUserSettingsAttribute( string displayPath = null )
            : base( SettingsUsage.EditorUser, displayPath )
        { }
    }

    public interface IRuntimeSettingsAttribute
    {
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete( "Use " + nameof( OverrideOptions ) + " instead." )]
        bool allowRuntimeFileOverrides { get; set; }

        [Obsolete( "Use " + nameof( OverrideOptions ) + " instead." )]
        bool allowRuntimeFileWatchers { get; set; }

        [Obsolete( "Use " + nameof( OverrideOptions ) + " instead." )]
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
        public static bool AllowsFileOverrides( this IRuntimeSettingsAttribute attribute ) => ( attribute.OverrideOptions & OverrideOptions.File ) != 0;
        public static bool AllowsFileWatchers( this IRuntimeSettingsAttribute attribute ) => ( attribute.OverrideOptions & OverrideOptions.FileWatcher ) != 0;
        public static bool AllowsCommandlineOverrides( this IRuntimeSettingsAttribute attribute ) => ( attribute.OverrideOptions & OverrideOptions.Commandline ) != 0;
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
        /// See <see cref="FileWatcher"/> and <see cref="Commandline"/>
        /// </summary>
        All = FileWatcher | Commandline
    }
}
