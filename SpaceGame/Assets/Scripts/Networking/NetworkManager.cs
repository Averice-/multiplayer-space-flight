using RiptideNetworking;
using RiptideNetworking.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShardStudios {


    public enum StarSystemID : ushort {
        NewSol = 1,
        Babylon = 2
    }


    public enum ClientMessageID : ushort {
        Connected = 1,
        LoginFailed,
        GetHash,
        ConnectToShard,
        ShardStartup,
        ShardAddPlayer,
        ShardSubPlayer,
        CreatePlayer,
        SendPlayerActiveData,
        PlayerLeft,
        ReadyForData,
        PlayerJoined,
        SpawnEntity
    }

    public class NetworkManager : MonoBehaviour
    {

        private static NetworkManager _instance;
        public static NetworkManager Instance {
            get => _instance;
            private set {
                if( _instance == null )
                    _instance = value;
                else if ( _instance != value ){
                    Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public Client Client { get; private set; }
        public ushort masterServerId { get; private set; }
        public StarSystemID CurrentSystem { get; private set; }

        public Client ShardClient { get; private set; }

        #if SHARD
            public StarSystemID systemId{ get; private set; }
            public Server Server { get; private set; }
        #endif

        [SerializeField] string ip;
        [SerializeField] ushort port;

        [SerializeField] ushort shardPort;
        [SerializeField] string shardIp;
        [SerializeField] ushort maxClients;

        private void Awake(){

            #if SHARD

                Debug.Log("Shard Starting");
                systemId = StarSystemID.NewSol;
                Debug.LogError("Force the build console open...");

            #endif
            Instance = this;

            DontDestroyOnLoad(this.gameObject);

        }

        private void Start(){

            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            Client = new Client();
            #if SHARD

                // Get the star system of this shard.
                var args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++){
                    if (args[i] == "--starname" && args.Length > i + 1){
                        systemId = (StarSystemID)int.Parse(args[i + 1]);
                    }else if( args[i] == "--ip" && args.Length > i + 1){
                        shardIp = args[i + 1];
                    }else if( args[i] == "--port" && args.Length > i + 1){
                        shardPort = (ushort)int.Parse(args[i + 1]);
                    }else if( args[i] == "--maxclients" && args.Length > i + 1){
                        maxClients = (ushort)int.Parse(args[i + 1]);
                    }
                }

                // Start our server. The Client talks to the master server.
                Server = new Server();

                Server.ClientConnected += NewPlayer;
                Server.ClientDisconnected += PlayerLeft;

                Server.Start(shardPort, maxClients);

                ConnectMaster();
                
            #else

                ShardClient = new Client();
                ShardClient.Connected += ShardClientConnected;
                ShardClient.ConnectionFailed += ShardClientConnectionFailed;
                ShardClient.Disconnected += ShardClientDisconnected;

            #endif

        }

        public void ConnectMaster(){
            Client.Connect($"{ip}:{port}");

            #if SHARD

                Message message = Message.Create(MessageSendMode.reliable, ClientMessageID.ShardStartup);
                message.AddUShort(shardPort);
                message.AddUShort((ushort)systemId);
                message.AddString(shardIp);
                message.AddUShort(maxClients);

                Client.Send(message);

                // Load system scene.
                SceneManager.LoadScene((int)systemId);

            #endif
        }

        private void FixedUpdate(){
            #if SHARD
                Server.Tick();
            #else
                if( ShardClient != null && ShardClient.IsConnected ){
                    ShardClient.Tick();
                }
            #endif
            Client.Tick();
        }

        private void OnApplicationQuit(){
            #if SHARD
                Server.Stop();

                Server.ClientConnected -= NewPlayer;
                Server.ClientDisconnected -= PlayerLeft;
            #else
                if( ShardClient != null && ShardClient.IsConnected ){
                    ShardClient.Disconnect();
                }
            #endif
            Client.Disconnect();

        }

        [MessageHandler((ushort)ClientMessageID.GetHash)]
        private static void GetClientHash(Message received){
            LoadingUI.SetLoadingText("Receiving unique identifier...");
            NetworkManager.Instance.masterServerId = received.GetUShort();
        }


        #if SHARD
            private void PlayerLeft(object sender, ClientDisconnectedEventArgs e){
                Message message = Message.Create(MessageSendMode.reliable, ClientMessageID.ShardSubPlayer);
                Client.Send(message);

                Message shardMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.PlayerLeft);
                shardMessage.AddUShort(e.Id);
                Server.SendToAll(shardMessage);

                Player.CleanupPlayer(e.Id);

                // Do cleanup.
            }

            private void NewPlayer(object sender, ServerClientConnectedEventArgs e){
                Message message = Message.Create(MessageSendMode.reliable, ClientMessageID.ShardAddPlayer);
                Client.Send(message);
            }
        #endif

        public static string Md5(string strToEncrypt){

            System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
            byte[] bytes = ue.GetBytes(strToEncrypt);
        
            // encrypt bytes
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);
        
            // Convert the encrypted bytes back to a string (base 16)
            string hashString = "";
            for (int i = 0; i < hashBytes.Length; i++){
                hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
            }
	  
	        return hashString.PadLeft(32, '0');
	    }

        [MessageHandler((ushort)ClientMessageID.ConnectToShard)]
        private static void ConnectToShard(Message details){

            Debug.Log("Received Connect Message");

            if( NetworkManager.Instance.ShardClient.IsConnected ){
                NetworkManager.Instance.ShardClient.Disconnect();
                // Do cleanup.
            }

            string serverIp = details.GetString();
            ushort serverPort = details.GetUShort();
            ushort systemId = details.GetUShort();

            NetworkManager.Instance.CurrentSystem = (StarSystemID)systemId;

            LoadingUI.SetLoadingText("Connecting to Shard ["+NetworkManager.Instance.CurrentSystem+"]...");
            NetworkManager.Instance.ShardClient.Connect($"{serverIp}:{serverPort}");

            //NetworkManager.Instance.StartCoroutine(NetworkManager.Instance.LoadStarSystem(systemId));
        }  

        private void ShardClientConnected(object sender, EventArgs e){
            NetworkManager.Instance.StartCoroutine(NetworkManager.Instance.LoadStarSystem((ushort)CurrentSystem));
        }

        private void ShardClientConnectionFailed(object sender, EventArgs e){
            LoadingUI.SetActive(true);
            LoadingUI.SetLoadingText("Connection Failed.. Reloading...");

            SceneManager.LoadScene(0);
        }

        private void ShardClientDisconnected(object sender, EventArgs e){
            LoadingUI.SetActive(true);
            LoadingUI.SetLoadingText("Loading main menu...");

            SceneManager.LoadScene(0);
        }

        IEnumerator LoadStarSystem(ushort sceneId){
            
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync((int)sceneId);

            while (!asyncLoad.isDone){
                yield return null;
            }

            SendReadyToShard();
        }

        void SendReadyToShard(){

            LoadingUI.SetLoadingText("Creating player data...");
            Message readyMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.CreatePlayer);
            readyMessage.AddUShort(masterServerId);

            ShardClient.Send(readyMessage);
            
        }

    }

}
