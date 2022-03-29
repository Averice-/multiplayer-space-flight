using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace ShardStudios {

    public class DataHandler : MonoBehaviour
    {

        private static DataHandler _instance;
        public static DataHandler Instance {
            get => _instance;
            private set {
                if( _instance == null )
                    _instance = value;
                else if ( _instance != value ){
                    Debug.Log($"{nameof(DataHandler)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public string databaseKey = "alphabetsoupisgoodforyou";
        public string GetEncryptedKey(){
            return Md5(databaseKey);
        }

        void Awake(){
            Instance = this;
        }

        public string Md5(string strToEncrypt){

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

    }

}
