using UnityEngine;

namespace TrashRush.Game
{
    public enum WeaponRarity
    {
        Common,
        Rare,
        Epic
    }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "Trash Rush/Game/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private string Name;
        [SerializeField] private string Description;
        [SerializeField] private string Id;

        [SerializeField] private GameObject _weaponPrefab;
        [SerializeField] private Sprite _weaponSprite;
        [SerializeField] private WeaponRarity _rarity = WeaponRarity.Common;
        [SerializeField] private Color _rarityColor = Color.white;
        [SerializeField] private SpriteRenderer _backgroundSlashPrefab;
        [SerializeField] private Slash _slashPrefab;
        [SerializeField] private EffectBase _slashStreakEffect;

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? name : Name;
        public string WeaponDescription => Description;
        public string WeaponId => Id;
        public GameObject WeaponPrefab => _weaponPrefab;
        public Sprite WeaponSprite => _weaponSprite;
        public WeaponRarity Rarity => _rarity;
        public Color RarityColor => _rarityColor;
        public string RarityText => _rarity.ToString().ToUpperInvariant();
        public SpriteRenderer BackgroundSlashPrefab => _backgroundSlashPrefab;
        public Slash SlashPrefab => _slashPrefab;
        public EffectBase SlashStreakEffect => _slashStreakEffect;
    }
}
