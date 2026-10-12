using DG.Tweening;
using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "SlashConfig", menuName = "Trash Rush/Game/Slash Config")]
    public sealed class SlashConfig : ScriptableObject
    {
        [SerializeField] private float _scaleTime = 0.08f;
        [SerializeField] private float _holdTime = 0.04f;
        [SerializeField] private float _descaleTime = 0.12f;
        [SerializeField, Min(0f)] private float _minDragWorldDistance = 1f;
        [SerializeField, Min(0.01f)] private float _dragTime = 1f;
        [SerializeField] private bool _pulseEnabled = true;
        [SerializeField] private float _pulseScale = 1.06f;
        [SerializeField] private float _pulseTime = 0.035f;
        [SerializeField] private Ease _scaleEase = Ease.OutQuad;
        [SerializeField] private Ease _pulseEase = Ease.InOutSine;
        [SerializeField] private Ease _descaleEase = Ease.InQuad;
        [SerializeField] private bool _useUnscaledTime;
        [SerializeField, Min(0f)] private float _decalProjectorLifetime = 2f;
        [SerializeField, Min(0f)] private float _decalProjectorFadeTime = 0.35f;
        [SerializeField, Min(0f)] private float _decalProjectorMinHeight = 3f;
        [SerializeField, Min(0f)] private float _decalProjectorMaxHeight = 8f;
        [Header("Background Slash")]
        [SerializeField] private bool _backgroundSlashEnabled = true;
        [SerializeField, Min(0f)] private float _backgroundSlashLifetime = 2f;
        [SerializeField, Min(0f)] private float _backgroundSlashFadeTime = 0.35f;
        [SerializeField, Min(0f)] private float _backgroundSlashMinHeight = 2.5f;
        [SerializeField, Min(0f)] private float _backgroundSlashMaxHeight = 10f;
        [SerializeField] private Vector2 _backgroundSlashScaleMultiplier = Vector2.one;
        [SerializeField] private Color _backgroundSlashColor = Color.white;
        [SerializeField] private int _backgroundSlashSortingOrderOffset = 1;
        [Header("Voxel LEGO")]
        [SerializeField, Range(0f, 1f)] private float _splitHitBreakRatio = 0.2f;
        [Tooltip("How quickly the split push slows down. Higher values stop it sooner.")]
        [SerializeField, Min(0.1f)] private float _splitPushDamping = 4f;
        public float SplitPushDamping => Mathf.Max(0.1f, _splitPushDamping);
        [SerializeField, Min(0f)] private float _splitSeparationDistance = 0.3f;
        public float SplitSeparationDistance => Mathf.Max(0f, _splitSeparationDistance);
        public float SplitHitBreakRatio => Mathf.Clamp01(_splitHitBreakRatio);
        [SerializeField, Min(0.001f)] private float _brickCutHalfWidth = 0.04f;
        [SerializeField] private Vector2 _brickLaunchSpeed = new(3f, 6f);
        [SerializeField, Min(0f)] private float _brickLateralSpeed = 2f;
        [SerializeField, Min(0f)] private float _brickSpinSpeed = 4f;
        [SerializeField, Min(0.1f)] private float _brickLifetime = 5f;
        public float BrickCutHalfWidth => Mathf.Max(0.001f, _brickCutHalfWidth);
        public Vector2 BrickLaunchSpeed => new(Mathf.Max(0.01f, _brickLaunchSpeed.x),
            Mathf.Max(Mathf.Max(0.01f, _brickLaunchSpeed.x), _brickLaunchSpeed.y));
        public float BrickLateralSpeed => Mathf.Max(0f, _brickLateralSpeed);
        public float BrickSpinSpeed => Mathf.Max(0f, _brickSpinSpeed);
        public float BrickLifetime => Mathf.Max(0.1f, _brickLifetime);
        public float ScaleTime => _scaleTime;
        public float HoldTime => _holdTime;
        public float DescaleTime => _descaleTime;
        public float MinDragWorldDistance => Mathf.Max(0f, _minDragWorldDistance);
        public float DragTime => Mathf.Max(0.01f, _dragTime);
        public bool PulseEnabled => _pulseEnabled;
        public float PulseScale => _pulseScale;
        public float PulseTime => _pulseTime;
        public Ease ScaleEase => _scaleEase;
        public Ease PulseEase => _pulseEase;
        public Ease DescaleEase => _descaleEase;
        public bool UseUnscaledTime => _useUnscaledTime;
        public float DecalProjectorLifetime => _decalProjectorLifetime;
        public float DecalProjectorFadeTime => _decalProjectorFadeTime;
        public float DecalProjectorMinHeight => _decalProjectorMinHeight;
        public float DecalProjectorMaxHeight => _decalProjectorMaxHeight;
        public bool BackgroundSlashEnabled => _backgroundSlashEnabled;
        public float BackgroundSlashLifetime => Mathf.Max(0f, _backgroundSlashLifetime);
        public float BackgroundSlashFadeTime => Mathf.Max(0f, _backgroundSlashFadeTime);
        public float BackgroundSlashMinHeight => Mathf.Max(0f, _backgroundSlashMinHeight);
        public float BackgroundSlashMaxHeight =>
            Mathf.Max(BackgroundSlashMinHeight, _backgroundSlashMaxHeight);
        public Vector2 BackgroundSlashScaleMultiplier => new(
            Mathf.Max(0f, _backgroundSlashScaleMultiplier.x),
            Mathf.Max(0f, _backgroundSlashScaleMultiplier.y));
        public Color BackgroundSlashColor => _backgroundSlashColor;
        public int BackgroundSlashSortingOrderOffset => _backgroundSlashSortingOrderOffset;
    }
}
