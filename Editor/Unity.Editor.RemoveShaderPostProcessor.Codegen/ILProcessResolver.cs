using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.IO;
using System.Linq;
using System;
using System.Threading;

namespace UTJ.ShaderVariantStripping.CodeGen
{
    // Reference netcode for gameobject's code for Resolver logic 
    internal class ILProcessResolver : IAssemblyResolver
    {
        private readonly string[] m_AssemblyReferences;
        private readonly Dictionary<string, AssemblyDefinition> m_AssemblyCache = new Dictionary<string, AssemblyDefinition>();
        private readonly ICompiledAssembly m_CompiledAssembly;
        private AssemblyDefinition m_SelfAssembly;

        private DefaultAssemblyResolver m_DefaultAssemblyResolver;

        public ILProcessResolver(ICompiledAssembly compiledAssembly)
        {
            m_DefaultAssemblyResolver = new DefaultAssemblyResolver();
            m_CompiledAssembly = compiledAssembly;
            m_AssemblyReferences = compiledAssembly.References;
            //System.IO.File.AppendAllText("debuglog.txt", "Start::" + compiledAssembly.Name + "(" + compiledAssembly.References.Length + "\n");
        }

        public void Dispose() { }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {

            try
            {
                var val = m_DefaultAssemblyResolver.Resolve(name);
                if (val != null)
                {
                    return val;
                }
            }
            catch (Exception ex) { 
            
            }
            return Resolve(name, new ReaderParameters(ReadingMode.Deferred));
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            try
            {
                var val = m_DefaultAssemblyResolver.Resolve(name, parameters);
                if (val != null)
                {
                    return val;
                }
            }catch(Exception ex)
            {

            }

            lock (m_AssemblyCache)
            {

                if (name.Name == m_CompiledAssembly.Name)
                {
                    return m_SelfAssembly;
                }

                var fileName = FindFile(name);
                if (fileName == null)
                {
                    return null;
                }

                var lastWriteTime = File.GetLastWriteTime(fileName);
                var cacheKey = $"{fileName}{lastWriteTime}";
                if (m_AssemblyCache.TryGetValue(cacheKey, out var result))
                {
                    return result;
                }

                parameters.AssemblyResolver = this;

                var ms = MemoryStreamFor(fileName);
                var pdb = $"{fileName}.pdb";
                if (File.Exists(pdb))
                {
                    parameters.SymbolStream = MemoryStreamFor(pdb);
                }

                var assemblyDefinition = AssemblyDefinition.ReadAssembly(ms, parameters);
                m_AssemblyCache.Add(cacheKey, assemblyDefinition);

                return assemblyDefinition;
            }
        }

        private string FindFile(AssemblyNameReference name)
        {
            var fileName = m_AssemblyReferences.FirstOrDefault(r => Path.GetFileName(r) == $"{name.Name}.dll");
            return fileName;
        }

        private static MemoryStream MemoryStreamFor(string fileName)
        {
            return Retry(10, TimeSpan.FromSeconds(1), () =>
            {
                byte[] byteArray;
                using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byteArray = new byte[fileStream.Length];
                var readLength = fileStream.Read(byteArray, 0, (int)fileStream.Length);
                if (readLength != fileStream.Length)
                {
                    throw new InvalidOperationException("File read length is not full length of file.");
                }

                return new MemoryStream(byteArray);
            });
        }

        private static MemoryStream Retry(int retryCount, TimeSpan waitTime, Func<MemoryStream> func)
        {
            try
            {
                return func();
            }
            catch (IOException)
            {
                if (retryCount == 0)
                {
                    throw;
                }

                Console.WriteLine($"Caught IO Exception, trying {retryCount} more times");
                Thread.Sleep(waitTime);

                return Retry(retryCount - 1, waitTime, func);
            }
        }

        public void SetSelfAssembly(AssemblyDefinition assemblyDefinition)
        {
            m_SelfAssembly = assemblyDefinition;
        }
    }
}