using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class EndGame : MonoBehaviour
{
    public Animator animator;
    public AudioSource audioSource;
    public AudioSource audioSource2;
    public void OnTriggerEnter(Collider collider)
    {
        
        StartCoroutine(wait());
        
        

    }
    
    IEnumerator wait()
    {
        //Print the time of when the function is first called.
        Debug.Log("Started Coroutine at timestamp : " + Time.time);
        animator.SetTrigger("fade");
        audioSource.Play();
        audioSource2.Play();
        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(25);
        Application.Quit();
        //After we have waited 5 seconds print the time again.
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
        Debug.Log("uhhhhh it quit?");
    }
}
