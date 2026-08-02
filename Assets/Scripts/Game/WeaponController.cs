using System;
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

        private bool _started;

        public WeaponController(
            WeaponSystem weaponSystem,
            SlashManager slashManager,
            SlashDecalSpawner slashDecalSpawner,
            SlashObjectService slashObjectService,
            ScoreSystem scoreSystem)
        {
            _weaponSystem = weaponSystem;
            _slashManager = slashManager;
            _slashDecalSpawner = slashDecalSpawner;
            _slashObjectService = slashObjectService;
            _scoreSystem = scoreSystem;
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
            int streak)
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

            if (_slashDecalSpawner != null &&
                _slashDecalSpawner.TrySpawn(
                    startWorldPosition,
                    endWorldPosition,
                    streak,
                    out var slashObject))
            {
                var registeredStreak = _slashManager.RegisterHit();
                _scoreSystem?.RegisterSlash(registeredStreak);
                _slashObjectService?.TryRegisterSlash(slashObject);
                return true;
            }

            _slashManager.RegisterMiss();
            return false;
        }
    }
}
