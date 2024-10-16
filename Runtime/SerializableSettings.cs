﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace SerializableSettings
{
    public interface ISerializableSettings
    {
        void SaveAsJsonFile(string filename = null);
        void LoadFromJsonFile(string filename = null);
    }

    internal interface IOverridableSettings
    {
        internal bool IsRuntimeInstance { get; }
        internal List<IOverrideOrigin> OverrideOrigins { get; }
        internal OverrideOptions OverrideOptions { get; }
    }

    public delegate void SettingsChangedDelegate();

    public abstract partial class SerializableSettings<T> : Settings<T>, ISerializableSettings, IOverridableSettings
        where T : SerializableSettings<T>
    {
        private static SynchronizationContext _syncContext;
        private static Queue<Action> _syncContextQueue = new Queue<Action>();

        private static JsonSerializer _jsonSerializer;
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = UnityEngineObjectContractResolver.Instance,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        [NonSerialized]
        private List<IOverrideOrigin> _overrideOrigins;
        [NonSerialized]
        private bool _isRuntimeInstance;

        List<IOverrideOrigin> IOverridableSettings.OverrideOrigins => _overrideOrigins;
        OverrideOptions IOverridableSettings.OverrideOptions => (Attribute as IRuntimeSettingsAttribute)?.OverrideOptions ?? OverrideOptions.None;
        bool IOverridableSettings.IsRuntimeInstance => _isRuntimeInstance;

        private event SettingsChangedDelegate _changed;

        public event SettingsChangedDelegate Changed
        {
            add
            {
                var runtimeSettingsAttribute = Attribute as IRuntimeSettingsAttribute;
                if (runtimeSettingsAttribute != null &&
                    (runtimeSettingsAttribute.OverrideOptions.HasFlag(OverrideOptions.InMemoryDeferred) == false &&
                    runtimeSettingsAttribute.OverrideOptions.HasFlag(OverrideOptions.FileWatcher) == false))
                {
                    Debug.LogWarning($"{GetType().Name} will never raise {nameof(Changed)} during runtime. It is not flagged for deferred overrides: " +
                        $"({nameof(OverrideOptions)}.{nameof(OverrideOptions.FileWatcher)}, " +
                        $"{nameof(OverrideOptions)}.{nameof(OverrideOptions.InMemoryDeferred)}).");
                }

                _changed += value;
            }

            remove => _changed -= value;
        }

        override protected void RaiseChanged()
        {
            _changed?.Invoke();
        }

        internal sealed override void InitializeInstance()
        {
            var runtimeSettingsAttribute = Attribute as IRuntimeSettingsAttribute;
            if (runtimeSettingsAttribute == null || runtimeSettingsAttribute.OverrideOptions == OverrideOptions.None
#if UNITY_EDITOR
                || EditorApplication.isPlayingOrWillChangePlaymode == false
#endif
            )
                return;

            T runtimeInstance = null;
            var overrideOptions = runtimeSettingsAttribute.OverrideOptions;

            // Load runtime overrides from json file if it's allowed
            if (overrideOptions.HasFlag(OverrideOptions.File))
                LoadInitialRuntimeFileOverrides(ref runtimeInstance);

            // Load runtime overrides from commandline if it's allowed
            if (overrideOptions.HasFlag(OverrideOptions.Commandline))
                LoadRuntimeCommandlineOverrides(ref runtimeInstance);

            // Load runtime overrides from custom in-memory place if it's allowed
            if (overrideOptions.HasFlag(OverrideOptions.InMemory))
                LoadInitialInMemoryOverrides(ref runtimeInstance);

            if (overrideOptions.HasFlag(OverrideOptions.FileWatcher) || overrideOptions.HasFlag(OverrideOptions.InMemoryDeferred))
            {
                // Make sure to retrieve sync context so we react on the main thread
                Application.onBeforeRender += FetchSyncContext;

                // If there is not runtime instance by now, we should create one.
                // We do not know if there will be changes coming in later during
                // runtime either via file or in-memory. 
                // By creating a runtime instance now we can be sure the instance
                // being requested now will be the same.
                if(runtimeInstance == null)
                {
                    runtimeInstance = ScriptableObject.Instantiate(_instance);
                    UpdateRuntimeInstanceName(runtimeInstance);
                }
            }

            if (runtimeInstance != null)
            {
                var overridesString = UpdateRuntimeInstanceName(runtimeInstance);

                Debug.Log($"Created {typeof(T).Name} runtime instance {overridesString}");

#if UNITY_EDITOR
                EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
                EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
#endif

                _instance = runtimeInstance;
                _instance._isRuntimeInstance = true;
            }
        }

        private static string UpdateRuntimeInstanceName(T runtimeInstance)
        {
            var overridableSettings = (IOverridableSettings)runtimeInstance;
            var overridesString = overridableSettings.OverrideOrigins == null ?
                "with no overrides yet" :
                $"with overrides from: {string.Join(", ", overridableSettings.OverrideOrigins.Select(oo => oo.ToString()))}";
            runtimeInstance.name = $"{Instance.name} ({overridesString})";
            return overridesString;
        }

        private static void FetchSyncContext()
        {
            Application.onBeforeRender -= FetchSyncContext;
            _syncContext = SynchronizationContext.Current;

            while (_syncContextQueue.Count > 0)
            {
                var action = _syncContextQueue.Dequeue();
                _syncContext.Post(_ => action(), null);
            }
        }

        static void PostToMainThread(Action action)
        {
            if (_syncContext == null)
            {
                _syncContextQueue.Enqueue(action);
                return;
            }

            if (SynchronizationContext.Current == _syncContext)
            {
                action();
                return;
            }

            _syncContext.Post(_ => action(), null);
        }

        private static bool PopulateFromReader(ref T runtimeInstance, TextReader textReader, string jsonPath, bool throwWhenPathNotFound)
        {
            using var jr = new JsonTextReader(textReader)
            {
                DateParseHandling = DateParseHandling.None
            };

            JToken jToken = JObject.Load(jr);

            if (jToken == null)
                throw new Exception("Empty JToken");

            if (jsonPath != null)
                jToken = jToken.SelectToken(jsonPath, throwWhenPathNotFound);

            if (jToken == null)
                return false;

            PopulateFromJToken(ref runtimeInstance, jToken);

            return true;
        }

        private static void PopulateFromJToken(ref T runtimeInstance, JToken jToken)
        {
            if (runtimeInstance == null)
                runtimeInstance = ScriptableObject.Instantiate(_instance);

            if (_jsonSerializer == null)
                _jsonSerializer = JsonSerializer.Create(_jsonSerializerSettings);

            using (var jsonReader = jToken.CreateReader())
                _jsonSerializer.Populate(jsonReader, runtimeInstance);
        }

#if UNITY_EDITOR
        // Check if we are leaving play mode and reinitialize to revert from runtime instance.
        private static void EditorApplication_playModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange != PlayModeStateChange.ExitingPlayMode)
                return;

            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;

            ClearFileWatchers();
            ClearInMemoryEventListener();

            _instance = null;
            Initialize();
        }
#endif

        public void SaveAsJsonFile(string filename = null)
        {
            if (filename == null)
                filename = SerializableSettings<T>.Filename;

            filename = Path.ChangeExtension(filename, ".json");

            using (var fs = File.CreateText(filename))
            {
                fs.Write(JsonConvert.SerializeObject(this, _jsonSerializerSettings));
            }
        }

        public void LoadFromJsonFile(string filename = null)
        {
            if (filename == null)
                filename = SerializableSettings<T>.Filename;

            filename = Path.ChangeExtension(filename, ".json");

            if (File.Exists(filename) == false)
                return;

            var json = File.ReadAllText(filename);
            JsonConvert.PopulateObject(json, this, _jsonSerializerSettings);
        }
    }
}
