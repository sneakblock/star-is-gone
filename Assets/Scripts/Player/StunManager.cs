using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunManager : MonoBehaviour
{
    bool stunning = false;
    int numCharges = 0;
    public GameObject stunCone;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // controls to trigger stunning, validate with numCharges

        if (stunning) {
            stunCone.GetComponent<Collider>().enabled = true;
        } else {
            stunCone.GetComponent<Collider>().enabled = false;
        }
    }

    void OnTriggerEnter(Collider collider) {
        // collider for charge pickups
    }
}
