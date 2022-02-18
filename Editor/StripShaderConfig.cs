using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Callbacks;

namespace UTJ.ShaderVariantStripping
{
    internal class StripShaderConfig
    {
        private const string ConfigFile = "ShaderVariants/v2_config.txt";

        [System.Serializable]
        struct ConfigData {
            public bool enabled;
            public bool logEnabled;
            public bool strictVariantStripping;
            public bool disableOtherStipper;
            public int order;
            public List<string> excludeVariantCollection;
        }

        private static ConfigData currentConfig;

        public static bool IsEnable
        {
            get { return currentConfig.enabled; }
            set
            {
                var backupFlag = ShouldRemoveOther;
                currentConfig.enabled = value;
                SaveConfigData();

                if( backupFlag != ShouldRemoveOther)
                {
                    ReloadCode();
                }
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
                var backupFlag = ShouldRemoveOther;

                currentConfig.strictVariantStripping = value;
                if (!value)
                {
                    DisableOtherStipper = false;
                }
                SaveConfigData();
                if (backupFlag != ShouldRemoveOther)
                {
                    ReloadCode();
                }
            }
        }

        public static int Order
        {
            get
            {
                return currentConfig.order;
            }
            set
            {
                currentConfig.order = value;
                SaveConfigData();
            }
        }
        public static bool DisableOtherStipper
        {
            get
            {
                return currentConfig.disableOtherStipper;
            }
            set
            {
                var backupFlag = ShouldRemoveOther;
                currentConfig.disableOtherStipper = value;
                SaveConfigData();
                if (backupFlag != ShouldRemoveOther)
                {
                    ReloadCode();
                }
            }
        }

        private static bool ShouldRemoveOther
        {
            get
            {
                return currentConfig.enabled & currentConfig.strictVariantStripping & currentConfig.disableOtherStipper;
            }
        }

        private static void ReloadCode()
        {
            AssetDatabase.ImportAsset("Packages/com.utj.stripvariant/Editor/StripShaderConfig.cs", ImportAssetOptions.ForceUpdate);
        }

        [InitializeOnLoadMethod]
        public static void Init()
        {
            if( !File.Exists(ConfigFile))
            {
                currentConfig = new ConfigData()
                {
                    enabled = true,
                    strictVariantStripping = false,
                    disableOtherStipper = false,
                    logEnabled = true,
                    order = int.MinValue,
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
            var str = JsonUtility.ToJson(currentConfig);
            string dir = Path.GetDirectoryName(ConfigFile);
            if ( !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(ConfigFile, str);
        }

        public static List<ShaderVariantCollection> GetVariantCollectionAsset()
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

        public static void SetVariantCollection(List<ShaderVariantCollection> list)
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