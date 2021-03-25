using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class TVInteractable : MonoBehaviour
{
    public DialsUI dialsUI;
    private PlayerControls _controls;
    private Collider _collider;
    private Animator _anim;
    public GameObject behaviorToBeTriggered;

    private void Awake()
    {
        _controls = new PlayerControls();
    }

    private void OnEnable()
    {
        _controls.Enable();
    }

    private void OnDisable()
    {
        _controls.Disable();
    }

    private void Start()
    {
        _collider = GetComponent<Collider>();
        _anim = GameObject.FindWithTag("buttonprompt").GetComponent<Animator>();
        //dialsUI = GameObject.FindGameObjectWithTag("DialUI").GetComponent<DialsUI>();
        dialsUI.enabled = false;
        dialsUI.HideUI();
    }

    private void OnTriggerEnter(Collider _collider)
    {
        _anim.SetBool("showButton", true);
    }

    private void OnTriggerStay(Collider _collider)
    {
        if (_controls.Standard.Interact.ReadValue<float>() > .5f)
        {
            //Debug.Log("Player has interacted with " + this.name);
            dialsUI.enabled = true;


            _anim.SetBool("showButton", false);
            dialsUI.ShowUI();
        }

    }

    private void OnTriggerExit(Collider _collider)
    {
        if (_anim.GetBool("showButton"))
        {
            _anim.SetBool("showButton", false);
            dialsUI.HideUI();
        }
    }

}
