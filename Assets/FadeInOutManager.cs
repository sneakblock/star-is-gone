using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeInOutManager : MonoBehaviour
{
    
    private Animator anim;
    
    private void OnEnable()
    {
        anim = GetComponent<Animator>();
        anim.SetBool("fadeIn", true);
    }

    private void OnDisable()
    {
        anim = GetComponent<Animator>();
        anim.SetBool("fadeOut", true);
    }
}
