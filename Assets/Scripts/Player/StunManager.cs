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
        if (numCharges > 0) { // check number of charges
            // check player input
                // subtract a charge and enabled stunning
        } else if () { // if not still holding input
            stunning = false;
        }

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
