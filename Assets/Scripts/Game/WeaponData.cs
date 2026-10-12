using GameLift.Audio;
using System.Collections.Generic;
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

        [SerializeField] private List<SoundData> _slashSounds;

        [Header("Hit Effect Scaling")]
        [SerializeField] private bool _enableLeafWindScaling;
        [SerializeField] private bool _enableObjectRotationScaling;
        [SerializeField] private bool _enableScreenShakeScaling;

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
        public List<SoundData> SlashSound => _slashSounds;
        public bool EnableLeafWindScaling => _enableLeafWindScaling;
        public bool EnableObjectRotationScaling => _enableObjectRotationScaling;
        public bool EnableScreenShakeScaling => _enableScreenShakeScaling;

        public LeafWindData ScaleLeafWindData(LeafWindData baseData, int streak)
        {
            return _enableLeafWindScaling
                ? baseData.Scale(GetStreakScale(streak))
                : baseData;
        }

        public float ScaleObjectRotation(float baseAngle, int streak)
        {
            return ScaleHitEffectValue(
                baseAngle,
                streak,
                _enableObjectRotationScaling);
        }

        public float ScaleScreenShake(float baseStrength, float maxStrength, int streak)
        {
            var scaledStrength = ScaleHitEffectValue(
                baseStrength,
                streak,
                _enableScreenShakeScaling);

            return Mathf.Min(
                scaledStrength,
                Mathf.Max(Mathf.Max(0f, baseStrength), Mathf.Max(0f, maxStrength)));
        }

        private static float ScaleHitEffectValue(
            float baseValue,
            int streak,
            bool enabled)
        {
            baseValue = Mathf.Max(0f, baseValue);

            return enabled
                ? baseValue * GetStreakScale(streak)
                : baseValue;
        }

        private static float GetStreakScale(int streak)
        {
            return Mathf.Max(1, streak);
        }

        public SoundData GetRandomSlashSound()
        {
            int randomIndex = UnityEngine.Random.Range(0, _slashSounds.Count);

            return _slashSounds[randomIndex];
        }
    }
}
