using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static UTJ.ShaderVariantStripping.ProjectSVCData;


namespace UTJ.ShaderVariantStripping
{
    public class RuntimePassIndexConverter 
    {
        private struct SubShaderKey : IEqualityComparer<SubShaderKey>
        {
            public Shader shader;
            public uint subShaderIndex;

            public SubShaderKey (Shader sh, uint subIdx)
            {
                this.shader = sh;
                this.subShaderIndex = subIdx;
            }

            public bool Equals(SubShaderKey x, SubShaderKey y)
            {
                return (x.shader==y.shader) && (x.subShaderIndex==y.subShaderIndex);
            }

            public int GetHashCode(SubShaderKey obj)
            {
                return obj.GetHashCode() + (int)obj.subShaderIndex;
            }
        }

        private Dictionary<SubShaderKey, Dictionary<uint, int>> passGpuProgramCount = new Dictionary<SubShaderKey, Dictionary<uint, int>>();
        private List<uint> indeciesBuffer = new List<uint>();


        public void Initialize()
        {
            passGpuProgramCount.Clear();
            indeciesBuffer.Clear();
        }

        public void SetExecuteNum(Shader shader,ShaderSnippetData snippetData,int count)
        {
            if(snippetData.shaderType != ShaderType.Fragment)
            {
                return;
            }
            uint subShaderIdx = snippetData.pass.SubshaderIndex;
            Dictionary<uint, int> passCount;
            SubShaderKey key = new SubShaderKey(shader, subShaderIdx);

            int maxIndex = GetMaxIndex(shader,subShaderIdx);
            if( maxIndex > (int)snippetData.pass.PassIndex)
            {
                this.Initialize();
            }
            if (!this.passGpuProgramCount.TryGetValue(key, out passCount))
            {
                passCount = new Dictionary<uint, int>();
                this.passGpuProgramCount.Add(key, passCount );
            }
            passCount.Add( snippetData.pass.PassIndex , count);
        }

        public PassIdentifier GetRuntimePassIdentifier(Shader shader, ref ShaderSnippetData snippetData)
        {
            this.SetupList(shader, snippetData );
            uint count = 0;
            int length = this.indeciesBuffer.Count;
            for (int i = 0; i < length; i++)
            {
                if (indeciesBuffer[i] >= snippetData.pass.PassIndex)
                {
                    break;
                }
                ++count;
            }
            return new PassIdentifier(snippetData.pass.SubshaderIndex ,count);
        }

        private int GetMaxIndex(Shader shader,uint subShaderIndex)
        {

            SubShaderKey key = new SubShaderKey(shader, subShaderIndex);
            Dictionary<uint, int> passCount;

            if (!this.passGpuProgramCount.TryGetValue(key, out passCount))
            {
                return -1;
            }
            uint maxPassIdx = 0;
            foreach (var passIdx in passCount.Keys)
            {
                if(maxPassIdx < passIdx)
                {
                    maxPassIdx = passIdx;
                }
            }

            return (int)maxPassIdx;

        }

        private void SetupList(Shader shader, ShaderSnippetData snippetData)
        {
            SubShaderKey key = new SubShaderKey(shader, snippetData.pass.SubshaderIndex);


            this.indeciesBuffer.Clear();
            Dictionary<uint, int> passCount;

            if (!this.passGpuProgramCount.TryGetValue(key, out passCount))
            {
                return;
            }
            foreach (var kvs in passCount)
            {
                if( kvs.Value <= 0)
                {
                    continue;
                }
                this.indeciesBuffer.Add( kvs.Key );
            }
            this.indeciesBuffer.Sort();
        }

    }
}