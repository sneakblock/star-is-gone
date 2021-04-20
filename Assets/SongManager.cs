using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongManager : MonoBehaviour
{

    public AudioSource from;

    public AudioSource to;

    private Collider _coll;
    // Start is called before the first frame update
    void Start()
    {
        _coll = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider _coll)
    {
        from.Pause();
        to.Play();
    }
}
