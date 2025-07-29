using System.IO;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UTJ.ShaderVariantStripping
{

    public class TestMissMatchWin : EditorWindow
    {
        [MenuItem("Tools/TestMissMatch")]
        public static void Create()
        {
            TestMissMatchWin.GetWindow<TestMissMatchWin>();
        }

        IConnectionState attachProfilerState;

        public void OnEnable()
        {

            attachProfilerState = PlayerConnectionGUIUtility.GetConnectionState(this, OnConnected);
        }
        private void OnDisable()
        {
            attachProfilerState.Dispose();
        }

        private void OnConnected(string player)
        {
            Debug.Log(string.Format("MyWindow connected to {0}", player));
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Player",GUILayout.Width(200) );
            PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(attachProfilerState);

            EditorGUILayout.EndHorizontal();
            if ( GUILayout.Button("Get FromConnection"))
            {
                CreateGSCFromMissMatchLog.Instance.SendRequest();
            }
        }
    }

    public class CreateGSCFromMissMatchLog : UnityEngine.Object
    {
        public static readonly System.Guid RequestSendAllMissMatch = new System.Guid("88AD4F8F-7C5E-410F-8EED-FF569CCA7C65");
        public static readonly System.Guid SendAllMissMatch = new System.Guid("88AD4F8F-7C5E-410F-8EED-FF569CCA7C66");

        public static readonly byte[] dummySendData = new byte[0];

        public static CreateGSCFromMissMatchLog Instance { get; private set; } = new CreateGSCFromMissMatchLog();


        private CreateGSCFromMissMatchLog()
        {
            EditorConnection.instance.Register(SendAllMissMatch, OnRecieveAllMissMatch);
        }

        public void Dispose()
        {
            EditorConnection.instance.Unregister(SendAllMissMatch, OnRecieveAllMissMatch);
        }

        public void SendRequest()
        {
            EditorConnection.instance.TrySend(RequestSendAllMissMatch, dummySendData);
        }

        private static void OnRecieveAllMissMatch(MessageEventArgs messageEventArgs)
        {
            var str = System.Text.UTF8Encoding.UTF8.GetString( messageEventArgs.data );
            System.IO.File.WriteAllText("test.txt", str);
            ExecuteRecivedString(str);
        }

        public static void ExecuteRecivedString(string str)
        {
            string[] lines = str.Split('\n');
            if (lines == null || lines.Length < 2)
            {
                return;
            }
            var gsc = GetGSCObject(str);
            if(gsc == null)
            {
                return;
            }
            for (int i = 1; i < lines.Length; i++)
            {
                ExecuteLine(gsc, lines[i]);
            }
            RuntimePlatform platform;
            GraphicsDeviceType deviceType;
            GetHeaderInfo(str, out platform, out deviceType);
            var path = GetGSCPathName(platform, deviceType);
            gsc.SaveToFile(path);
        }
        private static void ExecuteLine(GraphicsStateCollection graphicsState,string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }
            var columns = line.Split(',');
            if (columns == null || columns.Length < 6)
            {
                return;
            }
            Shader shader = Shader.Find(columns[0]);
            uint subShaderIdx, passIdx;
            if(!uint.TryParse(columns[2], out subShaderIdx))
            {
                return;
            }
            if(!uint.TryParse(columns[3], out passIdx))
            {
                return;
            }
            PassIdentifier passIdentifier = new PassIdentifier(subShaderIdx, passIdx);

            string[] variants = columns[5].Split(' ');
            LocalKeyword[] keywords = new LocalKeyword[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                keywords[i] = new LocalKeyword( shader, variants[i] );
            }
            graphicsState.AddVariant(shader, passIdentifier, keywords);
        }

        private static GraphicsStateCollection GetGSCObject( string str)
        {
            RuntimePlatform platform;
            GraphicsDeviceType deviceType;
            if(!GetHeaderInfo(str, out platform, out deviceType))
            {
                Debug.LogWarning("Wrong format");
                return null;
            }
            var path = GetGSCPathName(platform, deviceType);
            var gsc = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(path);
            if(gsc == null)
            {
                gsc = new GraphicsStateCollection();
                gsc.runtimePlatform = platform;
                gsc.graphicsDeviceType = deviceType;
                gsc.SaveToFile(path);
            }
            return gsc;
        }


        private static string GetGSCPathName(RuntimePlatform platform,GraphicsDeviceType type)
        {
            const string dir = "Assets/MissingVariant";
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            var sb = new System.Text.StringBuilder(64);

            sb.Append(dir).Append("/MissingGSC_").
                Append(platform.ToString()).Append("_").Append(type.ToString());
            sb.Append(".graphicsstate");
            return sb.ToString();
        }

        private static bool  GetHeaderInfo(string str,
            out RuntimePlatform platform,
            out GraphicsDeviceType type)
        {
            platform = RuntimePlatform.WindowsEditor;
            type = GraphicsDeviceType.Null;
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }
            int lineEndIdx = str.IndexOf('\n');
            string head = str.Substring(0, lineEndIdx);
            var columns = head.Split(',');
            if( columns == null || columns.Length < 2)
            {
                return false;
            }
            int c0, c1;
            if (!int.TryParse(columns[0], out c0))
            {
                return false;
            }
            if (!int.TryParse(columns[1], out c1))
            {
                return false;
            }
            platform = (RuntimePlatform)c0;
            type = (GraphicsDeviceType)c1;
            return true;
        }
    }
}