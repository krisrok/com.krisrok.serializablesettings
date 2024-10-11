namespace SerializableSettings.Editor
{
#if UNITY_EDITOR
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
#endif
}
