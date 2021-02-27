using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{

    private int health = 100;

    public void TakeDamage(int damage)
    {
        this.health -= damage;
    }

    // Update is called once per frame
    void Update()
    {
        if (health == 0)
        {
            KillPlayer();
        }
    }

    void KillPlayer()
    {
        GetComponent<Animator>().enabled = false;
        GetComponent<CharacterController>().enabled = false;
        GetComponent<ThirdPersonMovement>().enabled = false;
        SetRigidBodyState(false);
        SetCollidersState(true);
    }

    void Respawn()
    {
        
    }

    void SetRigidBodyState(bool state)
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = state;
        }
    }
    
    void SetCollidersState(bool state)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = state;
        }
    }
    
    
}
