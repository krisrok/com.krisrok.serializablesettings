using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SerializableSettings
{
    public abstract partial class SerializableSettings<T> : Settings<T>, ISerializableSettings, IOverridableSettings
        where T : SerializableSettings<T>
    {
        private static List<FileSystemWatcher> _originFileWatchers;

        internal static void LoadInitialRuntimeFileOverrides(ref T runtimeInstance)
        {
            LoadOverridesFromAllFiles(ref runtimeInstance, fromWatcher: false);

            if (runtimeInstance == null)
                return;

            if ((Attribute as IRuntimeSettingsAttribute).OverrideOptions.HasFlag(OverrideOptions.FileWatcher))
                SetupOriginFileWatchers(runtimeInstance);
        }

        #region filewatchers
        private static void SetupOriginFileWatchers(IOverridableSettings overridableSettings)
        {
            foreach (var fileOrigins in overridableSettings.OverrideOrigins.OfType<FileOverrideOrgin>())
            {
                var fsw = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(fileOrigins.FilePath)), Path.GetFileName(fileOrigins.FilePath));
                fsw.NotifyFilter = NotifyFilters.LastWrite;
                fsw.Changed += OverrideOriginFileChanged;
                fsw.EnableRaisingEvents = true;

                if (_originFileWatchers == null)
                    _originFileWatchers = new List<FileSystemWatcher>();
                _originFileWatchers.Add(fsw);
            }
        }

        private static void ClearFileWatchers()
        {
            if (_originFileWatchers == null)
                return;

            foreach (var fsw in _originFileWatchers)
            {
                fsw.EnableRaisingEvents = false;
                fsw.Changed -= OverrideOriginFileChanged;
            }

            _originFileWatchers = null;
        }

        private static void OverrideOriginFileChanged(object sender, FileSystemEventArgs e)
        {
            PostToMainThread(() =>
            {
                var changed = LoadOverridesFromAllFiles(ref _instance, fromWatcher: true);

                if (changed)
                {
                    var overridesString = UpdateRuntimeInstanceName(_instance);

                    _instance._changed?.Invoke();

                    Debug.Log($"Updated {typeof(T).Name} runtime instance {overridesString}");
                }
            });
        }
        #endregion

        private static bool LoadOverridesFromAllFiles(ref T runtimeInstance, bool fromWatcher)
        {
            var changed = false;

            var mainJsonFilename = "Settings.json";
            changed |= LoadOverridesFromFile(ref runtimeInstance, mainJsonFilename, fromWatcher, jsonPath: typeof(T).Name);

            var jsonFilename = Filename + ".json";
            changed |= LoadOverridesFromFile(ref runtimeInstance, jsonFilename, fromWatcher);

            foreach(var additionalJsonFile in CommandlineHelper.SettingsFiles)
            {
                changed |= LoadOverridesFromFile(ref runtimeInstance, additionalJsonFile, fromWatcher, jsonPath: typeof(T).Name);
            }

            return changed;
        }

        private static bool LoadOverridesFromFile(ref T runtimeInstance, string jsonFilePath, bool fromWatcher, string jsonPath = null)
        {
            try
            {
                if (Path.IsPathRooted(jsonFilePath) == false)
                    jsonFilePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", jsonFilePath));

                if (File.Exists(jsonFilePath))
                {
                    using var streamReader = File.OpenText(jsonFilePath);

                    if (PopulateFromReader(ref runtimeInstance, streamReader, jsonPath, throwWhenPathNotFound: false))
                    {
                        AddFileOverrideOrigin(runtimeInstance, jsonFilePath, fromWatcher);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading overrides from {jsonFilePath} for {typeof(T).Name}\n{ex}");
            }

            return false;
        }

        private static void AddFileOverrideOrigin(T instance, string filePath, bool fromWatcher)
        {
            if (instance._overrideOrigins == null)
            {
                instance._overrideOrigins = new List<IOverrideOrigin>();
            }

            if (fromWatcher)
            {
                instance._overrideOrigins.Add(new FileWatcherOverrideOrgin(filePath));
            }
            else
            {
                instance._overrideOrigins.Add(new FileOverrideOrgin(filePath));
            }
        }
    }
}
