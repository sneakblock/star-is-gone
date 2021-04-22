using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParkConvoController : MonoBehaviour
{

    private PlayerControls controls;
    private Collider coll;
    //public Transform faceObj;
    public GameObject player;
    public GameObject bf;
    private NewPlayerMovement movement;
    private Animator anim;
    private Animator bfAnim;
    public GameObject enemy;
    public GameObject radio;
    private AudioSource radioAudioSource;
    public AudioSource music;
    private bool wasTriggered = false;
    
    private void Awake()
    {
        controls = new PlayerControls();
        coll = GetComponent<Collider>();
        movement = player.GetComponent<NewPlayerMovement>();
        anim = player.GetComponent<Animator>();
        bfAnim = bf.GetComponent<Animator>();
        enemy.SetActive(false);
        radio.GetComponent<Interactable>().enabled = false;
        radio.GetComponent<Collider>().enabled = false;
        radioAudioSource = radio.GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void OnTriggerStay(Collider coll)
    {
        if (controls.Standard.Interact.ReadValue<float>() > .5f && !wasTriggered)
        {
            wasTriggered = true;
            //RotatePlayer(faceObj);
            SeatPlayer();
        }
    }

    private void SeatPlayer()
    {
        movement.enabled = false;
        anim.SetBool("sitting", true);
        music.Pause();
        radioAudioSource.Play();
    }

    public void CompleteConvo()
    {
        music.Play();
        anim.SetBool("sitting", false);
        movement.enabled = true;
        bfAnim.SetBool("stand", true);
        StartCoroutine(wait(3));
        coll.enabled = false;
        radio.GetComponent<Interactable>().enabled = true;
        radio.GetComponent<Collider>().enabled = true;
    }

    IEnumerator wait(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        enemy.SetActive(true);
        bf.SetActive(false);
    }

}
