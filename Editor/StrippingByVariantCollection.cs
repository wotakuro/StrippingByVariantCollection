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
///#define DEBUG_STRIPPING_VARIANT

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Build;
using UnityEngine.Rendering;
using System.Text;
using static UnityEngine.UI.Image;

namespace UTJ.ShaderVariantStripping
{
    public class StrippingByVariantCollection : IPreprocessShaders
    {
        private static StrippingByVariantCollection instance;

        private bool isInitialized = false;
        private Dictionary<Shader,HashSet<ShaderVariantsInfo> > shaderVariants;
        private List<ShaderCompilerData> compileResultBuffer;

        private class SortShaderKeywordComparer : IComparer<ShaderKeyword>
        {
            private Shader shader;
            public SortShaderKeywordComparer(Shader sh)
            {
                this.shader = sh;
            }

            public int Compare(ShaderKeyword x, ShaderKeyword y)
            {
#if UNITY_2021_2_OR_NEWER
                return x.name.CompareTo(y.name);
#else
                return ShaderKeyword.GetKeywordName(shader,x).CompareTo(ShaderKeyword.GetKeywordName(shader, y));
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
                int wordsLength = 0;
                if (words != null)
                {
                    wordsLength = words.Length;
                }
                this.shader = sh;
                this.passType = pass;
                this.keywords = words;
                this.keywordInfos = new List<ShaderKeyword>(wordsLength);
                for (int i = 0; i < wordsLength; ++i)
                {
                    if (string.IsNullOrEmpty(words[i])) { continue; }
                    ShaderKeyword shKeyword = new ShaderKeyword(sh, words[i]);
                    keywordInfos.Add(shKeyword);
                }
                keywordsForCheck = new List<string>();
                foreach (var keywordInfo in keywordInfos)
                {
#if UNITY_2022_2_OR_NEWER
                    if (!string.IsNullOrEmpty(keywordInfo.name) )
                    {
                        keywordsForCheck.Add(keywordInfo.name);
                    }
#else
                    if (!string.IsNullOrEmpty(ShaderKeyword.GetKeywordName(sh, keywordInfo)) &&
                        ShaderKeyword.GetKeywordType(sh, keywordInfo) != ShaderKeywordType.BuiltinDefault)
                    {
                        keywordsForCheck.Add(ShaderKeyword.GetKeywordName(sh, keywordInfo));
                    }
#endif
                }
                keywordsForCheck.Sort();
            }
        }

        private class ShaderVarintInfoComparerer : IEqualityComparer<ShaderVariantsInfo> { 

            public bool Equals(ShaderVariantsInfo x, ShaderVariantsInfo y)
            {
                if (x.shader != y.shader)
                {
                    return false;
                }
                if (x.passType != y.passType)
                {
                    return false;
                }

                if (x.keywordsForCheck == null && y.keywordsForCheck == null) { 
                    return true; 
                }
                if (x.keywordsForCheck == null) { 
                    return false;
                }
                if (y.keywordsForCheck == null) { 
                    return false;
                }

                if (x.keywordsForCheck.Count != y.keywordsForCheck.Count) { 
                    return false;
                }
                int cnt = x.keywordsForCheck.Count;
                for (int i = 0; i < cnt; ++i)
                {
                    if (x.keywordsForCheck[i] != y.keywordsForCheck[i]) {
                        return false; 
                    }
                }
                return true;
            }

            public int GetHashCode(ShaderVariantsInfo obj)
            {
                int hashCode = 0;
                if (obj.shader != null)
                {
                    hashCode += obj.shader.GetHashCode();
                }
                hashCode += obj.passType.GetHashCode();
                if (obj.keywordsForCheck != null)
                {
                    foreach (var keyword in obj.keywordsForCheck)
                    {
                        hashCode += keyword.GetHashCode();
                    }
                }
                return hashCode;
            }
        }




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
            this.compileResultBuffer = new List<ShaderCompilerData>(1024);
            this.shaderVariants = new Dictionary<Shader, HashSet<ShaderVariantsInfo>>();
            foreach (var variantCollection in variantCollections)
            {
                CollectVariants(this.shaderVariants, variantCollection);
            }
            if (StripShaderConfig.IsLogEnable)
            {
                this.InitLogInfo();
                this.SaveProjectVaraiants();
            }
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
            var excludeList = StripShaderConfig.GetExcludeVariantCollectionAsset();
            foreach(var exclude in excludeList)
            {
                if (exclude != null)
                {
                    collections.Remove(exclude);
                }
            }

