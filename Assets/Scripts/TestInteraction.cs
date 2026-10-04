using UnityEngine;

public class TestInteraction : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("Interacted with object!");
    }
}
