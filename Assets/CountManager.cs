using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountManager : MonoBehaviour
{
    public int conversations;

    public GameObject light;
    
    // Start is called before the first frame update
    void Start()
    {
        light.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (conversations == 3)
        {
            light.SetActive(true);
        }
    }
    

    public void addConvo()
    {
        conversations++;
    }
}
