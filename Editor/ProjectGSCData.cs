using System.Collections.Generic;
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

            public GraphicsStateVariantData()
            {

            }

            public GraphicsStateVariantData(GraphicsStateCollection.ShaderVariant variant, GraphicsStateKeyData keyData)
            {
                this.shader = variant.shader;
                this.passIdentifier = variant.passId;
                this.localKeywords = variant.keywords;
                this.stateKeyData = new List<GraphicsStateKeyData>();
                this.stateKeyData.Add(keyData);
                this.ConstructKeeywordForCheck();
            }

            public void SetupFromCompilerData(Shader shader,PassIdentifier pass,ref ShaderCompilerData compilerData)
            {
                this.shader = shader;
                this.passIdentifier = pass;
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
                if (!this.stateKeyData.Contains(keyData))
                {
                    this.stateKeyData.Add(keyData);
                }
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

            public bool IsInTheList(List<GraphicsStateKeyData> list)
            {
                foreach (var data in list)
                {
                    if ((this.version == data.version) &&
                    (this.graphicsDeviceType == data.graphicsDeviceType) &&
                    (this.runtimePlatform == data.runtimePlatform) &&
                    (this.qualityLevelName == data.qualityLevelName))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool IsMatchData(ref GraphcisStateRequestCondition condition)
            {
                if (condition.graphicsDeviceMatch)
                {
                    switch (condition.shaderPlatform)
                    {
                        case ShaderCompilerPlatform.D3D:
                            return (this.graphicsDeviceType == GraphicsDeviceType.Direct3D11) ||
                                 (this.graphicsDeviceType == GraphicsDeviceType.Direct3D12);
                        case ShaderCompilerPlatform.Vulkan:
                            return (this.graphicsDeviceType == GraphicsDeviceType.Vulkan);
                        case ShaderCompilerPlatform.XboxOneD3D11:
                        case ShaderCompilerPlatform.GameCoreXboxOne:
                        case ShaderCompilerPlatform.XboxOneD3D12:
                            return (this.graphicsDeviceType == GraphicsDeviceType.XboxOne) ||
                                (this.graphicsDeviceType == GraphicsDeviceType.XboxOneD3D12) ||
                             (this.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxOne) ||
                             (this.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxSeries);

                        case ShaderCompilerPlatform.PS4:
                            return (this.graphicsDeviceType == GraphicsDeviceType.PlayStation4);
                        case ShaderCompilerPlatform.PS5:
                        case ShaderCompilerPlatform.PS5NGGC:
                            return (this.graphicsDeviceType == GraphicsDeviceType.PlayStation5) ||
                                (this.graphicsDeviceType == GraphicsDeviceType.PlayStation5NGGC);


                        case ShaderCompilerPlatform.GLES3x:
                            return (this.graphicsDeviceType == GraphicsDeviceType.OpenGLES3);
                        case ShaderCompilerPlatform.WebGPU:
                            return (this.graphicsDeviceType == GraphicsDeviceType.WebGPU);
                        case ShaderCompilerPlatform.Metal:
                            return (this.graphicsDeviceType == GraphicsDeviceType.Metal);
                        case ShaderCompilerPlatform.OpenGLCore:
                            return (this.graphicsDeviceType == GraphicsDeviceType.OpenGLCore);
                    }

                    return false;
                }
                if (condition.runtimePlatformMacth)
                {
                    switch (condition.buildTarget)
                    {
                        case BuildTarget.Android:
                            break;
                        case BuildTarget.iOS:
                            break;
                    }
                    return false;
                }
                return true;
            }
        }
        public struct GraphcisStateRequestCondition {
            public ShaderCompilerPlatform shaderPlatform;
            public BuildTarget buildTarget;
            public bool graphicsDeviceMatch;
            public bool runtimePlatformMacth;

            public void SetDataFrom(ref ShaderCompilerData data)
            {
                this.shaderPlatform = data.shaderCompilerPlatform;
                this.buildTarget = data.buildTarget;
                   
            }
        }


        // vars
        private Dictionary<Shader, HashSet<GraphicsStateVariantData>> graphicsStatesVariants;

        Dictionary<Shader, List< GraphicsStateKeyData > > stateKeyData;
        private List<GraphicsStateCollection.ShaderVariant> variantBuffer;

        private GraphicsStateVariantData checkData = new GraphicsStateVariantData();

        private GraphicsStateVariantDataComparer comparer = new GraphicsStateVariantDataComparer();

        internal bool IsExistInGSC(Shader shader, 
            GraphcisStateRequestCondition condition)
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

#if DEBUG
        internal void DebugCondition(Shader shader)
        {
            List<GraphicsStateKeyData> list;
            if (this.stateKeyData.TryGetValue(shader, out list))
            {
                foreach (var key in list)
                {
                    Debug.Log("[Cnd]" + shader.name + "\n" + 
                        key.runtimePlatform +"::" + key.graphicsDeviceType +"::" + key.qualityLevelName +"::"+key.version);
                }

            }
            else
            {
                Debug.Log("[Cnd] Not found " + shader.name);
            }

        }
#endif


        internal HashSet<GraphicsStateVariantData> GetVariantsHashSet(Shader shader,ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            HashSet<GraphicsStateVariantData> originHashData;
            if (!this.graphicsStatesVariants.TryGetValue(shader, out originHashData))
            {
                return null;
            }
            HashSet<GraphicsStateVariantData> hashData = CreateCurrentStageVariantsInfo(originHashData,maskGetter);
            return hashData;
        }



        private static HashSet<GraphicsStateVariantData> CreateCurrentStageVariantsInfo(HashSet<GraphicsStateVariantData> origin, 
            ShaderKeywordMaskGetterPerSnippet maskGetter)
        {
            if (!maskGetter.HasCutoffKeywords())
            {
                return origin;
            }
            HashSet<GraphicsStateVariantData> copyData = new HashSet<GraphicsStateVariantData>(origin, new GraphicsStateVariantDataComparer());
            foreach (var info in origin)
            {
                var newKeywords = maskGetter.ConvertValidOnlyKeywords(info.keywordsForCheck);
                if (newKeywords == null)
                {
                    continue;
                }
                var newVariant = new GraphicsStateVariantData() {
                    keywordsForCheck = newKeywords,
                    shader = info.shader,
                    passIdentifier = info.passIdentifier,
                    stateKeyData = info.stateKeyData,
                };
                
                // todo
                if (!copyData.Contains(newVariant))
                {
                    copyData.Add(newVariant);
                }
            }
            return copyData;
        }

#if DEBUG

        internal void LogVariantDataList(string path , HashSet<GraphicsStateVariantData> hashSet)
        {
            var sb = new System.Text.StringBuilder(1024 * 1024);
            foreach(var info in hashSet)
            {
                if(info.passIdentifier.SubshaderIndex != 0 || info.passIdentifier.PassIndex != 1)
                {
                    continue;
                }
                sb.Append(info.shader.name).Append(" :: ").
                    Append(info.passIdentifier.SubshaderIndex).Append("-").
                    Append(info.passIdentifier.PassIndex).AppendLine();

                foreach(var keyword in info.keywordsForCheck ){
                    sb.Append(keyword).Append(" ");
                }
                sb.AppendLine();
            }
            System.IO.File.WriteAllText(path, sb.ToString());
        }
#endif


        internal bool IsExistVariantInGSC(
            Shader shader, PassIdentifier pass, ShaderCompilerData data,
             ref GraphcisStateRequestCondition condition,
             HashSet<GraphicsStateVariantData> hashData)
        {
            if (hashData == null) { return false; }
            var originData = new GraphicsStateVariantData();
            originData.SetupFromCompilerData(shader, pass, ref data);
            this.checkData.SetupFromCompilerData(shader, pass , ref data);
            GraphicsStateVariantData variantData;
            if (hashData.TryGetValue(checkData, out variantData))
            {
                foreach (var state in variantData.stateKeyData)
                {
                    if (state.IsMatchData(ref condition)) {
                        return true; 
                    }
                }
            }

            return false;
        }

        public void Initialize()
        {
            var statesCollections = GetProjectGraphicsStateCollections();
            this.graphicsStatesVariants = new Dictionary<Shader, HashSet<GraphicsStateVariantData>>();
            this.stateKeyData = new Dictionary<Shader, List< GraphicsStateKeyData> >();

            foreach (var stateCollection in statesCollections)
            {
                CollectGraphicsState(stateCollection);
            }
        }
        #region DEBUG
        public void SaveDebugLog()
        {
            var sb = new System.Text.StringBuilder(1024);

            foreach(var kvs in this.stateKeyData)
            {
                sb.AppendLine("---------------------------");
                sb.AppendLine(kvs.Key.name);
                foreach (var state in kvs.Value)
                {
                    sb.Append("  ");
                    AppendStateKeyDate(sb, state);
                    sb.Append('\n');
                }
            }
            System.IO.File.WriteAllText("GSCstateKeyDebug.txt", sb.ToString());
            sb.Clear();

            foreach(var kvs in this.graphicsStatesVariants)
            {
                sb.AppendLine("---------------------------");
                sb.AppendLine(kvs.Key.name);
                foreach (var variantData in kvs.Value)
                {
                    sb.Append("  ").Append(variantData.shader.name).Append("::").
                        Append(variantData.passIdentifier.SubshaderIndex).Append("-").Append(variantData.passIdentifier.PassIndex);
                    sb.Append("\n    Keyword:");
                    foreach(var keyword in variantData.keywordsForCheck)
                    {
                        sb.Append(" ").Append(keyword);
                    }
                    sb.AppendLine("");

                    // stateData
                    foreach(var state in variantData.stateKeyData)
                    {
                        sb.Append("      State:");
                        AppendStateKeyDate(sb, state);
                        sb.Append("\n");
                    }
                }
            }
            System.IO.File.WriteAllText("GSCvariantDebug.txt", sb.ToString());

        }
        private static void AppendStateKeyDate(System.Text.StringBuilder sb,GraphicsStateKeyData state)
        {

            sb.Append(state.graphicsDeviceType).Append(":").
                Append(state.runtimePlatform).Append(":ver_").Append(state.version).Append(":").
                Append(state.qualityLevelName);
        }
        #endregion DEBUG

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
                    variantDatasHash = new HashSet<GraphicsStateVariantData>(comparer);
                    variantDatasHash.Add(variantData);
                    this.graphicsStatesVariants.Add(shader, variantDatasHash);
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