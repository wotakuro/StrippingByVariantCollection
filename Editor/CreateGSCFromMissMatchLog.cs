using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace UTJ.ShaderVariantStripping
{

    public class TestMissMatchWin : EditorWindow
    {
        [MenuItem("Tools/TestMissMatch")]
        public static void Create()
        {
            TestMissMatchWin.GetWindow<TestMissMatchWin>();
        }


        public void OnEnable()
        {
            
        }
        public void OnGUI()
        {
            GUILayout.Label("Connect:"+EditorConnection.instance.ConnectedPlayers);
            if( GUILayout.Button("Get FromConnection"))
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
        }
    }
}