using System;
using Lean.Touch;
using UnityEngine;
using VContainer.Unity;

namespace TrashRush.Game
{
    public sealed class InputManager : IStartable, IDisposable
    {
        private const float DefaultMinDragWorldDistance = 1f;
        private const float DefaultDragTime = 1f;

        private readonly Camera _camera;
        private readonly SlashManager _slashManager;
        private readonly SlashDecalSpawner _slashDecalSpawner;
        private readonly SlashObjectService _slashObjectService;
        private readonly GamePauseService _pauseService;
        private readonly ScoreSystem _scoreSystem;
        private readonly SlashConfig _config;

        private LeanFinger _activeFinger;
        private Vector3 _startWorldPosition;

        public InputManager(
            Camera camera,
            SlashManager slashManager,
            SlashDecalSpawner slashDecalSpawner,
            SlashObjectService slashObjectService,
            GamePauseService pauseService,
            ScoreSystem scoreSystem,
            SlashConfig config)
        {
            _camera = camera;
            _slashManager = slashManager;
            _slashDecalSpawner = slashDecalSpawner;
            _slashObjectService = slashObjectService;
            _pauseService = pauseService;
            _scoreSystem = scoreSystem;
            _config = config;
        }

        public void Start()
        {
            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerUp += OnFingerUp;
            _slashManager.ObstacleHit += OnObstacleHit;
        }

        public void Dispose()
        {
            LeanTouch.OnFingerDown -= OnFingerDown;
            LeanTouch.OnFingerUp -= OnFingerUp;
            _slashManager.ObstacleHit -= OnObstacleHit;

            _activeFinger = null;
        }

        private void OnFingerDown(LeanFinger finger)
        {
            if (_pauseService.IsPaused ||
                !_slashObjectService.IsPlaying ||
                finger.StartedOverGui ||
                finger.IsOverGui)
            {
                return;
            }

            if (_activeFinger != null)
            {
                return;
            }

            _activeFinger = finger;
            _startWorldPosition = GetWorldPosition(finger);
        }

        private void OnFingerUp(LeanFinger finger)
        {
            if (_activeFinger != finger)
            {
                return;
            }

            _activeFinger = null;

            if (_pauseService.IsPaused ||
                !_slashObjectService.IsPlaying ||
                finger.IsOverGui ||
                finger.Age > DragTime)
            {
                return;
            }

            var endWorldPosition = GetWorldPosition(finger);
            var dragDistance = Vector3.Distance(_startWorldPosition, endWorldPosition);

            if (dragDistance < MinDragWorldDistance)
            {
                return;
            }

            _slashManager.Play(_startWorldPosition, endWorldPosition);
            var nextStreak = _slashManager.CurrentStreak + 1;

            if (_slashDecalSpawner.TrySpawn(
                    _startWorldPosition,
                    endWorldPosition,
                    nextStreak,
                    out var slashObject))
            {
                var streak = _slashManager.RegisterHit();
                _scoreSystem.RegisterSlash(streak);
                _slashObjectService.TryRegisterSlash(slashObject);
            }
            else
            {
                _slashManager.RegisterMiss();
            }
        }

        private void OnObstacleHit(ObstacleObject obstacle)
        {
            if (_slashObjectService.TryRegisterObstacleSlash(obstacle))
            {
                _slashManager.RegisterMiss();
            }
        }

        private Vector3 GetWorldPosition(LeanFinger finger)
        {
            var position = finger.GetWorldPosition(_slashManager.Depth - _camera.transform.position.z, _camera);
            position.z = _slashManager.Depth;

            return position;
        }

        private float MinDragWorldDistance =>
            _config != null ? Mathf.Max(0f, _config.MinDragWorldDistance) : DefaultMinDragWorldDistance;

        private float DragTime =>
            _config != null ? Mathf.Max(0.01f, _config.DragTime) : DefaultDragTime;
    }
}
