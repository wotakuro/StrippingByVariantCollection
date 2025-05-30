using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static UTJ.ShaderVariantStripping.ProjectSVCData;


namespace UTJ.ShaderVariantStripping
{
    public class ProjectGSCData 
    {


        internal class GraphicsStateVariantData
        {
            public Shader shader;
            public PassIdentifier passIdentifier;
            private LocalKeyword[] localKeywords;
            public List<GraphicsStateKeyData> stateKeyData;

            public List<string> keywordsForCheck;

            public GraphicsStateVariantData(GraphicsStateCollection.ShaderVariant variant, GraphicsStateKeyData keyData)
            {
                this.shader = variant.shader;
                this.passIdentifier = variant.passId;
                this.localKeywords = variant.keywords;
                this.stateKeyData = new List<GraphicsStateKeyData>();
                this.stateKeyData.Add(keyData);
                this.ConstructKeeywordForCheck();
            }

            public void SetupFromCompilerData(Shader shader,
                ref ShaderSnippetData snippetData,ref ShaderCompilerData compilerData)
            {
                this.shader = shader;
                this.passIdentifier = new PassIdentifier(snippetData.pass.SubshaderIndex, snippetData.pass.PassIndex);
                var keywords = compilerData.shaderKeywordSet.GetShaderKeywords();
                this.localKeywords = null;
                if(this.keywordsForCheck == null)
                {
                    this.keywordsForCheck = new List<string>(32);
                }
                this.keywordsForCheck.Clear();
                for (int i = 0; i < keywords.Length; ++i)
                {
                    string keywordName = keywords[i].name;

                    if (!string.IsNullOrEmpty(keywordName))
                    {
                        keywordsForCheck.Add(keywordName);
                    }
                }
                this.keywordsForCheck.Sort();
                this.stateKeyData = null;
            }

             
            private void ConstructKeeywordForCheck()
            {

                this.keywordsForCheck = new List<string>();

                foreach (var keywordInfo in this.localKeywords)
                {
                    if (!string.IsNullOrEmpty(keywordInfo.name))
                    {
                        keywordsForCheck.Add(keywordInfo.name);
                    }
                }
                keywordsForCheck.Sort();

            }

            public void AppendStateKeyData(GraphicsStateKeyData keyData)
            {
                this.stateKeyData.Add(keyData);
            }
        }


        internal struct GraphicsStateKeyData
        {
            public int version;
            public GraphicsDeviceType graphicsDeviceType;
            public RuntimePlatform runtimePlatform;
            public string qualityLevelName;

            public GraphicsStateKeyData(GraphicsStateCollection collection)
            {
                this.version = collection.version;
                this.graphicsDeviceType = collection.graphicsDeviceType;
                this.runtimePlatform = collection.runtimePlatform;
                this.qualityLevelName = collection.qualityLevelName;
            }

            public bool IsMatchData(ref GraphcisStateRequestCondition condition)
            {
                if (condition.graphicsDeviceMatch && condition.graphicsDeviceType != this.graphicsDeviceType)
                {
                    return false;
                }
                if (condition.runtimePlatformMacth && condition.runtimePlatform == this.runtimePlatform)
                {
                    return false;
                }
                return true;
            }
        }
        public struct GraphcisStateRequestCondition {
            public GraphicsDeviceType graphicsDeviceType;
            public RuntimePlatform runtimePlatform;
            public bool graphicsDeviceMatch;
            public bool runtimePlatformMacth;
        }


        // vars
        private Dictionary<Shader, HashSet<GraphicsStateVariantData>> graphicsStatesVariants;

        Dictionary<Shader, List< GraphicsStateKeyData > > stateKeyData;

        private List<GraphicsStateCollection.ShaderVariant> variantBuffer;

        internal bool IsExistInGSC(Shader shader, 
            ref ShaderSnippetData data,
            ref GraphcisStateRequestCondition condition)
        {
            List<GraphicsStateKeyData> list;
            if (this.stateKeyData.TryGetValue(shader, out list))
            {
                foreach (var key in list)
                {
                    if (key.IsMatchData(ref condition)){
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool IsExistVariantInGSC(
            Shader shader, ref ShaderSnippetData snippet, ref ShaderCompilerData data,
             ShaderKeywordMaskGetterPerSnippet maskGetter,
            ref GraphcisStateRequestCondition condition)
        {

            HashSet<GraphicsStateVariantData> hashData;
            if(this.graphicsStatesVariants.TryGetValue(shader, out hashData) ){
                GraphicsStateVariantData variantData;
//                hashData.TryGetValue(, out variantData);
            }
            return false;
        }

        internal void Initialize()
        {
            var statesCollections = GetProjectGraphicsStateCollections();
            this.graphicsStatesVariants = new Dictionary<Shader, HashSet<GraphicsStateVariantData>>();
            this.stateKeyData = new Dictionary<Shader, List< GraphicsStateKeyData> >();

            foreach (var stateCollection in statesCollections)
            {
                CollectGraphicsState(stateCollection);
            }
        }

        private void CollectGraphicsState(GraphicsStateCollection graphicsStates)
        {
            GraphicsStateKeyData stateKeyData = new GraphicsStateKeyData(graphicsStates);

            if(variantBuffer == null)
            {
                variantBuffer = new List<GraphicsStateCollection.ShaderVariant>();
            }
            else
            {
                variantBuffer.Clear();
            }
            graphicsStates.GetVariants(variantBuffer);

            foreach (var variant in variantBuffer)
            {
                Shader shader = variant.shader;
                if (!shader)
                {
                    continue;
                }
                List<GraphicsStateKeyData> keydataList;
                if(this.stateKeyData.TryGetValue(shader, out keydataList) ){
                    if (!keydataList.Contains(stateKeyData))
                    {
                        keydataList.Add(stateKeyData);
                    }
                }
                else
                {
                    keydataList = new List<GraphicsStateKeyData>();
                    keydataList.Add(stateKeyData);
                    this.stateKeyData.Add(shader, keydataList);
                }
                var variantData = new GraphicsStateVariantData(variant, stateKeyData);

                HashSet<GraphicsStateVariantData> variantDatasHash;
                GraphicsStateVariantData variantDataInHash;
                if ( !graphicsStatesVariants.TryGetValue(shader, out variantDatasHash) ){
                    variantDatasHash = new HashSet<GraphicsStateVariantData>(new GraphicsStateVariantDataComparer());
                }
                if (variantDatasHash.TryGetValue(variantData, out variantDataInHash))
                {
                    variantDataInHash.AppendStateKeyData(stateKeyData);
                }
                else
                {
                    variantDatasHash.Add(variantData);
                }
            }
        }






        private static List<GraphicsStateCollection> GetProjectGraphicsStateCollections()
        {
            var collections = new List<GraphicsStateCollection>();
            var guids = AssetDatabase.FindAssets("t: GraphicsStateCollection");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(path);
                if (obj != null)
                {
                    collections.Add(obj);
                }
            }
            var excludeList = StripShaderConfig.GetExcludeGSC();
            foreach (var exclude in excludeList)
            {
                if (exclude != null)
                {
                    collections.Remove(exclude);
                }
            }

            return collections;
        }
    }
}