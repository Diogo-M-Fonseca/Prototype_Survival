using UnityEngine;

public interface IInteractable
{
    string Prompt { get; }
    bool CanInteract {  get; }
    void Interact(GameObject gameObject);
}
