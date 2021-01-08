using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyFormChangeManager : MonoBehaviour
{

    public GameObject form1;
    public GameObject form2;

    void Start()
    {
        form1.SetActive(true);
        form2.SetActive(false);
        InvokeRepeating("FormChanger", 0, 4);
    }

    void Update()
    {
        if (form1.activeSelf && !form2.activeSelf)
        {
            form2.transform.position = form1.transform.position;
            form2.transform.forward = form1.transform.forward;
        } else if (!form1.activeSelf && form2.activeSelf)
        {
            form1.transform.position = form2.transform.position;
            form1.transform.forward = form2.transform.forward;
        }
    }


    void FormChanger()
    {
        if (Random.Range(0, 100) <= 50)
        {
            if (form1.activeSelf && !form2.activeSelf)
            {
                Debug.Log("changing to feral form");
                SwapForms(form1, form2);
            }
        }
        else
        {
            if (form2.activeSelf && !form1.activeSelf)
            {
                Debug.Log("changing to human form");
                SwapForms(form2, form1);
            }
        }
    }

    IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(2);
    }

    void SwapForms(GameObject form1, GameObject form2)
    {
        form2.GetComponentInChildren<Renderer>().enabled = false;
        form2.SetActive(true);
        Cooldown();
        form2.GetComponentInChildren<Renderer>().enabled = true;
        form1.SetActive(false);
    }
}
