using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickerLight : MonoBehaviour
{

    public Light strobe;
    // Start is called before the first frame update
    void Start()
    {
        flickerIt();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator flickerIt()
    {
        for (int i = 1; i > 0; i++)
        {
            yield return new WaitForSeconds(.5f);
            
           
            strobe.intensity = 3;
            
            yield return new WaitForSeconds(.02f);
            strobe.intensity = 0;
            
            Debug.Log("flciker");
        }
    }
    
}
