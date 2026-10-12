using System;
using System.Collections.Generic;
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
        private readonly Dictionary<SlashObject, Tween> _fragmentTweens = new();

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
            _activeObject.SetFallPosition(_activeObject.transform.position, true);
            despawnPosition.y -= GetTopOffsetFromRoot(_activeObject);

            var spawnedObject = _activeObject;
            var fallStart = spawnedObject.FallPosition;
            _fallTween = DOVirtual.Float(fallStart.y, despawnPosition.y, data.FallDuration,
                    y => spawnedObject.SetFallPosition(new Vector3(fallStart.x, y, fallStart.z)))
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

                return bounds.max.y - slashObject.FallPosition.y;
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

            return colliderBounds.max.y - slashObject.FallPosition.y;
        }

        public void Stop(SlashObject slashObject)
        {
            if (slashObject != null && _fragmentTweens.TryGetValue(slashObject, out var fragmentTween))
            {
                fragmentTween.Kill(false);
                _fragmentTweens.Remove(slashObject);
                return;
            }
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
            foreach (var entry in _fragmentTweens)
            {
                entry.Value.Kill(false);
                if (entry.Key != null) UnityEngine.Object.Destroy(entry.Key.gameObject);
            }
            _fragmentTweens.Clear();
        }

        public void TrackFragment(SlashObject source, SlashObject fragment)
        {
            var sourceTween = source == _activeObject ? _fallTween :
                (_fragmentTweens.TryGetValue(source, out var tween) ? tween : null);
            float remaining = sourceTween != null && sourceTween.IsActive()
                ? Mathf.Max(0.01f, sourceTween.Duration() - sourceTween.Elapsed()) : fragment.Data.FallDuration;
            float bottom = GetWorldPosition(_config.ViewportX, _config.DespawnViewportY).y;
            // Continue the configured fall without converting either half into a
            // dynamic body or assigning an artificial separation/fall velocity.
            var fragmentStart = fragment.FallPosition;
            var fall = DOVirtual.Float(fragmentStart.y, bottom - GetTopOffsetFromRoot(fragment), remaining,
                    y => fragment.SetFallPosition(new Vector3(fragmentStart.x, y, fragmentStart.z)))
                .SetEase(fragment.Data.FallEase).SetLink(fragment.gameObject);
            if (_config.UseUnscaledTime) fall.SetUpdate(true);
            _fragmentTweens.Add(fragment, fall);
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
