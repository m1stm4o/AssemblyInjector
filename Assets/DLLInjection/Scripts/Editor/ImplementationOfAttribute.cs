

namespace DLLInjection {

    using System;

    public class ImplementationOfAttribute : Attribute {

        public Type target { get; set; }

        public ImplementationOfAttribute(Type targetType) {

            target = targetType;
        }
    }
}
