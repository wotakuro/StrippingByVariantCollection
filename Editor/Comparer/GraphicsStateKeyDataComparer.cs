using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static UTJ.ShaderVariantStripping.ProjectGSCData;

namespace UTJ.ShaderVariantStripping
{
    internal class GraphicsStateKeyDataComparer : IEqualityComparer<GraphicsStateKeyData>
    {
        public GraphicsStateKeyDataComparer()
        {
        }

        public bool Equals(GraphicsStateKeyData x, GraphicsStateKeyData y)
        {
            if(x.runtimePlatform != y.runtimePlatform)
            {
                return false;
            }
            if (x.graphicsDeviceType != y.graphicsDeviceType)
            {
                return false;
            }
            if (x.version != y.version)
            {
                return false;
            }
            if (x.qualityLevelName != y.qualityLevelName)
            {
                return false;
            }
            return true;
        }

        public int GetHashCode(GraphicsStateKeyData obj)
        {
            int baseVal =  obj.version  + 
                (((int)obj.runtimePlatform) << 8) + 
                ((int)obj.graphicsDeviceType << 16);
            if (obj.qualityLevelName != null)
            {
                baseVal += obj.qualityLevelName.GetHashCode();
            }
            return baseVal;
        }
    }
}