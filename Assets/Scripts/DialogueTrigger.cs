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

    public void Start()
    {
        _collider = GetComponent<Collider>();
    }
    public void OnTriggerEnter(Collider _collider)
    {
        if (!hasBeenTriggered)
        {
            hasBeenTriggered = true;
            TriggerDialogue();
        }
    }

    public void TriggerDialogue()
    {
        FindObjectOfType<DialogueManager>().StartDialogue(dialogue);
    }

}
