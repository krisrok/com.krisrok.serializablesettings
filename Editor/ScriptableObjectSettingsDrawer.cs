using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SerializableSettings.Editor
{
    [CustomPropertyDrawer( typeof( Settings<> ), true )]
    [CustomPropertyDrawer( typeof( ISerializableSettings ), true )]
    public class ScriptableObjectSettingsDrawer : PropertyDrawer
    {
        private static Dictionary<Type, SettingsAttributeBase> _settingsAttributeLookup = new Dictionary<Type, SettingsAttributeBase>();
        private static Dictionary<Type, string> _displayPathLookup = new Dictionary<Type, string>();
        private static bool _isContentInited;
        private static GUIContent _settingsIcon;

        private static void InitContent()
        {
            if( _isContentInited == false )
                return;

            _isContentInited = true;

            _settingsIcon = EditorGUIUtility.IconContent( "Settings" );
        }

        public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
        {
            InitContent();

            var settingsInstance = ( ( ScriptableObject )property.objectReferenceValue );

            var fieldType = fieldInfo.FieldType;

            var attribute = GetSettingsAttribute( fieldType );

            var textFieldRect = position;
            textFieldRect.width -= 20;
            if( textFieldRect.Contains( Event.current.mousePosition ) && Event.current.type == EventType.MouseDown && Event.current.button == 0 )
            {
                // Jump to project settings
                if( settingsInstance == null )
                {
                    SettingsService.OpenProjectSettings( GetDisplayPath( fieldType ) );
                }
                // Or ping the object in project window
                else
                {
                    EditorGUIUtility.PingObject( property.objectReferenceValue );
                }
            }

            EditorGUI.PropertyField( position, property, label );

            if( settingsInstance == null && attribute != null )
            {
                var name = GetDisplayPath( fieldType );

                // Clear ObjectField drawer
                textFieldRect.xMin += EditorGUIUtility.labelWidth;
                EditorGUI.LabelField( textFieldRect, GUIContent.none, EditorStyles.textField );

                // Leave some space for icon and draw name
                textFieldRect.xMin += textFieldRect.height;
                EditorGUI.LabelField( textFieldRect, name, EditorStyles.textField );

                // Draw cogwheel icon
                var iconRect = position;
                iconRect.x += EditorGUIUtility.labelWidth;
                iconRect.width = iconRect.height;
                EditorGUI.LabelField( iconRect, _settingsIcon );
            }

            label.tooltip = null;
        }

        private string GetDisplayPath( Type type )
        {
            if( _displayPathLookup.ContainsKey( type ) == false )
            {
                var propInfo = type.GetProperty( "displayPath", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy );
                _displayPathLookup[ type ] = propInfo.GetValue( null ) as string;
            }

            return _displayPathLookup[ type ];
        }

        private static SettingsAttributeBase GetSettingsAttribute( Type type )
        {
            if( _settingsAttributeLookup.ContainsKey( type ) == false )
            {
                return _settingsAttributeLookup[ type ] = type.GetCustomAttributes( typeof( SettingsAttributeBase ), true ).FirstOrDefault() as SettingsAttributeBase;
            }

            return _settingsAttributeLookup[ type ];
        }
    }
}
