using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HealthSystem : MonoBehaviour
{
    public GameObject spawnPoint;
    private int health = 100;

    public void TakeDamage(int damage)
    {
        this.health -= damage;
    }

    void Start()
    {
        RespawnMemory mem = spawnPoint.GetComponent<RespawnMemory>();
        transform.position = mem.GetSpawnPoint();
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
        Debug.Log("Player Dead!");
        GetComponent<Animator>().enabled = false;
        GetComponent<CharacterController>().enabled = false;
        GetComponent<NewPlayerMovement>().enabled = false;
        SetRigidBodyState(false);
        SetCollidersState(true);
        StartCoroutine(Respawn());
    }
    
    IEnumerator Respawn()
    {
        DontDestroyOnLoad(spawnPoint);
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
