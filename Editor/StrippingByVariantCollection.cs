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


namespace UTJ.ShaderVariantStripping
{
    public class StrippingByVariantCollection : IPreprocessShaders
    {
        private static StrippingByVariantCollection instance;

        private bool isInitialized = false;
        private Dictionary<Shader,HashSet<ShaderVariantsInfo> > shaderVariants;
        private List<ShaderCompilerData> compileResultBuffer;

        private ShaderVariantStripLogger shaderVariantStripLogger = new ShaderVariantStripLogger();


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
                shaderVariantStripLogger.InitLogInfo();
                shaderVariantStripLogger.SaveProjectVaraiants(this.shaderVariants);
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
                targetHashset = new HashSet<ShaderVariantsInfo>(new ShaderVarintInfoComparer());
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


        private bool IsExistSVC(Dictionary<Shader, HashSet<ShaderVariantsInfo>> shaderVariants, Shader shader)
        {
            HashSet<ShaderVariantsInfo> variantsHashSet = null;
            if( shaderVariants.TryGetValue(shader, out variantsHashSet) )
            {
                return (variantsHashSet.Count > 0) ;
            }
            return false;
        }

        private bool IsExistInSVC(HashSet<ShaderVariantsInfo> variantsHashSet,
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
            HashSet< ShaderVariantsInfo > copyData = new HashSet<ShaderVariantsInfo >(origin, new ShaderVarintInfoComparer() );
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
                shaderVariantStripLogger.LogAllInVariantColllection(shader, snippet, shaderCompilerData);
                return;
            }

            double startTime = EditorApplication.timeSinceStartup;
            int startVariants = shaderCompilerData.Count;

            ShaderKeywordMaskGetterPerSnippet maskGetter = new ShaderKeywordMaskGetterPerSnippet(shader, snippet);
            if (StripShaderConfig.IgnoreStageOnlyKeyword)
            {
                maskGetter.ConstructOnlyKeyword();
            }
            shaderVariantStripLogger.ClearStringBuffers();

            bool isExistShader = IsExistSVC(this.shaderVariants, shader);
            if (!isExistShader)
            {
                if (StripShaderConfig.StrictVariantStripping)
                {
                    shaderVariantStripLogger.LogNotInVariantColllection(shader, snippet, shaderCompilerData);
                    shaderCompilerData.Clear();
                }
                else
                {
                    shaderVariantStripLogger.LogAllInVariantColllection(shader, snippet, shaderCompilerData);
                }
                return;
            }

            HashSet<ShaderVariantsInfo> variantsHashSet = null;
            if (shaderVariants.TryGetValue(shader, out variantsHashSet))
            {
#if DEBUG_STRIPPING_VARIANT
                shaderVariantStripLogger.LogShaderVariantsInfo("Before", shader, snippet, variantsHashSet);
#endif
                variantsHashSet = CreateCurrentStageVariantsInfo(variantsHashSet, maskGetter);
#if DEBUG_STRIPPING_VARIANT
                shaderVariantStripLogger.LogShaderVariantsInfo("After", shader, snippet, variantsHashSet);
#endif
            }
            else
            {
                variantsHashSet = null;
            }

            // Set ShaderCompilerData List
            this.compileResultBuffer.Clear();
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                bool isExistsVariant = IsExistInSVC(variantsHashSet, shader, snippet, shaderCompilerData[i],maskGetter);

                /// log 
                if (isExistsVariant)
                {
                    shaderVariantStripLogger.AppendIncludeShaderInfo(shader, snippet, shaderCompilerData[i]);
                    this.compileResultBuffer.Add(shaderCompilerData[i]);
                }
                else
                {
                    shaderVariantStripLogger.AppendExcludeShaderInfo(shader, snippet, shaderCompilerData[i]);
                }
            }

            // CreateList
            shaderCompilerData.Clear();
            foreach (var data in this.compileResultBuffer)
            {
                shaderCompilerData.Add(data);
            }

            shaderVariantStripLogger.LogResult(shader, snippet, shaderCompilerData, maskGetter, startTime, startVariants);
        }


    }
}