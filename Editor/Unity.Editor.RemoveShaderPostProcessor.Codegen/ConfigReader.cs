using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UTJ.ShaderVariantStripping.CodeGen
{
    public class ConfigReader
    {

        private const string ConfigFile = "ShaderVariants/v2_config.txt";

        public bool ExecuteRemoveOthers { get; private set; }

        public ConfigReader()
        {
            try
            {
                Init();
            }catch(System.Exception e)
            {
                ExecuteRemoveOthers = false;

            }
        }

        private void Init()
        {
            if (!File.Exists(ConfigFile))
            {
                ExecuteRemoveOthers = false;
                return;
            }
            string str = File.ReadAllText(ConfigFile);
            bool enabled = GetFlag(str, "enabled");
            bool strictMode = GetFlag(str, "strictVariantStripping");
            bool disableUnityStrip = GetFlag(str, "disableUnityStrip");

            /*
            System.IO.File.WriteAllText("debug.txt",
                "enabled:" + enabled + "\n" +
                "strictMode:" + strictMode + "\n" +
                "disableOthers:" + disableOthers + "\n" );
            */
            ExecuteRemoveOthers = enabled & strictMode & disableUnityStrip;
        }


        private bool GetFlag(string str,string target,bool defulatVal = false) {
            int disableStart = str.IndexOf(target);
            if(disableStart <0)
            {
                return defulatVal;
            }
              
            int idx = str.IndexOf(':', disableStart);
            if(idx < 0)
            {
                return defulatVal;
            }
            ++idx;
            for (; char.IsWhiteSpace(str[idx]); ++idx) ;
            if (str[idx] == '"') { ++idx; }


            if ( str[idx] == 't' || str[idx + 1] == 'T' ){
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
