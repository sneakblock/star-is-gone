using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunManager : MonoBehaviour
{
    bool stunning = false;
    bool attacking = false;
    int numCharges = 2;
    public float stunConeLength = 20f;
    public float stunConeAngle = 90f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("# of stun charges: " + numCharges);
        if (numCharges > 0 && attacking) {
            stunning = true;
        } else {
            stunning = false;
        }

        if (stunning) {

        } else {

        }
    }

    public void PickupCharge() {
        numCharges++;
    }

    public void SetIsAttacking(bool isAttacking) {
        if (attacking && !isAttacking) {
            if (numCharges > 0) {
                numCharges--; // subtract a charge when releasing attack
            }
        }
        attacking = isAttacking;
    }

    public bool GetStunning() {
        return stunning;
    }
}
