using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public Text uiText;
    private Queue<string> textQueue;
    public Animator anim;
    
    void Start()
    {
        textQueue = new Queue<string>();
    }

    public void StartDialogue(Dialogue dialogue)
    {
        
        Debug.Log("Triggering text.");
        anim.SetBool("appear", true);
        textQueue.Clear();
        
        foreach (string text in dialogue.texts)
        {
            textQueue.Enqueue(text);
        }

        DisplayNextText();
        
    }

    public void DisplayNextText()
    {
        if (textQueue.Count == 0)
        {
            EndDialogue();
            Debug.Log("Dialogue ended.");
            return;
        }

        string text = textQueue.Dequeue();
        Debug.Log("Displaying" + text);
        StopAllCoroutines();
        StartCoroutine(TypeText(text));

    }

    public void EndDialogue()
    {
        Debug.Log("Ending text");
        anim.SetBool("appear", false);
    }

    IEnumerator TypeText(string text)
    {
        uiText.text = "";
        foreach (char character in text.ToCharArray())
        {
            uiText.text += character;
            yield return null;
        }
        yield return new WaitForSeconds(3);
        DisplayNextText();
    }

}
