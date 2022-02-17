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
            public List<string> exclude;
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


    }

}