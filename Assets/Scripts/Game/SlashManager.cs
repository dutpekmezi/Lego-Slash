using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TrashRush.Game
{
    public sealed class SlashManager : IDisposable
    {
        private readonly WeaponSystem _weaponSystem;
        private readonly SlashConfig _slashConfig;
        private readonly StreakConfig _streakConfig;
        private readonly PositionConfig _positionConfig;
        private readonly Transform _parent;
        private readonly List<Slash> _activeSlashes = new();

        private Tween _streakResetTween;
        private int _currentStreak;

        public event Action<int> StreakChanged;
        public event Action<Vector3, Vector3> SlashPlayed;
        public event Action<ObstacleObject> ObstacleHit;

        public int CurrentStreak => _currentStreak;

        public float Depth
        {
            get
            {
                if (_positionConfig != null)
                {
                    return _positionConfig.SlashZ;
                }

                var slashPrefab = _weaponSystem?.CurrentWeapon?.SlashPrefab;

                if (slashPrefab == null)
                {
                    return 0f;
                }

                if (_parent == null)
                {
                    return slashPrefab.Depth;
                }

                return _parent.TransformPoint(slashPrefab.transform.localPosition).z;
            }
        }

        public SlashManager(
            WeaponSystem weaponSystem,
            SlashConfig slashConfig,
            StreakConfig streakConfig,
            PositionConfig positionConfig,
            Transform parent)
        {
            _weaponSystem = weaponSystem;
            _slashConfig = slashConfig;
            _streakConfig = streakConfig;
            _positionConfig = positionConfig;
            _parent = parent;
        }

        public void Play(Vector3 startPosition, Vector3 endPosition)
        {
            SlashPlayed?.Invoke(startPosition, endPosition);

            var slashPrefab = _weaponSystem?.CurrentWeapon?.SlashPrefab;

            if (slashPrefab == null)
            {
                return;
            }

            var slash = UnityEngine.Object.Instantiate(slashPrefab, _parent);
            var position = slash.transform.position;
            position.z = Depth;
            slash.transform.position = position;
            slash.Construct(_slashConfig);
            _activeSlashes.Add(slash);
            slash.Play(
                startPosition,
                endPosition,
                OnSlashCompleted,
                OnObstacleHit);
        }

        public int RegisterHit()
        {
            _currentStreak++;
            StreakChanged?.Invoke(_currentStreak);
            RestartStreakTimeout();
            return _currentStreak;
        }

        public void RegisterMiss()
        {
            ResetStreak();
        }

        public void Dispose()
        {
            KillStreakTimeout();
            _currentStreak = 0;

            for (var i = _activeSlashes.Count - 1; i >= 0; i--)
            {
                DestroySlash(_activeSlashes[i]);
            }

            _activeSlashes.Clear();
        }

        private void RestartStreakTimeout()
        {
            KillStreakTimeout();

            _streakResetTween = DOTween.Sequence()
                .AppendInterval(_streakConfig != null ? _streakConfig.ResetDelay : 1.5f)
                .AppendCallback(OnStreakTimeout);

            if (_streakConfig != null && _streakConfig.UseUnscaledTime)
            {
                _streakResetTween.SetUpdate(true);
            }
        }

        private void ResetStreak()
        {
            KillStreakTimeout();

            if (_currentStreak == 0)
            {
                return;
            }

            _currentStreak = 0;
            StreakChanged?.Invoke(_currentStreak);
        }

        private void OnStreakTimeout()
        {
            _streakResetTween = null;

            if (_currentStreak == 0)
            {
                return;
            }

            _currentStreak = 0;
            StreakChanged?.Invoke(_currentStreak);
        }

        private void KillStreakTimeout()
        {
            if (_streakResetTween != null && _streakResetTween.IsActive())
            {
                _streakResetTween.Kill(false);
            }

            _streakResetTween = null;
        }

        private void OnSlashCompleted(Slash slash)
        {
            _activeSlashes.Remove(slash);
            DestroySlash(slash);
        }

        private void OnObstacleHit(ObstacleObject obstacle)
        {
            ObstacleHit?.Invoke(obstacle);
        }

        private static void DestroySlash(Slash slash)
        {
            if (slash == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(slash.gameObject);
        }
    }
}
