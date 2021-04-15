using System.Collections;
using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using UnityEngine;
using Aura2API;

public class FlickerLights : MonoBehaviour
{
    
    private ArrayList<Light> lights = new ArrayList<Light>();

    void Start()
    {
        foreach (Light light in GameObject.Find("Lights").GetComponentsInChildren<Light>())
        {
            lights.Add(light);
        }
        InvokeRepeating(nameof(Flicker), 0, .1f);
    }

    private void Flicker()
    {
        foreach (Light light in lights)
        {
            var chance = Random.Range(0, 100);
            light.intensity = chance >= 50 ? 5 : 0;
        }
    }
}
