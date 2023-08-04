using System.IO;
using UnityEditor;
using UnityEngine;

namespace SerializableSettings.Editor
{
    static class CreateNewSettingsType
    {
        [MenuItem( "Assets/Create/Settings/Create new RuntimeProjectSettings type...", priority = 999 )]
        public static void CreateRuntimeProjectSettings()
        {
            var projectPath = Path.GetFullPath( Directory.GetParent( Application.dataPath ).ToString() );
            var templatePath = Path.GetFullPath( Path.Combine( projectPath, AssetDatabase.GUIDToAssetPath( AssetDatabase.FindAssets( "RuntimeProjectSettings.cs" )[ 0 ] ) ) );

            CreateScriptAsset( templatePath, "Settings.cs" );
        }

        public static void CreateScriptAsset( string templatePath, string defaultFilename )
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile( templatePath, defaultFilename );
        }

    }
}
