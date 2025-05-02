using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using static UTJ.ShaderVariantStripping.StrippingByVariantCollection;

namespace UTJ.ShaderVariantStripping
{

    class ShaderVarintInfoComparer : IEqualityComparer<ShaderVariantsInfo>
    {

        public bool Equals(ShaderVariantsInfo x, ShaderVariantsInfo y)
        {
            if (x.shader != y.shader)
            {
                return false;
            }
            if (x.passType != y.passType)
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

        public int GetHashCode(ShaderVariantsInfo obj)
        {
            int hashCode = 0;
            if (obj.shader != null)
            {
                hashCode += obj.shader.GetHashCode();
            }
            hashCode += obj.passType.GetHashCode();
            if (obj.keywordsForCheck != null)
            {
                foreach (var keyword in obj.keywordsForCheck)
                {
                    hashCode += keyword.GetHashCode();
                }
            }
            return hashCode;
        }
    }



}