﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Normal.Realtime;
using Michsky.UI.ModernUIPack;

public class EventCountdown : MonoBehaviour
{

    private float currentTime = 0f;
    public float countdownDuration = 10.0f;
    public TMP_Text eventStartingNotificationDescription;
    public string scene = "The Circle";
    private EventManager eventManager;
    private GameObject localPlayer;

    private char cameraString = '0';

    // Start is called before the first frame update
    void Start()
    {
        currentTime = countdownDuration;
        eventManager = GameObject.Find("EventManager").GetComponent<EventManager>();
        eventManager.OnEventsChange.AddListener(ReceiveEvent);
    }

    void ReceiveEvent()
    {
        if (eventManager.GetEvents() == null) return;

        Debug.Log("Two states:");
        Debug.Log(cameraString);
        Debug.Log(eventManager.GetEvents()[2]);

        if (cameraString == eventManager.GetEvents()[2]){
            if (eventManager.GetEvents()[0] == '0')
            {
                scene = "The Circle";
            }

            countdownDuration = 10.0f;
            if (eventManager.GetEvents()[1] == '1')
            {

            }
            else
            {
                currentTime = countdownDuration;
            }
        }
        else
        {
            Debug.Log("Camera changed");
        }

        cameraString = eventManager.GetEvents()[2];

    }


    // Update is called once per frame
    void Update()
    {
        if (eventManager.GetEvents() == null) return;

        if (eventManager.GetEvents()[1] == '1')
        {
            if (currentTime > 0)
            {
                currentTime -= 1 * Time.deltaTime;
            }
            else { 
                GameObject.Find("Realtime").GetComponent<Realtime>().Disconnect();
                Loading.sceneString = scene;
                SceneManager.LoadScene("Loading");
            }
        }
        eventStartingNotificationDescription.text = "A virtual event is starting! You will automatically join the event in " + currentTime.ToString("0") + " seconds.";
    }
}


