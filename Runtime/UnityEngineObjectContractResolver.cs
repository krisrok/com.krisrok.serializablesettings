using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SerializableSettings
{
    public class UnityEngineObjectContractResolver : DefaultContractResolver
    {
        internal static UnityEngineObjectContractResolver Instance { get; } = new UnityEngineObjectContractResolver();

        private static string[] _ignoredMemberNames = new[] { nameof( UnityEngine.Object.name ), nameof( UnityEngine.Object.hideFlags ) };

        private static Regex _backingFieldRegex = new Regex( "<(.*?)>k__BackingField", RegexOptions.Compiled );

        private UnityEngineObjectContractResolver() { }

        protected override List<MemberInfo> GetSerializableMembers( Type objectType )
        {
            var members = base.GetSerializableMembers( objectType );

            AddMissingMembers( objectType, members );

            return members;
        }

        /// <summary>
        /// Adds fields decorated with [<see cref="SerializeField"/>] if they are valid.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="alreadyAdded"></param>
        /// <returns></returns>
        // from https://github.com/jilleJr/Newtonsoft.Json-for-Unity.Converters/blob/master/Packages/Newtonsoft.Json-for-Unity.Converters/UnityConverters/UnityTypeContractResolver.cs
        private static void AddMissingMembers( Type type, List<MemberInfo> alreadyAdded )
        {
            foreach( var memberInfo in type.GetFields( BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy )
                .Cast<MemberInfo>()
                .Concat( type.GetProperties( BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy ) )
                .Where( o => o.GetCustomAttribute<SerializeField>() != null
                    && !alreadyAdded.Contains( o )
                    && !IsBackingFieldOfAlreadyAddedProperty( o as FieldInfo, alreadyAdded ) ) )
            {
                alreadyAdded.Add( memberInfo );
            }
        }

        /// <summary>
        /// Checks if the serialized field is a backing field of an already added property.
        /// </summary>
        /// <returns>Returns true if conditions are met</returns>
        private static bool IsBackingFieldOfAlreadyAddedProperty( FieldInfo fieldInfo, List<MemberInfo> alreadyAdded )
        {
            if( fieldInfo == null )
                return false;

            var match = _backingFieldRegex.Match( fieldInfo.Name );
            if( match.Success == false )
                return false;

            return alreadyAdded.Any( mi => mi is PropertyInfo && mi.Name == match.Groups[ 1 ].Value );
        }

        protected override JsonProperty CreateProperty( MemberInfo member, MemberSerialization memberSerialization )
        {
            var jsonProperty = base.CreateProperty( member, memberSerialization );

            if( jsonProperty.Ignored )
                return jsonProperty;

            if( member.DeclaringType == typeof( UnityEngine.Object ) && _ignoredMemberNames.Contains( member.Name ) )
            {
                jsonProperty.Ignored = true;
                return jsonProperty;
            }

            //TODO: match serialization of props only when serialized by unity as well

            var propertyInfo = member as PropertyInfo;

            if( member.GetCustomAttribute<SerializeField>() != null )
            {
                jsonProperty.Ignored = false;
                jsonProperty.Writable = propertyInfo != null ? propertyInfo.CanWrite : true;
                jsonProperty.Readable = propertyInfo != null ? propertyInfo.CanRead : true;
                jsonProperty.HasMemberAttribute = true;
            }
            else
            {
                if( jsonProperty.Writable == false && propertyInfo != null )
                {
                    if( propertyInfo.CanWrite == false )
                    {
                        // this is most likely an auto-property without any setter
                        jsonProperty.Ignored = true;
                    }
                    else
                    {
                        if( propertyInfo.GetSetMethod( nonPublic: true ) != null )
                        {
                            // found a setter after all, so we'll set it to be writable
                            jsonProperty.Writable = true;
                        }
                        else
                        {
                            // otherwise just ignore it
                            jsonProperty.Ignored = true;
                        }
                    }
                }
            }

            return jsonProperty;
        }
    }
}
