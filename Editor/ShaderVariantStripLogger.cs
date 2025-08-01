
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Build;
using UnityEngine.Rendering;
using System.Text;
using static UTJ.ShaderVariantStripping.ProjectSVCData;
using System.Diagnostics;

namespace UTJ.ShaderVariantStripping
{

    public class ShaderVariantStripLogger 
    {

        private const string LogDirectory = "ShaderVariants/Builds";

        private string dateTimeStr;

        private StringBuilder includeVariantsBuffer;
        private StringBuilder excludeVariantsBuffer;


        
        internal void InitLogInfo()
        {

            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            var dateTime = System.DateTime.Now;
            this.dateTimeStr = dateTime.ToString("yyyyMMdd_HHmmss");

            // string builder
            this.includeVariantsBuffer = new StringBuilder(1024);
            this.excludeVariantsBuffer = new StringBuilder(1024);
        }


        internal void SaveProjectGSCShaders(string debugStr)
        {
            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            string dir = LogDirectory + "/" + dateTimeStr;
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(dir + "/ProjectGSCShaders.txt", debugStr);
        }
        internal void SaveProjectGSCVaraiants(string debugStr)
        {
            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            string dir = LogDirectory + "/" + dateTimeStr;
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(dir + "/ProjectGSCData.txt", debugStr);
        }

        internal void SaveProjectSVCVaraiants(string debugStr)
        {
            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            string dir = LogDirectory + "/" + dateTimeStr;
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(dir + "/ProjectSVCData.txt", debugStr);
        }

        internal void DumpConfig()
        {
            string dir = LogDirectory + "/" + dateTimeStr;
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            StripShaderConfig.LogConfigData(dir + "/Config.txt");
        }

        internal void SaveResult(Shader shader, ShaderSnippetData snippet)
        {

            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            string shaderName = ShaderNameUtility.GetShaderNameForPath(shader);
            string includeDir = LogDirectory + "/" + dateTimeStr + "/Include";
            string excludeDir = LogDirectory + "/" + dateTimeStr + "/Exclude";
            string name = shaderName + ShaderNameUtility.GetSnipetName(snippet);

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

        internal void LogKeywordMask(ShaderKeywordMaskGetterPerSnippet maskGetter)
        {

            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            string maskLogDir = LogDirectory + "/" + dateTimeStr + "/KeywordLog/";

            if (!System.IO.Directory.Exists(maskLogDir))
            {
                System.IO.Directory.CreateDirectory(maskLogDir);
            }
            var path = System.IO.Path.Combine(maskLogDir, maskGetter.LogFileName);
            System.IO.File.AppendAllText(path, maskGetter.GetLogStr());
        }

        internal void ClearStringBuffers()
        {
            if (includeVariantsBuffer != null)
            {
                this.includeVariantsBuffer.Length = 0;
            }
            if (excludeVariantsBuffer != null)
            {
                this.excludeVariantsBuffer.Length = 0;
            }
        }

        internal void AppendIncludeShaderInfo(Shader shader, ShaderSnippetData snippet, ShaderCompilerData compilerData)
        {
            this.AppendShaderInfo(includeVariantsBuffer, shader, snippet, compilerData);
        }
        internal void AppendExcludeShaderInfo(Shader shader, ShaderSnippetData snippet, ShaderCompilerData compilerData)
        {
            this.AppendShaderInfo(excludeVariantsBuffer, shader, snippet, compilerData);
        }

        private void AppendShaderInfo(StringBuilder sb, Shader shader, ShaderSnippetData snippet, ShaderCompilerData compilerData)
        {

            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            if (sb.Length == 0)
            {
                sb.Append("Shader:").Append(shader.name).Append("\n");
                sb.Append("SubShader:" ).Append(snippet.pass.SubshaderIndex).Append("\nPass;").Append(snippet.pass.PassIndex).Append("\n");
                sb.Append("ShaderType:").Append(snippet.shaderType).Append("\n").
                    Append("PassName:").Append(snippet.passName).Append("\n").
                    Append("PassType:").Append(snippet.passType).Append("\n\n");
            }

            sb.Append("BuildTarget:").Append(compilerData.buildTarget).Append("\nShaderPlatform:").Append(compilerData.shaderCompilerPlatform).Append("\n");
            
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
                sb.Append(keyword.name).Append(" ");
            }



            sb.Append("\n KeywordType:");
            foreach (var keyword in sortKeywords)
            {
                if (!ShaderKeyword.IsKeywordLocal(keyword))
                {
                    sb.Append(ShaderKeyword.GetGlobalKeywordType(keyword)).Append(" ");
                }
                else
                {
                    var localKeyword = new LocalKeyword(shader, keyword.name);
                    sb.Append(localKeyword.type).Append(" ");
                }
            }


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
            sb.Append("\n").Append("\n");
        }



