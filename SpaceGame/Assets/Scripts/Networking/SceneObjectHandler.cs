using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardStudios {

    public class SceneObjectHandler : MonoBehaviour
    {

        void Awake(){
            #if SHARD
                GetClientObject().SetActive(false);
                //LoadingUI.SetActive(false);
            #else
                GetShardObject().SetActive(false);
            #endif
        }

        public static GameObject GetClientObject(){
            return GameObject.Find("--CLIENT--");
        }

        public static GameObject GetShardObject(){
            return GameObject.Find("--SHARD--");
        }
    }

}