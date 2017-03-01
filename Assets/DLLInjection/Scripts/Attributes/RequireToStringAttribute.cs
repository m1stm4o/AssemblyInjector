
namespace DLLInjection {

    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class RequireToStringAttribute : ShouldBeInjectedAttribute {

    }
}