            return collections;
        }

        private void CollectVariants(Dictionary<Shader, HashSet<ShaderVariantsInfo>> variants,
            ShaderVariantCollection variantCollection)
        {
            var obj = new SerializedObject(variantCollection);
            var shadersProp = obj.FindProperty("m_Shaders");
            for (int i = 0; i < shadersProp.arraySize; ++i)
            {
                var shaderProp = shadersProp.GetArrayElementAtIndex(i);
                var shader = shaderProp.FindPropertyRelative("first").objectReferenceValue as Shader;

                var variantsProp = shaderProp.FindPropertyRelative("second.variants");
                CollectVariants(variants, shader, variantsProp);
            }
        }

        private void CollectVariants(Dictionary<Shader, HashSet<ShaderVariantsInfo>> variants, Shader shader, SerializedProperty variantsProp)
        {
            HashSet<ShaderVariantsInfo> targetHashset = null;
            if(!variants.TryGetValue(shader, out targetHashset))
            {
                targetHashset = new HashSet<ShaderVariantsInfo>(new ShaderVarintInfoComparerer());
                variants.Add(shader, targetHashset);
            }


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
                if (!targetHashset.Contains(variant))
                {
                    targetHashset.Add(variant);
                }
            }
        }


        private bool IsExistShader(Dictionary<Shader, HashSet<ShaderVariantsInfo>> shaderVariants, Shader shader)
        {
            HashSet<ShaderVariantsInfo> variantsHashSet = null;
            if( shaderVariants.TryGetValue(shader, out variantsHashSet) )
            {
                return (variantsHashSet.Count > 0) ;
            }
            return false;
        }

        private bool IsExist(HashSet<ShaderVariantsInfo> variantsHashSet,
            Shader shader, ShaderSnippetData snippet, ShaderCompilerData data,
             ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            var keywords = data.shaderKeywordSet.GetShaderKeywords();
            var compiledKeyword = Convert(shader, keywords);

            // have to include no keyword set.
            if (compiledKeyword.Count == 0)
            {
                return true;
            }

            var targetInfo = new ShaderVariantsInfo(shader, snippet.passType, compiledKeyword.ToArray());
            if (variantsHashSet == null)
            {
                Debug.LogError("variantHashSet is null");
                return false;
            }
            bool flag = (variantsHashSet.Contains(targetInfo));
            if (!flag && StripShaderConfig.IgnoreStageOnlyKeyword )
            {
                bool isRemoved = RemoveStageOnlyKeyword(compiledKeyword,maskGetter);
                if(isRemoved)
                {
                    // only program keyword only
                    if (compiledKeyword.Count == 0)
                    {
                        return true;
                    }

                    var excludePgonlyKeywordTarget = new ShaderVariantsInfo(shader, snippet.passType, compiledKeyword.ToArray());
                    flag |= flag = (variantsHashSet.Contains(targetInfo));
                }
            }

            return flag;
        }

        private bool RemoveStageOnlyKeyword(List<string> compiledKeyword, ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            List<int> removeIndex = new List<int>();
            int count = compiledKeyword.Count;
            for (int i=0;i<count;++i)
            {
                var keyword = compiledKeyword[i];
                if (maskGetter.IsThisProgramTypeOnlyKeyword(keyword))
                {
                    removeIndex.Add(i);
                }
            }

            return (removeIndex.Count > 0);
        }

        private List<string> Convert(Shader shader, ShaderKeyword[] keywords)
        {
            List<string> converted = new List<string>(keywords.Length);
            for (int i = 0; i < keywords.Length; ++i)
            {

#if UNITY_2022_2_OR_NEWER
                string keywordName = keywords[i].name;
#else
                string keywordName = ShaderKeyword.GetKeywordName(shader, keywords[i]);
#endif

#if UNITY_2022_2_OR_NEWER
                    if (!string.IsNullOrEmpty(keywordName) )
                {
                    converted.Add(keywordName);
                }
#else
                if (!string.IsNullOrEmpty( keywordName ) &&
                    ShaderKeyword.GetKeywordType(shader,keywords[i]) != ShaderKeywordType.BuiltinDefault)
                {
                    converted.Add(keywordName);
                }
#endif
            }
            converted.Sort();
            return converted;
        }

        private HashSet<ShaderVariantsInfo> CreateCurrentStageVariantsInfo(HashSet<ShaderVariantsInfo> origin,
            ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            if (!maskGetter.HasCutoffKeywords() )
            {
                return origin;
            }
            HashSet< ShaderVariantsInfo > copyData = new HashSet<ShaderVariantsInfo >(origin, new ShaderVarintInfoComparerer() );
            foreach(var info in origin)
            {
                var newKeywords = maskGetter.ConvertValidOnlyKeywords(info.keywords);
                if(newKeywords == null) { 
                    continue;
                }
                var newVariant = new ShaderVariantsInfo(info.shader, info.passType, newKeywords);
                if(!copyData.Contains(newVariant)){
                    copyData.Add(newVariant);
                }
            }
            return copyData;
        }

