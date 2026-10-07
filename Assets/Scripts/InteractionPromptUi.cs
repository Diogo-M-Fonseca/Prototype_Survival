using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUi : MonoBehaviour
{
    [SerializeField] private PlayerInteraction _interaction;

    [SerializeField] private HotbarController _hotbar;

    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private string _format = "{0} to {1}";

    [Header("Reticle")]
    [SerializeField] private Image _reticle;
    [SerializeField] private Color _idleColor = Color.white;
    [SerializeField] private Color _activeColor = Color.green;
    [SerializeField, Min(0.1f)] private float _activeScale = 1.5f;

    private IInteractable _lastTarget;
    private string _lastPrompt;
    private string _lastUseKey;
    private string _lastUsePrompt;


    private void LateUpdate()
    {
        IInteractable target = _interaction.Current;

        // O item na mão tem efeito sobre o que está sob a mira?
        string useKey = null;
        string usePrompt = null;
        bool hasUse = _hotbar != null && _hotbar.TryGetUsePrompt(out useKey, out usePrompt);

        bool active = target != null || hasUse;

        Apply(active);

        if (!active)
        {
            return;
        }

        string prompt = target != null ? target.Prompt : null;
        if (target == _lastTarget && prompt == _lastPrompt
            && useKey == _lastUseKey && usePrompt == _lastUsePrompt)
        {
            return;
        }

        _lastTarget = target;
        _lastPrompt = prompt;
        _lastUseKey = useKey;
        _lastUsePrompt = usePrompt;

        if (_promptText != null)
        {
            // Uma linha para a interação do mundo e outra para o item na mão.
            string text = target != null ? string.Format(_format, _interaction.InteractKey, prompt) : "";
            if (hasUse)
            {
                if (text.Length > 0) text += "\n";
                text += string.Format(_format, useKey, usePrompt);
            }
            _promptText.text = text;
        }
    }

    private void Apply(bool active)
    {
        if (_promptText != null) _promptText.enabled = active;

        if (_reticle != null)
        {
            _reticle.color = active ? _activeColor : _idleColor;
            _reticle.rectTransform.localScale = Vector3.one * (active ? _activeScale : 1f);
        }
    }

}