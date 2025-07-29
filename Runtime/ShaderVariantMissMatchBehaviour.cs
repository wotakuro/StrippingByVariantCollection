#if DEVELOPMENT_BUILD || UNITY_EDITOR
/**
MIT License

Copyright (c) 2024 Yusuke Kurokawa

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Rendering;
using PlayerConnection = UnityEngine.Networking.PlayerConnection.PlayerConnection;


namespace UTJ.ShaderVariantStripping.Runtime
{

    public class ShaderVariantMissMatchBehaviour : MonoBehaviour
    {

        public static readonly System.Guid RequestSendAllMissMatch = new System.Guid("88AD4F8F-7C5E-410F-8EED-FF569CCA7C65");
        public static readonly System.Guid SendAllMissMatch = new System.Guid("88AD4F8F-7C5E-410F-8EED-FF569CCA7C66");

        public struct ShaderNotFoundError : IComparer<ShaderNotFoundError>
        {
            public string shader;
            public string realShader;
            public int subShader;
            public int pass;
            public string stage;
            public string variant;

            public static ShaderNotFoundError CreateData(string str)
            {
                bool isStartFromShader = str.StartsWith("Shader ");
                bool endWithNotFound = str.EndsWith(" not found.");
                // this is not shader error
                if (!isStartFromShader || !endWithNotFound)
                {
                    return CreateErrorData();
                }
                int variantIdx = str.IndexOf(": variant ");
                if (variantIdx == -1)
                {
                    return CreateErrorData();
                }
                int subShaderIdx = str.IndexOf(", subshader ");
                if (subShaderIdx == -1)
                {
                    return CreateErrorData();
                }
                int passIdx = str.IndexOf(", pass ");
                if (passIdx == -1)
                {
                    return CreateErrorData();
                }
                int stageIdx = str.IndexOf(", stage ");
                if (stageIdx == -1)
                {
                    return CreateErrorData();
                }
                int realShaderIdx = str.IndexOf(" (real shader ");
                int shaderEndIndex = 0;
                if (realShaderIdx == -1)
                {
                    shaderEndIndex = subShaderIdx;
                }
                else
                {
                    shaderEndIndex = realShaderIdx;
                }
                const int shaderStartIdx = 7;

                string shaderName = str.Substring(shaderStartIdx, shaderEndIndex - shaderStartIdx);
                string realShaderName = null;
                if (realShaderIdx != -1)
                {
                    int realShaderStartIdx = realShaderIdx + 14;
                    realShaderName = str.Substring(realShaderStartIdx, subShaderIdx - realShaderStartIdx);
                }
                int subShaderStartIdx = subShaderIdx + 12;
                string subShaderName = str.Substring(subShaderStartIdx, passIdx - subShaderStartIdx);
                int subShaderParam;
                if (!int.TryParse(subShaderName, out subShaderParam))
                {
                    return CreateErrorData();
                }

                int passStartIdx = passIdx + 7;
                string passName = str.Substring(passStartIdx, stageIdx - passStartIdx);
                int passParam;
                if (!int.TryParse(passName, out passParam))
                {
                    return CreateErrorData();
                }

                int stageStartIdx = stageIdx + 8;
                string stageName = str.Substring(stageStartIdx, variantIdx - stageStartIdx);
                int variantStartIdx = variantIdx + 10;
                int variantEndIdx = str.Length - 11;
                string variantName = str.Substring(variantStartIdx, variantEndIdx - variantStartIdx);

                return new ShaderNotFoundError
                {
                    shader = shaderName,
                    realShader = realShaderName,
                    subShader = subShaderParam,
                    pass = passParam,
                    stage = stageName,
                    variant = variantName
                };

            }

            public bool IsValid()
            {
                return (shader != null);
            }

            private static ShaderNotFoundError CreateErrorData()
            {
                return new ShaderNotFoundError { shader = null };
            }

            public int Compare(ShaderNotFoundError x, ShaderNotFoundError y)
            {
                int shaderComp = string.Compare(x.shader, y.shader);
                if (shaderComp != 0) { return shaderComp; }
                if (x.realShader == null && y.realShader != null)
                {
                    return 1;
                }
                else if (x.realShader != null && y.realShader == null)
                {
                    return -1;
                }
                else if (x.realShader != null && y.realShader != null)
                {
                    int realShaderComp = string.Compare(x.realShader, y.realShader);
                }
                int subShaderComp = x.subShader.CompareTo(y.subShader);
                if (subShaderComp != 0)
                {
                    return subShaderComp;
                }
                int passComp = x.pass.CompareTo(y.pass);
                if (passComp != 0)
                {
                    return passComp;
                }
                int stageComp = x.stage.CompareTo(y.stage);
                if (stageComp != 0)
                {
                    return stageComp;
                }
                return x.variant.CompareTo(y.variant);
            }

            public override string ToString()
            {
                var sb = new System.Text.StringBuilder(512);
                sb.Append("Shader:").Append(shader).Append("\n");
                if (realShader != null)
                {
                    sb.Append("realShader:").Append(realShader).Append("\n");
                }
                sb.Append("subShader ").Append(subShader).Append("\n");
                sb.Append("pass:").Append(pass).Append("\n");
                sb.Append("stage:").Append(stage).Append("\n");
                sb.Append("variant:").Append(variant).Append("\n");
                return sb.ToString();
            }
        }

        /// <summary>
        /// ShaderVariantMatching発生時のエラーのDelgate
        /// </summary>
        /// <param name="shaderInfo">発生した時の情報</param>
        public delegate void VariantMissMatchEvent(ShaderNotFoundError shaderInfo);

        public VariantMissMatchEvent variantMissMatchEvent;

        public string logFile
        {
            get
            {
                return writeToLog;
            }
            set
            {
                if(writeToLog != value)
                {
                    WriteCurrentFrameToLog(value);
                }
                writeToLog = value;
            }
        }
        private string writeToLog = null;

        private HashSet<ShaderNotFoundError> shaderErrors = new HashSet<ShaderNotFoundError>();
        private List<ShaderNotFoundError> shaderErrorAtThisFrame = new List<ShaderNotFoundError>();
        private List<ShaderNotFoundError> shaderErrorsBuffer = new List<ShaderNotFoundError>();


        private static ShaderVariantMissMatchBehaviour instance;

        private StringBuilder stringBuilderBuffer = new StringBuilder(1024);
        private StringBuilder missMatchStringBuffer;

        private GraphicsDeviceType currentGraphicDeviceType;
        private RuntimePlatform currentPlatform;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Create()
        {
            var gmo = new GameObject("CatchShaderNotFoundLog", typeof(ShaderVariantMissMatchBehaviour));
            GameObject.DontDestroyOnLoad(gmo);
        }

        public static ShaderVariantMissMatchBehaviour Instance
        {
            get { return instance; }
        }

        private void Awake()
        {
            GameObject.DontDestroyOnLoad(this.gameObject);
            instance = this;
            this.currentGraphicDeviceType = SystemInfo.graphicsDeviceType;
            this.currentPlatform = Application.platform;

            Application.logMessageReceivedThreaded += OnHandleLog;
            PlayerConnection.instance.Register(RequestSendAllMissMatch, OnRecieveSendAllMissMatchRequest);
        }


        private void OnRecieveSendAllMissMatchRequest(MessageEventArgs args)
        {
            byte[] message;
            lock (this)
            {
                if (this.missMatchStringBuffer == null)
                {
                    this.missMatchStringBuffer = new StringBuilder(1024);
                }
                else
                {
                    this.missMatchStringBuffer.Clear();
                }
                BuildStringBuilder(missMatchStringBuffer);
                message = Encoding.UTF8.GetBytes(missMatchStringBuffer.ToString());
            }
            PlayerConnection.instance.Send(SendAllMissMatch,message);
        }

        private void BuildStringBuilder(StringBuilder sb)
        {
            sb.Append((int)this.currentPlatform).Append(",").
                Append((int)this.currentGraphicDeviceType);
            sb.Append('\n');

            foreach (var error in this.shaderErrors)
            {
                sb.Append(error.shader).Append(',').
                    Append(error.realShader).Append(',').
                    Append(error.subShader).Append(',').
                    Append(error.pass).Append(',').
                    Append(error.stage).Append(',').
                    Append(error.variant).Append('\n');
            }

        }

        private void OnDestroy()
        {
            instance = null;
        }


        private void Update()
        {
            lock (this)
            {
                shaderErrorsBuffer.Clear();
                foreach (var obj in shaderErrorAtThisFrame)
                {
                    this.shaderErrorsBuffer.Add(obj);
                }
                shaderErrorAtThisFrame.Clear();
            }

            // execute
            stringBuilderBuffer.Clear();
            foreach (var obj in shaderErrorsBuffer)
            {
                if (variantMissMatchEvent != null)
                {
                    variantMissMatchEvent.Invoke(obj);
                }
                if (!string.IsNullOrEmpty(logFile))
                {
                    AppendToLogText(stringBuilderBuffer, obj);
                }
            }
            if (!string.IsNullOrEmpty(logFile) && stringBuilderBuffer.Length > 0)
            {
                System.IO.File.AppendAllText(logFile, stringBuilderBuffer.ToString());
            }
        }


        private void WriteCurrentFrameToLog(string path)
        {
            stringBuilderBuffer.Clear();
            stringBuilderBuffer.AppendLine("shader,realShader,subShader,pass,stage,variant");
            lock (this)
            {
                foreach(var obj in shaderErrors)
                {
                    if (!shaderErrorsBuffer.Contains(obj))
                    {
                        AppendToLogText(stringBuilderBuffer,  obj);
                    }
                }
            }
            System.IO.File.WriteAllText(path,stringBuilderBuffer.ToString());
        }
        private void AppendToLogText(StringBuilder sb,ShaderNotFoundError obj)
        {
            sb.Append(obj.shader).Append(",");
            if (obj.realShader != null)
            {
                sb.Append(obj.realShader).Append(",");
            }
            else
            {
                sb.Append(",");
            }
            sb.Append(obj.subShader).Append(",");
            sb.Append(obj.pass).Append(",");
            sb.Append(obj.stage).Append(",");
            sb.Append(obj.variant).Append(",");
            sb.Append("\n");

        }

        private void OnHandleLog(string logString, string stackTrace, LogType type)
        {
            if (type != LogType.Error)
            {
                return;
            }
            var obj = ShaderNotFoundError.CreateData(logString);
            if (!obj.IsValid())
            {
                return;
            }
            lock (this)
            {
                if (!shaderErrors.Contains(obj))
                {
                    this.shaderErrors.Add(obj);
                    this.shaderErrorAtThisFrame.Add(obj);
                }
            }
        }

    }
}
#endif