using Newtonsoft.Json;
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
        void SaveAsJsonFile( string filename = null );
        void LoadFromJsonFile( string filename = null );
    }

    internal interface IOverridableSettings
    {
        internal List<IOverrideOrigin> OverrideOrigins { get; set; }
        internal bool UseOriginFileWatchers { get; set; }
    }

    public abstract class SerializableSettings<T> : Settings<T>, ISerializableSettings, IOverridableSettings
        where T : SerializableSettings<T>
    {
        private static List<FileSystemWatcher> _originFileWatchers;
        private static SynchronizationContext _syncContext;

        private static JsonSerializer _jsonSerializer;
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = UnityEngineObjectContractResolver.Instance,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        List<IOverrideOrigin> IOverridableSettings.OverrideOrigins { get; set; }
        bool IOverridableSettings.UseOriginFileWatchers { get; set; }

        internal sealed override void InitializeInstance()
        {
            // Load runtime overrides from json file if it's allowed and we're actually in runtime.
            if( Attribute is IRuntimeSettingsAttribute && ( Attribute as IRuntimeSettingsAttribute ).AllowsFileOverrides()
#if UNITY_EDITOR
                          && EditorApplication.isPlayingOrWillChangePlaymode
#endif
            )
                _instance = LoadInitialRuntimeFileOverrides();

            // Load runtime overrides from commandline if it's allowed and we're actually in runtime.
            if( Attribute is IRuntimeSettingsAttribute && ( Attribute as IRuntimeSettingsAttribute ).AllowsCommandlineOverrides()
#if UNITY_EDITOR
                          && EditorApplication.isPlayingOrWillChangePlaymode
#endif
            )
                _instance = LoadRuntimeCommandlineOverrides();
        }

        internal static T LoadRuntimeCommandlineOverrides()
        {
            //var args = Environment.GetCommandLineArgs();
            var args = new[] { "unity.exe", "-test1", "-settings:TestSettings.Sub.Integer=55", "-s:TestSettings.Boolean=false" };
            var runtimeInstance = _instance;

            foreach( var settingsArg in args.Where( arg => arg.StartsWith( "-settings:" ) || arg.StartsWith( "-s:" ) ) )
            {
                var colonIndex = settingsArg.IndexOf( ':' ) + 1;
                if( settingsArg.Length - colonIndex <= 0 )
                {
                    Debug.LogWarning( $"Invalid settings argument format ({settingsArg}): Missing assignment" );
                    continue;
                }

                var assignment = settingsArg.Substring( colonIndex );

                var assignmentParts = assignment.Split( new[] { '=' }, StringSplitOptions.RemoveEmptyEntries );
                if( assignmentParts.Length < 2 )
                {
                    Debug.LogWarning( $"Invalid settings argument format ({settingsArg}): Missing '='" );
                    continue;
                }

                var propertyPath = assignmentParts[ 0 ];
                var value = assignmentParts[ 1 ];

                var propertyPathParts = propertyPath.Split( new[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
                if( propertyPathParts.Length < 2 )
                {
                    Debug.LogWarning( $"Invalid settings argument format ({settingsArg}): Property path too short" );
                    continue;
                }

                var propertyPathSettingsName = propertyPathParts[ 0 ];
                if( propertyPathSettingsName == Filename || propertyPathSettingsName == typeof( T ).Name )
                {
                    var root = new JObject();
                    var current = root;
                    for( var i = 1; i < propertyPathParts.Length; i++ )
                    {
                        if( propertyPathParts.Length - 1 == i )
                        {
                            current.Add( propertyPathParts[ i ], JValue.CreateString( value ) );
                        }
                        else
                        {
                            current.Add( propertyPathParts[ i ], current = new JObject() );
                        }
                    }

                    if( runtimeInstance == null )
                        runtimeInstance = ScriptableObject.Instantiate( _instance );

                    using( var jsonReader = root.CreateReader() )
                        _jsonSerializer.Populate( jsonReader, _instance );

                    AddCommandlineOverrideOrigin( runtimeInstance, settingsArg );
                }
            }

            return runtimeInstance;
        }

        internal static T LoadInitialRuntimeFileOverrides()
        {
            var runtimeInstance = LoadOverridesFromAllFiles( null, fromWatcher: false );

            if( runtimeInstance == null )
                return null;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
#endif

            var overridableSettings = ( IOverridableSettings )runtimeInstance;
            var overridesString = $"with overrides from file{( overridableSettings.OverrideOrigins.Count > 1 ? "s" : "" )}: {string.Join( ", ", overridableSettings.OverrideOrigins )}";
            Debug.Log( $"Created {typeof( T ).Name} runtime instance {overridesString}" );

            runtimeInstance.name = $"{Instance.name} ({overridesString})";

            if( ( Attribute as IRuntimeSettingsAttribute ).AllowsFileWatchers() )
                SetupOriginFileWatchers( overridableSettings );

            return runtimeInstance;
        }

        #region filewatchers
        private static void SetupOriginFileWatchers( IOverridableSettings overridableSettings )
        {
            overridableSettings.UseOriginFileWatchers = true;

            Application.onBeforeRender += Application_onBeforeRender;

            foreach( var fileOrigins in overridableSettings.OverrideOrigins.OfType<FileOverrideOrgin>() )
            {
                var fsw = new FileSystemWatcher( Path.GetDirectoryName( Path.GetFullPath( fileOrigins.FilePath ) ), Path.GetFileName( fileOrigins.FilePath ) );
                fsw.NotifyFilter = NotifyFilters.LastWrite;
                fsw.Changed += OverrideOriginFileChanged;
                fsw.EnableRaisingEvents = true;

                if( _originFileWatchers == null )
                    _originFileWatchers = new List<FileSystemWatcher>();
                _originFileWatchers.Add( fsw );
            }
        }

        private static void Application_onBeforeRender()
        {
            Application.onBeforeRender -= Application_onBeforeRender;
            _syncContext = SynchronizationContext.Current;
        }

        private static void ClearOriginFileWatchers()
        {
            if( _originFileWatchers == null )
                return;

            foreach( var fsw in _originFileWatchers )
            {
                fsw.EnableRaisingEvents = false;
                fsw.Changed -= OverrideOriginFileChanged;
            }

            _originFileWatchers = null;
        }

        private static void OverrideOriginFileChanged( object sender, FileSystemEventArgs e )
        {
            _syncContext.Post( _ =>
            {
                LoadOverridesFromAllFiles( _instance, fromWatcher: true );

                var overridableSettings = ( IOverridableSettings )_instance;

                var filePaths = string.Join( ", ", overridableSettings.OverrideOrigins.OfType<FileOverrideOrgin>().Select( foo => foo.FilePath ) );
                var overridesString = $"with overrides from file{( overridableSettings.OverrideOrigins.Count > 1 ? "s" : "" )}: {filePaths}";
                Debug.Log( $"Updated {typeof( T ).Name} runtime instance {overridesString}" );
            }, null );
        }
        #endregion

        private static T LoadOverridesFromAllFiles( T runtimeInstance, bool fromWatcher )
        {
            var mainJsonFilename = "Settings.json";
            runtimeInstance = LoadOverridesFromFile( runtimeInstance, mainJsonFilename, fromWatcher, jsonPath: Filename );

            var jsonFilename = Filename + ".json";
            runtimeInstance = LoadOverridesFromFile( runtimeInstance, jsonFilename, fromWatcher );

            return runtimeInstance;
        }

        private static T LoadOverridesFromFile( T runtimeInstance, string jsonFilePath, bool fromWatcher, string jsonPath = null )
        {
            T localRuntimeInstance = null;
            try
            {
                if( Path.IsPathRooted( jsonFilePath ) == false )
                    jsonFilePath = Path.GetFullPath( Path.Combine( Application.dataPath, "..", jsonFilePath ) );

                if( File.Exists( jsonFilePath ) )
                {
                    using var jr = new JsonTextReader( File.OpenText( jsonFilePath ) )
                    {
                        DateParseHandling = DateParseHandling.None
                    };

                    JToken jToken = JObject.Load( jr );

                    if( jToken == null )
                        return runtimeInstance;

                    if( jsonPath != null )
                        jToken = jToken.SelectToken( jsonPath );

                    if( jToken == null )
                        return runtimeInstance;

                    if( runtimeInstance == null )
                        localRuntimeInstance = runtimeInstance = ScriptableObject.Instantiate( _instance );

                    if( _jsonSerializer == null )
                        _jsonSerializer = JsonSerializer.Create( _jsonSerializerSettings );

                    using( var jsonReader = jToken.CreateReader() )
                        _jsonSerializer.Populate( jsonReader, runtimeInstance );

                    AddFileOverrideOrigin( runtimeInstance, jsonFilePath, fromWatcher );

                    return runtimeInstance;
                }
            }
            catch( Exception ex )
            {
                Debug.LogError( $"Error loading overrides from {jsonFilePath} for {typeof( T ).Name}\n{ex}" );

                if( localRuntimeInstance )
                {
#if UNITY_EDITOR
                    if( Application.isPlaying == false )
                        ScriptableObject.DestroyImmediate( localRuntimeInstance );
                    else
#endif
                        ScriptableObject.Destroy( localRuntimeInstance );
                }
            }

            return runtimeInstance;
        }

        private static void AddFileOverrideOrigin( T instance, string filePath, bool fromWatcher )
        {
            var oi = ( ( IOverridableSettings )instance );
            if( oi == null )
                return;

            if( oi.OverrideOrigins == null )
            {
                oi.OverrideOrigins = new List<IOverrideOrigin>();
            }

            if( fromWatcher )
            {
                oi.OverrideOrigins.Add( new FileWatcherOverrideOrgin( filePath ) );
            }
            else
            {
                oi.OverrideOrigins.Add( new FileOverrideOrgin( filePath ) );
            }
        }

        private static void AddCommandlineOverrideOrigin( T instance, string argument )
        {
            var oi = ( ( IOverridableSettings )instance );
            if( oi == null )
                return;

            if( oi.OverrideOrigins == null )
            {
                oi.OverrideOrigins = new List<IOverrideOrigin>();
            }

            oi.OverrideOrigins.Add( new CommandlineOverrideOrgin( argument ) );
        }

#if UNITY_EDITOR
        // Check if we are leaving play mode and reinitialize to revert from runtime instance.
        private static void EditorApplication_playModeStateChanged( PlayModeStateChange stateChange )
        {
            if( stateChange != PlayModeStateChange.ExitingPlayMode )
                return;

            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;

            ClearOriginFileWatchers();

            _instance = null;
            Initialize();
        }
#endif

        public void SaveAsJsonFile( string filename = null )
        {
            if( filename == null )
                filename = SerializableSettings<T>.Filename;

            filename = Path.ChangeExtension( filename, ".json" );

            using( var fs = File.CreateText( filename ) )
            {
                fs.Write( JsonConvert.SerializeObject( this, _jsonSerializerSettings ) );
            }
        }

        public void LoadFromJsonFile( string filename = null )
        {
            if( filename == null )
                filename = SerializableSettings<T>.Filename;

            filename = Path.ChangeExtension( filename, ".json" );

            if( File.Exists( filename ) == false )
                return;

            var json = File.ReadAllText( filename );
            JsonConvert.PopulateObject( json, this, _jsonSerializerSettings );
        }
    }
}
