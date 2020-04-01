using System;
using PostSharp.Extensibility;

namespace PostSharp.Community.UnsafeMemoryChecker
{
    /// <summary>
    /// If this attribute is defined, it means that pointer write instructions should be instrumented and should throw
    /// an exception if access to uncontrolled memory is attempted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    [RequirePostSharp("PostSharp.Community.UnsafeMemoryChecker.Weaver", "CheckUnsafeMemoryAccessTask")]
    public class CheckUnsafeMemoryAttribute : Attribute
    {
        
    }
}