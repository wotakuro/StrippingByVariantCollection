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
    public class StripProcessShaders : IPreprocessShaders
    {
        private static StripProcessShaders instance;

        private bool isInitialized = false;
        private List<ShaderCompilerData> compileResultBuffer;

        private ShaderVariantStripLogger shaderVariantStripLogger = new ShaderVariantStripLogger();
        private ProjectSVCData projectSVCData = new ProjectSVCData();



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

            if (StripShaderConfig.IsLogEnable)
            {
                shaderVariantStripLogger.InitLogInfo();
                shaderVariantStripLogger.SaveProjectVaraiants(this.projectSVCData.GetAllShaderVariantsInProjectSVC() );
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

            bool isExistShader = this.projectSVCData.IsExistSVC( shader);
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

            var  variantsHashSet = projectSVCData.GetVariantsHashSet(shader,maskGetter);

            // Set ShaderCompilerData List
            this.compileResultBuffer.Clear();
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                bool isExistsVariant = projectSVCData.IsExistInSVC(variantsHashSet, shader, snippet, shaderCompilerData[i],maskGetter);

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