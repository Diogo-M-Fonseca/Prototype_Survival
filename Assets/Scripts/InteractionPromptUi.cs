using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUi : MonoBehaviour
{
    [SerializeField] private PlayerInteraction _interaction;
    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private string _format = "{0} to {1}";

    [Header("Reticle")]
    [SerializeField] private Image _reticle;
    [SerializeField] private Color _idleColor = Color.white;
    [SerializeField] private Color _ActiveColor = Color.green;
    [SerializeField, Min(0.1f)] private float _activeScale = 1.5f;

    private bool _wasActive;
    private IInteractable _lastTarget;
    private string _lastPrompt;

    private void Start()
    {
       Apply(false);
    }

    private void LateUpdate()
    {
        IInteractable target = _interaction.Current;
        bool active = target != null;

        Apply(active);

        if (!active)
        {
            return;
        }

        string prompt = target.Prompt;
        if (target == _lastTarget && prompt == _lastPrompt)
        {
            return;
        }

        _lastTarget = target;
        _lastPrompt = prompt;

        if (_promptText != null)
        {
            _promptText.text = string.Format(_format, _interaction.InteractKey, prompt);
        }
    }

    private void Apply(bool active)
    {
        if (_promptText != null) _promptText.enabled = active;

        if (_reticle != null)
        {
            _reticle.color = active ? _ActiveColor : _idleColor;
            _reticle.rectTransform.localScale = Vector3.one * (active ? _activeScale : 1f);
        }
    }

}
