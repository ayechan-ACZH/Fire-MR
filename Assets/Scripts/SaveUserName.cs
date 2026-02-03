using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class SaveUserName : MonoBehaviour
{
    [SerializeField] private TMP_InputField playerNameInputField;

    [SerializeField] private GameObject UserNamePanel;

    [SerializeField] private GameObject TrainerUIPanel;


    public string currentPlayerName = "SaveUser";
    //ExtinguishFire extinguishFire;

    public void SetPlayerName()
    {
        currentPlayerName = playerNameInputField.text;
        Debug.Log("Player name set to: " + currentPlayerName);

        UserNamePanel.SetActive(false);
        TrainerUIPanel.SetActive(true);
    }
          
}
