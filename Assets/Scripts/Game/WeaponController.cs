using GameLift.Audio;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace TrashRush.Game
{
    public sealed class WeaponController : IStartable, IDisposable
    {
        private readonly WeaponSystem _weaponSystem;
        private readonly SlashManager _slashManager;
        private readonly SlashDecalSpawner _slashDecalSpawner;
        private readonly SlashObjectService _slashObjectService;
        private readonly ScoreSystem _scoreSystem;
        private readonly IAudioService _audioService;
        private bool _started;

        public WeaponController(
            WeaponSystem weaponSystem,
            SlashManager slashManager,
            SlashDecalSpawner slashDecalSpawner,
            SlashObjectService slashObjectService,
            ScoreSystem scoreSystem,
            IAudioService audioService)
        {
            _weaponSystem = weaponSystem;
            _slashManager = slashManager;
            _slashDecalSpawner = slashDecalSpawner;
            _slashObjectService = slashObjectService;
            _scoreSystem = scoreSystem;
            this._audioService = audioService;
        }

        public void Start()
        {
            _started = true;
        }

        public void Dispose()
        {
            _started = false;
        }

        public bool Slash(
            Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            int streak,
            Vector2? screenSlashDirection = null)
        {
            if (!_started ||
                _weaponSystem?.CurrentWeapon == null ||
                _slashManager == null)
            {
                return false;
            }

            var weaponData = _weaponSystem.CurrentWeapon;

            _slashManager.Play(
                weaponData,
                startWorldPosition,
                endWorldPosition,
                streak);

            SlashObject slashObject = null;
            bool decalHit = _slashDecalSpawner != null && _slashDecalSpawner.TrySpawn(
                    startWorldPosition,
                    endWorldPosition,
                    streak,
                    out slashObject);
            // Resolve decals first while the cells still belong to their model.
            // Release cells before progress completion can destroy the original mesh.
            SlashObject brickOwner = null;
            var hitObjects = new List<SlashObject>();
            bool brickHit = _slashDecalSpawner != null && _slashDecalSpawner.TrySlashBricks(
                startWorldPosition, endWorldPosition, out brickOwner, hitObjects);
            if (slashObject == null) slashObject = brickOwner;
            if (decalHit || brickHit)
            {
                var registeredStreak = _slashManager.RegisterHit();
                _scoreSystem?.RegisterSlash(registeredStreak);
                if (slashObject != null && !hitObjects.Contains(slashObject)) hitObjects.Add(slashObject);
                _slashObjectService?.TryRegisterSlashes(
                    hitObjects, startWorldPosition, endWorldPosition, screenSlashDirection);

                var _weaponData = _weaponSystem.CurrentWeapon;
                _audioService.Play(weaponData.GetRandomSlashSound());

                return true;
            }

            _slashManager.RegisterMiss();
            return false;
        }
    }
}
