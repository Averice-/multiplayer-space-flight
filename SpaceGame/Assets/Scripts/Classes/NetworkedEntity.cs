using RiptideNetworking;
using RiptideNetworking.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardStudios {

    public enum NetworkType {
        Static,
        Dynamic,
        AnimationOnly,
        TransformOnly
    }

    public class NetworkedEntity : MonoBehaviour
    {

        public static Dictionary<uint, NetworkedEntity> Entities = new Dictionary<uint, NetworkedEntity>();
        public static uint idCount = 0;

        public NetworkType networkTyped { get; private set; }
        public uint Id { get; private set; }
        public string resourceName { get; private set; }
        public ushort Owner { get; private set; }

        #if SHARD

            public static void Spawn(string entityPrefab, Vector3 position, Quaternion rotation, ushort id_owner = 0){

                Debug.Log("hgfjhgfhjfjh");
                GameObject newObject = (GameObject)Instantiate(Resources.Load($"NetworkedEntities/{entityPrefab}"), position, rotation);
                NetworkedEntity netEntity = newObject.GetComponent<NetworkedEntity>();

                if( netEntity == null ){
                    Destroy(newObject);
                    Debug.Log("Entity destroyed it is not a NetworkedEntity");
                    return;
                }

                idCount++;
                netEntity.Id = idCount;
                netEntity.resourceName = entityPrefab;
                netEntity.Owner = id_owner;

                Message spawnEntityMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.SpawnEntity);
                spawnEntityMessage.AddUInt(idCount);
                spawnEntityMessage.AddString(entityPrefab);
                spawnEntityMessage.AddVector3(position);
                spawnEntityMessage.AddQuaternion(rotation);
                spawnEntityMessage.AddUShort(id_owner);

                NetworkManager.Instance.Server.SendToAll(spawnEntityMessage);

                netEntity.OnSpawn();

                Entities.Add(idCount, netEntity);

            }

            // Broadcasts all entities that have been spawned to new players.
            public static void BroadcastAll(ushort player_id){

                foreach( KeyValuePair<uint, NetworkedEntity> ent in Entities ){

                    Message spawnEntityMessage = Message.Create(MessageSendMode.reliable, ClientMessageID.SpawnEntity);
                    spawnEntityMessage.AddUInt(ent.Value.Id);
                    spawnEntityMessage.AddString(ent.Value.resourceName);
                    spawnEntityMessage.AddVector3(ent.Value.transform.position);
                    spawnEntityMessage.AddQuaternion(ent.Value.transform.rotation);
                    spawnEntityMessage.AddUShort(ent.Value.Owner);

                    NetworkManager.Instance.Server.Send(spawnEntityMessage, player_id);

                }

            }

        #else

            [MessageHandler((ushort)ClientMessageID.SpawnEntity)]
            public static void Spawn(Message message){
                
                uint entId = message.GetUInt();
                string entityPrefab = message.GetString();
                Vector3 position = message.GetVector3();
                Quaternion rotation = message.GetQuaternion();
                ushort owner = message.GetUShort();

                GameObject newEntity = (GameObject)Instantiate(Resources.Load($"NetworkedEntities/{entityPrefab}"), position, rotation);
                NetworkedEntity netEntity = newEntity.GetComponent<NetworkedEntity>();

                netEntity.Id = entId;
                netEntity.resourceName = entityPrefab;
                netEntity.Owner = owner;

                netEntity.OnSpawn();

                Entities.Add(entId, netEntity);
            }

        #endif

        public static NetworkedEntity GetEntityById(uint id){
            if( Entities.ContainsKey(id) ){
                return Entities[id];
            }
            return null;
        }

        public static List<NetworkedEntity> GetEntitiesOfOwner(ushort ownerId){
            List<NetworkedEntity> result = new List<NetworkedEntity>();
            foreach( KeyValuePair<uint, NetworkedEntity> ent in Entities ){
                if( ent.Value.Owner == ownerId ) {
                    result.Add(ent.Value);
                }
            }
            return result;
        }

        public virtual void OnSpawn(){
            Debug.Log($"Entity[{Id}] - Spawned.");
        }

        void OnDestroy(){
            Entities.Remove(Id);
        }

    }

}
