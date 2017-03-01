
namespace DLLInjection {

    using System;
    using System.Linq;
    using System.Reflection;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using UnityEngine;

    [ImplementationOf(typeof(InsertLogAttribute))]
    public class InsertLogInjector : InjectorImplementation {

        MethodInfo debugLogMethod;

        public override bool ProcessMethod(MethodDefinition methodDefinition) {

            if (methodDefinition.HasBody) {

                Debug.Log(methodDefinition.Name);

                if (debugLogMethod == null) {

                    debugLogMethod = typeof(Debug).GetMethod("Log", new Type[] { typeof(object) });
                }

                var logReference = methodDefinition.Module.ImportReference(debugLogMethod);

                var moduleDefinition = methodDefinition.Module;

                var typeName = methodDefinition.DeclaringType.FullName;

                var ilProcessor = methodDefinition.Body.GetILProcessor();

                var first = methodDefinition.Body.Instructions.First();

                var parameters = methodDefinition.Parameters;

                if (parameters.Count == 0) {

                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, "Enter " + typeName + "." + methodDefinition.Name));

                } else {

                    var formatInstructions = Format("Enter " + typeName + "." + methodDefinition.Name + " : ", moduleDefinition, parameters,
                        p => p.Name, (p, i) => new[] { Instruction.Create(i < 256 ? OpCodes.Ldarg_S : OpCodes.Ldarg, p) }, p => p.ParameterType);

                    for (int i = 0; i < formatInstructions.Count; i++) {
                        ilProcessor.InsertBefore(first, formatInstructions[i]);
                    }
                }

                ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, logReference));

                var last = methodDefinition.Body.Instructions.Last();

                if (methodDefinition.ReturnType.FullName != "System.Void") {

                    ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Dup));

                    var v = new VariableDefinition(methodDefinition.ReturnType);

                    methodDefinition.Body.Variables.Add(v);

                    ilProcessor.InsertBefore(last, Instruction.Create(v.Index < 256 ? OpCodes.Stloc_S : OpCodes.Stloc, v));

                    ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Ldstr, "Exit " + typeName + "." + methodDefinition.Name + " : "));


                    ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Ldloc_S, v));

                    if (v.VariableType.IsValueType) {

                        ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Box, v.VariableType));
                    }

                    var concat = moduleDefinition.ImportReference(typeof(string).GetMethod("Concat", new[] { typeof(object), typeof(object) }));

                    ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Call, concat));

                } else {

                    ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Ldstr, "Exit " + typeName + "." + methodDefinition.Name));
                }

                ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Call, logReference));

                return true;
            }

            return false;
        }
    }
}