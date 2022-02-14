#if ENABLE_KILL_OTHER_SHADER_STRIPPER


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor.Rendering;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using System.IO;
using Mono.Cecil.Cil;

namespace UTJ.ShaderVariantStripping
{
    public class ILUtility:ILPostProcessor
    {

        [MenuItem("Tools/Check")]
        public static void Check()
        {
            var classes = GetSubClasses<UnityEditor.Build.IPreprocessShaders>();
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach( var cls in classes)
            {
                Debug.Log(cls.FullName);
            }
        }

        private static List<System.Type> GetSubClasses<T>()
        {
            var result = new List<System.Type>();
            var asms = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach( var asm in asms)
            {
                var types = asm.GetTypes();
                foreach(var t in types)
                {
                    if(t == typeof(T)) { continue; }
                    if (typeof(T).IsAssignableFrom(t) ){
                        result.Add(t);
                    }
                }
            }

            return result;
        }

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            var isForEditor = compiledAssembly.Defines?.Contains("UNITY_EDITOR") ?? false;

            // Debug.Log("ILPostProcess " + compiledAssembly.Name + "::" + isForEditor);
            if (!isForEditor) { return false; }
            return true;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {

            var diagnostics = new List<DiagnosticMessage>();
            var inMemoryAssembly = compiledAssembly.InMemoryAssembly;

            var peData = inMemoryAssembly.PeData;
            var pdbData = inMemoryAssembly.PdbData;

            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                ReadingMode = ReadingMode.Immediate
            };

           var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData));
            //            assemblyDefinition.modu
            bool isModified = false;

            isModified = RemoveFromAssembly(assemblyDefinition);

            if (isModified)
            {

                var peStream = new MemoryStream();
                var pdbStream = new MemoryStream();
                var writeParameters = new WriterParameters
                {
                    SymbolWriterProvider = new PortablePdbWriterProvider(),
                    WriteSymbols = true,
                    SymbolStream = pdbStream
                };

                assemblyDefinition.Write(peStream, writeParameters);
                peStream.Flush();
                pdbStream.Flush();

                peData = peStream.ToArray();
                pdbData = pdbStream.ToArray();
                //
                System.IO.File.WriteAllText(compiledAssembly.Name + ".txt", "removed");
                return new ILPostProcessResult(new InMemoryAssembly(peData, pdbData), diagnostics);
            }
            return new ILPostProcessResult(null, diagnostics);
        }

        private bool RemoveFromAssembly(AssemblyDefinition assemblyDefinition)
        {
            if (assemblyDefinition == null) { return false; }
            var flag = false;
            foreach( var module in assemblyDefinition.Modules)
            {
                flag |= Remove(module);
            }
            return flag;
        }

        private bool Remove(ModuleDefinition module)
        {

            var types = module.Types;
            var remove = new List<TypeDefinition>();

            foreach (var type in types)
            {
                var interfaces = type.Interfaces;
                foreach (var inter in interfaces)
                {
                    if (inter.InterfaceType.FullName == "UnityEditor.Build.IPreprocessShaders")
                    {
                        remove.Add(type);
                    }
                }
            }
            foreach (var re in remove)
            {
                module.Types.Remove(re);
            }
            return (remove.Count > 0) ;
        }


    }
}
#endif