using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private Animator anim;
    private string sceneName;
    // Start is called before the first frame update
    void Start()
    {
        anim = GameObject.FindWithTag("faderToBlack").GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeToBlack(sceneName));
    }
    
    private IEnumerator FadeToBlack(string sceneName)
    {
        Debug.Log("Loading scene " + sceneName);
        anim.SetBool("fadeToBlack", true);
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(sceneName);
    }
}
