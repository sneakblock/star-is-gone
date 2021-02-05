using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePlayer : MonoBehaviour
{
    

    
    // Start is called before the first frame update
    void Start()
    { 
        Debug.Log("Enemy did damage");
        GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player");
        HealthSystem target = playerGameObject.GetComponent<HealthSystem>();
        target.takeDamage(100);
    }
}
