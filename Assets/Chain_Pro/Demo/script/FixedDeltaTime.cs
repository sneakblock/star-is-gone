using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FixedDeltaTime : MonoBehaviour
{
    [SerializeField] float fixedDeltaTimeValue = 0.005f;

    void Start()
    {
        Time.fixedDeltaTime = fixedDeltaTimeValue;
    }

}