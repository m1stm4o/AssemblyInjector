
namespace DLLInjection {

    using System;
    using System.Collections.Generic;
    using System.Text;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    [ImplementationOf(typeof(ImplementationOfAttribute))]
    public abstract class InjectorImplementation {

        public virtual bool ProcessType(TypeDefinition typeDefinition) { return false; }

        public virtual bool ProcessMethod(MethodDefinition methodDefinition) { return false; }

        protected static IList<Instruction> Format<T>(string prefix, ModuleDefinition moduleDefinition, IList<T> elements,
                 Func<T, string> GetName, Func<T, int, IEnumerable<Instruction>> Load, Func<T, TypeReference> GetType) {

            var instructions = new List<Instruction>();

            MethodReference formatReference;

            var formatStringBuilder = new StringBuilder(prefix);

            formatStringBuilder.Append("[");

            for (int i = 0; i < elements.Count; i++) {
                formatStringBuilder.Append(GetName(elements[i]) + "={" + i + "}");

                if (i < elements.Count - 1)
                    formatStringBuilder.Append(", ");
            }

            formatStringBuilder.Append("]");

            instructions.Add(Instruction.Create(OpCodes.Ldstr, formatStringBuilder.ToString()));

            if (elements.Count < 4) {

                var formatParamArray = new Type[elements.Count + 1];
                formatParamArray[0] = typeof(string);

                for (int i = 0; i < elements.Count; i++) {

                    formatParamArray[i + 1] = typeof(object);

                    var ele = elements[i];

                    instructions.AddRange(Load(ele, i));

                    var type = GetType(ele);

                    if (type.IsByReference) {

                        var elementType = type.GetElementType();

                        instructions.Add(Instruction.Create(OpCodes.Ldobj, elementType));

                        if (elementType.IsValueType) {

                            instructions.Add(Instruction.Create(OpCodes.Box, elementType));
                        }
                    } else if (type.IsValueType) {

                        instructions.Add(Instruction.Create(OpCodes.Box, type));
                    }
                }

                formatReference = moduleDefinition.ImportReference(typeof(string).GetMethod("Format", formatParamArray));

            } else {

                Func<int, Instruction> CreateLdc_I4 = val =>
                val < 256 ? Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)val) : Instruction.Create(OpCodes.Ldc_I4, val);

                instructions.Add(CreateLdc_I4(elements.Count));
                instructions.Add(Instruction.Create(OpCodes.Newarr, moduleDefinition.ImportReference(typeof(object))));

                for (int i = 0; i < elements.Count; i++) {

                    instructions.Add(Instruction.Create(OpCodes.Dup));

                    instructions.Add(CreateLdc_I4(i));

                    var ele = elements[i];

                    instructions.AddRange(Load(ele, i));

                    var type = GetType(ele);

                    if (type.IsByReference) {

                        var elementType = type.GetElementType();

                        instructions.Add(Instruction.Create(OpCodes.Ldobj, elementType));

                        if (elementType.IsValueType) {

                            instructions.Add(Instruction.Create(OpCodes.Box, elementType));
                        }
                    } else if (type.IsValueType) {

                        instructions.Add(Instruction.Create(OpCodes.Box, type));
                    }

                    instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
                }

                formatReference = moduleDefinition.ImportReference(typeof(string).GetMethod("Format", new[] { typeof(string), typeof(object[]) }));
            }

            instructions.Add(Instruction.Create(OpCodes.Call, formatReference));

            return instructions;
        }
    }
}
