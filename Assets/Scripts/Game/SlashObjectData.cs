using DG.Tweening;
using GameLift.Audio;
using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "SlashObjectData", menuName = "Trash Rush/Game/Slash Object Data")]
    public sealed class SlashObjectData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private string _id;
        [SerializeField] private SlashObject _prefab;
        [SerializeField] private Texture2D _slashTexture;
        [SerializeField] private EffectBase _hitEffect;
        [Header("Cell Fracture")]
        [SerializeField] private SoundData _hitSound;
        [Tooltip("0 breaks the entire model; 1 uses normal cuts; 10 releases no bricks.")]
        [SerializeField, Range(0f, 10f)] private float _hardness = 1f;
        [Tooltip("Allow voxel models to split into independently slashable objects.")]
        [SerializeField] private bool _slashable;
        [Tooltip("Minimum brick count for a new slashable half. Smaller halves are released as debris.")]
        [SerializeField, Min(1)] private int _minSlashablePieceCount = 10;
        [Tooltip("Opposite push perpendicular to the cut for both split modes. 0 disables the push.")]
        [SerializeField, Range(0f, 10f)] private float _splitPushForce = 0.6f;
        [Tooltip("0 releases bricks under gravity only. Higher values increase random XY and world -Z launch speed.")]
        [SerializeField, Range(0f, 10f)] private float _fragmentationForce = 5f;
        [Header("Object Rotation")]
        [Tooltip("Hit angular velocity impulse in radians per second. No rotation tween is used.")]
        [SerializeField, Range(0f, 10f)] private float _hitRotationForce = 1f;
        public float HitRotationForce => Mathf.Clamp(_hitRotationForce, 0f, 10f);
        [SerializeField] private bool _rotationEnabled = true;
        [SerializeField, Min(0f)] private float _rotationDuration = 4f;
        [SerializeField, Min(0f)] private float _minRotationAngle = 20f;
        [SerializeField, Min(0f)] private float _maxRotationAngle = 90f;
        [SerializeField] private Ease _rotationEase = Ease.OutSine;
        [Header("Fall")]
        [SerializeField, Min(0.01f)] private float _fallDuration = 9f;
        [SerializeField] private Ease _fallEase = Ease.Linear;
        [Header("Cell Fracture")]
        public GameObject CellFractured;
        [Tooltip("Main impulse perpendicular to the slash on the gameplay camera plane.")]
        [SerializeField, Min(0f)] private float _cellFractureForce = 5f;
        [SerializeField, Min(0f)] private float _cellFractureUpwardForce = 0.5f;
        [SerializeField, Min(0f)] private float _cellFractureRandomForce = 0.25f;
        [SerializeField, Min(0f)] private float _cellFractureTorqueForce = 0.5f;
        [SerializeField, Min(0f)] private float _cellFractureDestroyDelay = 2f;
        [SerializeField, Min(0f)] private float _cellFracturedDestroyDelay = 2f;

        public string Name => _displayName;
        public string DisplayName => Name;
        public string Id => _id;
        public SlashObject Prefab => _prefab;
        public Texture2D SlashTexture => _slashTexture;
        public EffectBase HitEffect => _hitEffect;
        public SoundData HitSound => _hitSound;
        public float Hardness => Mathf.Clamp(_hardness, 0f, 10f);
        public bool Slashable => _slashable;
        public int MinSlashablePieceCount => Mathf.Max(1, _minSlashablePieceCount);
        public float SplitPushForce => Mathf.Clamp(_splitPushForce, 0f, 10f);
        public float FragmentationForce => Mathf.Clamp(_fragmentationForce, 0f, 10f);
        public float BrickReleaseRatio => Hardness <= 1f ? 1f : (10f - Hardness) / (9f * Hardness);
        public bool RotationEnabled => _rotationEnabled;
        public float RotationDuration => Mathf.Max(0f, _rotationDuration);
        public float MinRotationAngle => Mathf.Max(0f, _minRotationAngle);
        public float MaxRotationAngle => Mathf.Max(MinRotationAngle, _maxRotationAngle);
        public Ease RotationEase => _rotationEase;
        public float FallDuration => Mathf.Max(0.01f, _fallDuration);
        public Ease FallEase => _fallEase;
        public float CellFractureForce => Mathf.Max(0f, _cellFractureForce);
        public float CellFractureUpwardForce => Mathf.Max(0f, _cellFractureUpwardForce);
        public float CellFractureRandomForce => Mathf.Max(0f, _cellFractureRandomForce);
        public float CellFractureTorqueForce => Mathf.Max(0f, _cellFractureTorqueForce);
        public float CellFractureDestroyDelay => Mathf.Max(0f, _cellFractureDestroyDelay);
        public float CellFracturedDestroyDelay => Mathf.Max(0f, _cellFracturedDestroyDelay);
    }
}
