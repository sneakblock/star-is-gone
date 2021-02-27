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
        
        form1.GetComponentInChildren<Renderer>().enabled = true;
        form2.GetComponentInChildren<Renderer>().enabled = false;
        if (GetComponent<AIManager>().changesForm)
        {
            InvokeRepeating("FormChanger", 0, 4);
        }
    }

   


    void FormChanger()
    {
        if (Random.Range(0, 100) <= 50)
        {
            if (form1.GetComponentInChildren<Renderer>().enabled && !form2.GetComponentInChildren<Renderer>().enabled)
            {
                SwapForms(form1, form2);
            }
        }
        else
        {
            if (form2.GetComponentInChildren<Renderer>().enabled && !form1.GetComponentInChildren<Renderer>().enabled)
            {
                SwapForms(form2, form1);
            }
        }
    }

    IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(2);
    }

    void SwapForms(GameObject fromForm, GameObject toForm)
    {
        fromForm.GetComponentInChildren<Renderer>().enabled = false;
        toForm.transform.position = fromForm.transform.position;
        toForm.transform.forward = fromForm.transform.forward;
        toForm.GetComponentInChildren<Renderer>().enabled = true;
    }
}
