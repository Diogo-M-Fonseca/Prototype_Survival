using UnityEngine;

public abstract class Interactable : MonoBehaviour, IInteractable
{
    [SerializeField] private string _prompt;
    [SerializeField] private bool _canInteract = true;

    protected virtual string DefaultPrompt => "interact";

    public virtual string Prompt => string.IsNullOrEmpty(_prompt) ? DefaultPrompt : _prompt;
    public virtual bool CanInteract => _canInteract;

    public void SetInteractable(bool value) => _canInteract = value;

    public void Interact(GameObject interactor)
    {
        if (!CanInteract)
        {
            return;
        }
        OnInteract(interactor);
    }

    protected abstract void OnInteract(GameObject interactor);

}
