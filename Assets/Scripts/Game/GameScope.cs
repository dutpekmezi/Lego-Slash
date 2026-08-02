using GameLift.UI.SettingsPopup;
using TMPro;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TrashRush.Game
{
    public sealed class GameScope : LifetimeScope
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _cameraRig;
        [SerializeField] private WeaponDatas _weaponDatas;
        [SerializeField] private SlashConfig _slashConfig;
        [SerializeField] private StreakConfig _streakConfig;
        [SerializeField] private ScoreConfig _scoreConfig;
        [SerializeField] private PositionConfig _positionConfig;
        [SerializeField] private HitEffectConfig _hitEffectConfig;
        [SerializeField] private SlashObjectSpawnConfig _slashObjectSpawnConfig;
        [SerializeField] private GameObject _decalProjectorPrefab;
        [SerializeField] private GameObject _scoreCanvas;
        [SerializeField] private TMP_Text _scoreLabel;
        [SerializeField] private TMP_Text _streakLabel;
        [SerializeField] private LayerMask _slashHitLayerMask = ~0;
        [SerializeField, Min(2)] private int _slashHitSampleCount = 24;
        [Header("Effects")]
        [SerializeField] private Transform _effectsRoot;
        [SerializeField] private Transform _background;
        [Header("Leaf Slash Wind")]
        [SerializeField] private EffectBase _fallingLeaves;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_camera);
            builder.RegisterInstance(_weaponDatas);
            builder.RegisterInstance(_slashConfig);
            builder.RegisterInstance(_streakConfig);
            builder.RegisterInstance(_scoreConfig);
            builder.RegisterInstance(_positionConfig);
            builder.RegisterInstance(_slashObjectSpawnConfig);
            builder.Register<WeaponSystem>(Lifetime.Singleton);
            builder.Register<SlashManager>(Lifetime.Singleton)
                .WithParameter(transform);
            builder.RegisterEntryPoint<CameraStreakZoomService>(Lifetime.Singleton)
                .WithParameter(_cameraRig)
                .WithParameter(_streakConfig);
            builder.RegisterEntryPoint<LeafSlashWindService>(Lifetime.Singleton)
                .WithParameter(_hitEffectConfig)
                .WithParameter(_fallingLeaves);
            builder.RegisterEntryPoint<ScenePositionService>(Lifetime.Singleton)
                .WithParameter(_background)
                .WithParameter(_fallingLeaves);
            builder.Register<SlashDecalSpawner>(Lifetime.Singleton)
                .WithParameter(_hitEffectConfig)
                .WithParameter("decalProjectorPrefab", _decalProjectorPrefab)
                .WithParameter(_slashHitLayerMask)
                .WithParameter(_slashHitSampleCount)
                .WithParameter("effectsRoot", _effectsRoot)
                .WithParameter("background", _background);
            builder.Register<SlashObjectSpawnService>(Lifetime.Singleton)
                .WithParameter(transform);
            builder.Register<ObstacleObjectSpawnService>(Lifetime.Singleton)
                .WithParameter(transform);
            builder.Register<SlashGameResultService>(Lifetime.Singleton)
                .WithParameter(_scoreCanvas);
            builder.RegisterEntryPoint<ScoreSystem>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<ScoreView>(Lifetime.Singleton)
                .WithParameter(_scoreLabel);
            builder.RegisterEntryPoint<StreakView>(Lifetime.Singleton)
                .WithParameter(_streakLabel);
            builder.RegisterEntryPoint<SlashObjectService>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<WeaponController>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<InputManager>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<SettingsButton>();
            builder.RegisterEntryPoint<GamePauseService>(Lifetime.Singleton)
                .AsSelf();
        }
    }
}
