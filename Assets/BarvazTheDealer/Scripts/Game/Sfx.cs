using UnityEngine;

namespace BarvazTheDealer.Game
{
    /// <summary>Music loop + a small pool of one-shot sound effect voices.</summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        private AudioSource _music;
        private AudioSource[] _voices;
        private int _next;
        private bool _musicOn = true;
        private float _musicTarget = 0.42f;
        private Coroutine _duck;

        public bool MusicOn
        {
            get { return _musicOn; }
        }

        private void Awake()
        {
            Instance = this;
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.volume = 0f;
            _music.clip = GameAssets.Clip("music_lounge");
            if (_music.clip != null) _music.Play();

            _voices = new AudioSource[12];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }
        }

        private void Update()
        {
            float target = _musicOn ? _musicTarget : 0f;
            _music.volume = Mathf.MoveTowards(_music.volume, target, Time.unscaledDeltaTime * 0.6f);
        }

        public void ToggleMusic()
        {
            _musicOn = !_musicOn;
        }

        /// <summary>Briefly lowers the music under a big moment (e.g. the slap).</summary>
        public void Duck(float level, float seconds)
        {
            if (_duck != null) StopCoroutine(_duck);
            _duck = StartCoroutine(DuckRoutine(level, seconds));
        }

        private System.Collections.IEnumerator DuckRoutine(float level, float seconds)
        {
            _musicTarget = 0.42f * level;
            yield return new WaitForSecondsRealtime(seconds);
            _musicTarget = 0.42f;
        }

        public static void Play(string name, float volume = 1f, float pitchJitter = 0.05f)
        {
            if (Instance == null) return;
            var clip = GameAssets.Clip(name);
            if (clip == null) return;
            var v = Instance._voices[Instance._next];
            Instance._next = (Instance._next + 1) % Instance._voices.Length;
            v.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            v.volume = volume;
            v.clip = clip;
            v.Play();
        }
    }
}
