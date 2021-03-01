using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kino;

public class EffectsManager : MonoBehaviour
{
    public GameObject camera;
    public GameObject player;
    public float intensityMultiplier = 1f;
    public float duration = 0.5f;
    public bool enableOnLook = false;
    Datamosh datamosh;
    DigitalGlitch digitalGlitch;
    AnalogGlitch analogGlitch;
    float timeSinceGlitch = 0f;
    float damageOffset = 0f;

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("UpdateEffects", 0.0f, 0.3f);
        datamosh = camera.GetComponent<Datamosh>();
        digitalGlitch = camera.GetComponent<DigitalGlitch>();
        analogGlitch = camera.GetComponent<AnalogGlitch>();
        damageOffset = 1f;
        SetDatamosh(damageOffset);
    }

    // Update is called once per frame
    void Update()
    {
        timeSinceGlitch += Time.deltaTime;
        if (damageOffset > 0f) {
            damageOffset -= Time.deltaTime;
        } else {
            damageOffset = 0f;
        }
    }

    void UpdateEffects() {
        if (enableOnLook) {
            bool inView = false;
            Vector3 dirToEnemy = transform.position - camera.transform.position;
            float angleToEnemy = Vector3.Angle(new Vector3(dirToEnemy.x, 0, dirToEnemy.z), new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z));
            angleToEnemy = Math.Abs(180f - angleToEnemy); 

            RaycastHit hit;
            // Debug.DrawRay (camera.transform.position, dirToEnemy, Color.red, 0f, true);
            if(Physics.Raycast(camera.transform.position, dirToEnemy, out hit, 100f)) {
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject == gameObject || hitObject.transform.IsChildOf(transform)) { // line of sight is not blocked
                    inView = true;
                }
            }

            if (angleToEnemy < (180f - camera.GetComponent<Camera>().fieldOfView) || !inView) {
                angleToEnemy = 0;
            }
            angleToEnemy += damageOffset;
            if (player.GetComponent<HealthSystem>().GetHealth() <= 0) {
                angleToEnemy = 180f;
                intensityMultiplier = 1f;
            }
                
            SetDatamosh(intensityMultiplier * angleToEnemy / 180f);  
            SetDigitalGlitch(intensityMultiplier * angleToEnemy / 180f);  
            SetAnalogGlitch(intensityMultiplier * angleToEnemy / 180f); 
        } else {
            if (player.GetComponent<HealthSystem>().GetHealth() <= 0) {
                damageOffset = 180f;
            } else {
                SetDigitalGlitch(damageOffset);  
            }
            SetDatamosh(damageOffset);
        }
     
    }

    void SetDatamosh(float intensity) {
        datamosh.entropy = Mathf.Clamp(intensity, 0, 1);
        if (intensity == 0f && timeSinceGlitch > duration) {
            datamosh.Reset();
        } else if (intensity > 0f) {
            datamosh.Glitch();
            timeSinceGlitch = 0f;
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

    public void SetDamageOffset (float num) {
        damageOffset = num;
    }
}
