using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Callbacks;

using UnityEngine.Experimental.Rendering;

namespace UTJ.ShaderVariantStripping
{
    internal class StripShaderConfig
    {
        private const string ConfigFile = "ShaderVariants/v3_config.txt";

        [System.Serializable]
        struct ConfigData {
            public bool enabled;
            public bool logEnabled;
            public bool strictVariantStripping;
            public bool safeMode;
            public int order;
            public List<string> excludeVariantCollection;

            // from Unity6
            // Set the default value to False in consideration of migration.
            public bool disableSVC; // SVC = ShaderVariantCollection;
            public bool disableGSC;// GSC = GraphicsStateCollection

            public bool matchGSCGraphicsAPI;
            public bool matchGSCPlatform;
            public List<string> exlucdeGSC;

        }


        private static ConfigData currentConfig;

        public static bool IsEnable
        {
            get { return currentConfig.enabled; }
            set
            {
                currentConfig.enabled = value;
                SaveConfigData();

            }
        }
        public static bool IsLogEnable
        {
            get { return currentConfig.logEnabled; }
            set {
                currentConfig.logEnabled = value;
                SaveConfigData();
            }
        }



        public static bool StrictVariantStripping
        {
            get { return currentConfig.strictVariantStripping; }
            set
            {

                currentConfig.strictVariantStripping = value;
                SaveConfigData();
            }
        }


        // from U6
        #region UNITY_6

        public static bool SafeMode
        {
            get
            {
                return currentConfig.safeMode;
            }
            set
            {
                currentConfig.safeMode = value;
                SaveConfigData();
            }
        }


        public static bool UseSVC
        {
            get { return !currentConfig.disableSVC; }
            set
            {
                currentConfig.disableSVC = !value;
                SaveConfigData();
            }
        }
        public static bool UseGSC
        {
            get { return !currentConfig.disableGSC; }
            set
            {
                currentConfig.disableGSC = !value;
                SaveConfigData();
            }
        }
        public static bool MatchGSCGraphicsAPI
        {
            get { return !currentConfig.matchGSCGraphicsAPI; }
            set
            {
                currentConfig.matchGSCGraphicsAPI = value;
                SaveConfigData();
            }
        }
        public static bool MatchGSCPlatform
        {
            get { return !currentConfig.matchGSCPlatform; }
            set
            {
                currentConfig.matchGSCPlatform = value;
                SaveConfigData();
            }
        }

        #endregion UNITY_6



        [InitializeOnLoadMethod]
        public static void Init()
        {
            if( !File.Exists(ConfigFile))
            {
                currentConfig = new ConfigData()
                {
                    enabled = true,
                    strictVariantStripping = false,
                    disableGSC = false,
                    disableSVC = false,
                    logEnabled = true,
                    order = int.MaxValue,
                    safeMode = true,
                };
                return;
            }
            currentConfig = ReadConfigData();
        }

        private static ConfigData ReadConfigData()
        {
            string str = File.ReadAllText(ConfigFile);
            var data = JsonUtility.FromJson<ConfigData>(str);
            return data;
        }

        private static void SaveConfigData()
        {
            SaveConfigData(ConfigFile);
        }
        internal static void LogConfigData(string path)
        {
            if(System.IO.File.Exists(path)){
                return;
            }
            SaveConfigData(path);
        }
        private static void SaveConfigData(string path)
        {
            var str = JsonUtility.ToJson(currentConfig);
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, str);

        }

        public static List<ShaderVariantCollection> GetExcludeVariantCollectionAsset()
        {
            List<ShaderVariantCollection> list = new List<ShaderVariantCollection>();
            if( currentConfig.excludeVariantCollection != null)
            {
                foreach( var collectionPath in currentConfig.excludeVariantCollection)
                {
                    var variantCollectionAsset = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(collectionPath);
                    list.Add(variantCollectionAsset);
                }
            }
            return list;
        }

        public static void SetExcludeVariantCollection(List<ShaderVariantCollection> list)
        {
            List<string> paths = new List<string>();
            foreach (var collectionAsset in list)
            {
                paths.Add(AssetDatabase.GetAssetPath(collectionAsset));
            }
            if(currentConfig.excludeVariantCollection == null)
            {
                currentConfig.excludeVariantCollection = new List<string>();
            }

            if(!IsSameList(paths,currentConfig.excludeVariantCollection))
            {
                currentConfig.excludeVariantCollection = paths;
                SaveConfigData();
            }
        }



        public static List<GraphicsStateCollection> GetExcludeGSC()
        {
            var list = new List<GraphicsStateCollection>();
            if (currentConfig.exlucdeGSC != null)
            {
                foreach (var collectionPath in currentConfig.exlucdeGSC)
                {
                    var variantCollectionAsset = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(collectionPath);
                    list.Add(variantCollectionAsset);
                }
            }
            return list;
        }

        public static void SetExcludeGSC(List<GraphicsStateCollection> list)
        {
            List<string> paths = new List<string>();
            foreach (var collectionAsset in list)
            {
                paths.Add(AssetDatabase.GetAssetPath(collectionAsset));
            }
            if (currentConfig.exlucdeGSC == null)
            {
                currentConfig.exlucdeGSC = new List<string>();
            }

            if (!IsSameList(paths, currentConfig.exlucdeGSC))
            {
                currentConfig.exlucdeGSC = paths;
                SaveConfigData();
            }
        }

        private static bool IsSameList(List<string> src1, List<string> src2)
        {
            if(src2 == null)
            {
                return false;
            }
            if( src1.Count != src2.Count) { return false; }

            for(int i = 0; i < src1.Count; ++i)
            {
                if(src1[i] != src2[i]) { return false; }
            }
            return true;
        }

    }

}