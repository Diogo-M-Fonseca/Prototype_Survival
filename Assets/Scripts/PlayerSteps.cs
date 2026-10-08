using UnityEngine;

public class PlayerSteps : MonoBehaviour
{
    [SerializeField] private AudioClip _footstepsLoop;

    [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;

    [SerializeField, Min(0f)] private float _minSpeed = 0.5f;

    [SerializeField, Min(0.1f)] private float _fadeSpeed = 8f;

    private CharacterController _controller;
    private AudioSource _source;
    private Vector3 _lastPosition;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        _source = gameObject.AddComponent<AudioSource>();
        _source.clip = _footstepsLoop;
        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = 0f; 
        _source.volume = 0f;
    }

    private void Start()
    {
        _lastPosition = transform.position;
    }

    private void Update()
    {
        if (_footstepsLoop == null) return;

        // A velocidade vem da posição (o PlayerMove move o controller em dois passos,
        // por isso controller.velocity não é fiável aqui).
        Vector3 delta = transform.position - _lastPosition;
        _lastPosition = transform.position;
        delta.y = 0f;

        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        bool walking = speed > _minSpeed && _controller.isGrounded;

        float targetVolume = walking ? _volume : 0f;
        _source.volume = Mathf.MoveTowards(_source.volume, targetVolume, _fadeSpeed * Time.deltaTime);

        if (walking)
        {
            if (!_source.isPlaying)
            {
                _source.UnPause(); // continua de onde ficou
                if (!_source.isPlaying) _source.Play();
            }
        }
        else if (_source.isPlaying && _source.volume <= 0.001f)
        {
            _source.Pause();
        }
    }
}
