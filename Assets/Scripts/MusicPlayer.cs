using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip _music;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;

    [SerializeField, Min(0f)] private float _fadeInTime = 2f;

    private AudioSource _source;

    private void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.clip = _music;
        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.volume = _fadeInTime > 0f ? 0f : _volume;
    }

    private void Start()
    {
        if (_music != null) _source.Play();
    }

    private void Update()
    {
        if (_fadeInTime <= 0f || _source.volume >= _volume) return;

        _source.volume = Mathf.MoveTowards(_source.volume, _volume, Time.unscaledDeltaTime * _volume / _fadeInTime);
    }
}
