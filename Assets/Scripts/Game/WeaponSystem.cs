using System;
using System.Collections.Generic;
using GameLift.Save;
using UnityEngine;

namespace TrashRush.Game
{
    public sealed class WeaponSystem
    {
        private static readonly IReadOnlyList<WeaponData> EmptyWeapons =
            Array.Empty<WeaponData>();

        private readonly WeaponDatas _weaponDatas;
        private readonly ISaveService _saveService;

        public event Action<WeaponData> WeaponChanged;

        public IReadOnlyList<WeaponData> Weapons =>
            _weaponDatas != null ? _weaponDatas.Weapons : EmptyWeapons;

        public WeaponData CurrentWeapon { get; private set; }

        public WeaponSystem(
            WeaponDatas weaponDatas,
            ISaveService saveService)
        {
            _weaponDatas = weaponDatas;
            _saveService = saveService;
            CurrentWeapon = FindEquippedWeapon();

            if (CurrentWeapon != null)
            {
                MarkPurchased(CurrentWeapon);
                return;
            }

            CurrentWeapon = _weaponDatas != null
                ? _weaponDatas.DefaultWeapon
                : null;
        }

        public bool Equip(int index)
        {
            if (index < 0 || index >= Weapons.Count)
            {
                return false;
            }

            return Equip(Weapons[index]);
        }

        public bool Equip(WeaponData weapon)
        {
            if (weapon == null || !Contains(weapon))
            {
                return false;
            }

            var changed = CurrentWeapon != weapon;
            if (CurrentWeapon == weapon)
            {
                SaveEquippedWeapon(weapon);
                return true;
            }

            CurrentWeapon = weapon;
            SaveEquippedWeapon(weapon);

            if (changed)
            {
                WeaponChanged?.Invoke(CurrentWeapon);
            }

            return true;
        }

        public bool IsEquipped(WeaponData weapon)
        {
            return weapon != null && CurrentWeapon == weapon;
        }

        public bool IsPurchased(WeaponData weapon)
        {
            return weapon != null &&
                   (weapon.Cost <= 0 ||
                    IsEquipped(weapon) ||
                    LoadPurchased(weapon));
        }

        public void MarkPurchased(WeaponData weapon)
        {
            if (weapon == null || weapon.Cost <= 0)
            {
                return;
            }

            _saveService?.Raw.Save(GetPurchasedKey(weapon), true.ToString());
        }

        private WeaponData FindEquippedWeapon()
        {
            var equippedWeaponId = _saveService?.Raw.LoadData(WeaponSaveKeys.EquippedWeapon);

            if (string.IsNullOrWhiteSpace(equippedWeaponId))
            {
                return null;
            }

            for (var i = 0; i < Weapons.Count; i++)
            {
                var weapon = Weapons[i];

                if (weapon != null &&
                    weapon.SaveId == equippedWeaponId)
                {
                    return weapon;
                }
            }

            return null;
        }

        private bool LoadPurchased(WeaponData weapon)
        {
            return _saveService != null &&
                   _saveService.Raw.LoadBool(GetPurchasedKey(weapon));
        }

        private bool Contains(WeaponData weapon)
        {
            for (var i = 0; i < Weapons.Count; i++)
            {
                if (Weapons[i] == weapon)
                {
                    return true;
                }
            }

            return false;
        }

        private void SaveEquippedWeapon(WeaponData weapon)
        {
            if (weapon == null)
            {
                return;
            }

            _saveService?.Raw.Save(WeaponSaveKeys.EquippedWeapon, weapon.SaveId);
        }

        private static string GetPurchasedKey(WeaponData weapon)
        {
            return $"{WeaponSaveKeys.PurchasedWeaponPrefix}{weapon.SaveId}";
        }
    }
}
