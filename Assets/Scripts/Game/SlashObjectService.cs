using System;
using System.Collections.Generic;
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
        private readonly List<SlashObject> _activePieces = new();
        private bool _registeringBatch;
        private LevelData _currentLevelData;
        private int _currentObjectIndex;
        private bool _disposed;
        private bool _isPlaying;
        private GameObject _sideWalls;
        private BoxCollider _leftWall;
        private BoxCollider _rightWall;
        private PhysicsMaterial _sideWallMaterial;
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
            CreateSideWalls();
            LoadLevel();
        }

        public void LateTick()
        {
            UpdateSideWalls();
            if (!IsPlaying || _activeObject == null)
            {
                return;
            }

            foreach (var piece in _activePieces)
                if (piece != null && IsBelowViewport(piece))
                {
                    OnObjectReachedBottom(piece);
                    return;
                }

            UpdateObstacleSpawning();

        }

        public void Dispose()
        {
            _disposed = true;
            _isPlaying = false;
            _spawnService.Dispose();
            ClearActivePieces();
            _obstacleSpawnService.Dispose();
            _activeObject = null;
            _currentLevelData = null;
            _currentObjectIndex = 0;
            ResetObstacleSchedule();
            if (_sideWalls != null) UnityEngine.Object.Destroy(_sideWalls);
            if (_sideWallMaterial != null) UnityEngine.Object.Destroy(_sideWallMaterial);
        }

        private void CreateSideWalls()
        {
            if (_camera == null || _sideWalls != null) return;
            _sideWalls = new GameObject("GameplaySideWalls");
            _sideWalls.transform.SetParent(_camera.transform, false);
            _sideWallMaterial = new PhysicsMaterial("GameplaySideWallBounce")
            {
                bounciness = 0.65f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                staticFriction = 0f,
                dynamicFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            _leftWall = CreateSideWall("LeftBoundary");
            _rightWall = CreateSideWall("RightBoundary");
            UpdateSideWalls();
        }

        private BoxCollider CreateSideWall(string name)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(_sideWalls.transform, false);
            var collider = wall.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.sharedMaterial = _sideWallMaterial;
            return collider;
        }

        private void UpdateSideWalls()
        {
            if (_camera == null || _leftWall == null || _rightWall == null) return;
            float depth = _camera.farClipPlane - _camera.nearClipPlane;
            float midpoint = _camera.nearClipPlane + depth * 0.5f;
            var left = _camera.transform.InverseTransformPoint(
                _camera.ViewportToWorldPoint(new Vector3(0f, 0.5f, midpoint)));
            var right = _camera.transform.InverseTransformPoint(
                _camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, midpoint)));
            float height = Vector3.Distance(
                _camera.ViewportToWorldPoint(new Vector3(0.5f, 0f, midpoint)),
                _camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, midpoint)));
            const float thickness = 1f;
            // Inner faces meet the viewport edges. Extend above/below the screen
            // and through its clipping range, without adding a floor or ceiling.
            _leftWall.transform.localPosition = left - Vector3.right * (thickness * 0.5f);
            _rightWall.transform.localPosition = right + Vector3.right * (thickness * 0.5f);
            _leftWall.size = _rightWall.size = new Vector3(thickness, height * 3f, depth);
        }

        public bool TryRegisterSlash(
            SlashObject slashObject,
            Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            Vector2? screenSlashDirection = null)
        {
            if (!IsPlaying ||
                slashObject == null ||
                !_activePieces.Contains(slashObject) ||
                !slashObject.TryRegisterSlash(out var completed))
            {
                return false;
            }

            if (!completed) slashObject.ApplyHitRotation(_camera, startWorldPosition, endWorldPosition);

            SlashProgressChanged?.Invoke(
                slashObject,
                slashObject.ReleasedBricks,
                slashObject.TotalBricks);

            if (completed)
            {
                // Reuse the input delta; project the existing endpoints for non-input callers.
                var screenDirection = screenSlashDirection ?? (_camera != null
                    ? (Vector2)(_camera.WorldToScreenPoint(endWorldPosition) -
                                _camera.WorldToScreenPoint(startWorldPosition))
                    : Vector2.zero);
                CompleteObject(slashObject, screenDirection);
            }

            return true;
        }

        public void TryRegisterSlashes(List<SlashObject> objects, Vector3 start, Vector3 end,
            Vector2? screenDirection = null)
        {
            _registeringBatch = true;
            try
            {
                foreach (var piece in objects) TryRegisterSlash(piece, start, end, screenDirection);
            }
            finally { _registeringBatch = false; }
            AdvanceIfCompleted();
        }

        private void OnSplitCreated(SlashObject source, SlashObject fragment)
        {
            _activePieces.Add(fragment);
            fragment.SplitCreated += OnSplitCreated;
            _spawnService.TrackFragment(source, fragment);
        }

        private void ClearActivePieces()
        {
            foreach (var piece in _activePieces)
            {
                if (piece == null) continue;
                piece.SplitCreated -= OnSplitCreated;
                piece.MarkEscaped();
                _spawnService.Stop(piece);
                UnityEngine.Object.Destroy(piece.gameObject);
            }
            _activePieces.Clear();
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

        private void CompleteObject(SlashObject completedObject, Vector2 screenSlashDirection)
        {
            if (!IsPlaying || _activeObject == null)
            {
                return;
            }

            _activePieces.Remove(completedObject);
            completedObject.SplitCreated -= OnSplitCreated;
            _activeObject = _activePieces.Count > 0 ? _activePieces[0] : null;
            _spawnService.Stop(completedObject);
            completedObject.Explode(_camera, screenSlashDirection);

            if (!_registeringBatch) AdvanceIfCompleted();
        }

        private void AdvanceIfCompleted()
        {
            if (!IsPlaying || _activePieces.Count > 0) return;
            _currentObjectIndex++;
            SpawnCurrentObject();
        }

        private void SpawnCurrentObject()
        {
            if (!IsPlaying || _currentLevelData == null)
            {
                return;
            }

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

            _activePieces.Add(_activeObject);
            _activeObject.SplitCreated += OnSplitCreated;

            SlashProgressChanged?.Invoke(_activeObject, 0, _activeObject.TotalBricks);
        }

        private void CompleteLevel()
        {
            if (!IsPlaying)
            {
                return;
            }

            _isPlaying = false;
            _currentLevelData = null;
            ResetObstacleSchedule();
            LevelEnded?.Invoke(true);
            _resultService.ShowWin();
        }

        private void OnObjectReachedBottom(SlashObject escapedObject)
        {
            if (!IsPlaying || escapedObject == null || !_activePieces.Contains(escapedObject))
            {
                return;
            }

            _isPlaying = false;
            _activeObject = null;
            _spawnService.Stop(escapedObject);
            _obstacleSpawnService.Dispose();
            ResetObstacleSchedule();
            escapedObject.MarkEscaped();
            UnityEngine.Object.Destroy(escapedObject.gameObject);
            ClearActivePieces();

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
            ResetObstacleSchedule();

            if (_activeObject != null)
            {
                var failedObject = _activeObject;
                _activeObject = null;
                _spawnService.Stop(failedObject);
                failedObject.MarkEscaped();
                UnityEngine.Object.Destroy(failedObject.gameObject);
            }
            ClearActivePieces();

            _currentLevelData = null;
            LevelEnded?.Invoke(false);
            _resultService.ShowLose();
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
