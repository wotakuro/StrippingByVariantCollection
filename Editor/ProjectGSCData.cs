using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using UnityEngine.Experimental.Rendering;


namespace UTJ.ShaderVariantStripping
{
    public class ProjectGSCData 
    {

        struct GraphicsStateData
        {

        }

        private void Set()
        {
            GraphicsStateCollection collection;
        }

        private static List<GraphicsStateCollection> GetProjectGraphicsStateCollections()
        {
            var collections = new List<GraphicsStateCollection>();
            var guids = AssetDatabase.FindAssets("t: GraphicsStateCollection");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(path);
                if (obj != null)
                {
                    collections.Add(obj);
                }
            }
            var excludeList = StripShaderConfig.GetExcludeGSC();
            foreach (var exclude in excludeList)
            {
                if (exclude != null)
                {
                    collections.Remove(exclude);
                }
            }

            return collections;
        }
    }
}