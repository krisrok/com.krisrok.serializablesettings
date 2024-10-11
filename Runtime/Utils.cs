using UnityEngine;
namespace SerializableSettings
{
    internal static class Utils
    {
        public static void SafeDestroy(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
                ScriptableObject.DestroyImmediate(obj);
            else
#endif
                ScriptableObject.Destroy(obj);
        }
    }
}
