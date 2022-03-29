using RiptideNetworking;
using RiptideNetworking.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardStudios {

    public class Player 
    {

        public static Dictionary<ushort, Player> playerList = new Dictionary<ushort, Player>();

        public string Username { get; private set; }
        public ushort Id { get; private set; } 

        public ulong credits { get; private set; }
        public int activeShip { get; private set; }
        public int currentStation { get; private set; }
        public string reputation { get; private set; }
        public int corpId { get; private set; }
        public int corpRank { get; private set; }
        
        public static Player LocalPlayer;

        #if SHARD

            public ushort masterServerId { get; private set; }

            [MessageHandler((ushort)ClientMessageID.CreatePlayer)]
            private static void CreatePlayer( ushort from, Message received ){

                Player newPlayer = new Player();

                newPlayer.Id = from;
                newPlayer.masterServerId = received.GetUShort();

                Debug.Log("Player Created!");
                playerList.Add(from, newPlayer);

                Message readyForDataMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.ReadyForData);
                readyForDataMessage.AddUShort(newPlayer.masterServerId);

                NetworkManager.Instance.Client.Send(readyForDataMessage);

                // Tell other players to do this.

            }

            private static Message GetPlayerActiveDataMessage(Player player){

                Message newPlayerMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.PlayerJoined);

                newPlayerMessage.AddUShort(player.Id);
                newPlayerMessage.AddString(player.Username);
                newPlayerMessage.AddULong(player.credits);
                newPlayerMessage.AddInt(player.activeShip);
                newPlayerMessage.AddInt(player.currentStation);
                newPlayerMessage.AddString(player.reputation);
                newPlayerMessage.AddInt(player.corpId);
                newPlayerMessage.AddInt(player.corpRank);

                return newPlayerMessage;

            }

            [MessageHandler((ushort)ClientMessageID.SendPlayerActiveData)]
            private static void SetPlayerActiveData( Message received ){

                ushort rec_id = received.GetUShort();
                foreach( KeyValuePair<ushort,Player> outPlayer in playerList ){
                    if( outPlayer.Value.masterServerId == rec_id ){

                        Player player = outPlayer.Value;

                        if( player != null ){
                            player.Username = received.GetString();
                            player.credits = received.GetULong();
                            player.activeShip = received.GetInt();
                            player.currentStation = received.GetInt();
                            player.reputation = received.GetString();
                            player.corpId = received.GetInt();
                            player.corpRank = received.GetInt();

                            // Send new player to everyone.
                            NetworkManager.Instance.Server.SendToAll(GetPlayerActiveDataMessage(player));

                            // Send already connected players to new player.
                            foreach(KeyValuePair<ushort, Player> otherPlayers in playerList){
                                if( otherPlayers.Value.masterServerId != rec_id ){
                                    NetworkManager.Instance.Server.Send(GetPlayerActiveDataMessage(otherPlayers.Value), player.Id);
                                }
                            }

                        }

                        break;

                    }
                }

            }

        #else

            [MessageHandler((ushort)ClientMessageID.PlayerLeft)]
            private static void PlayerLeftShard( Message received ){
                ushort idOfLeftPlayer = received.GetUShort();
                CleanupPlayer(idOfLeftPlayer);
                // Cleanup players shit.
            }

            [MessageHandler((ushort)ClientMessageID.PlayerJoined)]
            private static void ClientReceiveActiveData( Message received ){

                ushort playerId = received.GetUShort();

                Player localPlayer = new Player();
                localPlayer.Id = playerId;


                if( localPlayer != null ){

                    localPlayer.Username = received.GetString();
                    localPlayer.credits = received.GetULong();
                    localPlayer.activeShip = received.GetInt();
                    localPlayer.currentStation = received.GetInt();
                    localPlayer.reputation = received.GetString();
                    localPlayer.corpId = received.GetInt();
                    localPlayer.corpRank = received.GetInt();

                    Debug.Log($"{localPlayer.Username} is flying: {localPlayer.activeShip} from: {localPlayer.currentStation}");

                    if( playerId == NetworkManager.Instance.ShardClient.Id ){
                        LocalPlayer = localPlayer;
                        LoadingUI.SetLoadingText("Receiving player persistent data...");
                        LoadingUI.SetActive(false);
                    }

                    playerList.Add(playerId, localPlayer);

                }else{

                    Debug.Log("Receieved Active Data but cannot find player.");

                }


            }

        #endif

        public static void CleanupPlayer(ushort id){
            // Cleanup players ship etc..
            playerList.Remove(id);

        }

    }

}
