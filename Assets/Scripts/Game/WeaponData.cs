using UnityEngine;

namespace TrashRush.Game
{
    public enum WeaponRarity
    {
        Common,
        Rare,
        Epic
    }

    public static class WeaponSaveKeys
    {
        public const string EquippedWeapon = "trash_rush_equipped_weapon";
        public const string PurchasedWeaponPrefix = "trash_rush_weapon_purchased_";
    }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "Trash Rush/Game/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private string Name;
        [SerializeField] private string Description;
        [SerializeField] private string Id;
        [SerializeField, Min(0)] private int _cost;

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
        public string SaveId => string.IsNullOrWhiteSpace(Id) ? name : Id;
        public int Cost => Mathf.Max(0, _cost);
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
