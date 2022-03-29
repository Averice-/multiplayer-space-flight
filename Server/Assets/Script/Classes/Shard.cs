using RiptideNetworking;
using RiptideNetworking.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardStudios {

    public class Shard
    {

        public static Dictionary<ushort, Shard> Shards = new Dictionary<ushort, Shard>();

        public ushort Id { get; private set; }
        public string ipAddress { get; private set; }
        public ushort Port { get; private set; }
        public StarSystemID systemId { get; private set; }
        public ushort playerCount { get; private set; }
        public ushort maxPlayers { get; private set; }

        public static void RemoveShard(ushort id){
            Shards.Remove(id);
        }

        [MessageHandler((ushort)ClientMessageID.ShardStartup)]
        private static void CreateShard( ushort from, Message message ){
            Shard newShard = new Shard();
            newShard.Id = from;
            newShard.Port = message.GetUShort();
            newShard.systemId = (StarSystemID)message.GetUShort();
            newShard.ipAddress = message.GetString();
            newShard.maxPlayers = message.GetUShort();

            Shards.Add(newShard.Id, newShard);

            Debug.Log("Shard Added to stack");
        }

        [MessageHandler((ushort)ClientMessageID.ShardAddPlayer)]
        private static void PlayerJoinShard( ushort from, Message message ){
            Shards[from].playerCount++;
        }

        [MessageHandler((ushort)ClientMessageID.ShardSubPlayer)]
        private static void PlayerLeftShard( ushort from, Message message ){
            Shards[from].playerCount--;
        }

        public static List<Shard> GetShardsOfSystem(StarSystemID system){

            List<Shard> foundShards = new List<Shard>();

            foreach(KeyValuePair<ushort, Shard> entry in Shards){
                if( entry.Value.systemId == system && entry.Value.playerCount < entry.Value.maxPlayers ){
                    foundShards.Add(entry.Value);
                }
            }

            return foundShards;
        }

        public static StarSystemID GetSystemFromStationId(int stationId){

            return (StarSystemID)Mathf.Floor((float)stationId / 100f);

        }

    }

}
