using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BarEvent : MonoBehaviour
{

    public AudioSource audioSource;
    public GameObject boy;
    private bool triggered = false;
    public float secondsActive;


    void Start()
    {
        boy.SetActive(false);
    }
    public void StartEvent()
    {
        Debug.Log("Bar Event Started!");
        if (!triggered)
        {
            audioSource.Play();
            Debug.Log("Playing audio...");
            boy.SetActive(true);
            StartCoroutine(waiter());
        }
    }

    IEnumerator waiter()
    {
        yield return new WaitForSeconds(secondsActive);
        audioSource.Pause();
        boy.SetActive(false);
        triggered = true;
    }

    
}
