using System;
using System.Collections.Generic;

namespace TrashRush.Game
{
    public sealed class WeaponSystem
    {
        private static readonly IReadOnlyList<WeaponData> EmptyWeapons =
            Array.Empty<WeaponData>();

        private readonly WeaponDatas _weaponDatas;

        public event Action<WeaponData> WeaponChanged;

        public IReadOnlyList<WeaponData> Weapons =>
            _weaponDatas != null ? _weaponDatas.Items : EmptyWeapons;

        public WeaponData CurrentWeapon { get; private set; }

        public WeaponSystem(WeaponDatas weaponDatas)
        {
            _weaponDatas = weaponDatas;

            for (var i = 0; i < Weapons.Count; i++)
            {
                if (Weapons[i] == null)
                {
                    continue;
                }

                CurrentWeapon = Weapons[i];
                break;
            }
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
