using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// not used
// for debug
namespace UTJ
{
    public class ShaderKeywordMaskGetter
    {
        public struct PassInfo : IComparer<PassInfo>
        {
            public int subShaderIndex;
            public int passIndex;

            public int Compare(PassInfo x, PassInfo y)
            {
                int val = x.subShaderIndex.CompareTo(y.subShaderIndex);
                if(val != 0) { return val; }
                return x.passIndex.CompareTo(y.passIndex);
            }

            public override string ToString()
            {
                return subShaderIndex.ToString() + "-" + passIndex.ToString();
            }
        }

        private const int FLAG_VERTEX = 0x01;
        private const int FLAG_FRAGMENT = 0x02;
        private const int FLAG_GEOMETRY = 0x04;
        private const int FLAG_HULL = 0x08;
        private const int FLAG_DOMAIN = 0x10;
        private const int FLAG_RAYTRACE = 0x20;

        private Shader shader;
        private Dictionary<string, int> keywordFlags;
        private List<string> keywords;

        private HashSet<PassInfo> passInfos;

        public ShaderKeywordMaskGetter(Shader sh)
        {
            this.shader = sh;
            ConstructFlag();

            // Debug 
            //DebugKeywords();
        }
        public List<string> allKeywords
        {
            get
            {
                return keywords;
            }
        }

        public List<PassInfo> GetAllPasses()
        {
            var list = new List<PassInfo>();
            foreach(PassInfo passId in passInfos){
                list.Add(passId);
            }
            return list;
        }


        private void ConstructFlag()
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
            this.passInfos = new HashSet<PassInfo>();
            this.keywordFlags = new Dictionary<string, int>();
            int arraySize = subShadersProp.arraySize;
            for (int i = 0; i < arraySize; ++i)
            {
                ExecuteSubShader(subShadersProp.GetArrayElementAtIndex(i), i);
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

        void ExecuteSubShader(SerializedProperty subShaderProp, int subShaderIdx)
        {
            var passesProp = subShaderProp.FindPropertyRelative("m_Passes");
            if (passesProp == null || !passesProp.isArray)
            {
                return;
            }
            int arraySize = passesProp.arraySize;
            for (int i = 0; i < arraySize; ++i)
            {
                var currentPassProp = passesProp.GetArrayElementAtIndex(i);
                ExecutePass(currentPassProp, "progVertex", FLAG_VERTEX,
                    subShaderIdx, i);
                ExecutePass(currentPassProp, "progFragment", FLAG_FRAGMENT,
                    subShaderIdx, i);
                ExecutePass(currentPassProp, "progGeometry", FLAG_GEOMETRY,
                    subShaderIdx, i);
                ExecutePass(currentPassProp, "progHull", FLAG_HULL,
                    subShaderIdx, i);
                ExecutePass(currentPassProp, "progDomain", FLAG_DOMAIN,
                    subShaderIdx, i);
                ExecutePass(currentPassProp, "progRayTracing", FLAG_RAYTRACE,
                    subShaderIdx, i);
            }

        }

        void ExecutePass(SerializedProperty passProp, string pgStage, int flag,
            int subShaderIdx, int passIdx)
        {
            var stageProp = passProp.FindPropertyRelative(pgStage);
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
                string keywordName = this.keywords[index];

                int flags = 0;
                if (this.keywordFlags.TryGetValue(keywordName, out flags))
                {
                    this.keywordFlags[keywordName] = flags | flag;
                }
                else
                {
                    this.keywordFlags.Add(keywordName, flag);
                }
            }
        }

        private void DebugKeywords()
        {
            foreach (var keyword in this.keywords)
            {
                string str = keyword + "::";
                if (IsUsedForVertexProgram(keyword))
                {
                    str += "v";
                }
                else { str += "-"; }
                if (IsUsedForFragmentProgram(keyword))
                {
                    str += "f";
                }
                else { str += "-"; }
                Debug.Log(str);
            }
        }


        #region GET_FLAGS
        public bool IsUsedForVertexProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_VERTEX);
        }
        public bool IsUsedForFragmentProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_FRAGMENT);
        }
        public bool IsUsedForGeometryProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_GEOMETRY);
        }
        public bool IsUsedForHullProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_HULL);
        }
        public bool IsUsedForDomainProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_DOMAIN);
        }
        public bool IsUsedForRaytraceProgram(string keyword)
        {
            return IsUsedForProgram(keyword, FLAG_RAYTRACE);
        }
        private bool IsUsedForProgram(string keyword, int flag)
        {
            int val;
            if (keywordFlags == null)
            {
                return true;
            }
            if (!keywordFlags.TryGetValue(keyword, out val))
            {
                return true;
            }
            return ((val & flag) == flag);
        }

        #endregion GET_FLAGS

    }
}