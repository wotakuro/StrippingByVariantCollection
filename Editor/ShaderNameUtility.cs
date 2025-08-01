using UnityEditor.Rendering;
using UnityEngine;

namespace UTJ.ShaderVariantStripping
{
    public class ShaderNameUtility
    {
        public static string GetShaderNameForPath(Shader shader)
        {

            string shaderName = shader.name.Replace("/", "_").Replace('|', '-');
            return shaderName;
        }

        public static string GetShaderShortNameForPath(Shader shader)
        {
            var shortShaderName = shader.name.Replace('|', '-');
            int lastSlashIndex = shortShaderName.LastIndexOf('/');
            if (lastSlashIndex != -1)
            {
                shortShaderName = shortShaderName.Substring(lastSlashIndex + 1);
            }
            return shortShaderName;
        }

        public static string GetSnipetName(ShaderSnippetData snippetData)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("_").Append(snippetData.shaderType.ToString()).Append( "_").Append( snippetData.pass.SubshaderIndex).Append("_").
                Append(snippetData.pass.PassIndex);
            return sb.ToString();
        }
    }
}