using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using System.Text;
using System.Linq;

namespace UTJ
{
    // from Unity 2022.2 or later
    // Variant Strip behaviour changed.
    // from 2022...
    // "multi_compile_fragment" keywords is appears only fragment stage.
    public class ShaderKeywordMaskGetterPerSnippet 
    {
        private static readonly string[] ShaderTypeTable = 
        {
            "progVertex",
            "progFragment",
            "progGeometry",
            "progHull",
            "progDomain",
            "progRayTracing"
        };
        private HashSet<string> validKeywords;
        private List<string> keywords;
        private Shader shader;
        private ShaderSnippetData snippetData;

        public ShaderKeywordMaskGetterPerSnippet(Shader sh, ShaderSnippetData snippet)
        {
            this.shader = sh;
            this.snippetData = snippet;
            int typeIndex = GetTypeIndex(snippet.shaderType);
            int subShaderIndex = (int)snippet.pass.SubshaderIndex;
            int passIndex = (int)snippet.pass.PassIndex;
            ConstructFlag(subShaderIndex, passIndex, ShaderTypeTable[typeIndex]);
        }
        public bool HasCutoffKeywords()
        {
            if(validKeywords == null)
            {
                return false;
            }
            return (validKeywords.Count != keywords.Count);
        }

        public string LogFileName
        {
            get
            {
                return this.shader.name.Replace("/", "_") +"-"+ this.snippetData.shaderType + "-" +
                    this.snippetData.pass.SubshaderIndex + "-" + this.snippetData.pass.PassIndex + ".txt";
            }
        }

        public string GetLogStr()
        {
            StringBuilder stringBuilder = new StringBuilder(1024);
            stringBuilder.Append("Shader:").Append(shader.name).Append("\n");
            stringBuilder.Append("ShaderType:").Append(snippetData.shaderType).Append("\n");
            stringBuilder.Append("SubShaderIndex:").Append(snippetData.pass.SubshaderIndex).Append("\n");
            stringBuilder.Append("PassIndex:").Append(snippetData.pass.PassIndex).Append("\n");
            stringBuilder.Append("PassType:").Append(snippetData.passType).Append("\n");
            stringBuilder.Append("PassName:").Append(snippetData.passName).Append("\n");
            stringBuilder.Append("Keywords:").Append("\n");
            foreach ( var keyword in this.keywords)
            {
                bool validFlag = this.ValidKeyword(keyword);
                stringBuilder.Append("  ").Append(keyword).Append(":").Append(validFlag).Append("\n");
            }
            return stringBuilder.ToString();
        }

        public bool ValidKeyword(string keyword)
        {
            if(validKeywords == null)
            {
                return true;
            }
            return validKeywords.Contains(keyword);
        }
        public string []ConvertValidOnlyKeywords(string[] keywords)
        {
            if(keywords == null) { return null; }
            int validCount = 0;
            int length = keywords.Length;
            for(int i= 0; i < length; ++i)
            {
                if (ValidKeyword(keywords[i])){
                    validCount++;
                }
            }
            if(validCount == length)
            {
                return null;
            }
            var newKeywords = new string[validCount];
            int index = 0;
            for (int i = 0; i < length; ++i)
            {
                if (ValidKeyword(keywords[i]))
                {
                    newKeywords[index] = keywords[i];
                    ++index;
                }
            }
            return newKeywords;
        }

        private static int GetTypeIndex(ShaderType type)
        {
            switch(type)
            {
                case ShaderType.Vertex:
                    return 0;
                case ShaderType.Fragment:
                    return 1;
                case ShaderType.Geometry:
                    return 2;
                case ShaderType.Hull:
                    return 3;
                case ShaderType.Domain:
                    return 4;
                case ShaderType.RayTracing:
                    return 5;
            }
            return -1;
        }
        private void ConstructFlag(int subShaderIndex,int passIndex,string typeStr)
        {
            var serializeObject = new SerializedObject(shader);
            // get keyword list
            var keywordsProp = serializeObject.FindProperty("m_ParsedForm.m_KeywordNames");
            if (keywordsProp == null || !keywordsProp.isArray)
            {
                return;
            }
            this.CollectKeywordStrings(keywordsProp);

            // sub shaders
            var subShadersProp = serializeObject.FindProperty("m_ParsedForm.m_SubShaders");
            if (subShadersProp == null || !subShadersProp.isArray)
            {
                return;
            }
            int arraySize = subShadersProp.arraySize;
            if (subShaderIndex < 0 || subShaderIndex >= arraySize)
            {
                return;
            }
            ExecuteSubShader(subShadersProp.GetArrayElementAtIndex(subShaderIndex), passIndex,typeStr);            
        }

        private void ExecuteSubShader(SerializedProperty subShaderProp, int passIndex, string typeStr)
        {
            var passesProp = subShaderProp.FindPropertyRelative("m_Passes");
            if (passesProp == null || !passesProp.isArray)
            {
                return;
            }
            int arraySize = passesProp.arraySize;
            if(passIndex < 0 || passIndex >= arraySize)
            {
                return;
            }
            var currentPassProp = passesProp.GetArrayElementAtIndex(passIndex);
            ExecutePass(currentPassProp, typeStr);

        }

        private void ExecutePass(SerializedProperty passProp, string typeStr)
        {
            var stageProp = passProp.FindPropertyRelative(typeStr);
            if (stageProp == null) { return; }
            var masksProp = stageProp.FindPropertyRelative("m_SerializedKeywordStateMask");
            if (masksProp == null || !masksProp.isArray)
            {
                return;
            }
            this.validKeywords = new HashSet<string>();

            int arraySize = masksProp.arraySize;
            for (int i = 0; i < arraySize; ++i)
            {
                int index = masksProp.GetArrayElementAtIndex(i).intValue;
                string keywordName = this.keywords[index];
                this.validKeywords.Add(keywordName);
            }
        }


        private void CollectKeywordStrings(SerializedProperty keywordsProp)
        {
            int arraySize = keywordsProp.arraySize;
            this.keywords = new List<string>(arraySize);
            for (int i = 0; i < arraySize; ++i)
            {
                this.keywords.Add(keywordsProp.GetArrayElementAtIndex(i).stringValue);
            }
        }


    }
}