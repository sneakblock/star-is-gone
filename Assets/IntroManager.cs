using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroManager : MonoBehaviour
{

    private PlayerControls controls;

    private Animator playerAnim;
    // Start is called before the first frame update
    void Start()
    {
        controls = new PlayerControls();
        playerAnim = GameObject.FindWithTag("Player").GetComponent<Animator>();
        playerAnim.Play("LayedOut");
        StartCoroutine(IntroSequencer());
    }

    IEnumerator IntroSequencer()
    {
        controls.Disable();
        playerAnim.Play("LayedOut");
        yield return new WaitForSeconds(5);
        playerAnim.SetTrigger("getUp");
        yield return new WaitForSeconds(5);
        controls.Enable();
    }

}
