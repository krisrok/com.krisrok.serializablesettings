using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using System.Text.RegularExpressions;

namespace SerializableSettings
{
    internal class CommandlineHelper
    {
        internal struct SettingsArg
        {
            public string OriginalArgument;
            public string[] PropertyPathParts;
            public string Value;
        }

        private static List<SettingsArg> _settingsArgs;
        public static IEnumerable<SettingsArg> SettingsArgs
        {
            get
            {
                if(_settingsArgs == null)
                {
                    _settingsArgs = ParseCommandlineArgs();
                }

                return _settingsArgs;
            }
        }

        private static List<SettingsArg> ParseCommandlineArgs()
        {
            var args = Environment.GetCommandLineArgs();
            //var args = new[] { "unity.exe", "-test1", "-settings:TestSettings.Sub.Integer='Hello'", "-s:TestSettings.Boolean=true" };

            var result = new List<SettingsArg>();

            //var settingsArgRegex = new Regex(@"^-{1,2}(setting)?s:(?<propertyPath>.*?)=(?<value>.*)");

            foreach (var settingsArg in args.Where(arg => arg.StartsWith("--settings:") || arg.StartsWith("-settings:") || arg.StartsWith("-s:")))
            {
                var colonIndex = settingsArg.IndexOf(':') + 1;
                if (settingsArg.Length - colonIndex <= 0)
                {
                    Debug.LogWarning($"Invalid settings argument format ({settingsArg}): Missing assignment");
                    continue;
                }

                var assignment = settingsArg.Substring(colonIndex);

                var assignmentParts = assignment.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (assignmentParts.Length < 2)
                {
                    Debug.LogWarning($"Invalid settings argument format ({settingsArg}): Missing '='");
                    continue;
                }

                var propertyPath = assignmentParts[0];
                var value = assignmentParts[1];

                var propertyPathParts = propertyPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (propertyPathParts.Length < 2)
                {
                    Debug.LogWarning($"Invalid settings argument format ({settingsArg}): Property path too short");
                    continue;
                }

                result.Add(new SettingsArg
                {
                    OriginalArgument = settingsArg,
                    PropertyPathParts = propertyPathParts,
                    Value = value
                });
            }

            return result;
        }

    }
}
