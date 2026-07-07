using System;
using GameLift.Levels;
using UnityEngine;
using VContainer.Unity;

namespace TrashRush.Game
{
    public sealed class SlashObjectService : IStartable, ILateTickable, IDisposable
    {
        private readonly Camera _camera;
        private readonly LevelService<BaseLevelData> _levelService;
        private readonly SlashObjectSpawnService _spawnService;
        private readonly ObstacleObjectSpawnService _obstacleSpawnService;
        private readonly SlashGameResultService _resultService;

        private SlashObject _activeObject;
        private LevelData _currentLevelData;
        private int _currentObjectIndex;
        private bool _disposed;
        private bool _isPlaying;
        private bool _monitorHorizontalBounds;
        private int _remainingObstacleSpawns;
        private float _obstacleSpawnTimer;

        public event Action<LevelData> LevelStarted;
        public event Action<SlashObject, int, int> SlashProgressChanged;
        public event Action<bool> LevelEnded;

        public bool IsPlaying => _isPlaying && !_disposed;
        public SlashObject ActiveObject => _activeObject;

        public SlashObjectService(
            Camera camera,
            LevelService<BaseLevelData> levelService,
            SlashObjectSpawnService spawnService,
            ObstacleObjectSpawnService obstacleSpawnService,
            SlashGameResultService resultService)
        {
            _camera = camera;
            _levelService = levelService;
            _spawnService = spawnService;
            _obstacleSpawnService = obstacleSpawnService;
            _resultService = resultService;
        }

        public void Start()
        {
            LoadLevel();
        }

