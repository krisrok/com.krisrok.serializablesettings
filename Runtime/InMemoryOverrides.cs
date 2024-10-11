using System.Collections.Generic;

namespace SerializableSettings
{
    public class InMemoryOverrides
    {
        internal struct Item
        {
            public string Description;
            public string Json;
        }

        internal static List<Item> UntypedOverrides = new List<Item>();

        internal delegate void UntypedOverridesAddedDelegate(Item item);

        internal static event UntypedOverridesAddedDelegate UntypedOverridesAdded;

        //private static Dictionary<Type, string> _overrides;

        /// <summary>
        /// <para>
        /// Add json contents which act the same way as Settings.json.
        /// Every settings object has to be a top-level object of this structure.
        /// </para>
        /// <para>
        /// Example: If you want to override two settings values, one in a class called FooSettings 
        /// and another in a class called BarSettings, the json sould look something like this:
        /// </para>
        /// <code>
        /// {
        ///     "FooSettings":
        ///     {
        ///         "MyString": "Hello"
        ///     },
        ///     "BarSettings":
        ///     {
        ///         "MyInt": 303
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <param name="json"></param>
        /// <param name="description">Description of where the overrides come from, e.g. "Socket".</param>
        public static void Add(string json, string description = null)
        {
            var item = new Item
            {
                Json = json,
                Description = description
            };

            UntypedOverrides.Add(item);

            UntypedOverridesAdded?.Invoke(item);
        }
    }
}
