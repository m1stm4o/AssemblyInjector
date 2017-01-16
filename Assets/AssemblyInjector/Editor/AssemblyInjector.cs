using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEngine;

public class AssemblyInjector {

    [UnityEditor.Callbacks.DidReloadScripts]
    [UnityEditor.Callbacks.PostProcessScene]
    [MenuItem("AssemblyInjector/Inject")]
    public static void Inject() {

        try {

            EditorApplication.LockReloadAssemblies();

            var assemblyPaths = new HashSet<string>();
            var assemblySearchDirectories = new HashSet<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {

                if (assembly.Location.Replace('\\', '/').StartsWith(Application.dataPath.Substring(0, Application.dataPath.Length - 7))) {
                    assemblyPaths.Add(assembly.Location);
                }

                assemblySearchDirectories.Add(Path.GetDirectoryName(assembly.Location));
            }

            var assemblyResolver = new DefaultAssemblyResolver();

            foreach (var searchDirectory in assemblySearchDirectories) {
                assemblyResolver.AddSearchDirectory(searchDirectory);
            }

            var readParameters = new ReaderParameters() {
                AssemblyResolver = assemblyResolver
            };

            foreach (var assemblyPath in assemblyPaths) {

                if (File.Exists(assemblyPath)) {

                    var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readParameters);

                    if (DoInject(assemblyDefinition)) {
                        assemblyDefinition.Write(assemblyPath);
                        Debug.Log("Assembly Processed: " + assemblyPath);
                    }
                } else {
                    Debug.LogError("Assembly doesn't exist: " + assemblyPath);
                }
            }

        } catch (Exception e) {

            Debug.LogException(e);

        } finally {

            EditorApplication.UnlockReloadAssemblies();
        }
    }

    static bool DoInject(AssemblyDefinition assemblyDefinition) {

        var processed = false;

        if (assemblyDefinition.CustomAttributes.All(a => a.AttributeType.Name != "AssemblyInjectedAttribute")) {

            foreach (var moduleDefinition in assemblyDefinition.Modules) {

                var debugLogReference = moduleDefinition.ImportReference(typeof(Debug).GetMethod("Log", new Type[] { typeof(object) }));

                foreach (var typeDefinition in moduleDefinition.GetTypes()) {

                    var requireToStringAttribute = typeDefinition.CustomAttributes
                        .FirstOrDefault(a => a.AttributeType.Name == "RequireToStringAttribute");

                    if (requireToStringAttribute != null) {

                        if (AddToString(typeDefinition)) {

                            processed = true;
                        }
                    }

                    foreach (var methodDefinition in typeDefinition.Methods) {

                        var insertLogAttribute = methodDefinition.CustomAttributes
                            .FirstOrDefault(a => a.AttributeType.Name == "InsertLogAttribute"); ;

                        if (insertLogAttribute != null) {

                            if (InsertLog(methodDefinition, debugLogReference)) {

                                processed = true;
                            }
                        }
                    }
                }
            }

            if (processed) {

                var attr = assemblyDefinition.MainModule.ImportReference(typeof(AssemblyInjectedAttribute).GetConstructor(new Type[] { }));

                var customAttribute = new CustomAttribute(attr);

                assemblyDefinition.CustomAttributes.Add(customAttribute);
            }
        }

        return processed;
    }

    static bool AddToString(TypeDefinition typeDefinition) {

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

    static bool InsertLog(MethodDefinition methodDefinition, MethodReference logReference) {

        if (methodDefinition.HasBody) {

            Debug.Log(methodDefinition.Name);

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

    static IList<Instruction> Format<T>(string prefix, ModuleDefinition moduleDefinition, IList<T> elements,
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
