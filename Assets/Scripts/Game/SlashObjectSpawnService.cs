using System;
using DG.Tweening;
using UnityEngine;

namespace TrashRush.Game
{
    public sealed class SlashObjectSpawnService : IDisposable
    {
        private readonly Camera _camera;
        private readonly SlashObjectSpawnConfig _config;
        private readonly PositionConfig _positionConfig;
        private readonly Transform _parent;

        private SlashObject _activeObject;
        private Tween _fallTween;

        public SlashObjectSpawnService(
            Camera camera,
            SlashObjectSpawnConfig config,
            PositionConfig positionConfig,
            Transform parent)
        {
            _camera = camera;
            _config = config;
            _positionConfig = positionConfig;
            _parent = parent;
        }

        public SlashObject Spawn(SlashObjectData data)
        {
            if (_camera == null ||
                _config == null ||
                _positionConfig == null ||
                data == null ||
                data.Prefab == null)
            {
                Debug.LogError("[Trash Rush] SlashObject cannot spawn because its setup is incomplete.");
                return null;
            }

            DisposeActiveObject();

            _activeObject = UnityEngine.Object.Instantiate(data.Prefab, _parent);
            _activeObject.Initialize(data);

            var spawnPosition = GetWorldPosition(_config.ViewportX, _config.SpawnViewportY);
            var despawnPosition = GetWorldPosition(_config.ViewportX, _config.DespawnViewportY);
            var rootToRotationCenter =
                _activeObject.RotationTarget.position - _activeObject.transform.position;

            _activeObject.transform.position = spawnPosition - rootToRotationCenter;
            despawnPosition.y -= GetTopOffsetFromRoot(_activeObject);

            var spawnedObject = _activeObject;
            _fallTween = spawnedObject.transform
                .DOMoveY(despawnPosition.y, data.FallDuration)
                .SetEase(data.FallEase)
                .SetLink(spawnedObject.gameObject)
                .OnComplete(() =>
                {
                    _fallTween = null;
                });

            if (_config.UseUnscaledTime)
            {
                _fallTween.SetUpdate(true);
            }

            return spawnedObject;
        }

        private static float GetTopOffsetFromRoot(SlashObject slashObject)
        {
            if (slashObject == null)
            {
                return 0f;
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

                return bounds.max.y - slashObject.transform.position.y;
            }

            var colliders =
                slashObject.GetComponentsInChildren<Collider>(true);

            if (colliders.Length == 0)
            {
                return 0f;
            }

            var colliderBounds = colliders[0].bounds;

            for (var i = 1; i < colliders.Length; i++)
            {
                colliderBounds.Encapsulate(colliders[i].bounds);
            }

            return colliderBounds.max.y - slashObject.transform.position.y;
        }

        public void Stop(SlashObject slashObject)
        {
            if (slashObject == null || slashObject != _activeObject)
            {
                return;
            }

            KillFallTween();
            _activeObject = null;
        }

        public void Dispose()
        {
            DisposeActiveObject();
        }

        private Vector3 GetWorldPosition(float viewportX, float viewportY)
        {
            var distanceFromCamera = Mathf.Abs(_positionConfig.WoodZ - _camera.transform.position.z);
            var position = _camera.ViewportToWorldPoint(
                new Vector3(viewportX, viewportY, distanceFromCamera));
            position.z = _positionConfig.WoodZ;
            return position;
        }

        private void DisposeActiveObject()
        {
            KillFallTween();

            if (_activeObject != null)
            {
                UnityEngine.Object.Destroy(_activeObject.gameObject);
                _activeObject = null;
            }
        }

        private void KillFallTween()
        {
            if (_fallTween != null && _fallTween.IsActive())
            {
                _fallTween.Kill(false);
            }

            _fallTween = null;
        }
    }
}
