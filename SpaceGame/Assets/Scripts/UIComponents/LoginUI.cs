    using RiptideNetworking;
    using RiptideNetworking.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using ShardStudios;

    public class LoginUI : MonoBehaviour
    {

        private static LoginUI _instance;
        public static LoginUI Instance {
            get => _instance;
            private set {
                if( _instance == null )
                    _instance = value;
                else if ( _instance != value ){
                    Debug.Log($"{nameof(LoginUI)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        [SerializeField] GameObject loginCanvas;
        [SerializeField] TMP_InputField usernameInput;
        [SerializeField] TMP_InputField passwordInput;
        [SerializeField] Button connectButton;
        [SerializeField] TextMeshProUGUI errorMessageText;


        private void Awake(){
            Instance = this;
        }

        private void Start(){
            Retry();
            if( NetworkManager.Instance.Client != null ){
                NetworkManager.Instance.Client.Connected += LocalClientConnectionSuccess;
                NetworkManager.Instance.Client.ConnectionFailed += LocalClientConnectionFailed;
                NetworkManager.Instance.Client.Disconnected += LocalClientDisconnected;
            }
        }

        public void ConnectButtonClicked(){
            usernameInput.interactable = false;
            loginCanvas.SetActive(false);
            LoadingUI.SetActive(true);
            LoadingUI.SetLoadingText("Connecting to global server...");

            if( NetworkManager.Instance.Client.IsConnected ){
                SendLoginInfo();
            }else{
                NetworkManager.Instance.ConnectMaster();
            }

            // Show loading screen.
            // Load shard.
            // start scene with loading screen.
            // receive player data.
        }


        public void Retry() {
            LoadingUI.SetActive(false);
            usernameInput.interactable = true;
            loginCanvas.SetActive(true);

        }

        public void SendLoginInfo(){
            LoadingUI.SetLoadingText("Logging in..");
            Message message = Message.Create(MessageSendMode.reliable, ClientMessageID.Connected);
            message.AddString(usernameInput.text);
            message.AddString(NetworkManager.Md5(passwordInput.text));
            NetworkManager.Instance.Client.Send(message);
        }

        private void LocalClientConnectionSuccess(object sender, EventArgs e){
            SendLoginInfo();
            // Start loading screen data menu.
        }

        private void LocalClientConnectionFailed(object sender, EventArgs e){
            // Show can't connect message!
            Retry();
        }

        private void LocalClientDisconnected(object sender, EventArgs e){
            Retry(); /// CHANGE;
        }

        private void OnApplicationQuit(){  
            NetworkManager.Instance.Client.Connected -= LocalClientConnectionSuccess;
            NetworkManager.Instance.Client.ConnectionFailed -= LocalClientConnectionFailed;
            NetworkManager.Instance.Client.Disconnected -= LocalClientDisconnected;
        }

        private void OnDestroy(){
            NetworkManager.Instance.Client.Connected -= LocalClientConnectionSuccess;
            NetworkManager.Instance.Client.ConnectionFailed -= LocalClientConnectionFailed;
            NetworkManager.Instance.Client.Disconnected -= LocalClientDisconnected;
        }

        [MessageHandler((ushort)ClientMessageID.LoginFailed)]
        private static void LoginFailed(Message message){

            string messageError = message.GetString();
            // Do string thingy.
            LoginUI.Instance.Retry();
            LoginUI.Instance.ShowErrorMessage(messageError);
        }

        public void ShowErrorMessage(string text){
            errorMessageText.gameObject.SetActive(true);
            errorMessageText.text = text;
        }
    }
