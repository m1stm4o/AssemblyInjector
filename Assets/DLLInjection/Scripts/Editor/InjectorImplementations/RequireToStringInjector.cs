
namespace DLLInjection {

    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    [ImplementationOf(typeof(RequireToStringAttribute))]
    public class RequireToStringInjector : InjectorImplementation {

        public override bool ProcessType(TypeDefinition typeDefinition) {

            if (!(typeDefinition.IsAbstract && typeDefinition.IsSealed)) {//not static class

                if (typeDefinition.HasFields) {

                    var toStringMethod = typeDefinition.Methods.FirstOrDefault(m => m.Name == "ToString" && m.Parameters.Count == 0);

                    if (toStringMethod == null) {

                        var attr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;

                        toStringMethod = new MethodDefinition("ToString", attr, typeDefinition.Module.TypeSystem.String);

                        var ilProcessor = toStringMethod.Body.GetILProcessor();

                        var formatInstructions = Format(typeDefinition.FullName + ": ", typeDefinition.Module, typeDefinition.Fields,
                            f => f.Name, (f, i) => new[] { Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldfld, f) }, f => f.FieldType);

                        foreach (var instruction in formatInstructions) {
                            ilProcessor.Append(instruction);
                        }

                        ilProcessor.Emit(OpCodes.Ret);

                        typeDefinition.Methods.Add(toStringMethod);

                        return true;
                    }
                }
            }

            return false;
        }
    }
}