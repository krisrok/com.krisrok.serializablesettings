using System;

#if !ODIN_INSPECTOR
namespace SerializableSettings
{
    // Stand-in for Odin's attribute for compatibility
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
    public class HideMonoScriptAttribute : Attribute
    { }
}
#endif
