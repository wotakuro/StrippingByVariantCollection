using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static UTJ.ShaderVariantStripping.ProjectGSCData;

namespace UTJ.ShaderVariantStripping
{
    internal class GraphicsStateVariantDataComparer : IEqualityComparer<GraphicsStateVariantData>
    {
        public GraphicsStateVariantDataComparer()
        {
        }

        public bool Equals(GraphicsStateVariantData x, GraphicsStateVariantData y)
        {
            if (x.shader != y.shader)
            {
                return false;
            }
            if (x.passIdentifier != y.passIdentifier)
            {
                return false;
            }

            if (x.keywordsForCheck == null && y.keywordsForCheck == null)
            {
                return true;
            }
            if (x.keywordsForCheck == null)
            {
                return false;
            }
            if (y.keywordsForCheck == null)
            {
                return false;
            }

            if (x.keywordsForCheck.Count != y.keywordsForCheck.Count)
            {
                return false;
            }
            int cnt = x.keywordsForCheck.Count;
            for (int i = 0; i < cnt; ++i)
            {
                if (x.keywordsForCheck[i] != y.keywordsForCheck[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(GraphicsStateVariantData obj)
        {
            int baseVal = (int)obj.passIdentifier.SubshaderIndex << 4 + (int)obj.passIdentifier.PassIndex;
            if(obj.shader != null)
            {
                baseVal += obj.shader.GetHashCode();
            }
            if (obj.keywordsForCheck != null)
            {
                foreach (var str in obj.keywordsForCheck)
                {
                    baseVal += str.GetHashCode();
                }
            }
            return baseVal;
        }
    }
}