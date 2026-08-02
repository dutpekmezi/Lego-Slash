using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrashRush.Game
{
    public sealed class WeaponSystem
    {
        private static readonly IReadOnlyList<WeaponData> EmptyWeapons =
            Array.Empty<WeaponData>();

        private readonly WeaponDatas _weaponDatas;

        public event Action<WeaponData> WeaponChanged;

        public IReadOnlyList<WeaponData> Weapons =>
            _weaponDatas != null ? _weaponDatas.Weapons : EmptyWeapons;

        public WeaponData CurrentWeapon { get; private set; }

        public WeaponSystem(WeaponDatas weaponDatas)
        {
            _weaponDatas = weaponDatas;
            CurrentWeapon = FindEquippedWeapon();

            if (CurrentWeapon != null)
            {
                return;
            }

            CurrentWeapon = weaponDatas.DefaultWeapon;

            return;
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

            if (CurrentWeapon == weapon)
            {
                return true;
            }

            CurrentWeapon = weapon;
            WeaponChanged?.Invoke(CurrentWeapon);
            return true;
        }

        private WeaponData FindEquippedWeapon()
        {
            var equippedWeaponId = PlayerPrefs.GetString(WeaponSaveKeys.EquippedWeapon, string.Empty);

            if (string.IsNullOrWhiteSpace(equippedWeaponId))
            {
                return null;
            }

            for (var i = 0; i < Weapons.Count; i++)
            {
                var weapon = Weapons[i];

                if (weapon != null &&
                    weapon.SaveId == equippedWeaponId &&
                    IsPurchased(weapon))
                {
                    return weapon;
                }
            }

            return null;
        }

        private WeaponData FindFirstPurchasedWeapon()
        {
            for (var i = 0; i < Weapons.Count; i++)
            {
                var weapon = Weapons[i];

                if (weapon != null && IsPurchased(weapon))
                {
                    return weapon;
                }
            }

            return null;
        }

        private bool IsPurchased(WeaponData weapon)
        {
            return weapon != null &&
                   (weapon.Cost <= 0 ||
                    PlayerPrefs.GetInt($"{WeaponSaveKeys.PurchasedWeaponPrefix}{weapon.SaveId}", 0) == 1);
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
    }
}
