using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterDiner : MonoBehaviour
{
    public void OnTriggerEnter(Collider collider)
    {
        SceneManager.LoadScene("diner");
    }
}
