using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindBlower : MonoBehaviour
{
    public float windForce;
    public Vector3 windDir;
    public int secondsRepeating;

    private GameObject[] windObjects;
    
    // Start is called before the first frame update
    void Start()
    {
        windObjects = GameObject.FindGameObjectsWithTag("windable");
    }

    // Update is called once per frame
    void Update()
    {
        InvokeRepeating("ApplyWindForce", 0, secondsRepeating);
    }

    void ApplyWindForce()
    {
        foreach (GameObject obj in windObjects)
        {
            Rigidbody[] rbs = obj.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rbs)
            {
                rb.AddForce(windDir * windForce);
            }
        }
    }
}
