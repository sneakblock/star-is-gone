using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomSoundLerper : MonoBehaviour
{

    public bool loopLerp = true;
    public int intervalSeconds;
    public float maxVolume;
    public float minVolume;
    public float shortestFadeDuration;
    public float longestFadeDuration;
    private AudioSource audiosource;
    

    private void Start()
    {
        audiosource = GetComponent<AudioSource>();
        StartCoroutine(beginFading(audiosource, intervalSeconds, maxVolume, minVolume,
            shortestFadeDuration, longestFadeDuration, loopLerp));
    }

    

    IEnumerator beginFading(AudioSource audiosource, int intervalSeconds, float maxVolume, float minVolume, 
        float shortestFadeDuration, float longestFadeDuration, bool loopLerp)
    {
        while (loopLerp)
        {
            yield return new WaitForSeconds(intervalSeconds);
        
            float duration = Random.Range(shortestFadeDuration, longestFadeDuration);
            float targetVolume = Random.Range(minVolume, maxVolume);
            float currentVolume = audiosource.volume;
            float currentTime = 0;
        
            Debug.Log("Lerping sound from " + currentVolume + " to " + targetVolume + " over " + duration + 
                      " seconds ");

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                audiosource.volume = Mathf.Lerp(currentVolume, targetVolume, currentTime / duration);
            }
        }
    }
}
