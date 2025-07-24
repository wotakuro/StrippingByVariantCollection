using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using System.Text;
using System.Linq;
using UTJ.ShaderVariantStripping;

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


        private HashSet<string> pgTypeOnlyKeyword;
        private bool isExecuteConstructOnlyKeyword = false;

        public ShaderKeywordMaskGetterPerSnippet(Shader sh, ShaderSnippetData snippet)
        {
            this.shader = sh;
            this.snippetData = snippet;
            int typeIndex = GetTypeIndex(snippet.shaderType);
            int subShaderIndex = (int)snippet.pass.SubshaderIndex;
            int passIndex = (int)snippet.pass.PassIndex;
            ConstructValidKeywords(subShaderIndex, passIndex, ShaderTypeTable[typeIndex]);
        }

        #region SEARCH_OTHER_PROGRAM_TYPE

        public void ConstructOnlyKeyword()
        {
            var serializeObject = new SerializedObject(shader);

            this.isExecuteConstructOnlyKeyword = true;
            int subShaderIndex = (int)this.snippetData.pass.SubshaderIndex;
            int passIndex = (int)snippetData.pass.PassIndex;
            int typeIndex = GetTypeIndex(snippetData.shaderType);
            // sub shaders
            var subShadersProp = serializeObject.FindProperty("m_ParsedForm.m_SubShaders");
            if (subShadersProp == null || !subShadersProp.isArray)
            {
                return;
            }
            int subShadersSize = subShadersProp.arraySize;
            if (subShaderIndex < 0 || subShaderIndex >= subShadersSize)
            {
                return;
            }
            var subShaderProp = subShadersProp.GetArrayElementAtIndex(subShaderIndex);
            // pass
            var passesProp = subShaderProp.FindPropertyRelative("m_Passes");
            if (passesProp == null || !passesProp.isArray)
            {
                return;
            }
            int passesSize = passesProp.arraySize;
            if (passIndex < 0 || passIndex >= passesSize)
            {
                return;
            }
            var currentPassProp = passesProp.GetArrayElementAtIndex(passIndex);
            var onlyKeywordIndex = ConstructPassKeywordStateMask(currentPassProp, ShaderTypeTable[typeIndex]);
            for (int i = 0; i < ShaderTypeTable.Length; i++)
            {
                if (i != typeIndex)
                {
                    RemovePassKeywordStateMaskIfExist(currentPassProp, ShaderTypeTable[i], onlyKeywordIndex);
                }
            }
            this.pgTypeOnlyKeyword = new HashSet<string>();
            foreach(var index in onlyKeywordIndex)
            {

                pgTypeOnlyKeyword.Add(this.keywords[index]);
            }
        }

        private HashSet<int> ConstructPassKeywordStateMask(SerializedProperty passProp, string typeStr)
        {
            HashSet<int> passKeywordStateMask = null;
            var stageProp = passProp.FindPropertyRelative(typeStr);
            if (stageProp == null) { return null; }
            var masksProp = stageProp.FindPropertyRelative("m_SerializedKeywordStateMask");
            if (masksProp == null || !masksProp.isArray)
            {
                return null;
            }

            int arraySize = masksProp.arraySize;
            passKeywordStateMask = new HashSet<int>();
            for (int i = 0; i < arraySize; ++i)
            {
                int index = masksProp.GetArrayElementAtIndex(i).intValue;
                passKeywordStateMask.Add(index);
            }
            return passKeywordStateMask;
        }
        private void RemovePassKeywordStateMaskIfExist(SerializedProperty passProp, string typeStr, HashSet<int> passKeywordStateMask)
        {
            if(passKeywordStateMask == null) { return; }
            var stageProp = passProp.FindPropertyRelative(typeStr);
            if (stageProp == null) { return; }
            var masksProp = stageProp.FindPropertyRelative("m_SerializedKeywordStateMask");
            if (masksProp == null || !masksProp.isArray)
            {
                return;
            }

            int arraySize = masksProp.arraySize;
            for (int i = 0; i < arraySize; ++i)
            {
                int index = masksProp.GetArrayElementAtIndex(i).intValue;
                passKeywordStateMask.Remove(index);
            }
        }
        #endregion SEARCH_OTHER_PROGRAM_TYPE


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
                return ShaderNameUtility.GetShaderNameForPath(shader) + ShaderNameUtility.GetSnipetName(snippetData) + ".txt";
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
            stringBuilder.Append("Keywords:").Append(keywords.Count).Append("\n");
            foreach ( var keyword in this.keywords)
            {
                bool validFlag = this.ValidKeyword(keyword);
                stringBuilder.Append("  ").Append(keyword).Append(":").Append(validFlag);
                if (this.isExecuteConstructOnlyKeyword)
                {
                    stringBuilder.Append("  OnlyThisProgramType:").Append(this.IsThisProgramTypeOnlyKeyword(keyword) );
                }
                stringBuilder.Append("\n");
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
        public bool IsThisProgramTypeOnlyKeyword(string keyword)
        {
            if (this.pgTypeOnlyKeyword == null)
            {
                return true;
            }
            return pgTypeOnlyKeyword.Contains(keyword);
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
            if(validCount == length || validCount == 0)
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

        public List<string> ConvertValidOnlyKeywords(List<string> keywords)
        {
            if (keywords == null) { return null; }
            int validCount = 0;
            int length = keywords.Count;
            for (int i = 0; i < length; ++i)
            {
                if (ValidKeyword(keywords[i]))
                {
                    validCount++;
                }
            }
            if (validCount == length || validCount == 0)
            {
                return null;
            }
            var newKeywords = new List<string>(validCount);
            for (int i = 0; i < length; ++i)
            {
                if (ValidKeyword(keywords[i]))
                {
                    newKeywords.Add(keywords[i]);
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
        private void ConstructValidKeywords(int subShaderIndex,int passIndex,string typeStr)
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
            CreateValidKeywordInSubShader(subShadersProp.GetArrayElementAtIndex(subShaderIndex), passIndex,typeStr);            
        }

        private void CreateValidKeywordInSubShader(SerializedProperty subShaderProp, int passIndex, string typeStr)
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
            CreateValidKeywordInPass(currentPassProp, typeStr);

        }

        private void CreateValidKeywordInPass(SerializedProperty passProp, string typeStr)
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