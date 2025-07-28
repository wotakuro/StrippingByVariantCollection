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

        private ProjectGSCData projectGSCData = new ProjectGSCData();
        private List<ProjectGSCData.GraphcisStateRequestCondition> conditionsForPerShader;




        private HashSet<ShaderCompilerPlatform> platformsBuffer = new HashSet<ShaderCompilerPlatform>();


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
            this.projectGSCData.Initialize();

            if (StripShaderConfig.IsLogEnable)
            {
                shaderVariantStripLogger.InitLogInfo();
                shaderVariantStripLogger.SaveProjectSVCVaraiants(this.projectSVCData.GetDebugStr() );
                shaderVariantStripLogger.DumpConfig();
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

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            this.Initialize();

            if (!StripShaderConfig.IsEnable)
            {
                shaderVariantStripLogger.LogAllInVariantColllection(shader, snippet, shaderCompilerData,false);
                return;
            }

            double startTime = EditorApplication.timeSinceStartup;
            int startVariants = shaderCompilerData.Count;

            ShaderKeywordMaskGetterPerSnippet maskGetter = new ShaderKeywordMaskGetterPerSnippet(shader, snippet);
//            if (StripShaderConfig.IgnoreStageOnlyKeyword)
            {
                maskGetter.ConstructOnlyKeyword();
            }
            shaderVariantStripLogger.ClearStringBuffers();

            bool isExistShader = false;

            bool isExistShaderInSVC = this.projectSVCData.IsExistSVC( shader);
            isExistShader |= isExistShaderInSVC;


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
                    isExistShader |= isExistShaderInGSC;
                    //break;
                }
            }


            if(!isExistShader)
            {
                if (StripShaderConfig.StrictVariantStripping)
                {
                    // safe mode
                    if ( StripShaderConfig.SafeMode )
                    {
                        this.ExecuteSafeMode(shader, snippet, shaderCompilerData);
                        shaderVariantStripLogger.LogResult(shader, snippet, shaderCompilerData, maskGetter, startTime, startVariants);
                    }
                    else
                    {
                        shaderVariantStripLogger.LogNotInVariantColllection(shader, snippet, shaderCompilerData);
                        shaderCompilerData.Clear();
                    }
                }
                else
                {
                    shaderVariantStripLogger.LogAllInVariantColllection(shader, snippet, shaderCompilerData,true);
                }
                return;
            }

            var  variantsHashSet = projectSVCData.GetVariantsHashSet(shader,maskGetter);
            var variantGSCHashSet = projectGSCData.GetVariantsHashSet(shader, maskGetter);

            // Set ShaderCompilerData List
            this.compileResultBuffer.Clear();
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                bool isExistVariant = false;


                if (StripShaderConfig.UseGSC && !isExistVariant)
                {
                    gcsConditionData.buildTarget = shaderCompilerData[i].buildTarget;
                    gcsConditionData.shaderPlatform = shaderCompilerData[i].shaderCompilerPlatform;
                    bool isExistsVariantInGSC = projectGSCData.IsExistVariantInGSC(shader,
                        ref snippet, shaderCompilerData[i] , ref gcsConditionData, variantGSCHashSet);

                    isExistVariant |= isExistsVariantInGSC;
                }

                // use shader variant collection
                if (StripShaderConfig.UseSVC && !isExistVariant)
                {
                    isExistVariant |= projectSVCData.IsExistInSVC(variantsHashSet, shader, snippet, shaderCompilerData[i], maskGetter);
                }
                /// log 
                if (isExistVariant)
                {
                    shaderVariantStripLogger.AppendIncludeShaderInfo(shader, snippet, shaderCompilerData[i]);
                    this.compileResultBuffer.Add(shaderCompilerData[i]);
                }
                else
                {
                    shaderVariantStripLogger.AppendExcludeShaderInfo(shader, snippet, shaderCompilerData[i]);
                }

            }

            // safe mode
            if(StripShaderConfig.SafeMode && compileResultBuffer.Count == 0)
            {
                this.ExecuteSafeMode(shader, snippet, shaderCompilerData);
                shaderVariantStripLogger.LogResult(shader, snippet, shaderCompilerData, maskGetter, startTime, startVariants);
                return;
            }

            // CreateList             
            shaderCompilerData.Clear();
            foreach (var data in this.compileResultBuffer)
            {
                shaderCompilerData.Add(data);
            }

            shaderVariantStripLogger.LogResult(shader, snippet, shaderCompilerData, maskGetter, startTime, startVariants);
        }


        private void ExecuteSafeMode(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            this.platformsBuffer.Clear();
            this.compileResultBuffer.Clear();
            shaderVariantStripLogger.ClearStringBuffers();

            foreach (var data in shaderCompilerData)
            {
                var platform = data.shaderCompilerPlatform;
                if (!this.platformsBuffer.Contains(platform))
                {
                    shaderVariantStripLogger.AppendIncludeShaderInfo(shader, snippet, data);
                    compileResultBuffer.Add(data);
                    this.platformsBuffer.Add(platform);
                }
                else
                {
                    shaderVariantStripLogger.AppendExcludeShaderInfo(shader, snippet, data);
                }
            }
            shaderCompilerData.Clear();

            foreach (var data in this.compileResultBuffer)
            {
                shaderCompilerData.Add(data);
            }

        }

    }
}