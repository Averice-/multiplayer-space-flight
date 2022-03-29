using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardStudios {

    public class CameraRig : MonoBehaviour
    {

        private static CameraRig _instance;
        public static CameraRig Instance {
            get => _instance;
            private set {
                if( _instance == null )
                    _instance = value;
                else if ( _instance != value ){
                    Debug.Log($"{nameof(CameraRig)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public Transform followObject;
        public float smoothSpeed = 8f;

        public static void SetFollowObject(Transform follow ){
            Instance.followObject = follow;
        }

        public static Quaternion SmoothDamp(Quaternion a, Quaternion b, float lambda, float dt){
            return Quaternion.Slerp(a, b, 1 - Mathf.Exp(-lambda * dt));
        }

        void Awake(){
            Instance = this;
        }

        private void MoveCamera()
        {
            if (followObject == null)
                return;

            transform.position = followObject.position;

            var targetRigRotation = Quaternion.LookRotation(followObject.forward, transform.up);
            transform.rotation = SmoothDamp(transform.rotation, targetRigRotation, smoothSpeed, Time.deltaTime);
        }

        void FixedUpdate(){
            MoveCamera();
        }
        
    }


}
