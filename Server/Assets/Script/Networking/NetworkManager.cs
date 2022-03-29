using RiptideNetworking;
using RiptideNetworking.Transports.RudpTransport;
using RiptideNetworking.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardStudios {

    public enum StarSystemID : ushort {
        NewSol = 1,
        Babylon
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

        public Server Server {get; private set;}

        [SerializeField] private ushort port;
        [SerializeField] private ushort maxClients;

        private void Awake(){
            Instance = this;
        }

        private void Start(){
            
            Application.targetFrameRate = 60;

            #if UNITY_EDITOR
                        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
            #else
                        Console.Title = "Server";
                        Console.Clear();
                        Application.SetStackTraceLogType(UnityEngine.LogType.Log, StackTraceLogType.None);
                        RiptideLogger.Initialize(Debug.Log, true);
            #endif

            Server = new Server();

            Server.ClientConnected += UserConnected;
            Server.ClientDisconnected += UserLeft;

            Server.Start(port, maxClients);
        }

        private void FixedUpdate(){
            Server.Tick();
        }

        private void OnApplicationQuit(){
            Server.Stop();
            Server.ClientConnected -= UserConnected;
            Server.ClientDisconnected -= UserLeft;
        }

        private void UserLeft(object sender, ClientDisconnectedEventArgs e){
            if( Shard.Shards.ContainsKey(e.Id) ){
                Shard.RemoveShard(e.Id);
                return;
            }
            User.RemoveUser(e.Id); // don't do this if shard.
        }

        private void UserConnected(object sender, ServerClientConnectedEventArgs e){
           // Debug.Log(((RudpConnection)e.Client).RemoteEndPoint);

        }
    }

}
