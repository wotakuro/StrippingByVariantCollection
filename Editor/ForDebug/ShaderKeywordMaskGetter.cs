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

            public PassInfo(int subShader , int pass)
            {
                this.subShaderIndex = subShader;
                this.passIndex = pass;  
            }

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

        public class PassDetail
        {
            public string passName;
            public List<string> tags;
        }

        private const int FLAG_VERTEX = 0x01;
        private const int FLAG_FRAGMENT = 0x02;
        private const int FLAG_GEOMETRY = 0x04;
        private const int FLAG_HULL = 0x08;
        private const int FLAG_DOMAIN = 0x10;
        private const int FLAG_RAYTRACE = 0x20;

        private Shader shader;
        private Dictionary<string, int> keywordFlags;

        private Dictionary<PassInfo, Dictionary<string, int>> keywordFlagPerPass;
        private Dictionary<PassInfo, PassDetail> passDeitalInfos;

        private List<string> keywords;


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

        public List<PassInfo> allPasses
        {
            get
            {
                List<PassInfo> allPass = new List<PassInfo>(keywordFlagPerPass.Count);
                foreach(var key in keywordFlagPerPass.Keys)
                {
                    allPass.Add(key);
                }
                allPass.Sort((a, b) =>
                {
                    return (a.subShaderIndex - b.subShaderIndex) <<16 + (a.passIndex - b.passIndex);
                });
                return allPass;
            }
        }


        public PassDetail GetPassDetail(PassInfo passInfo)
        {
            PassDetail detail;
            if(this.passDeitalInfos.TryGetValue(passInfo, out detail) ){
                return detail;
            }
            return null;
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
            this.keywordFlags = new Dictionary<string, int>();
            this.keywordFlagPerPass = new Dictionary<PassInfo, Dictionary<string, int>>();
            this.passDeitalInfos = new Dictionary<PassInfo, PassDetail>();
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
            PassInfo passInfoKey = new PassInfo(subShaderIdx,passIdx);
            Dictionary<string, int> perPassFlags;
            if(!keywordFlagPerPass.TryGetValue(passInfoKey, out perPassFlags))
            {
                perPassFlags = new Dictionary<string, int>();
                this.keywordFlagPerPass.Add(passInfoKey, perPassFlags );
            }
            PassDetail passDetail;
            if (!this.passDeitalInfos.TryGetValue(passInfoKey,out passDetail))
            {
                passDetail = CreatePassDetail(passProp);
                this.passDeitalInfos.Add(passInfoKey , passDetail );
            }

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
                // add per pass
                if(perPassFlags.TryGetValue(keywordName,out flags)){
                    perPassFlags[keywordName] = flags | flag;
                }
                else
                {
                    perPassFlags.Add(keywordName, flag);
                }
            }
        }

        public PassDetail CreatePassDetail(SerializedProperty passProp)
        {
            PassDetail passDetail = new PassDetail();
            //"m_State.mName"
            var nameProp = passProp.FindPropertyRelative("m_State.m_Name");
            if(nameProp!= null)
            {
                passDetail.passName = nameProp.stringValue;
            }

            var tagsProp = passProp.FindPropertyRelative("m_State.m_Tags.tags");
            if(tagsProp == null || !tagsProp.isArray)
            {
                tagsProp = passProp.FindPropertyRelative("m_Tags.tags");
            }
            if (tagsProp != null && tagsProp.isArray)
            {
                passDetail.tags = new List<string>(tagsProp.arraySize);
                for(int i = 0;i < tagsProp.arraySize; ++i)
                {
                    var tagProp = tagsProp.GetArrayElementAtIndex(i);
                    var firstProp = tagProp.FindPropertyRelative("first");
                    var secondProp = tagProp.FindPropertyRelative("second");

                    string tagStr = firstProp.stringValue +" : " + secondProp.stringValue;
                    passDetail.tags.Add(tagStr);
                }
            }
            return passDetail;
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
        #region GET_FLAGS_PER_PASS
        public bool IsUsedForVertexProgram(int subPassIdx,int passIdx, string keyword)
        {
            PassInfo infokey = new PassInfo(subPassIdx, passIdx);
            return IsUsedPerPassForProgram(infokey, keyword, FLAG_VERTEX);
        }
        public bool IsUsedForFragmentProgram(int subPassIdx, int passIdx, string keyword)
        {
            PassInfo infokey = new PassInfo(subPassIdx, passIdx);
            return IsUsedPerPassForProgram(infokey, keyword, FLAG_FRAGMENT);
        }
        public bool IsUsedForGeometryProgram(int subPassIdx, int passIdx, string keyword)
        {
            PassInfo infokey = new PassInfo(subPassIdx, passIdx);
            return IsUsedPerPassForProgram(infokey, keyword, FLAG_GEOMETRY);
        }
        public bool IsUsedForHullProgram(int subPassIdx, int passIdx, string keyword)
        {
            PassInfo infokey = new PassInfo(subPassIdx, passIdx);
            return IsUsedPerPassForProgram(infokey, keyword, FLAG_HULL);
        }
        public bool IsUsedForDomainProgram(int subPassIdx, int passIdx, string keyword)
        {
            PassInfo infokey = new PassInfo(subPassIdx, passIdx);
            return IsUsedPerPassForProgram(infokey, keyword, FLAG_DOMAIN);
        }
        public bool IsUsedForRaytraceProgram(int subPassIdx, int passIdx, string keyword)
        {
            PassInfo infokey = new PassInfo(subPassIdx, passIdx);
            return IsUsedPerPassForProgram(infokey, keyword, FLAG_RAYTRACE);
        }

        private bool IsUsedPerPassForProgram(PassInfo infokey,string keyword, int flag)
        {
            Dictionary<string, int> perPass;
            if (keywordFlagPerPass == null)
            {
                return false;
            }
            if ( !this.keywordFlagPerPass.TryGetValue(infokey, out perPass))
            {
                return false;
            }
            int val;
            if (perPass == null)
            {
                return false;
            }
            if (!perPass.TryGetValue(keyword, out val))
            {
                return false;
            }
            return ((val & flag) == flag);
        }
        #endregion

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
                return false;
            }
            if (!keywordFlags.TryGetValue(keyword, out val))
            {
                return false;
            }
            return ((val & flag) == flag);
        }

        #endregion GET_FLAGS

    }
}