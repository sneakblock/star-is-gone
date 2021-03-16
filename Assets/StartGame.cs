using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
    
    private Animator anim;
    
    private void Start()
    {
        anim = GameObject.FindWithTag("faderToBlack").GetComponentInChildren<Animator>();
    }
    
    public void LoadLevel()
    {
        Debug.Log("starting game");
        StartCoroutine(FadeToBlack());
    }

    private IEnumerator FadeToBlack()
    {
        anim.SetBool("fadeToBlack", true);
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene("Overworld");
    }
}