        internal void LogAllInVariantColllection(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData,
            bool isStripEnable)
        {

            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                AppendShaderInfo(includeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
            }
            if (isStripEnable)
            {
                this.includeVariantsBuffer.Append("\n========= Logged AllVaraint Info =========\n");
            }
            else {
                this.includeVariantsBuffer.Append("\n========= Skip Stripping. So AllVaraint Info =========\n");
            }

            string filePath;
            GetPathNames(LogDirectory + "/" + dateTimeStr + "/Include/", shader, snippet, out filePath);
            System.IO.File.AppendAllText(filePath, includeVariantsBuffer.ToString());
        }
        internal void LogNotInVariantColllection(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }

            for (int i = 0; i < shaderCompilerData.Count; ++i)
            {
                AppendShaderInfo(excludeVariantsBuffer, shader, snippet, shaderCompilerData[i]);
            }
            this.excludeVariantsBuffer.Append("\n========= Logged All variantInfo=========\n");

            string filePath;
            GetPathNames(LogDirectory + "/" + dateTimeStr + "/Exclude/", shader, snippet, out filePath);
            System.IO.File.AppendAllText(filePath, excludeVariantsBuffer.ToString());
        }

        private static void GetPathNames(string dirHead, Shader shader, ShaderSnippetData snippet, out string filePath)
        {
            var shortShaderName = ShaderNameUtility.GetShaderShortNameForPath(shader);
            string shaderName = ShaderNameUtility.GetShaderNameForPath(shader);
            string name = shortShaderName + "_" + snippet.shaderType.ToString() + "_" + snippet.pass.SubshaderIndex + "_" + snippet.pass.PassIndex;
            string dir = dirHead + shaderName;

            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            filePath = System.IO.Path.Combine(dir, name) + ".txt";
        }

        internal void LogResult(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData,
            ShaderKeywordMaskGetterPerSnippet maskGetter,
            double startTime, int startVariants)
        {
            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }

            double endTime = EditorApplication.timeSinceStartup;
            int endVariants = shaderCompilerData.Count;

            string dir = LogDirectory + "/" + dateTimeStr;
            string executeLog = dir + "/ExecuteLog";
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            if (!System.IO.Directory.Exists(executeLog))
            {
                System.IO.Directory.CreateDirectory(executeLog);
            }
            var tmpSb = new StringBuilder();
            tmpSb.Append("Info:").Append(snippet.shaderType).Append(" pass").
                Append(snippet.pass.SubshaderIndex).Append("-").Append(snippet.pass.PassIndex).Append(" ").
                Append(snippet.passType).Append(" \"").Append(snippet.passName).Append("\"\n");
            tmpSb.Append("ExecuteTime:").Append(endTime - startTime).Append(" sec\n").
                Append("Variants:").Append(startVariants).Append("->").Append(endVariants).Append("\n");

            System.IO.File.AppendAllText(executeLog +"/"+ ShaderNameUtility.GetShaderNameForPath(shader) + ".log",
                tmpSb.ToString());

            SaveResult(shader, snippet);
            LogKeywordMask(maskGetter);


        }

        internal void LogShaderVariantsInfo(string header,Shader shader,ShaderSnippetData data, HashSet<ShaderVariantsInfo> variants)
        {
            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
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
    }
}