#if !ODIN_INSPECTOR
using System;

namespace SerializableSettings
{
    // Stand-in for Odin's attribute for compatibility
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
    public class HideMonoScriptAttribute : Attribute
    { }
}
#endif
