using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UTJ.ShaderVariantStripping
{
    internal class SortShaderKeywordComparer : IComparer<ShaderKeyword>
    {
        private Shader shader;
        public SortShaderKeywordComparer(Shader sh)
        {
            this.shader = sh;
        }

        public int Compare(ShaderKeyword x, ShaderKeyword y)
        {
#if UNITY_2021_2_OR_NEWER
            return x.name.CompareTo(y.name);
#else
                return ShaderKeyword.GetKeywordName(shader,x).CompareTo(ShaderKeyword.GetKeywordName(shader, y));
#endif
        }
    }
}