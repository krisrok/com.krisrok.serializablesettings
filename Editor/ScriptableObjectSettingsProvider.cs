// Derivative of "https://github.com/hextantstudios/com.hextantstudios.utilities" 
//  Original Copyright 2021 by Hextant Studios. https://HextantStudios.com
//  Used under CC BY 4.0. http://creativecommons.org/licenses/by/4.0/
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace SerializableSettings.Editor
{
    
    using Editor = UnityEditor.Editor;

    // SettingsProvider helper used to display settings for a ScriptableObject
    // derived class.
    internal class ScriptableObjectSettingsProvider : SettingsProvider
    {
        public ScriptableObjectSettingsProvider( Func<ScriptableObject> settingsGetter,
            SettingsScope scope, string displayPath ) :
            base( displayPath, scope ) => _settingsGetter = settingsGetter;

        private readonly Func<ScriptableObject> _settingsGetter;

        // The settings instance being edited.
        private ScriptableObject _settingsScriptableObject;
        private ISettingsInternals _settingsInternals;
        private ISerializableSettings _serializableSettings;
        private IOverridableSettings _overridableSettings;
        private bool _isRuntimeInstance;

        // True if the keywords set has been built.
        bool _keywordsBuilt;

        // Cached editor used to render inspector GUI.
        Editor _editor;
        private bool _isOdinEditor;
        private bool _hasHideMonoScriptAttribute;

#if ODIN_INSPECTOR
        private static bool _doneWaitingForOdin;
        private bool _waitingForOdin;
#endif

        // Called when the settings are displayed in the UI.
        public override void OnActivate( string searchContext,
            UnityEngine.UIElements.VisualElement rootElement )
        {
            _settingsScriptableObject = _settingsGetter();
            _settingsInternals = _settingsScriptableObject as ISettingsInternals;
            _serializableSettings = _settingsScriptableObject as ISerializableSettings;
            _overridableSettings = _settingsScriptableObject as IOverridableSettings;
            _isRuntimeInstance = _overridableSettings?.IsRuntimeInstance ?? false;

            _hasHideMonoScriptAttribute = _settingsScriptableObject.GetType().GetCustomAttributes( typeof( HideMonoScriptAttribute ), true ).Length > 0;
#if ODIN_INSPECTOR
            if( _doneWaitingForOdin == false)
                _waitingForOdin = true;
            else
#endif
            CreateEditor();

            base.OnActivate( searchContext, rootElement );
        }

        // Called when the settings are no longer displayed in the UI.
        public override void OnDeactivate()
        {
            if( _editor != null )
            {
                Editor.DestroyImmediate( _editor );
                _editor = null;
            }

            base.OnDeactivate();
        }

        public override void OnTitleBarGUI()
        {
            base.OnTitleBarGUI();

            if( _serializableSettings == null )
                return;

            var dropdownRect = EditorGUILayout.GetControlRect();
            var dropdownButtonRect = dropdownRect;
            dropdownButtonRect.x += dropdownRect.width - dropdownRect.height * 2;
            dropdownButtonRect.width = dropdownRect.height * 2;
            if( EditorGUI.DropdownButton( dropdownButtonRect, EditorGUIUtility.IconContent( "SaveAs" ), FocusType.Keyboard ) )
            {
                var menu = new GenericMenu();
                menu.AddItem( new GUIContent( "Load from .json" ), false, () =>
                {
                    var directory = Path.GetFullPath( Path.Combine( Application.dataPath, ".." ) );
                    var filename = EditorUtility.OpenFilePanel( $"Load from .json", directory, "json" );
                    if( string.IsNullOrEmpty( filename ) )
                        return;

                    Undo.RecordObject( _settingsScriptableObject, "Load from .json" );
                    _serializableSettings.LoadFromJsonFile( filename );
                    Undo.FlushUndoRecordObjects();
                } );
                menu.AddItem( new GUIContent( "Save as .json" ), false, () =>
                {
                    var directory = Path.GetFullPath( Path.Combine( Application.dataPath, ".." ) );
                    var filename = EditorUtility.SaveFilePanel( $"Save as .json", directory, _settingsInternals.Filename, "json" );
                    if( string.IsNullOrEmpty( filename ) )
                        return;

                    _serializableSettings.SaveAsJsonFile( filename );
                } );
                menu.DropDown( dropdownButtonRect );
            }
        }

        // Displays the settings.
        public override void OnGUI( string searchContext )
        {
#if ODIN_INSPECTOR
            // Delay editor creation one frame so to be sure Odin is initialized
            if(_doneWaitingForOdin == false)
            {
                if( Event.current.type != EventType.Repaint )
                    return;

                if( _waitingForOdin )
                {
                    _waitingForOdin = false;
                }
                else
                {
                    CreateEditor();
                    _doneWaitingForOdin = true;
                }

                Repaint();
                return;
            }
#endif
            if( _settingsGetter == null || _editor == null ) return;

            // Set label width and indentation to match other settings.
            EditorGUIUtility.labelWidth = 250;
            GUILayout.BeginHorizontal();
            GUILayout.Space( 10 );
            GUILayout.BeginVertical();
            GUILayout.Space( 10 );

            // If rendererd by Unity, manually draw a "Script" field if there is no HideMonoScript attribute.
            if( _isOdinEditor == false && _hasHideMonoScriptAttribute == false )
            {
                EditorGUI.BeginDisabledGroup( true );
                EditorGUILayout.ObjectField( "Script", MonoScript.FromScriptableObject( _settingsScriptableObject ), _settingsScriptableObject.GetType(), false );
                EditorGUI.EndDisabledGroup();
            }

            if( _isRuntimeInstance )
            {
                var richTextStyle = new GUIStyle( EditorStyles.label );
                richTextStyle.richText = true;

                GUILayout.BeginVertical( EditorStyles.helpBox );
                GUILayout.Label( "This is a <b>runtime</b> instance: Changes will <b>not</b> be saved automatically!", richTextStyle );

                GUILayout.Space( 10 );

                var deferredStuff = new List<string>();
                if (_overridableSettings.OverrideOptions.HasFlag(OverrideOptions.FileWatcher))
                    deferredStuff.Add("file watchers");
                if (_overridableSettings.OverrideOptions.HasFlag(OverrideOptions.InMemoryDeferred))
                    deferredStuff.Add("deferred in-memory changes");

                if(deferredStuff.Count > 0)
                {
                    GUILayout.Label( $"Overrides from {string.Join(" and ", deferredStuff)} are active. Beware of the loading order!" );
                }

                GUILayout.Space( 10 );

                if( _overridableSettings.OverrideOrigins == null )
                {
                    GUILayout.Label($"No overrides haved been loaded yet.");
                }
                else
                {
                    GUILayout.Label( $"Overrides have been loaded in following order:" );

                    GUILayout.BeginHorizontal();
                    GUILayout.Space( 20 );
                    GUILayout.BeginVertical();

                    for( var i = 0; i < _overridableSettings.OverrideOrigins.Count; i++ )
                    {
                        var o = _overridableSettings.OverrideOrigins[ i ];
                        GUILayout.Label( $"{i + 1}.: {o.ToString()}" );
                    }

                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.Space( 10 );
            }

            EditorGUI.BeginChangeCheck();

            // Draw the editor's GUI.
            _editor.OnInspectorGUI();

            if(EditorGUI.EndChangeCheck())
            {
                _settingsInternals.RaiseChangedInternal();
            }

            // Reset label width and indent.
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;
        }

        // Build the set of keywords on demand from the settings fields.
        public override bool HasSearchInterest( string searchContext )
        {
            if( !_keywordsBuilt )
            {
                using( var serializedSettings = new SerializedObject( _settingsGetter() ) )
                {
                    keywords = GetSearchKeywordsFromSerializedObject( serializedSettings );
                }
                _keywordsBuilt = true;
            }
            return base.HasSearchInterest( searchContext );
        }

        private void CreateEditor()
        {
            _editor = Editor.CreateEditor( _settingsScriptableObject );
#if ODIN_INSPECTOR
            _isOdinEditor = _editor is OdinEditor;
#endif
        }
    }
}
