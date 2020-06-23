using UnityEngine;
using UnityEditor;
using System.IO;

using UnityEditor.Callbacks;

namespace UTJ
{
    public class StripEnableChecker
    {
        private const string ConfigFile = "ShaderVariants/config.txt";
        private const string MenuName = "Tools/UTJ/ShaderVariantStrip/Enable";
        private const string RestMenuName = "Tools/UTJ/ShaderVariantStrip/ResetInfo";

        [System.Serializable]
        struct ConfigData {
            public bool flag;
        }
        // config 
        private static bool EnableFlag = false;

        public static bool IsEnable
        {
            get { return EnableFlag; }
        }

        [MenuItem(MenuName, priority = 1)]
        public static void ChangeMode()
        {
            var flag = Menu.GetChecked(MenuName);
            flag = !flag;
            Menu.SetChecked(MenuName, flag);
            EnableFlag = flag;
            SaveConfigData();
        }

        [InitializeOnLoadMethod]
        public static void Init()
        {
            if( !File.Exists(ConfigFile))
            {
                EnableFlag = true;
                Menu.SetChecked(MenuName, EnableFlag);
                return;
            }
            var config = ReadConfigData();
            EnableFlag = config.flag;
            Menu.SetChecked(MenuName, EnableFlag);
        }

        private static ConfigData ReadConfigData()
        {
            string str = File.ReadAllText(ConfigFile);
            var data = JsonUtility.FromJson<ConfigData>(str);
            return data;
        }

        private static void SaveConfigData()
        {
            ConfigData data = new ConfigData() { flag = EnableFlag };
            var str = JsonUtility.ToJson(data);
            string dir = Path.GetDirectoryName(ConfigFile);
            if ( !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(ConfigFile, str);
        }

        [MenuItem(RestMenuName)]
        public static void Reset()
        {
            StrippingByVariantCollection.ResetData();
        }

    }

}