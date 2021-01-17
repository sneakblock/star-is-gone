using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class MoveTester : MonoBehaviour
{
    private PlayerControls controls;
    private void Awake()
    {
        controls = new PlayerControls();
        controls.Standard.Move.performed += ctx => MoveMe();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    void MoveMe()
    {
        Debug.Log("Stick moved");
    }
}
