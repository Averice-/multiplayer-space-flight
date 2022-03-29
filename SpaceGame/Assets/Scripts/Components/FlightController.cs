using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardStudios {

    [RequireComponent(typeof(Rigidbody))]
    public class FlightController : MonoBehaviour
    {

        public float forceMultiplier = 100f;

        Vector3 linearForce = new Vector3( 50f, 100f, 100f );
        Vector3 angularForce = new Vector3( 1f, 1f, 1f );

        Vector3 appliedLinearForce = Vector3.zero;
        Vector3 appliedAngularForce = Vector3.zero;

        float pitchSensitivity = 1f;
        float yawSensitivity = 1f;
        float rollSensitivity = 1f;

        Rigidbody rigidBody;
        Camera mainCamera;

        float pitch = 0f;
        float yaw = 0f;
        float roll = 0f;

        float bankLimit = 35f;

        float throttle = 0f;
        const float throttleKeyboardIncreaseAmount = 0.1f;

        public float Throttle { get{ return throttle; } set { throttle = Mathf.Clamp(value, 0f, 1f); } }

        void Start(){
            rigidBody = GetComponent<Rigidbody>();
            mainCamera = Camera.main;
        }

        public void SetPhysicsInputs(Vector3 linearInput){

            appliedLinearForce = Vector3.Scale(linearForce, linearInput);
            appliedAngularForce = Vector3.Scale(angularForce, new Vector3(pitch, yaw, roll));

        }

        void Update(){
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 1000f;

            if( mainCamera != null ){

                Vector3 gotoPosition = mainCamera.ScreenToWorldPoint(mousePosition);
                TurnTowardsPoint(gotoPosition);

                BankShipRelativeToUpVector(mousePosition, mainCamera.transform.up);

            }

        }

        private void BankShipRelativeToUpVector(Vector3 mousePos, Vector3 upVector){

            float bankInfluence = (mousePos.x - (Screen.width * 0.5f)) / (Screen.width * 0.5f);

            bankInfluence = Mathf.Clamp(bankInfluence, -1f, 1f);

            // Throttle modifies the bank angle so that when at idle, the ship just flatly yaws.
            bankInfluence *= throttle;

            if( bankInfluence > -.2f && bankInfluence < .2f ){
                 roll = Mathf.MoveTowards(roll, 0f, 5f * Time.deltaTime);
                 return;
            }

            float bankTarget = bankInfluence * bankLimit;

            // Here's the special sauce. Roll so that the bank target is reached relative to the
            // up of the camera.
            float bankError = Vector3.SignedAngle(transform.up, upVector, transform.forward);
            bankError = bankError - bankTarget;

            // Clamp this to prevent wild inputs.
            bankError = Mathf.Clamp(bankError * 0.1f, -1f, 1f);

            // Roll to minimze error.
            roll = bankError * rollSensitivity;

        }

        private void TurnTowardsPoint(Vector3 gotoPos){
            Vector3 localGotoPos = transform.InverseTransformVector(gotoPos - transform.position).normalized;

            pitch = Mathf.Clamp(-localGotoPos.y * pitchSensitivity, -1f, 1f);
            yaw = Mathf.Clamp(localGotoPos.x * yawSensitivity, -1f, 1f);

        }


        void FixedUpdate(){

            rigidBody.AddRelativeForce(appliedLinearForce * forceMultiplier, ForceMode.Force);
            rigidBody.AddRelativeTorque(appliedAngularForce * forceMultiplier, ForceMode.Force);

        }



    }

}
