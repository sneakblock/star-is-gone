using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kino;

public class EffectsManager : MonoBehaviour
{
    public GameObject camera;
    public float intensityMultiplier = 1;
    Datamosh datamosh;
    DigitalGlitch digitalGlitch;
    AnalogGlitch analogGlitch;

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("UpdateEffects", 2.0f, 0.3f);
        datamosh = camera.GetComponent<Datamosh>();
        digitalGlitch = camera.GetComponent<DigitalGlitch>();
        analogGlitch = camera.GetComponent<AnalogGlitch>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void UpdateEffects() {
        Vector3 dirToCam = camera.transform.position - transform.position;
        float angleToCam = Vector3.Angle(new Vector3(dirToCam.x, 0, dirToCam.z), new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z));
        angleToCam = Math.Abs(180f - angleToCam); 
        if (angleToCam > 30) {
            angleToCam = 0;
        }
        SetDatamosh(intensityMultiplier * angleToCam / 180f);  
        SetDigitalGlitch(intensityMultiplier * angleToCam / 180f);  
        SetAnalogGlitch(intensityMultiplier * angleToCam / 180f);  
    }

    void SetDatamosh(float intensity) {
        datamosh.entropy = Mathf.Clamp(intensity, 0, 1);
        if (intensity == 0f) {
            datamosh.Reset();
        } else {
            datamosh.Glitch();
        }
    }

    void SetDigitalGlitch(float intensity) {
        digitalGlitch.intensity = intensity;
    }
    void SetAnalogGlitch(float intensity) {
        analogGlitch.scanLineJitter = intensity;
        analogGlitch.verticalJump = intensity;
        analogGlitch.colorDrift = intensity;
    }
}
