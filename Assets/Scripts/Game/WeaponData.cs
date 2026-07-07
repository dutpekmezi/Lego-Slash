using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Trash Rush/Game/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private GameObject _weaponPrefab;
        [SerializeField] private SpriteRenderer _backgroundSlashPrefab;
        [SerializeField] private Slash _slashPrefab;
        [SerializeField] private EffectBase _slashStreakEffect;

        public GameObject WeaponPrefab => _weaponPrefab;
        public SpriteRenderer BackgroundSlashPrefab => _backgroundSlashPrefab;
        public Slash SlashPrefab => _slashPrefab;
        public EffectBase SlashStreakEffect => _slashStreakEffect;
    }
}
