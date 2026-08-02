using System.Collections;
using UnityEngine;

namespace TrashRush.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public class EffectBase : MonoBehaviour
    {
        [SerializeField] private bool _includeChildren = true;
        [SerializeField] private bool _playOnEnable;
        [SerializeField] private bool _clearBeforePlay = true;
        [SerializeField] private bool _clearOnStop = true;

        [Header("Restart With Delay")]
        [SerializeField] private bool _restartWithDelayEnabled;
        [SerializeField, Min(0f)] private float _restartDelay = 0.25f;
        [SerializeField] private bool _useUnscaledTime;

        private ParticleSystem _primaryParticleSystem;
        private ParticleSystem[] _particleSystems;
        private Coroutine _restartRoutine;

        public ParticleSystem PrimaryParticleSystem
        {
            get
            {
                CacheParticleSystems();
                return _primaryParticleSystem;
            }
        }

        public ParticleSystem[] ParticleSystems
        {
            get
            {
                CacheParticleSystems();
                return _particleSystems;
            }
        }

        public float Duration
        {
            get
            {
                CacheParticleSystems();
                var duration = 0f;

                for (var i = 0; i < _particleSystems.Length; i++)
                {
                    if (_particleSystems[i] == null)
                    {
                        continue;
                    }

                    duration = Mathf.Max(duration, _particleSystems[i].main.duration);
                }

                return duration;
            }
        }

        public float TotalLifetime
        {
            get
            {
                CacheParticleSystems();
                var lifetime = 0f;

                for (var i = 0; i < _particleSystems.Length; i++)
                {
                    if (_particleSystems[i] == null)
                    {
                        continue;
                    }

                    var main = _particleSystems[i].main;
                    lifetime = Mathf.Max(
                        lifetime,
                        main.duration +
                        main.startDelay.constantMax +
                        main.startLifetime.constantMax);
                }

                return lifetime;
            }
        }

        protected virtual void Awake()
        {
            CacheParticleSystems();
        }

        protected virtual void OnEnable()
        {
            CacheParticleSystems();

            if (_restartWithDelayEnabled)
            {
                RestartWithDelay(_restartDelay);
                return;
            }

            if (_playOnEnable)
            {
                Play();
            }
        }

        protected virtual void OnDisable()
        {
            StopRestartWithDelay();
        }

        protected virtual void OnDestroy()
        {
            StopRestartWithDelay();
        }

        protected virtual void OnValidate()
        {
            _restartDelay = Mathf.Max(0f, _restartDelay);
            CacheParticleSystems();
        }

        public void Play()
        {
            PlayInternal(_clearBeforePlay);
        }

        public void Pause()
        {
            CacheParticleSystems();

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] != null)
                {
                    _particleSystems[i].Pause(false);
                }
            }
        }

        public void Resume()
        {
            PlayInternal(false);
        }

        public void Stop()
        {
            Stop(_clearOnStop);
        }

        public void Stop(bool clear)
        {
            Stop(clear
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting);
        }

        public void Stop(ParticleSystemStopBehavior stopBehavior)
        {
            CacheParticleSystems();
            StopRestartWithDelay();

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] != null)
                {
                    _particleSystems[i].Stop(false, stopBehavior);
                }
            }
        }

        public void StopEmitting()
        {
            Stop(ParticleSystemStopBehavior.StopEmitting);
        }

        public void Clear()
        {
            CacheParticleSystems();

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] != null)
                {
                    _particleSystems[i].Clear(false);
                }
            }
        }

        public void Restart()
        {
            Stop(true);
            Play();
        }

        public void RestartWithDelay(float delay)
        {
            StopRestartWithDelay();

            if (!isActiveAndEnabled)
            {
                return;
            }

            _restartRoutine = StartCoroutine(RestartWithDelayRoutine(Mathf.Max(0f, delay)));
        }

        public void StopRestartWithDelay()
        {
            if (_restartRoutine == null)
            {
                return;
            }

            StopCoroutine(_restartRoutine);
            _restartRoutine = null;
        }

        public void Kill()
        {
            StopRestartWithDelay();
            Stop(true);
            gameObject.SetActive(false);
        }

        public void DestroySelf()
        {
            DestroySelf(0f);
        }

        public void DestroySelf(float delay)
        {
            StopRestartWithDelay();
            Stop(true);
            Destroy(gameObject, Mathf.Max(0f, delay));
        }

        public void SetAutoRestartWithDelay(bool enabled)
        {
            _restartWithDelayEnabled = enabled;

            if (_restartWithDelayEnabled && isActiveAndEnabled)
            {
                RestartWithDelay(_restartDelay);
                return;
            }

            StopRestartWithDelay();
        }

        public void SetRestartDelay(float delay)
        {
            _restartDelay = Mathf.Max(0f, delay);

            if (_restartWithDelayEnabled && isActiveAndEnabled)
            {
                RestartWithDelay(_restartDelay);
            }
        }

        private IEnumerator RestartWithDelayRoutine(float delay)
        {
            PlayInternal(false);

            while (isActiveAndEnabled)
            {
                yield return Wait(Duration);

                StopInternal(ParticleSystemStopBehavior.StopEmitting);

                if (delay > 0f)
                {
                    yield return Wait(delay);
                }
                else
                {
                    yield return null;
                }

                if (isActiveAndEnabled)
                {
                    PlayInternal(false);
                }
            }

            _restartRoutine = null;
        }

        private IEnumerator Wait(float seconds)
        {
            if (seconds <= 0f)
            {
                yield break;
            }

            var elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        private void StopInternal(ParticleSystemStopBehavior stopBehavior)
        {
            CacheParticleSystems();

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] != null)
                {
                    _particleSystems[i].Stop(false, stopBehavior);
                }
            }
        }

        private void PlayInternal(bool clearBeforePlay)
        {
            CacheParticleSystems();

            if (clearBeforePlay)
            {
                Clear();
            }

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] != null)
                {
                    _particleSystems[i].Play(false);
                }
            }
        }

        private void CacheParticleSystems()
        {
            if (_primaryParticleSystem == null)
            {
                _primaryParticleSystem = GetComponent<ParticleSystem>();
            }

            _particleSystems = _includeChildren
                ? GetComponentsInChildren<ParticleSystem>(true)
                : new[] { _primaryParticleSystem };
        }
    }
}
