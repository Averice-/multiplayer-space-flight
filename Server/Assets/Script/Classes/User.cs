using RiptideNetworking;
using RiptideNetworking.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ShardStudios {

    [System.Serializable]
    public struct ActiveData {
        public ulong credits;
        public int activeShip;
        public int currentStation;
        public string reputation;
        public int corpId;
        public int corpRank;

        public void Send(ushort shardId, string username, ushort masterServerId){
            Message activeDataMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.SendPlayerActiveData);
            activeDataMessage.AddUShort(masterServerId);
            activeDataMessage.AddString(username);
            activeDataMessage.AddULong(credits);
            activeDataMessage.AddInt(activeShip);
            activeDataMessage.AddInt(currentStation);
            activeDataMessage.AddString(reputation);
            activeDataMessage.AddInt(corpId);
            activeDataMessage.AddInt(corpRank);

            NetworkManager.Instance.Server.Send(activeDataMessage, shardId);
        }
    }

    public class User
    {

        public static Dictionary<ushort, User> Users = new Dictionary<ushort, User>();

        public ushort Id { get; private set; }
        public string Username { get; private set; }
        public uint Uid { get; private set; }
        public string passwordHash { get; private set; }
        public ActiveData playerData { get; private set; }

        Shard currentShard;

        public static void RemoveUser(ushort id){
            Users.Remove(id);
        }

        [MessageHandler((ushort)ClientMessageID.Connected)]
        private static void LoginUser( ushort from, Message message ){

            if( !Users.ContainsKey(from) ){

                User newUser = new User();
                newUser.Id = from;
                newUser.Username = message.GetString();
                newUser.passwordHash = message.GetString();

                Users.Add(newUser.Id, newUser);

            }else{

                User existingUser = Users[from];
                existingUser.Username = message.GetString();
                existingUser.passwordHash = message.GetString();

            }

            Users[from].CheckLoginCredentials();

        }

        void CheckLoginCredentials(){
            NetworkManager.Instance.StartCoroutine(Login());
        }

        [MessageHandler((ushort)ClientMessageID.ReadyForData)]
        private static void ShardReadyForClientData( ushort from, Message received ){

            ushort masterServerId = received.GetUShort();

            Debug.Log($"User: {masterServerId} asked for data.");
            User currentUser = Users[masterServerId];
            if( currentUser != null ){
                currentUser.playerData.Send(from, currentUser.Username, masterServerId);
            }

        }

        IEnumerator Login(){

            WWWForm details = new WWWForm();
            details.AddField("uname", Username);
            details.AddField("pass", passwordHash);

            using( UnityWebRequest www = UnityWebRequest.Post("https://shardstudios.net/galaxy/api/api.php?key=" + DataHandler.Instance.GetEncryptedKey() + "&action=login", details)){

                yield return www.SendWebRequest();

                if( www.result != UnityWebRequest.Result.Success ){
                    // Send request Error to Client;
                    Debug.Log(www.error);
                }else{
                    string messageResponse = www.downloadHandler.text;
                    Debug.Log(messageResponse);
                    if( messageResponse != "NULL" ){
                        // Transfer Client to Shard. Load player with username only.
                        Uid = (uint)int.Parse(messageResponse);
                        Debug.Log("Logged in!" + Uid);

                        Message hashMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.GetHash);
                        hashMessage.AddUShort(Id);
                        NetworkManager.Instance.Server.Send(hashMessage, Id);

                        NetworkManager.Instance.StartCoroutine(GetPlayerActiveData());

                    }else{

                        Message loginFailedMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.LoginFailed);
                        loginFailedMessage.AddString("Incorrect Username/Password");

                        NetworkManager.Instance.Server.Send(loginFailedMessage, Id);
                        // Send login error to client.
                    }
                }
            }
        }


        IEnumerator GetPlayerActiveData(){

            using( UnityWebRequest www= UnityWebRequest.Get("https://shardstudios.net/galaxy/api/api.php?key=" + DataHandler.Instance.GetEncryptedKey() + "&action=player&uid="+Uid.ToString()) ){

                yield return www.SendWebRequest();

                if( www.result != UnityWebRequest.Result.Success ){

                    Debug.Log(www.error);

                }else{

                    string messageResponse = www.downloadHandler.text;
                    Debug.Log(messageResponse);

                    if( messageResponse != "NULL" ){
                        // got our data.. load shard.
                        playerData = JsonUtility.FromJson<ActiveData>(messageResponse);
                        StarSystemID system = Shard.GetSystemFromStationId(playerData.currentStation);

                        SendUserToShard(system);

                    }else{

                        // Do new player setup.

                    }

                }
            }
        }

        private void SendUserToShard(StarSystemID system){

            List<Shard> listOfAcceptableShards = Shard.GetShardsOfSystem(system);

            if( listOfAcceptableShards.Count == 0 ){
                Debug.Log("No shard online for system: " + system);

                Message loginFailedMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.LoginFailed);
                loginFailedMessage.AddString("No Shard online for Star System[" + system + "]");

                NetworkManager.Instance.Server.Send(loginFailedMessage, Id);
                return;
            }

            Shard firstShard = listOfAcceptableShards[0];

            Message connectToShard = Message.Create(MessageSendMode.reliable, ClientMessageID.ConnectToShard);
            connectToShard.AddString(firstShard.ipAddress);
            connectToShard.AddUShort(firstShard.Port);
            connectToShard.AddUShort((ushort)firstShard.systemId);

            currentShard = firstShard;
            
            Debug.Log("Sending to user: " + Id.ToString());

            NetworkManager.Instance.Server.Send(connectToShard, Id);

        }
        
    }

}
