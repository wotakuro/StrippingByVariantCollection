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
using Unity.Android.Gradle.Manifest;


namespace UTJ.ShaderVariantStripping
{
    public class StripProcessShaders : IPreprocessShaders
    {
        private static StripProcessShaders instance;

        private bool isInitialized = false;
        private List<ShaderCompilerData> compileResultBuffer;

        private ShaderVariantStripLogger shaderVariantStripLogger = new ShaderVariantStripLogger();
        private ProjectSVCData projectSVCData = new ProjectSVCData();

#if UNITY_6000_0_OR_NEWER
        private ProjectGSCData projectGSCData = new ProjectGSCData();
        private List<ProjectGSCData.GraphcisStateRequestCondition> conditionsForPerShader;
#endif


        public StripProcessShaders()
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
            this.compileResultBuffer = new List<ShaderCompilerData>(1024);
            this.projectSVCData.Initialize();

#if UNITY_6000_0_OR_NEWER
            this.projectGSCData.Initialize();
#endif

            if (StripShaderConfig.IsLogEnable)
            {
                shaderVariantStripLogger.InitLogInfo();
                shaderVariantStripLogger.SaveProjectSVCVaraiants(this.projectSVCData.GetDebugStr() );
            }
            isInitialized = true;
        }




        public int callbackOrder
        {
            get
            {
                return StripShaderConfig.Order;
            }
        }

#if UNITY_6000_0_OR_NEWER
        private void ConstructGSCConditions(List<ProjectGSCData.GraphcisStateRequestCondition> list,
            IList<ShaderCompilerData> shaderCompilerData)
        {
            list.Clear();
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                var gcsConditionData = new ProjectGSCData.GraphcisStateRequestCondition()
                {
                    graphicsDeviceMatch = StripShaderConfig.MatchGSCGraphicsAPI,
                    runtimePlatformMacth = StripShaderConfig.MatchGSCPlatform,
                    shaderPlatform = shaderCompilerData[i].shaderCompilerPlatform,
                    buildTarget = shaderCompilerData[i].buildTarget,
                };

                if (!list.Contains(gcsConditionData))
                {
                    list.Add(gcsConditionData);
                }
            }
        }
#endif


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

            bool isExistShaderInSVC = this.projectSVCData.IsExistSVC( shader);

#if UNITY_6000_0_OR_NEWER

            var gcsConditionData = new ProjectGSCData.GraphcisStateRequestCondition()
            {
                graphicsDeviceMatch = StripShaderConfig.MatchGSCGraphicsAPI,
                runtimePlatformMacth = StripShaderConfig.MatchGSCPlatform
            };
            bool isExistShaderInGSC = false;
            if(conditionsForPerShader == null)
            {
                conditionsForPerShader = new List<ProjectGSCData.GraphcisStateRequestCondition>();
            }
            this.ConstructGSCConditions(conditionsForPerShader, shaderCompilerData);
            foreach (var condition in conditionsForPerShader) {
                bool flag = this.projectGSCData.IsExistInGSC(shader, ref snippet, condition);
                if (flag)
                {
                    isExistShaderInGSC = true;
                    //break;
                }
#if DEBUG
                Debug.Log("Condition " + condition.graphicsDeviceMatch);
#endif
            }
#endif

#if UNITY_6000_0_OR_NEWER
            if ( (!isExistShaderInSVC && StripShaderConfig.UseSVC)
                && !isExistShaderInGSC)
#else
            if (!isExistShaderInSVC && StripShaderConfig.UseSVC)
#endif
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

            var  variantsHashSet = projectSVCData.GetVariantsHashSet(shader,maskGetter);
#if UNITY_6000_0_OR_NEWER
            var variantGSCHashSet = projectGSCData.GetVariantsHashSet(shader, maskGetter);
#endif

            // Set ShaderCompilerData List
            this.compileResultBuffer.Clear();
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                bool isExist = false;


#if UNITY_6000_0_OR_NEWER
                if (StripShaderConfig.UseGSC && !isExist)
                {

                    bool isExistsVariantInGSC = projectGSCData.IsExistVariantInGSC(shader,
                        ref snippet, shaderCompilerData[i] , ref gcsConditionData, variantGSCHashSet);
                    isExist |= isExistShaderInGSC;
                }
#endif

                // use shader variant collection
                if (StripShaderConfig.UseSVC && !isExist)
                {
                    isExist |= projectSVCData.IsExistInSVC(variantsHashSet, shader, snippet, shaderCompilerData[i], maskGetter);
                }
                /// log 
                if (isExist)
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