using System.Collections.Generic;
using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "WeaponDatas", menuName = "Trash Rush/Game/Weapon Datas")]
    public sealed class WeaponDatas : ScriptableObject
    {
        [SerializeField] private List<WeaponData> _weaponDatas = new();

        [SerializeReference] private WeaponData _defaultWeaponData;

        public IReadOnlyList<WeaponData> Weapons => _weaponDatas;
        public WeaponData DefaultWeapon => _defaultWeaponData;
    }
}
