using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class dialogRange : MonoBehaviour
{

    public bool isConversation;
    public bool isRangeSound;
    public string text;
    public CountManager countManager;
    private bool triggered = false;
    private bool spoken = false;
    public AudioSource audioSource;
    
    private void OnTriggerEnter(Collider collider)
    {
        PopUpSystem pop = GameObject.FindGameObjectWithTag("DialogManager").GetComponent<PopUpSystem>();
        pop.popUp(text);
        if (countManager!= null && isConversation && !triggered)
        {
            countManager.addConvo();
            triggered = true;
        }

        if (audioSource != null && !isRangeSound && !spoken)
        {
            audioSource.Play();
            spoken = true;
        }

        if (audioSource != null && isRangeSound)
        {
            audioSource.Play();
        }
        
    }

    private void OnTriggerExit(Collider collider)
    {
        PopUpSystem pop = GameObject.FindGameObjectWithTag("DialogManager").GetComponent<PopUpSystem>();
        pop.close();
        if (audioSource != null && isRangeSound)
        {
            audioSource.Stop();
        }
    }
}
