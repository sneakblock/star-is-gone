using System;
using System.Collections;
using System.Collections.Generic;
using AmplifyShaderEditor;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    private Collider _collider;
    public Dialogue dialogue;
    private bool hasBeenTriggered = false;
    public bool isAutomatic;
    private GameObject _animGO;
    private Animator _anim;
    private bool didPress = false;

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
        }
    }

    public void OnTriggerStay(Collider _collider)
    {
        if (!isAutomatic)
        {
            if (!didPress)
            {
                _anim.SetBool("showButton", true);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                TriggerDialogue();
                didPress = true;
                _anim.SetBool("showButton", false);
            }
        }
    }

    public void OnTriggerExit(Collider _collider)
    {
        if (!isAutomatic)
        {
            _anim.SetBool("showButton", false);
            StartCoroutine(DidPressResetter());
        }
    }

    IEnumerator DidPressResetter()
    {
        yield return new WaitForSeconds(3);
        didPress = false;
    }

    public void TriggerDialogue()
    {
        FindObjectOfType<DialogueManager>().StartDialogue(dialogue);
    }

}
