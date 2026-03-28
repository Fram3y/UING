using UnityEngine;

public class PlayerHitArea : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var outline = other.gameObject.GetComponent<Outline>();

        if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("I have hit an enemy!");
            outline.enabled = true;
        }

        if (other.gameObject.CompareTag("Interactable"))
        {
            Debug.Log("I have hit an interactable object!");
            outline.enabled = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        var outline = other.gameObject.GetComponent<Outline>();

        if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("I have left an enemy!");
            outline.enabled = false;
        }

        if (other.gameObject.CompareTag("Interactable"))
        {
            Debug.Log("I have left an interactable object!");
            outline.enabled = false;
        }
    }
}