
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Build;
using UnityEngine.Rendering;
using System.Text;
using static UTJ.ShaderVariantStripping.StripProcessShaders;

namespace UTJ.ShaderVariantStripping
{

    public class ProjectSVCData
    {

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
                    if (!string.IsNullOrEmpty(keywordInfo.name))
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

        private Dictionary<Shader, HashSet<ShaderVariantsInfo>> shaderVariants;


        internal void Initialize()
        {
            var variantCollections = GetProjectShaderVariantCollections();
            this.shaderVariants = new Dictionary<Shader, HashSet<ShaderVariantsInfo>>();
            foreach (var variantCollection in variantCollections)
            {
                CollectVariants(this.shaderVariants, variantCollection);
            }
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
            foreach (var exclude in excludeList)
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
            if (!variants.TryGetValue(shader, out targetHashset))
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


        internal bool IsExistSVC(Shader shader)
        {
            return this.IsExistSVC(this.shaderVariants, shader);
        }
        private bool IsExistSVC(Dictionary<Shader, HashSet<ShaderVariantsInfo>> shaderVariants, Shader shader)
        {
            HashSet<ShaderVariantsInfo> variantsHashSet = null;
            if (shaderVariants.TryGetValue(shader, out variantsHashSet))
            {
                return (variantsHashSet.Count > 0);
            }
            return false;
        }

        internal bool IsExistInSVC(HashSet<ShaderVariantsInfo> variantsHashSet,
            Shader shader, ShaderSnippetData snippet, ShaderCompilerData data,
             ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            var keywords = data.shaderKeywordSet.GetShaderKeywords();
            var compiledKeyword = ConvertShaderKeyword(shader, keywords);

            // have to include no keyword set.
            if (compiledKeyword.Count == 0)
            {
                return true;
            }

            var targetInfo = new ShaderVariantsInfo(shader, snippet.passType, compiledKeyword.ToArray());
            if (variantsHashSet == null)
            {
                Debug.LogWarning("variantHashSet is null");
                return false;
            }
            bool flag = (variantsHashSet.Contains(targetInfo));
            if (!flag && StripShaderConfig.IgnoreStageOnlyKeyword)
            {
                bool isRemoved = RemoveStageOnlyKeyword(compiledKeyword, maskGetter);
                if (isRemoved)
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
            for (int i = 0; i < count; ++i)
            {
                var keyword = compiledKeyword[i];
                if (maskGetter.IsThisProgramTypeOnlyKeyword(keyword))
                {
                    removeIndex.Add(i);
                }
            }

            return (removeIndex.Count > 0);
        }

        private List<string> ConvertShaderKeyword(Shader shader, ShaderKeyword[] keywords)
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
                if (!string.IsNullOrEmpty(keywordName))
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

        internal HashSet<ShaderVariantsInfo> GetVariantsHashSet(Shader shader, ShaderKeywordMaskGetterPerSnippet maskGetter)
        {

            HashSet<ProjectSVCData.ShaderVariantsInfo> variantsHashSet = null;
            if (shaderVariants.TryGetValue(shader, out variantsHashSet))
            {
                //shaderVariantStripLogger.LogShaderVariantsInfo("Before", shader, snippet, variantsHashSet);
                variantsHashSet = ProjectSVCData.CreateCurrentStageVariantsInfo(variantsHashSet, maskGetter);
                //shaderVariantStripLogger.LogShaderVariantsInfo("After", shader, snippet, variantsHashSet);
            }
            return variantsHashSet;
        }

        private static HashSet<ShaderVariantsInfo> CreateCurrentStageVariantsInfo(HashSet<ShaderVariantsInfo> origin,
            ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            if (!maskGetter.HasCutoffKeywords())
            {
                return origin;
            }
            HashSet<ShaderVariantsInfo> copyData = new HashSet<ShaderVariantsInfo>(origin, new ShaderVarintInfoComparer());
            foreach (var info in origin)
            {
                var newKeywords = maskGetter.ConvertValidOnlyKeywords(info.keywords);
                if (newKeywords == null)
                {
                    continue;
                }
                var newVariant = new ShaderVariantsInfo(info.shader, info.passType, newKeywords);
                if (!copyData.Contains(newVariant))
                {
                    copyData.Add(newVariant);
                }
            }
            return copyData;
        }


        internal string GetDebugStr()
        {
            StringBuilder projectVaritantsBuffer = new StringBuilder(1024 * 16);
            StringBuilder shaderKeywordBuffer0 = new StringBuilder();
            StringBuilder shaderKeywordBuffer1 = new StringBuilder();

            var list = new List<ShaderVariantsInfo>(1024);
            foreach (var variantHashSet in this.shaderVariants.Values)
            {
                foreach (var val in variantHashSet)
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
            return projectVaritantsBuffer.ToString();
        }


    }
}
