
namespace DLLInjection {

    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class InsertLogAttribute : ShouldBeInjectedAttribute {

    }
}