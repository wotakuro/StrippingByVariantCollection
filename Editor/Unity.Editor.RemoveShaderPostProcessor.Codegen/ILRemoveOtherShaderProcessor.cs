#if ENABLE_KILL_OTHER_SHADER_STRIPPER


using System.Collections.Generic;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using System.IO;
using Mono.Cecil.Cil;

namespace UTJ.ShaderVariantStripping.CodeGen
{
    public class ILRemoveOtherShaderProcessor:ILPostProcessor
    {
        private ConfigReader configReader;

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            if(configReader == null)
            {
                configReader = new ConfigReader();
            }
            if (!configReader.ExecuteRemoveOthers)
            {
                return false;
            }
            if ( !compiledAssembly.Name.StartsWith("Unity.") &&
                compiledAssembly.Name.StartsWith("UnityEngine.") )
            {
                return false;
            }


            if (compiledAssembly.Name == "UTJ.StripVariant"){
                return false;
            }
            var isForEditor = compiledAssembly.Defines?.Contains("UNITY_EDITOR") ?? false;
            // Debugクラス読み込み前なので呼ぶと死ぬ
            // Debug.Log("ILPostProcess " + compiledAssembly.Name + "::" + isForEditor);
            //System.IO.File.WriteAllText(compiledAssembly.Name + ".txt", "AA:" + compiledAssembly.Name);
            if (!isForEditor) { return false; }
            return true;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var diagnostics = new List<DiagnosticMessage>();
            if (!WillProcess(compiledAssembly))
            {
                return new ILPostProcessResult(null, diagnostics);
            }

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

            isModified = RemoveImpFromAssemblyDefinition(assemblyDefinition);

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
                return new ILPostProcessResult(new InMemoryAssembly(peData, pdbData), diagnostics);
            }
            return new ILPostProcessResult(null, diagnostics);
        }

        private bool RemoveImpFromAssemblyDefinition(AssemblyDefinition assemblyDefinition)
        {
            if (assemblyDefinition == null) { return false; }
            var flag = false;
            foreach( var module in assemblyDefinition.Modules)
            {
                flag |= RemoveImpFromModuleDefinition(module);
            }
            return flag;
        }

        private bool RemoveImpFromModuleDefinition(ModuleDefinition module)
        {

            var types = module.Types;
            bool flag = false;

            foreach (var type in types)
            {
                var interfaces = type.Interfaces;
                InterfaceImplementation removeInterface = null;
                foreach (var inter in interfaces)
                {
                    if (inter.InterfaceType.FullName == "UnityEditor.Build.IPreprocessShaders")
                    {
                        removeInterface = inter;
                        //ToEmptyMethod(type);
                        flag = true;
                        break;
                    }
                }
                if (removeInterface != null) {
                    type.Interfaces.Remove(removeInterface);
                }
            }
            return flag;
        }

#if false
        private void ToEmptyMethod(TypeDefinition typeDefinition)
        {
            foreach (var method in typeDefinition.Methods)
            {
                if(method.Name == "OnProcessShader")
                {
                    var processor = method.Body.GetILProcessor();
                    // Clearすると実行時に mono_jitでなんか死ぬ…
                    method.Body.Instructions.Clear();

                    method.Body.Instructions.Insert(0, processor.Create(OpCodes.Ret));
                    method.Body.Instructions.Insert(1, processor.Create(OpCodes.Nop));
                }
            }
        }
#endif


    }
}
#endif