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
        private readonly WeaponController _weaponController;
        private readonly SlashObjectService _slashObjectService;
        private readonly GamePauseService _pauseService;
        private readonly SlashConfig _config;

        private LeanFinger _activeFinger;
        private Vector3 _startWorldPosition;

        public InputManager(
            Camera camera,
            SlashManager slashManager,
            WeaponController weaponController,
            SlashObjectService slashObjectService,
            GamePauseService pauseService,
            SlashConfig config)
        {
            _camera = camera;
            _slashManager = slashManager;
            _weaponController = weaponController;
            _slashObjectService = slashObjectService;
            _pauseService = pauseService;
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

            var nextStreak = _slashManager.CurrentStreak + 1;
            _weaponController.Slash(
                _startWorldPosition,
                endWorldPosition,
                nextStreak,
                finger.ScreenPosition - finger.StartScreenPosition);
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
