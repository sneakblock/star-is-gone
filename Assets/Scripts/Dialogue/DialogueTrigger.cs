using System;
using System.Collections;
using System.Collections.Generic;
using AmplifyShaderEditor;
using Aura2API;
using UnityEngine;
using Yarn.Unity;

public class DialogueTrigger : MonoBehaviour
{
    private PlayerControls controls;
    private Collider _collider;
    //public Dialogue dialogue;
    private bool hasBeenTriggered = false;
    public bool isAutomatic;
    private GameObject _animGO;
    private Animator _anim;
    //private bool didPress = false;
    public string startNode;
    private bool dialogueBeenTriggered = false;
    public bool canBeCanceled;
    


    public void Awake()
    {
        controls = new PlayerControls();
    }

    public void Start()
    {
        _collider = GetComponent<Collider>();
        _animGO = GameObject.FindWithTag("buttonprompt");
        _anim = _animGO.GetComponent<Animator>();
    }
    
    public void OnTriggerEnter(Collider _collider)
    {
        if (isAutomatic)
        {
            if (!hasBeenTriggered)
            {
                hasBeenTriggered = true;
                TriggerDialogue();
            }
        } else
        {
            _anim.SetBool("showButton", true);
        }
    }

    public void OnTriggerStay(Collider _collider)
    {
        if (!isAutomatic)
        {
            
            if (controls.Standard.Interact.ReadValue<float>() > .5f && !dialogueBeenTriggered)
            {

                TriggerDialogue();
                dialogueBeenTriggered = true;
            }
        }
    }
    

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    public void OnTriggerExit(Collider _collider)
    {
        if (!isAutomatic && canBeCanceled)
        {
            if (_anim.GetBool("showButton")) {
                _anim.SetBool("showButton", false);
            }
            FindObjectOfType<DialogueUI>().DialogueComplete();
            FindObjectOfType<DialogueRunner>().ResetDialogue();
            dialogueBeenTriggered = false;
        }
        
    }

    

    public void TriggerDialogue()
    {
        FindObjectOfType<DialogueRunner>().Stop();
        FindObjectOfType<DialogueRunner>().StartDialogue(startNode);
        Debug.Log("Sent order to start " + startNode + " to dialogue runner.");
        _anim.SetBool("showButton", false);
        //_anim.Play("closedbutton");
    }

}
