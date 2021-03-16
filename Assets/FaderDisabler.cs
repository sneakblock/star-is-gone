using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaderDisabler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("atGone"))
        {
            this.enabled = false;
        }
    }
}
