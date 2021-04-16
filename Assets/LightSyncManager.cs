using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSyncManager : MonoBehaviour
{

    public float bpm;
    public int peakIntensity;
    private List<Light> _channelA = new List<Light>();
    private List<Light> _channelB = new List<Light>();
    private List<Light> _channelC = new List<Light>();
    private float trueSeconds;
    private int channelCounter = 1;
    
    
    
    // Start is called before the first frame update
    void Start()
    {

        trueSeconds = 1 / (bpm / 60);
        
        foreach (var lightGo in GameObject.FindGameObjectsWithTag("channelA"))
        {
            _channelA.Add(lightGo.GetComponent<Light>());
        }
        foreach (var lightGo in GameObject.FindGameObjectsWithTag("channelB"))
        {
            _channelB.Add(lightGo.GetComponent<Light>());
        }
        foreach (var lightGo in GameObject.FindGameObjectsWithTag("channelC"))
        {
            _channelC.Add(lightGo.GetComponent<Light>());
        }
        
        InvokeRepeating(nameof(Flash), 0, trueSeconds);
        
    }

    void Flash()
    {
        switch (channelCounter)
        {
            case 1:
                foreach (var VARIABLE in _channelA)
                {
                    VARIABLE.intensity = peakIntensity;
                }

                foreach (var VARIABLE in _channelB)
                {
                    VARIABLE.intensity = 0;
                }

                foreach (var VARIABLE in _channelC)
                {
                    VARIABLE.intensity = 0;
                }
                break;
            case 2:
                foreach (var VARIABLE in _channelA)
                {
                    VARIABLE.intensity = 0;
                }

                foreach (var VARIABLE in _channelB)
                {
                    VARIABLE.intensity = peakIntensity;
                }

                foreach (var VARIABLE in _channelC)
                {
                    VARIABLE.intensity = 0;
                }
                break;
            case 3:
                foreach (var VARIABLE in _channelA)
                {
                    VARIABLE.intensity = 0;
                }

                foreach (var VARIABLE in _channelB)
                {
                    VARIABLE.intensity = 0;
                }

                foreach (var VARIABLE in _channelC)
                {
                    VARIABLE.intensity = peakIntensity;
                }
                break;
        }

        channelCounter++;
        if (channelCounter > 3)
        {
            channelCounter = 1;
        }
    }
}
