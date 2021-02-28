using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnMemory : MonoBehaviour
{
    public GameObject player;
    static bool firstSpawn = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 GetSpawnPoint () {
        if (firstSpawn) {
            firstSpawn = false;
            return player.transform.position;
        } else {
            return transform.position;
        }
    }
}
