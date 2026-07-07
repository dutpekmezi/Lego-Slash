using System.Collections.Generic;
using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "WeaponDatas", menuName = "Trash Rush/Game/Weapon Datas")]
    public sealed class WeaponDatas : ScriptableObject
    {
        [SerializeField] private List<WeaponData> _weaponDatas = new();

        public IReadOnlyList<WeaponData> Items => _weaponDatas;
    }
}
