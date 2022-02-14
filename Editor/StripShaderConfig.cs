using UnityEngine;
using UnityEditor;
using System.IO;

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
                if (!value)
                {
                    DisableOtherStipper = false;
                }
                SaveConfigData();
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
                currentConfig.disableOtherStipper = value;
                SaveConfigData();
            }
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