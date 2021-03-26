using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class MusicListener : MonoBehaviour
{
    public static MusicListener Instance
    {
        get { return _instance; }
    }
    private static MusicListener _instance;

    StudioListener listener;
    FMOD.Studio.EventInstance curr;
    FMOD.Studio.EventInstance curr_override;

    public AnimationCurve fade_curve;
    [Range(0, 10)]
    public float fade_duration;

    string curr_path, curr_override_path;
    bool fading;

    [Range(0, 10)]
    public float tension;
    [Range(0, 10)]
    public float tension_threshold;
    [FMODUnity.EventRef]
    public string battle_music;

    private void OnEnable()
    {
        _instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        listener = GetComponent<StudioListener>();
        fading = false;
    }

    public void EnterSong(string song)
    {
        if (curr_override_path != null)
        {
            if (!curr_path.Equals(song))
            {
                curr_path = song;
                curr = RuntimeManager.CreateInstance(song);
                curr.setVolume(0);
                curr.start();
            }
        }
        else if (!fading && (curr_path == null || !curr_path.Equals(song)))
        {
            StartCoroutine(FadeIn(song));
        }
        else
        {
            Debug.LogError("currently in another fade process");
        }
    }

    public IEnumerator FadeIn(string song)
    {
        float t = 0;
        FMOD.Studio.EventInstance newSong = RuntimeManager.CreateInstance(song);
        newSong.setVolume(0);
        newSong.start();

        fading = true;
        while (t < fade_duration)
        {
            t += Time.deltaTime;
            float f_in = fade_curve.Evaluate(t / fade_duration);
            float f_out = fade_curve.Evaluate(1 - (t / fade_duration));

            newSong.setVolume(f_in);
            if (curr_path != null)
            {
                curr.setVolume(f_out);
            }

            yield return null;
        }
        if (curr_path != null)
        {
            curr.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            curr.release();
        }
        curr = newSong;
        curr_path = song;

        fading = false;
        yield return null;
    }

    public void OverrideSong(string over)
    {
        StartCoroutine(FadeOverride(over));
    }

    private IEnumerator FadeOverride(string over)
    {
        float t = 0;
        fading = true;

        curr_override = RuntimeManager.CreateInstance(over);
        curr_override_path = over;
        curr_override.setVolume(0);
        curr_override.start();

        while (t < fade_duration)
        {
            t += Time.deltaTime;
            float f_in = fade_curve.Evaluate(t / fade_duration);
            float f_out = fade_curve.Evaluate(1 - (t / fade_duration));
            if (curr_path != null)
            {
                curr.setVolume(f_out);
            }
            curr_override.setVolume(f_in);

            yield return null;
        }

        fading = false;
        yield return null;
    }

    public void UnOverrideSong()
    {
        if (curr_override_path != null)
        {
            StartCoroutine(FadeoutOverride());
        }
    }

    private IEnumerator FadeoutOverride()
    {
        float t = 0;
        fading = true;

        float vol;
        curr_override.getVolume(out vol);
        curr_override.start();

        while (t < fade_duration)
        {
            t += Time.deltaTime;
            float f_in = fade_curve.Evaluate(t / fade_duration);
            float f_out = fade_curve.Evaluate(1 - (t / fade_duration));
            if (curr_path != null)
            {
                curr.setVolume(f_in);
            }
            curr_override.setVolume(f_out * vol);

            yield return null;
        }

        if (curr_override_path != null)
        {
            curr_override.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            curr_override.release();
        }

        curr_override_path = null;
        fading = false;
        yield return null;
    }

    float tension_temp = 0.0f;

    // Update is called once per frame
    void Update()
    {
        if (tension != tension_temp)
        {
            if (tension > tension_threshold && tension_temp <= tension_threshold)
            {
                OverrideSong(battle_music);
            }
            else if (tension < tension_threshold && tension_temp >= tension_threshold)
            {
                UnOverrideSong();
            }
        }
        tension_temp = tension;
    }
}
