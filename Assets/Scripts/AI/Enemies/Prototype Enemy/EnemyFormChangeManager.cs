using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyFormChangeManager : MonoBehaviour
{

    public GameObject form1;
    public GameObject form1Mesh;
    public GameObject form2;
    public GameObject form2Mesh;

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
                form2.GetComponentInChildren<Renderer>().enabled = true;
                form1Mesh.GetComponent<ShaderLerp>().Dissolve();
                form2Mesh.GetComponent<ShaderLerp>().Grow();
                StartCoroutine(SwapForms(1, 2));
            }
        }
        else
        {
            if (form2.GetComponentInChildren<Renderer>().enabled && !form1.GetComponentInChildren<Renderer>().enabled)
            {
                form1.GetComponentInChildren<Renderer>().enabled = true;
                form2Mesh.GetComponent<ShaderLerp>().Dissolve();
                form1Mesh.GetComponent<ShaderLerp>().Grow();
                StartCoroutine(SwapForms(2, 1));
            }
        }
    }

    IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(2);
    }

    IEnumerator SwapForms(int fromForm, int toForm)
    {
        Debug.Log("Swapping from " + fromForm);
        if (fromForm == 1) {
            yield return new WaitForSeconds(form1Mesh.GetComponent<ShaderLerp>().secondDuration);
            form1Mesh.GetComponentInChildren<Renderer>().enabled = false;
            form2.transform.position = form1.transform.position;
            form2.transform.forward = form1.transform.forward;
            // form2Mesh.GetComponentInChildren<Renderer>().enabled = true;
        } else if (fromForm == 2) {
            yield return new WaitForSeconds(form2Mesh.GetComponent<ShaderLerp>().secondDuration);
            form2Mesh.GetComponentInChildren<Renderer>().enabled = false;
            form1.transform.position = form2.transform.position;
            form1.transform.forward = form2.transform.forward;
            // form1Mesh.GetComponentInChildren<Renderer>().enabled = true;
        }
    }
}