        public void LateTick()
        {
            if (!IsPlaying || _activeObject == null)
            {
                return;
            }

            if (IsBelowViewport(_activeObject))
            {
                OnObjectReachedBottom(_activeObject);
                return;
            }

            UpdateObstacleSpawning();

            if (_monitorHorizontalBounds &&
                IsOutsideHorizontalViewport(_activeObject))
            {
                FailLevel();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _isPlaying = false;
            _spawnService.Dispose();
            _obstacleSpawnService.Dispose();
            _activeObject = null;
            _currentLevelData = null;
            _currentObjectIndex = 0;
            _monitorHorizontalBounds = false;
            ResetObstacleSchedule();
        }

        public bool TryRegisterSlash(SlashObject slashObject)
        {
            if (!IsPlaying ||
                slashObject == null ||
                slashObject != _activeObject ||
                !slashObject.TryRegisterSlash(out var completed))
            {
                return false;
            }

            SlashProgressChanged?.Invoke(
                slashObject,
                slashObject.CurrentSlash,
                slashObject.TotalSlash);

            if (completed)
            {
                CompleteActiveObject();
            }

            return true;
        }

        public bool TryRegisterObstacleSlash(ObstacleObject obstacle)
        {
            if (!IsPlaying ||
                _activeObject == null ||
                obstacle == null ||
                !obstacle.IsHittable)
            {
                return false;
            }

            _obstacleSpawnService.Stop(obstacle);

            if (!obstacle.TryExplode())
            {
                return false;
            }

            _monitorHorizontalBounds = true;
            return true;
        }

        private async void LoadLevel()
        {
            try
            {
                var levelData = await _levelService.LoadNextLevelData();

                if (_disposed)
                {
                    return;
                }

                if (!(levelData is LevelData gameLevelData))
                {
                    Debug.LogError(
                        "[Trash Rush] Current level data must be a LevelData asset.");
                    return;
                }

                if (gameLevelData.SlashObjects == null ||
                    gameLevelData.SlashObjects.Count == 0)
                {
                    Debug.LogError("[Trash Rush] LevelData must contain at least one SlashObjectData.");
                    return;
                }

                _currentLevelData = gameLevelData;
                _currentObjectIndex = 0;
                _remainingObstacleSpawns =
                    gameLevelData.MaxObstacleSpawnCount;
                _obstacleSpawnTimer =
                    gameLevelData.ObstacleSpawnInterval;
                _isPlaying = true;
                _levelService.OnLevelStarted();
                LevelStarted?.Invoke(gameLevelData);
                SpawnCurrentObject();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void CompleteActiveObject()
        {
            if (!IsPlaying || _activeObject == null)
            {
                return;
            }

            var completedObject = _activeObject;
            _activeObject = null;
            _spawnService.Stop(completedObject);
            completedObject.Explode();

            _currentObjectIndex++;
            SpawnCurrentObject();
        }

        private void SpawnCurrentObject()
        {
            if (!IsPlaying || _currentLevelData == null)
            {
                return;
            }

            _obstacleSpawnService.Dispose();
            _monitorHorizontalBounds = false;

            while (_currentObjectIndex < _currentLevelData.SlashObjects.Count &&
                   _currentLevelData.SlashObjects[_currentObjectIndex] == null)
            {
                Debug.LogWarning(
                    $"[Trash Rush] Skipping null SlashObjectData at index {_currentObjectIndex}.");
                _currentObjectIndex++;
            }

            if (_currentObjectIndex >= _currentLevelData.SlashObjects.Count)
            {
                CompleteLevel();
                return;
            }

            var slashObjectData = _currentLevelData.SlashObjects[_currentObjectIndex];
            _activeObject = _spawnService.Spawn(slashObjectData);

            if (_activeObject == null)
            {
                FailLevel();
                return;
            }

            SlashProgressChanged?.Invoke(_activeObject, 0, _activeObject.TotalSlash);
        }

        private void CompleteLevel()
        {
            if (!IsPlaying)
            {
                return;
            }

            _isPlaying = false;
            _currentLevelData = null;
            _obstacleSpawnService.Dispose();
            _monitorHorizontalBounds = false;
            ResetObstacleSchedule();
            LevelEnded?.Invoke(true);
            _resultService.ShowWin();
        }

        private void OnObjectReachedBottom(SlashObject escapedObject)
        {
            if (!IsPlaying || escapedObject == null || escapedObject != _activeObject)
            {
                return;
            }

            _isPlaying = false;
            _activeObject = null;
            _spawnService.Stop(escapedObject);
            _obstacleSpawnService.Dispose();
            _monitorHorizontalBounds = false;
            ResetObstacleSchedule();
            escapedObject.MarkEscaped();
            UnityEngine.Object.Destroy(escapedObject.gameObject);

            _currentLevelData = null;
            LevelEnded?.Invoke(false);
            _resultService.ShowLose();
        }

        private void FailLevel()
        {
            if (!IsPlaying)
            {
                return;
            }

            _isPlaying = false;
            _obstacleSpawnService.Dispose();
            _monitorHorizontalBounds = false;
            ResetObstacleSchedule();

            if (_activeObject != null)
            {
                var failedObject = _activeObject;
                _activeObject = null;
                _spawnService.Stop(failedObject);
                failedObject.MarkEscaped();
                UnityEngine.Object.Destroy(failedObject.gameObject);
            }

            _currentLevelData = null;
            LevelEnded?.Invoke(false);
            _resultService.ShowLose();
        }

        private bool IsOutsideHorizontalViewport(SlashObject slashObject)
        {
            if (_camera == null || slashObject == null)
            {
                return false;
            }

            var viewportPosition = _camera.WorldToViewportPoint(
                slashObject.RotationTarget.position);
            return viewportPosition.x < 0f || viewportPosition.x > 1f;
        }

        private bool IsBelowViewport(SlashObject slashObject)
        {
            if (_camera == null || slashObject == null)
            {
                return false;
            }

            var renderers =
                slashObject.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;

                for (var i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                return _camera.WorldToViewportPoint(
                    new Vector3(
                        bounds.center.x,
                        bounds.max.y,
                        bounds.center.z)).y < 0f;
            }

            return _camera.WorldToViewportPoint(
                slashObject.RotationTarget.position).y < 0f;
        }

        private void UpdateObstacleSpawning()
        {
            if (_remainingObstacleSpawns <= 0 ||
                _currentLevelData == null ||
                _activeObject == null ||
                _currentLevelData.ObstacleObjects == null ||
                _currentLevelData.ObstacleObjects.Count == 0)
            {
                return;
            }

            _obstacleSpawnTimer -= Time.deltaTime;
            var interval = _currentLevelData.ObstacleSpawnInterval;

            while (_obstacleSpawnTimer <= 0f &&
                   _remainingObstacleSpawns > 0)
            {
                var spawnCount = Mathf.Min(
                    _currentLevelData.ObstaclesPerSpawn,
                    _remainingObstacleSpawns);

                for (var i = 0; i < spawnCount; i++)
                {
                    if (!_obstacleSpawnService.TrySpawn(
                            _currentLevelData.ObstacleObjects,
                            _activeObject))
                    {
                        _remainingObstacleSpawns = 0;
                        return;
                    }

                    _remainingObstacleSpawns--;
                }

                _obstacleSpawnTimer += interval;
            }
        }

        private void ResetObstacleSchedule()
        {
            _remainingObstacleSpawns = 0;
            _obstacleSpawnTimer = 0f;
        }
    }
}
