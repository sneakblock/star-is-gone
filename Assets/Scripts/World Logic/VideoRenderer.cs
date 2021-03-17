using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoRenderer : MonoBehaviour
{
    public List<VideoClip> clips = new List<VideoClip>();
    public int currChannel = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.GetComponent<VideoPlayer>().clip = clips[currChannel];
        Debug.Log(clips[currChannel]);
        Debug.Log(gameObject.GetComponent<VideoPlayer>().clip);
    }

    public void changeChannel(int channelNum) {
        currChannel = Mathf.Clamp(channelNum, 0, clips.Count);
    }
}
