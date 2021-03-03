using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunManager : MonoBehaviour
{
    bool stunning = false;
    bool attacking = false;
    int numCharges = 0;
    public GameObject stunCone;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (numCharges > 0 && attacking) {
            stunning = true;
        } else {
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

    public void SetIsAttacking(bool isAttacking) {
        if (attacking && !isAttacking) {
            if (numCharges > 0) {
                numCharges--; // subtract a charge when releasing attack
            }
        }
        attacking = isAttacking;
    }
}
