
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Build;
using UnityEngine.Rendering;
using System.Text;
using static UTJ.ShaderVariantStripping.ProjectSVCData;

namespace UTJ.ShaderVariantStripping
{

    public class ShaderVariantStripLogger 
    {

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
        
        internal void InitLogInfo()
        {

            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
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

        internal void SaveProjectVaraiants(Dictionary<Shader, HashSet<ShaderVariantsInfo>> shaderVariants)
        {

            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
            var list = new List<ShaderVariantsInfo>(1024);
            foreach (var variantHashSet in shaderVariants.Values)
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

            string dir = LogDirectory + "/" + dateTimeStr;
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllText(dir + "/ProjectVariants.txt", projectVaritantsBuffer.ToString());
        }

        internal void SaveResult(Shader shader, ShaderSnippetData snippet)
        {

            if (!StripShaderConfig.IsLogEnable)
            {
                return;
            }
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

            this.includeVariantsBuffer.Length = 0;
            this.excludeVariantsBuffer.Length = 0;
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



        internal void LogAllInVariantColllection(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
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
            this.excludeVariantsBuffer.Append("\n==================\n");

            string filePath;
            GetPathNames(LogDirectory + "/" + dateTimeStr + "/Exclude/", shader, snippet, out filePath);
            System.IO.File.AppendAllText(filePath, excludeVariantsBuffer.ToString());
        }

        private static void GetPathNames(string dirHead, Shader shader, ShaderSnippetData snippet, out string filePath)
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
            Debug.Log(dir + ";;" + name + ":::");
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
                tmpSb.ToString());

            var data = new ShaderInfoData(shader, snippet);
            if (!alreadyWriteShader.Contains(data))
            {
                SaveResult(shader, snippet);
                LogKeywordMask(maskGetter);
                alreadyWriteShader.Add(data);
            }

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