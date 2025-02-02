﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;


public class PodiumSync : RealtimeComponent
{

    private PodiumSyncModel _model;

    private void Start()
    {
        // Get a reference to the mesh renderer
        //_meshRenderer = GetComponent<MeshRenderer>();
        //_characterMove = GetComponent<UpdateMove>.
    }

    private PodiumSyncModel model
    {
        set
        {
            if (_model != null)
            {
                // Unregister from events
                _model.podiumDidChange -= PodiumDidChange;
            }

            // Store the model
            _model = value;

            if (_model != null)
            {
                // Update the mesh render to match the new model
                UpdatePodium();

                // Register for events so we'll know if the color changes later
                _model.podiumDidChange += PodiumDidChange;
            }
        }
    }

    private void PodiumDidChange(PodiumSyncModel model, int value)
    {
        Debug.Log("Podium changed");
        GetComponent<ModifyPodium>().ReceivedNewPodium(value);
    }

    private void UpdatePodium()
    {
        // Get the color from the model and set it on the mesh renderer.

        Debug.LogWarning("Empty interaction value");
        return;

    }

    public void SetPodium(int podium)
    {
        Debug.Log(">>>>>");
        Debug.Log(_model.podium);
        Debug.Log("SetPodium setting to: " + podium);
        // Set the color on the model
        // This will fire the colorChanged event on the model, which will update the renderer for both the local player and all remote players.
        _model.podium = -1;
            
        _model.podium = podium;
    }

}
