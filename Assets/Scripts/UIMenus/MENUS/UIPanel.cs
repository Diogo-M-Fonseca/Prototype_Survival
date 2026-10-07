using System;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float _fadeDuration = 0.15f;

    private CanvasGroup _group;
    private float _targetAlpha = 1f;

    public bool IsVisible { get; private set; } = true;

    public event Action<bool> VisibilityChanged;

    private CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());

    public void Show() => SetVisible(true);

    public void Hide() => SetVisible(false);

    public void Toggle() => SetVisible(!IsVisible);

    public void SetVisible(bool visible, bool instant = false)
    {
        bool changed = IsVisible != visible;
        IsVisible = visible;
        _targetAlpha = visible ? 1f : 0f;
        Group.blocksRaycasts = visible;
        Group.interactable = visible;

        if (visible)
            gameObject.SetActive(true);

        if (instant || _fadeDuration <= 0f)
        {
            Group.alpha = _targetAlpha;
            if (!visible)
                gameObject.SetActive(false);
        }

        if (changed)
            VisibilityChanged?.Invoke(visible);
    }

    private void Update()
    {
        if (_fadeDuration <= 0f || Mathf.Approximately(Group.alpha, _targetAlpha))
            return;

        Group.alpha = Mathf.MoveTowards(
            Group.alpha,
            _targetAlpha,
            Time.unscaledDeltaTime / _fadeDuration
        );

        if (!IsVisible && Group.alpha <= 0f)
            gameObject.SetActive(false);
    }
}
