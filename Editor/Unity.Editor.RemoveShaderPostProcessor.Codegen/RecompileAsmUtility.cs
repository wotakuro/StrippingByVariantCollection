#if true 


using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.IO;
using System.Text;

namespace UTJ.ShaderVariantStripping.CodeGen
{
    public class RecompileAsmUtility
    {
        public struct AssemblyInfo
        {
            public string asmName;
            public string asmDefPath;
        }

        public static readonly string TempCompileTargetFile = "Temp/com.utj.stripvariants.asmtarget.txt";


        public static void Check()
        {
            var target = GetRecompileTarget(true);
            foreach (var asm in target) { 
                Debug.Log(asm.asmName + "::" + asm.asmDefPath);
            }
            string path = "Temp/com.utj.stripvariant.tmp.txt";
            WriteFile( path,target);
            var read = ReadFromFile(path);
            foreach (var asm in read)
            {
                Debug.Log("read:" + asm.asmName + "::" + asm.asmDefPath);
            }
        }

        public static bool RewriteFile(string path,List<AssemblyInfo> list , string removeAsmName)
        {
            int reoveIdx = -1;
            int cnt = list.Count;
            for(int i = 0;i < cnt;++i) {
                if(list[i].asmName == removeAsmName)
                {
                    reoveIdx = i;
                    break;
                }
            }
            if(reoveIdx >= 0)
            {
                list.RemoveAt(reoveIdx);
                WriteFile(path, list);
                return true;
            }
            return false;
        }

        public static List<AssemblyInfo> ReadFromFile(string path)
        {
            var result = new List<AssemblyInfo>();
            if (!File.Exists(path))
            {
                return result;
            }
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            foreach( var line in lines)
            {
                var separateIdx = line.IndexOf(',');
                if(separateIdx < 0) { continue; }
                AssemblyInfo info = new AssemblyInfo()
                {
                    asmName = line.Substring(0, separateIdx),
                    asmDefPath = line.Substring(separateIdx + 1, line.Length - separateIdx -1),
                };
                result.Add(info);
            }
            return result;
        }
        public static void WriteFile(string path, List<AssemblyInfo> list)
        {
            var stringBuilder = new StringBuilder(1024);
            bool isFirst = true;
            foreach (var asm in list)
            {
                if (!isFirst)
                {
                    stringBuilder.Append("\n");
                }
                stringBuilder.Append(asm.asmName).Append(",").Append(asm.asmDefPath);
                isFirst = false;
            }
            File.WriteAllText(path, stringBuilder.ToString());
        }

        public static List<AssemblyInfo> GetRecompileTarget(bool unityOnly)
        {
            var result = new List<AssemblyInfo>();
            {
                var guids = AssetDatabase.FindAssets("t:asmdef", new string[] { "Packages" });
                var classes = GetInterfaceAssemblies();
                List<MethodInfo> methods = new List<MethodInfo>();
                var asms = GetAsmDefs();
                foreach (var asm in classes)
                {
                    var name = asm.GetName().Name;
                    if(unityOnly && 
                        ( !name.StartsWith("Unity.") &&
                        !name.StartsWith("UnityEngine.") &&
                        !name.StartsWith("UnityEditor."))){
                        continue;
                    }

                    UnityEditorInternal.AssemblyDefinitionAsset asset;
                    if (asms.TryGetValue(name, out asset))
                    {
                        var info = new AssemblyInfo { asmName = name, asmDefPath = AssetDatabase.GetAssetPath(asset) };
                        result.Add(info);
                    }

                }
            }
            return result;
        }

        private static Dictionary<string , UnityEditorInternal.AssemblyDefinitionAsset> GetAsmDefs()
        {
            var dictionary = new Dictionary<string, UnityEditorInternal.AssemblyDefinitionAsset>();
            var guids = AssetDatabase.FindAssets("t:asmdef", new string[] { "Assets","Packages" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(path);
                dictionary.Add(obj.name, obj);
            }
            return dictionary;
        }


        private static List<Assembly> GetInterfaceAssemblies()
        {
            var result = new List<Assembly>();
            var asms = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                var types = asm.GetTypes();
                foreach (var t in types)
                {
                    if(t.IsInterface) { continue; }
                    var method = t.GetMethod("OnProcessShader", BindingFlags.Public | BindingFlags.Instance);
                    if (method != null )
                    {
                        var args = method.GetParameters();
                        if (args != null && args.Length == 3)
                        {
                            result.Add(asm);
                            break;
                        }
                    }
                }
            }
            return result;
        }


    }
}
#endif