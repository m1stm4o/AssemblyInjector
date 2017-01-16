using System;

[AttributeUsage(AttributeTargets.Assembly)]
public class AssemblyInjectedAttribute : Attribute {

}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class RequireToStringAttribute : Attribute {

}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class InsertLogAttribute : Attribute {

}
