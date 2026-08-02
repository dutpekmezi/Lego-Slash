using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VContainer;

namespace TrashRush.Game
{
    public sealed class Slash : MonoBehaviour
    {
        private const float ObstacleHitHalfDepth = 500f;

        [SerializeField] private Transform _mesh;
        [SerializeField] private SlashConfig _config;

        private readonly HashSet<int> _hitObstacleIds = new();

        private Vector3 _fullScale;
        private Vector3 _targetScale;
        private Renderer[] _renderers;
        private WindSlashEffect[] _windSlashEffects;
        private Sequence _sequence;
        private Sequence _pulseSequence;
        private Action<ObstacleObject> _obstacleHit;
        private bool _isPlaying;

        public float Depth => transform.position.z;

        [Inject]
        public void Construct(SlashConfig config)
        {
            _config = config;
        }

        private void Awake()
        {
            if (_mesh == null)
            {
                return;
            }

            _fullScale = _mesh.localScale;
            _targetScale = _fullScale;
            _renderers = _mesh.GetComponentsInChildren<Renderer>(true);
            _windSlashEffects = GetComponentsInChildren<WindSlashEffect>(true);
            _mesh.localScale = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_isPlaying && Time.timeScale > 0f)
            {
                CheckObstacleHits();
            }
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            _pulseSequence?.Kill();
            _obstacleHit = null;
        }

        public void Play(Vector3 position)
        {
            Play(position, Quaternion.identity, _fullScale);
        }

        public void Play(Vector3 startPosition, Vector3 endPosition)
        {
            Play(startPosition, endPosition, null);
        }

        public void Play(
            Vector3 startPosition,
            Vector3 endPosition,
            Action<Slash> completed,
            Action<ObstacleObject> obstacleHit = null)
        {
            Play(startPosition, endPosition, 0, completed, obstacleHit);
        }

        public void Play(
            Vector3 startPosition,
            Vector3 endPosition,
            int streak,
            Action<Slash> completed,
            Action<ObstacleObject> obstacleHit = null)
        {
            var delta = endPosition - startPosition;
            delta.z = 0f;

            var length = delta.magnitude;

            if (length <= Mathf.Epsilon)
            {
                completed?.Invoke(this);
                return;
            }

            var position = (startPosition + endPosition) * 0.5f;
            position.z = Depth;

            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;

            _obstacleHit = obstacleHit;
            _hitObstacleIds.Clear();
            _isPlaying = true;
            SetStreak(streak);
            Play(position, Quaternion.Euler(0f, 0f, angle), _fullScale, completed);
        }

        public void SetStreak(int streak)
        {
            if (_windSlashEffects == null)
            {
                return;
            }

            for (var i = 0; i < _windSlashEffects.Length; i++)
            {
                if (_windSlashEffects[i] != null)
                {
                    _windSlashEffects[i].SetStreak(streak);
                }
            }
        }

        private void Play(Vector3 position, Quaternion rotation, Vector3 targetScale, Action<Slash> completed = null)
        {
            if (_config == null || _mesh == null)
            {
                completed?.Invoke(this);
                return;
            }

            transform.SetPositionAndRotation(position, rotation);
            _targetScale = targetScale;
            _sequence?.Kill();
            _pulseSequence?.Kill();
            _mesh.localScale = Vector3.zero;

            _sequence = DOTween.Sequence()
                .SetLink(gameObject)
                .Append(_mesh.DOScale(_targetScale, _config.ScaleTime).SetEase(_config.ScaleEase))
                .AppendCallback(StartPulse)
                .AppendInterval(_config.HoldTime)
                .AppendCallback(StopPulse)
                .Append(_mesh.DOScale(Vector3.zero, _config.DescaleTime).SetEase(_config.DescaleEase))
                .AppendCallback(() =>
                {
                    _isPlaying = false;
                    completed?.Invoke(this);
                });

            if (_config.UseUnscaledTime)
            {
                _sequence.SetUpdate(true);
            }
        }

        private void StartPulse()
        {
            if (!_config.PulseEnabled || _config.PulseTime <= 0f || _config.HoldTime <= 0f)
            {
                return;
            }

            _pulseSequence = DOTween.Sequence()
                .SetLink(gameObject)
                .Append(_mesh.DOScale(_targetScale * _config.PulseScale, _config.PulseTime).SetEase(_config.PulseEase))
                .Append(_mesh.DOScale(_targetScale, _config.PulseTime).SetEase(_config.PulseEase))
                .SetLoops(-1);

            if (_config.UseUnscaledTime)
            {
                _pulseSequence.SetUpdate(true);
            }
        }

        private void StopPulse()
        {
            _pulseSequence?.Kill();
            _pulseSequence = null;
            _mesh.localScale = _targetScale;
        }

        private void CheckObstacleHits()
        {
            if (_obstacleHit == null ||
                _renderers == null ||
                _renderers.Length == 0)
            {
                return;
            }

            for (var i = 0; i < _renderers.Length; i++)
            {
                var slashRenderer = _renderers[i];

                if (slashRenderer == null || !slashRenderer.enabled)
                {
                    continue;
                }

                var boundsTransform = slashRenderer.transform;
                var localBounds = slashRenderer.localBounds;
                var lossyScale = boundsTransform.lossyScale;
                var halfExtents = Vector3.Scale(
                    localBounds.extents,
                    new Vector3(
                        Mathf.Abs(lossyScale.x),
                        Mathf.Abs(lossyScale.y),
                        Mathf.Abs(lossyScale.z)));

                if (halfExtents.x <= Mathf.Epsilon ||
                    halfExtents.y <= Mathf.Epsilon)
                {
                    continue;
                }

                halfExtents.z = ObstacleHitHalfDepth;
                var colliders = Physics.OverlapBox(
                    boundsTransform.TransformPoint(localBounds.center),
                    halfExtents,
                    boundsTransform.rotation,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                for (var colliderIndex = 0;
                     colliderIndex < colliders.Length;
                     colliderIndex++)
                {
                    var obstacle = colliders[colliderIndex]
                        .GetComponentInParent<ObstacleObject>();

                    if (obstacle == null ||
                        !obstacle.IsHittable ||
                        !_hitObstacleIds.Add(obstacle.GetInstanceID()))
                    {
                        continue;
                    }

                    _obstacleHit.Invoke(obstacle);
                }
            }
        }
    }
}