#if DEBUG_STRIPPING_VARIANT
        void LogShaderVariantsInfo(string header,Shader shader,ShaderSnippetData data, HashSet<ShaderVariantsInfo> variants)
        {
            StringBuilder stringBuilder = new StringBuilder(1024);
            stringBuilder.Append("---------------").Append(header).AppendLine("---------------");
            stringBuilder.Append("Shader:").AppendLine(shader.name);
            stringBuilder.Append("SubShader-Pass:").Append(data.pass.SubshaderIndex).Append("-").
                Append(data.pass.PassIndex).Append("\n");
            stringBuilder.Append("PassType:").Append(data.passType).Append("\n");
            stringBuilder.Append("ShaderType:").Append(data.shaderType).Append("\n");
            stringBuilder.Append("KeywordList ").Append(variants.Count).Append("\n");
            foreach (var info in variants)
            {
                foreach (var keyword in info.keywords)
                {
                    stringBuilder.Append(keyword).Append(" ");
                }
                stringBuilder.Append("\n");
            }
            System.IO.File.AppendAllText("variant_log.txt", stringBuilder.ToString());
        }
#endif

        public int callbackOrder
        {
            get
            {
                return StripShaderConfig.Order;
            }
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            this.Initialize();

            if (!StripShaderConfig.IsEnable)
            {
                LogAllInVariantColllection(shader, snippet, shaderCompilerData);
                return;
            }

            double startTime = EditorApplication.timeSinceStartup;
            int startVariants = shaderCompilerData.Count;

            ShaderKeywordMaskGetterPerSnippet maskGetter = new ShaderKeywordMaskGetterPerSnippet(shader, snippet);
            if (StripShaderConfig.IgnoreStageOnlyKeyword)
            {
                maskGetter.ConstructOnlyKeyword();
            }

            this.includeVariantsBuffer.Length = 0;
            this.excludeVariantsBuffer.Length = 0;

            bool isExistShader = IsExistShader(this.shaderVariants, shader);
            if (!isExistShader)
            {
                if (StripShaderConfig.StrictVariantStripping)
                {
                    if (StripShaderConfig.IsLogEnable)
                    {
                        LogNotInVariantColllection(shader, snippet, shaderCompilerData);
                    }
                    shaderCompilerData.Clear();
                }
                else if (StripShaderConfig.IsLogEnable)
                {
                    LogAllInVariantColllection(shader, snippet, shaderCompilerData);
                }
                return;
            }

            HashSet<ShaderVariantsInfo> variantsHashSet = null;
            if (shaderVariants.TryGetValue(shader, out variantsHashSet))
            {
#if DEBUG_STRIPPING_VARIANT
                LogShaderVariantsInfo("Before", shader, snippet, variantsHashSet);
#endif
                variantsHashSet = CreateCurrentStageVariantsInfo(variantsHashSet, maskGetter);
#if DEBUG_STRIPPING_VARIANT
                LogShaderVariantsInfo("After", shader, snippet, variantsHashSet);
#endif
            }
            else
            {
                variantsHashSet = null;
            }
            this.compileResultBuffer.Clear();
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                bool isExistsVariant = IsExist(variantsHashSet, shader, snippet, shaderCompilerData[i],maskGetter);

                if (StripShaderConfig.IsLogEnable)
                {
                    if (isExistsVariant)
                    {
                        AppendShaderInfo(includeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
                    }
                    else
                    {
                        AppendShaderInfo(excludeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
                    }
                }

                if (isExistsVariant)
                {
                    this.compileResultBuffer.Add(shaderCompilerData[i]);
                }
            }

            shaderCompilerData.Clear();
            foreach (var data in this.compileResultBuffer)
            {
                shaderCompilerData.Add(data);
            }

            if (StripShaderConfig.IsLogEnable)
            {

                double endTime = EditorApplication.timeSinceStartup;
                int endVariants = shaderCompilerData.Count;

                string dir = LogDirectory + "/" + dateTimeStr;
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                var tmpSb = new StringBuilder();
                tmpSb.Append("Info:").Append(snippet.shaderType).Append(" pass").
                    Append(snippet.pass.SubshaderIndex).Append("-").Append(snippet.pass.PassIndex).Append(" ").
                    Append(snippet.passType).Append(" \"").Append(snippet.passName).Append("\"\n");
                tmpSb.Append("ExecuteTime:").Append(endTime - startTime).Append(" sec\n").
                    Append("Variants:").Append(startVariants).Append("->").Append(endVariants).Append("\n");

                System.IO.File.AppendAllText(dir + "/" + shader.name.Replace("/", "_") + "_execute.log",
                    tmpSb.ToString() );

                var data = new ShaderInfoData(shader, snippet);
                if (!alreadyWriteShader.Contains(data))
                {
                    SaveResult(shader, snippet);
                    LogKeywordMask(maskGetter);
                    alreadyWriteShader.Add(data);
                }
            }
        }


        #region LOGGING
        private const string LogDirectory = "ShaderVariants/Builds";

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
            var list = new List<ShaderVariantsInfo>(1024);
            foreach(var variantHashSet in this.shaderVariants.Values)
            {
                foreach(var val in variantHashSet)
                {
                    list.Add(val);
                }
            }
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

            string dir = LogDirectory + "/" + dateTimeStr;
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(dir + "/ProjectVariants.txt", projectVaritantsBuffer.ToString());
        }

        private void SaveResult(Shader shader, ShaderSnippetData snippet)
        {
            string shaderName = shader.name.Replace("/", "_");
            string includeDir = LogDirectory + "/" + dateTimeStr + "/Include/" + shaderName;
            string excludeDir = LogDirectory + "/" + dateTimeStr + "/Exclude/" + shaderName;
            string name = shaderName + "_" + snippet.shaderType.ToString() + "_" + snippet.passName + "_" + snippet.passType;

            if (includeVariantsBuffer.Length != 0)
            {
                if (!System.IO.Directory.Exists(includeDir))
                {
                    System.IO.Directory.CreateDirectory(includeDir);
                }
                this.includeVariantsBuffer.Append("\n==================\n");
                System.IO.File.AppendAllText(System.IO.Path.Combine(includeDir, name) + ".txt", includeVariantsBuffer.ToString());
            }
            if (excludeVariantsBuffer.Length != 0)
            {
                if (!System.IO.Directory.Exists(excludeDir))
                {
                    System.IO.Directory.CreateDirectory(excludeDir);
                }
                this.excludeVariantsBuffer.Append("\n==================\n");
                System.IO.File.AppendAllText(System.IO.Path.Combine(excludeDir, name) + ".txt", excludeVariantsBuffer.ToString());
            }
        }

        private void LogKeywordMask(ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            string maskLogDir = LogDirectory + "/" + dateTimeStr + "/KeywordLog/" ;

            if (!System.IO.Directory.Exists(maskLogDir))
            {
                System.IO.Directory.CreateDirectory(maskLogDir);
            }
            var path = System.IO.Path.Combine(maskLogDir, maskGetter.LogFileName);
            System.IO.File.AppendAllText(path, maskGetter.GetLogStr());
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
            System.Array.Sort(sortKeywords, new SortShaderKeywordComparer(shader));
            sb.Append(" Keyword:");
            foreach (var keyword in sortKeywords)
            {
#if UNITY_2021_2_OR_NEWER
                sb.Append(keyword.name).Append(" ");
#else
                sb.Append( ShaderKeyword.GetKeywordName(shader,keyword)).Append(" ");
#endif
            }



            sb.Append("\n KeywordType:");
            foreach (var keyword in sortKeywords)
            {
#if UNITY_2022_2_OR_NEWER
                if (!ShaderKeyword.IsKeywordLocal(keyword))
                {
                    sb.Append(ShaderKeyword.GetGlobalKeywordType(keyword)).Append(" ");
                }
                else
                {
                    var localKeyword = new LocalKeyword(shader, keyword.name);
                    sb.Append(localKeyword.type).Append(" ");
                }
#else
                sb.Append(ShaderKeyword.GetKeywordType(shader, keyword)).Append(" ");
#endif
            }


#if UNITY_2022_2_OR_NEWER
            sb.Append("\n LocalkeywordInfo:");
            foreach (var keyword in sortKeywords)
            {
                if (!ShaderKeyword.IsKeywordLocal(keyword))
                {
                    sb.Append("Global ");
                }
                else
                {
                    var localKeyword = new LocalKeyword(shader, keyword.name);
                    if (localKeyword.isDynamic)
                    {
                        sb.Append("Dynamic-");
                    }
                    else
                    {
                        sb.Append("Static-");
                    }
                    if (localKeyword.isOverridable)
                    {
                        sb.Append("isOverridable ");
                    }
                    else
                    {
                        sb.Append("nonOverridable ");
                    }
                }
            }
#else
            sb.Append("\n IsLocalkeyword:");
            foreach (var keyword in sortKeywords)
            {
                sb.Append(ShaderKeyword.IsKeywordLocal(keyword)).Append(" ");
            }
#endif
            sb.Append("\n").Append("\n");
        }



        private void LogAllInVariantColllection(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {

            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                AppendShaderInfo(includeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
            }
            this.includeVariantsBuffer.Append("\n==================\n");

            string filePath;
            GetPathNames(LogDirectory + "/" + dateTimeStr + "/Include/", shader, snippet, out filePath);
            System.IO.File.AppendAllText(filePath, includeVariantsBuffer.ToString());
        }
        private void LogNotInVariantColllection(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }

            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                AppendShaderInfo(excludeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
            }
            this.excludeVariantsBuffer.Append("\n==================\n");

            string filePath;
            GetPathNames(LogDirectory + "/" + dateTimeStr + "/Exclude/", shader, snippet,out filePath);
            System.IO.File.AppendAllText(filePath, excludeVariantsBuffer.ToString());
        }

        private static void GetPathNames(string dirHead, Shader shader, ShaderSnippetData snippet,out string filePath)
        {
            var shortShaderName = shader.name.Replace('|', '-');
            int lastSlashIndex = shortShaderName.LastIndexOf('/');
            if (lastSlashIndex != -1)
            {
                shortShaderName = shortShaderName.Substring(lastSlashIndex + 1);
            }
            string shaderName = shader.name.Replace('/', '_').Replace('|', '-');
            string name = shortShaderName + "_" + snippet.shaderType.ToString() + "_" + snippet.passName + "_" + snippet.passType;
            string dir = dirHead + shaderName;

            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            Debug.Log(dir + ";;" + name +":::");
            filePath = System.IO.Path.Combine(dir, name) + ".txt";
        }
        #endregion LOGGING
    }
}