
namespace DLLInjection {

    using System;

    [AttributeUsage(AttributeTargets.Assembly)]
    public class AssemblyInjectedAttribute : Attribute {

    }

    public abstract class ShouldBeInjectedAttribute : Attribute {

    }
}
