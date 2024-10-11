using System.IO;
using UnityEditor;
using UnityEngine;

namespace SerializableSettings.Editor
{
    static class CreateNewSettingsType
    {
        [MenuItem("Assets/Create/Settings/Create new RuntimeProjectSettings type...", priority = 997)]
        public static void CreateRuntimeProjectSettings()
        {
            CreateSettingsFromTemplate("RuntimeProjectSettings.cs");
        }

        [MenuItem("Assets/Create/Settings/Create new EditorProjectSettings type...", priority = 998)]
        public static void CreateEditorProjectSettings()
        {
            CreateSettingsFromTemplate("EditorProjectSettings.cs");
        }

        [MenuItem("Assets/Create/Settings/Create new EditorUserSettings type...", priority = 999)]
        public static void CreateEditorUserSettings()
        {
            CreateSettingsFromTemplate("EditorUserSettings.cs");
        }

        private static void CreateSettingsFromTemplate(string templateFilename)
        {
            var projectPath = Path.GetFullPath(Directory.GetParent(Application.dataPath).ToString());
            var templatePath = Path.GetFullPath(Path.Combine(projectPath, AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(templateFilename)[0])));

            CreateScriptAsset(templatePath, "Settings.cs");
        }

        public static void CreateScriptAsset(string templatePath, string defaultFilename)
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, defaultFilename);
        }

        //[MenuItem("Assets/Create/Settings/Create new EditorProjectSettings type...", priority = 999)]
        //public static void CreateEditorProjectSettings()
        //{
        //    var projectPath = Path.GetFullPath(Directory.GetParent(Application.dataPath).ToString());
        //    var templatePath = Path.GetFullPath(Path.Combine(projectPath, AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("EditorProjectSettings.cs")[0])));

        //    CreateScriptAsset(templatePath, "Settings.cs");
        //}

        //public static void CreateScriptAsset(string templatePath, string defaultFilename)
        //{
        //    ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, defaultFilename);
        //}

    }
}
