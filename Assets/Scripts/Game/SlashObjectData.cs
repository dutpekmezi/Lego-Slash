using DG.Tweening;
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
        [SerializeField, Min(1)] private int _totalSlash = 1;
        [Header("Object Rotation")]
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
        [SerializeField, Min(0f)] private float _cellFractureForce = 5f;
        [SerializeField, Min(0f)] private float _cellFractureDestroyDelay = 2f;
        [SerializeField, Min(0f)] private float _cellFracturedDestroyDelay = 2f;

        public string Name => _displayName;
        public string DisplayName => Name;
        public string Id => _id;
        public SlashObject Prefab => _prefab;
        public Texture2D SlashTexture => _slashTexture;
        public EffectBase HitEffect => _hitEffect;
        public int TotalSlash => Mathf.Max(1, _totalSlash);
        public bool RotationEnabled => _rotationEnabled;
        public float RotationDuration => Mathf.Max(0f, _rotationDuration);
        public float MinRotationAngle => Mathf.Max(0f, _minRotationAngle);
        public float MaxRotationAngle => Mathf.Max(MinRotationAngle, _maxRotationAngle);
        public Ease RotationEase => _rotationEase;
        public float FallDuration => Mathf.Max(0.01f, _fallDuration);
        public Ease FallEase => _fallEase;
        public float CellFractureForce => Mathf.Max(0f, _cellFractureForce);
        public float CellFractureDestroyDelay => Mathf.Max(0f, _cellFractureDestroyDelay);
        public float CellFracturedDestroyDelay => Mathf.Max(0f, _cellFracturedDestroyDelay);
    }
}
