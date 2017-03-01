
namespace DLLInjection {

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Mono.Cecil;
    using UnityEditor;
    using UnityEngine;

    public class AssemblyInjector {

        [UnityEditor.Callbacks.DidReloadScripts]
        [UnityEditor.Callbacks.PostProcessScene]
        [MenuItem("AssemblyInjector/Execute Injection")]
        public static void Inject() {

            try {

                //this statement is used to force Editor to load 'Assembly-CSharp.dll'
                var t = typeof(ShouldBeInjectedAttribute);

                var injectorImplementationDict = CollectInjectorImplementations();

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

                        if (DoInject(assemblyDefinition, injectorImplementationDict)) {

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

        static IDictionary<string, InjectorImplementation> CollectInjectorImplementations() {

            var dict = new Dictionary<string, InjectorImplementation>();

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(InjectorImplementation)))) {

                var targetAttribute = type.GetCustomAttributes(typeof(ImplementationOfAttribute), false)
                    .Cast<ImplementationOfAttribute>()
                    .Select(a => a.target)
                    .FirstOrDefault(target => target != null && target.IsSubclassOf(typeof(ShouldBeInjectedAttribute)));

                if (targetAttribute != null) {

                    var attributeName = targetAttribute.Name;

                    if (!dict.ContainsKey(attributeName)) {

                        var instance = (InjectorImplementation)Activator.CreateInstance(type);

                        dict.Add(attributeName, instance);

                    } else {
                        Debug.LogError(string.Format(
                            "Error: Attempting to config a new injector {0} for attribute {1} which already has an injector {2}",
                            targetAttribute, attributeName, dict[attributeName]));
                    }
                }
            }

            return dict;
        }

        static bool DoInject(AssemblyDefinition assemblyDefinition, IDictionary<string, InjectorImplementation> injectorDict) {

            var processed = false;

            if (assemblyDefinition.CustomAttributes.All(a => a.AttributeType.Name != typeof(AssemblyInjectedAttribute).Name)) {

                foreach (var moduleDefinition in assemblyDefinition.Modules) {

                    foreach (var typeDefinition in moduleDefinition.GetTypes()) {

                        foreach (var attribute in typeDefinition.CustomAttributes) {

                            var attributeName = attribute.AttributeType.Name;

                            if (injectorDict.ContainsKey(attribute.AttributeType.Name)) {

                                if (injectorDict[attributeName].ProcessType(typeDefinition))
                                    processed = true;
                            }
                        }

                        foreach (var methodDefinition in typeDefinition.Methods) {

                            foreach (var attribute in methodDefinition.CustomAttributes) {

                                var attributeName = attribute.AttributeType.Name;

                                if (injectorDict.ContainsKey(attributeName)) {

                                    if (injectorDict[attributeName].ProcessMethod(methodDefinition))
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
    }
}
