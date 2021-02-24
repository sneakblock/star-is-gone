using System;
using System.Collections;
using System.Collections.Generic;
using AmplifyShaderEditor;
using Aura2API;
using UnityEngine;
using Yarn.Unity;

public class DialogueTriggerAI : MonoBehaviour
{
    private PlayerControls controls;
    private Collider _collider;
    //public Dialogue dialogue;
    private bool hasBeenTriggered = false;
    public bool isAutomatic;
    private GameObject _animGO;
    private Animator _anim;
    private bool didPress = false;
    public string startNode;
    


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
        }
    }

    public void OnTriggerStay(Collider _collider)
    {
        if (!isAutomatic)
        {
            if (!didPress)
            {
                _anim.SetBool("showButton", true);
                if (controls.Standard.Interact.ReadValue<float>() > .5f)
                {
                    TriggerDialogue();
                    didPress = true;
                    _anim.SetBool("showButton", false);
                    gameObject.transform.parent.gameObject.GetComponent<AIManager>().Interact(true);
                    Debug.Log("triggered");
                }
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
        if (!isAutomatic)
        {
            _anim.SetBool("showButton", false);
            FindObjectOfType<DialogueUI>().DialogueComplete();
            FindObjectOfType<DialogueRunner>().ResetDialogue();
            gameObject.transform.parent.gameObject.GetComponent<AIManager>().Interact(false);
            StartCoroutine(DidPressResetter());
        }
        
    }

    IEnumerator DidPressResetter()
    {
        yield return new WaitForSeconds(3);
        didPress = false;
        //gameObject.transform.parent.gameObject.GetComponent<AIManager>().Interact(false);
        Debug.Log("untriggered");
    }

    public void TriggerDialogue()
    {
        FindObjectOfType<DialogueRunner>().Stop();
        FindObjectOfType<DialogueRunner>().StartDialogue(startNode);
        Debug.Log("Sent order to start " + startNode + "to dialogue runner.");
    }

}
