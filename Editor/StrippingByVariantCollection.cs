/*
MIT License
Copyright (c) 2020 Yusuke Kurokawa
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#define DEBUG_LOG_STRIPPING_VARIANT

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Build;
using UnityEngine.Rendering;
using System.Text;

namespace UTJ
{
    public class StrippingByVariantCollection : IPreprocessShaders
    {
        private bool isInitialized = false;
        private static StrippingByVariantCollection instance;
        private List<ShaderVariantsInfo> shaderVariants;

        private class SortShaderKeyword : IComparer<ShaderKeyword>
        {
            private Shader shader;
            public SortShaderKeyword(Shader sh)
            {
                this.shader = sh;
            }

            public int Compare(ShaderKeyword x, ShaderKeyword y)
            {
#if UNITY_2019_3_OR_NEWER
                return ShaderKeyword.GetKeywordName(shader,x).CompareTo(ShaderKeyword.GetKeywordName(shader, y));
#else
                return x.GetKeywordName().CompareTo(y.GetKeywordName());
#endif
            }
        }

        public struct ShaderVariantsInfo
        {
            public Shader shader;
            public PassType passType;
            public string[] keywords;
            private List<ShaderKeyword> keywordInfos;

            public List<string> keywordsForCheck;

            public ShaderVariantsInfo(Shader sh, PassType pass, string[] words)
            {
                this.shader = sh;
                this.passType = pass;
                this.keywords = words;
                this.keywordInfos = new List<ShaderKeyword>(words.Length);
                for (int i = 0; i < words.Length; ++i)
                {
                    if (string.IsNullOrEmpty(words[i])) { continue; }
#if UNITY_2019 || UNITY_2019_3_OR_NEWER
                    ShaderKeyword shKeyword = new ShaderKeyword(sh, words[i]);
#else
                ShaderKeyword shKeyword = new ShaderKeyword(words[i]);
#endif
                    keywordInfos.Add(shKeyword);
                }
                keywordsForCheck = new List<string>();
                foreach (var keywordInfo in keywordInfos)
                {
#if UNITY_2019_3_OR_NEWER
                    if (!string.IsNullOrEmpty( ShaderKeyword.GetKeywordName(sh, keywordInfo)) &&
                        ShaderKeyword.GetKeywordType( sh,keywordInfo) != ShaderKeywordType.BuiltinAutoStripped)
                    {
                        keywordsForCheck.Add( ShaderKeyword.GetKeywordName(sh, keywordInfo) );
                    }
#else
                    if (!string.IsNullOrEmpty(keywordInfo.GetKeywordName()) &&
                        keywordInfo.GetKeywordType() != ShaderKeywordType.BuiltinAutoStripped)
                    {
                        keywordsForCheck.Add(keywordInfo.GetKeywordName());
                    }
#endif
                }
                keywordsForCheck.Sort();
            }
        }

#if DEBUG_LOG_STRIPPING_VARIANT
        private const string SaveDirectory = "ShaderVariants";

        private string dateTimeStr;

        private StringBuilder includeVariantsBuffer;
        private StringBuilder excludeVariantsBuffer;
        private StringBuilder shaderKeywordBuffer0;
        private StringBuilder shaderKeywordBuffer1;
        private StringBuilder projectVaritantsBuffer;

        private struct ShaderInfoData
        {
            public Shader shader;
            public ShaderSnippetData snippetData;

            public ShaderInfoData(Shader sh, ShaderSnippetData data)
            {
                this.shader = sh;
                this.snippetData = data;
            }
            public override int GetHashCode()
            {
                return this.shader.name.GetHashCode() + this.snippetData.passName.GetHashCode() +
                    this.snippetData.passType.GetHashCode() + this.snippetData.shaderType.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                ShaderInfoData data = (ShaderInfoData)obj;
                if (data.shader != this.shader)
                {
                    return false;
                }
                if (this.snippetData.passName == data.snippetData.passName &&
                   this.snippetData.passType == data.snippetData.passType &&
                   this.snippetData.shaderType == data.snippetData.shaderType)
                {
                    return true;
                }

                return false;
            }
        }

        private HashSet<ShaderInfoData> alreadyWriteShader = new HashSet<ShaderInfoData>();
#endif

        public StrippingByVariantCollection()
        {
            instance = this;
            Initialize();
        }

        public static void ResetData()
        {
            if(instance != null)
            {
                instance.isInitialized = false;
            }
        }

        private void Initialize()
        {
            if (isInitialized) { return; }
            var variantCollections = GetProjectShaderVariantCollections();
            this.shaderVariants = new List<ShaderVariantsInfo>();
            foreach (var variantCollection in variantCollections)
            {
                CollectVariants(this.shaderVariants, variantCollection);
            }
#if DEBUG_LOG_STRIPPING_VARIANT
            this.InitLogInfo();
            this.SaveProjectVaraiants();
#endif
            isInitialized = true;
        }

        private static List<ShaderVariantCollection> GetProjectShaderVariantCollections()
        {
            List<ShaderVariantCollection> collections = new List<ShaderVariantCollection>();
            var guids = AssetDatabase.FindAssets("t: ShaderVariantCollection");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);
                if (obj != null)
                {
                    collections.Add(obj);
                }
            }
            return collections;
        }

        private void CollectVariants(List<ShaderVariantsInfo> shaderVariants,
            ShaderVariantCollection variantCollection)
        {
            var obj = new SerializedObject(variantCollection);
            var shadersProp = obj.FindProperty("m_Shaders");
            for (int i = 0; i < shadersProp.arraySize; ++i)
            {
                var shaderProp = shadersProp.GetArrayElementAtIndex(i);
                var shader = shaderProp.FindPropertyRelative("first").objectReferenceValue as Shader;

                var variantsProp = shaderProp.FindPropertyRelative("second.variants");
                CollectVariants(shaderVariants, shader, variantsProp);
            }
        }

        private void CollectVariants(List<ShaderVariantsInfo> shaderVariants, Shader shader, SerializedProperty variantsProp)
        {
            for (int i = 0; i < variantsProp.arraySize; ++i)
            {
                var variantProp = variantsProp.GetArrayElementAtIndex(i);
                var keywords = variantProp.FindPropertyRelative("keywords").stringValue;
                var passType = variantProp.FindPropertyRelative("passType").intValue;

                string[] keywordsArray = null;
                if (keywords != null)
                {
                    keywords = keywords.Trim();
                }
                if (string.IsNullOrEmpty(keywords))
                {
                    keywordsArray = new string[] { "" };
                }
                else
                {
                    keywordsArray = keywords.Split(' ');
                }
                ShaderVariantsInfo variant = new ShaderVariantsInfo(shader, (PassType)passType, keywordsArray);
                shaderVariants.Add(variant);
            }
        }


        private bool IsExistShader(List<ShaderVariantsInfo> shaderVariants, Shader shader)
        {
            foreach (var variant in shaderVariants)
            {
                if (variant.shader == shader)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsExist(List<ShaderVariantsInfo> shaderVariants,
            Shader shader, ShaderSnippetData snippet, ShaderCompilerData data)
        {
            var keywords = data.shaderKeywordSet.GetShaderKeywords();
            var compiledKeyword = Convert(shader,keywords);

            if (compiledKeyword.Count == 0)
            {
                return true;
            }
            foreach (var variant in shaderVariants)
            {
                if (variant.shader != shader)
                {
                    continue;
                }
                if (variant.passType != snippet.passType)
                {
                    continue;
                }
                if (IsMatch(variant.keywordsForCheck, compiledKeyword))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsMatch(List<string> a, List<string> b)
        {
            if (a.Count != b.Count) { return false; }

            for (int i = 0; i < a.Count; ++i)
            {
                if (a[i] != b[i]) { return false; }
            }
            return true;
        }
        private List<string> Convert(Shader shader, ShaderKeyword[] keywords)
        {
            List<string> converted = new List<string>(keywords.Length);
            for (int i = 0; i < keywords.Length; ++i)
            {
#if UNITY_2019_3_OR_NEWER
                if (!string.IsNullOrEmpty( ShaderKeyword.GetKeywordName(shader, keywords[i]) ) &&
                    ShaderKeyword.GetKeywordType(shader,keywords[i]) != ShaderKeywordType.BuiltinAutoStripped)
                {
                    converted.Add(ShaderKeyword.GetKeywordName(shader, keywords[i]));
                }
#else
                if (!string.IsNullOrEmpty(keywords[i].GetKeywordName()) &&
                    keywords[i].GetKeywordType() != ShaderKeywordType.BuiltinAutoStripped)
                {
                    converted.Add(keywords[i].GetKeywordName());
                }
#endif
            }
            converted.Sort();
            return converted;
        }

        public int callbackOrder
        {
            get
            {
                return int.MaxValue;
            }
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            this.Initialize();

#if DEBUG_LOG_STRIPPING_VARIANT
            this.includeVariantsBuffer.Length = 0;
            this.excludeVariantsBuffer.Length = 0;
#endif
            bool isExistShader = IsExistShader(this.shaderVariants, shader);
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                bool shouldRemove = false;
                if (isExistShader && StripEnableChecker.IsEnable)
                {
                    bool isExistsVariant = IsExist(this.shaderVariants, shader, snippet, shaderCompilerData[i]);
                    shouldRemove = !isExistsVariant;
                }
#if DEBUG_LOG_STRIPPING_VARIANT
                if (shouldRemove)
                {
                    AppendShaderInfo(excludeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
                }
                else
                {
                    AppendShaderInfo(includeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
                }
#endif

                if (shouldRemove)
                {
                    shaderCompilerData.RemoveAt(i);
                    --i;
                }
            }
#if DEBUG_LOG_STRIPPING_VARIANT
            var data = new ShaderInfoData(shader, snippet);
            if (!alreadyWriteShader.Contains(data))
            {
                SaveResult(shader, snippet);
                alreadyWriteShader.Add(data);
            }
#endif
        }





#if DEBUG_LOG_STRIPPING_VARIANT

        private void InitLogInfo()
        {
            var dateTime = System.DateTime.Now;
            this.dateTimeStr = dateTime.ToString("yyyyMMdd_HHmmss");
            this.alreadyWriteShader.Clear();

            // string builder
            this.includeVariantsBuffer = new StringBuilder(1024);
            this.excludeVariantsBuffer = new StringBuilder(1024);
            this.shaderKeywordBuffer0 = new StringBuilder();
            this.shaderKeywordBuffer1 = new StringBuilder();
            this.projectVaritantsBuffer = new StringBuilder();
        }

        private void SaveProjectVaraiants()
        {
            var list = new List<ShaderVariantsInfo>(shaderVariants);
            list.Sort((a, b) =>
            {
                int shaderName = a.shader.name.CompareTo(b.shader.name);
                if (shaderName != 0)
                {
                    return shaderName;
                }

                int passType = a.passType - b.passType;
                if (passType != 0)
                {
                    return passType;
                }

                int keywordLengthVal = a.keywords.Length - b.keywords.Length;
                if (keywordLengthVal != 0)
                {
                    return keywordLengthVal;
                }

                shaderKeywordBuffer0.Length = 0;
                shaderKeywordBuffer1.Length = 0;

                foreach (var keyword in a.keywords)
                {
                    shaderKeywordBuffer0.Append(keyword).Append(" ");
                }
                foreach (var keyword in b.keywords)
                {
                    shaderKeywordBuffer1.Append(keyword).Append(" ");
                }

                return shaderKeywordBuffer0.ToString().CompareTo(shaderKeywordBuffer1.ToString());
            });

            string shName = null;
            foreach (var variant in list)
            {
                if (shName != variant.shader.name)
                {
                    projectVaritantsBuffer.Append(variant.shader.name);
                    projectVaritantsBuffer.Append("\n");
                    shName = variant.shader.name;
                }
                projectVaritantsBuffer.Append(" type:").
                    Append(variant.passType).Append("\n").Append(" keyword:");
                foreach (var keyword in variant.keywords)
                {
                    projectVaritantsBuffer.Append(keyword).Append(" ");
                }
                projectVaritantsBuffer.Append("\n\n");
            }

            string dir = SaveDirectory + "/" + dateTimeStr;
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(dir + "/ProjectVariants.txt", projectVaritantsBuffer.ToString());
        }

        private void SaveResult(Shader shader, ShaderSnippetData snippet)
        {
            string shaderName = shader.name.Replace("/", "_");
            string includeDir = SaveDirectory + "/" + dateTimeStr + "/Include/" + shaderName;
            string excludeDir = SaveDirectory + "/" + dateTimeStr + "/Exclude/" + shaderName;
            string name = shaderName + "_" + snippet.shaderType.ToString() + "_" + snippet.passName + "_" + snippet.passType;

            if (includeVariantsBuffer.Length != 0)
            {
                if (!System.IO.Directory.Exists(includeDir))
                {
                    System.IO.Directory.CreateDirectory(includeDir);
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(includeDir, name) + ".txt", includeVariantsBuffer.ToString());
            }
            if (excludeVariantsBuffer.Length != 0)
            {
                if (!System.IO.Directory.Exists(excludeDir))
                {
                    System.IO.Directory.CreateDirectory(excludeDir);
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(excludeDir, name) + ".txt", excludeVariantsBuffer.ToString());
            }
        }

        private void AppendShaderInfo(StringBuilder sb, Shader shader, ShaderSnippetData snippet, ShaderCompilerData compilerData)
        {
            if (sb.Length == 0)
            {
                sb.Append("Shader:" + shader.name).Append("\n");
                sb.Append("ShaderType:").Append(snippet.shaderType).Append("\n").
                    Append("PassName:").Append(snippet.passName).Append("\n").
                    Append("PassType:").Append(snippet.passType).Append("\n\n");
            }

            var keywords = compilerData.shaderKeywordSet.GetShaderKeywords();

            var sortKeywords = new ShaderKeyword[keywords.Length];
            for (int i = 0; i < keywords.Length; ++i)
            {
                sortKeywords[i] = keywords[i];
            }
            System.Array.Sort(sortKeywords, new SortShaderKeyword(shader));
            sb.Append(" Keyword:");
            foreach (var keyword in sortKeywords)
            {
#if UNITY_2019_3_OR_NEWER
                sb.Append( ShaderKeyword.GetKeywordName(shader,keyword)).Append(" ");
#else
                sb.Append(keyword.GetKeywordName()).Append(" ");
#endif
            }

            sb.Append("\n KeywordType:");
            foreach (var keyword in sortKeywords)
            {
#if UNITY_2019_3_OR_NEWER
                sb.Append(ShaderKeyword.GetKeywordType(shader, keyword)).Append(" ");
#else
                sb.Append(keyword.GetKeywordType()).Append(" ");
#endif
            }
#if UNITY_2019_3_OR_NEWER
            sb.Append("\n IsLocalkeyword:");
            foreach (var keyword in sortKeywords)
            {
                sb.Append(ShaderKeyword.IsKeywordLocal(keyword)).Append(" ");
            }
#endif
            sb.Append("\n").Append("\n");
        }
#endif
    }
}