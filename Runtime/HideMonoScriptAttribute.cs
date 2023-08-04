using System;

#if !ODIN_INSPECTOR
namespace Hextant
{
    // Stand-in for Odin's attribute for compatibility
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
    public class HideMonoScriptAttribute : Attribute
    { }
}
#endif