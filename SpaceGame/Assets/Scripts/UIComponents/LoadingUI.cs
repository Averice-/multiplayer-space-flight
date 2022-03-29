using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingUI : MonoBehaviour
{

    private static LoadingUI _instance;
    public static LoadingUI Instance {
        get => _instance;
        private set {
            if( _instance == null )
                _instance = value;
            else if ( _instance != value ){
                Debug.Log($"{nameof(LoadingUI)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    [SerializeField] TextMeshProUGUI loadingText;

    void Awake(){
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }


    public static void SetLoadingText(string messageText){
        LoadingUI.Instance.loadingText.text = messageText;
    }

    public static void SetActive(bool active){
        LoadingUI.Instance.gameObject.SetActive(active);
    }

}
