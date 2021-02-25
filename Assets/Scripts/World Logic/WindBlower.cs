using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindBlower : MonoBehaviour
{
    public float Xdir = 0;
    public float Ydir = 0;
    public float Zdir = 0;

    private GameObject[] windObjects;
    
    
    
    // Start is called before the first frame update
    void Start()
    {
        windObjects = GameObject.FindGameObjectsWithTag("windable");
    }

    // Update is called once per frame
    void Update()
    {
        foreach (GameObject obj in windObjects)
            {
                Rigidbody[] rbs = obj.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in rbs)
                {
                    rb.AddForce(new Vector3(Xdir + Random.Range(-1, 1), Ydir + Random.Range(-1, 1), Zdir + 
                        Random.Range(-1, 1)));
                }
            }
    }
}
