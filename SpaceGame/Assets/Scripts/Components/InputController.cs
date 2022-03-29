using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardStudios {

    public class InputController : MonoBehaviour
    {   

        public FlightController tempFlightCont;
        float strafe = 0f;
        float throttle = 0f;
        Vector3 mousePos = Vector3.zero;

        void Update(){
            mousePos = Input.mousePosition;
            mousePos.z = 1000f;

            strafe = Input.GetAxis("Horizontal");

            throttle += Input.GetAxis("Mouse ScrollWheel");
            throttle = Mathf.Clamp(throttle, 0f, 1f);

            tempFlightCont.Throttle = throttle;
            tempFlightCont.SetPhysicsInputs(new Vector3( strafe, 0f, throttle));
        }

    }

}
