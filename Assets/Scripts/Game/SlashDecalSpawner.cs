using System;
using System.Collections.Generic;
using DG.Tweening;
using GameLift.Pooling;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TrashRush.Game
{
    public sealed class SlashDecalSpawner
        : IDisposable
    {
        private const int OcclusionRefinementIterations = 8;
        private const string SlashParentName = "SlashParent";
        private const string DecalProjectorPointName = "DecalProjectorPoint";
        private const string MeshObjectName = "Mesh";
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int SlashTextureId = Shader.PropertyToID("Base_Map");

        private readonly Camera _camera;
        private readonly IPools _pools;
        private readonly WeaponSystem _weaponSystem;
        private readonly SlashConfig _config;
        private readonly HitEffectConfig _hitEffectConfig;
        private readonly StreakConfig _streakConfig;
        private readonly PositionConfig _positionConfig;
        private readonly Transform _effectsRoot;
        private readonly Transform _background;
        private readonly GameObject _decalProjectorPrefab;
        private readonly LayerMask _hitLayerMask;
        private readonly int _hitSampleCount;
        private readonly Dictionary<Texture2D, Material> _decalMaterials = new();
        private readonly List<SpriteRenderer> _activeBackgroundSlashSprites = new();

        private Tween _screenShakeTween;
        private Vector3 _cameraStartLocalPosition;
        private int _backgroundSlashBaseSortingOrder;

        public SlashDecalSpawner(
            Camera camera,
            IPools pools,
            WeaponSystem weaponSystem,
            SlashConfig config,
            HitEffectConfig hitEffectConfig,
            StreakConfig streakConfig,
            PositionConfig positionConfig,
            Transform effectsRoot,
            Transform background,
            GameObject decalProjectorPrefab,
            LayerMask hitLayerMask,
            int hitSampleCount)
        {
            _camera = camera;
            _pools = pools;
            _weaponSystem = weaponSystem;
            _config = config;
            _hitEffectConfig = hitEffectConfig;
            _streakConfig = streakConfig;
            _positionConfig = positionConfig;
            _effectsRoot = effectsRoot;
            _background = background;
            _decalProjectorPrefab = decalProjectorPrefab;
            _hitLayerMask = hitLayerMask;
            _hitSampleCount = Mathf.Max(2, hitSampleCount);
            _cameraStartLocalPosition = _camera != null
                ? _camera.transform.localPosition
                : Vector3.zero;

            InitializeWeaponPools();
        }

        private void InitializeWeaponPools()
        {
            if (_pools == null || _weaponSystem == null)
            {
                return;
            }

            var weapons = _weaponSystem.Weapons;

            for (var i = 0; i < weapons.Count; i++)
            {
                var weapon = weapons[i];

                if (weapon == null)
                {
                    continue;
                }

                if (weapon.BackgroundSlashPrefab != null)
                {
                    _pools.InitializePool(
                        weapon.BackgroundSlashPrefab.gameObject,
                        0);
                }

                if (weapon.SlashStreakEffect != null)
                {
                    _pools.InitializePool(
                        weapon.SlashStreakEffect.gameObject,
                        _streakConfig != null
                            ? _streakConfig.SlashStreakEffectPoolPreload
                            : 0);
                }

            }
        }

        public bool TrySpawn(Vector3 startWorldPosition, Vector3 endWorldPosition)
        {
            return TrySpawn(
                startWorldPosition,
                endWorldPosition,
                1,
                out _);
        }

        public bool TrySpawn(
            Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            out SlashObject slashObject)
        {
            return TrySpawn(
                startWorldPosition,
                endWorldPosition,
                1,
                out slashObject);
        }

        public bool TrySpawn(
            Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            int streak,
            out SlashObject slashObject)
        {
            slashObject = null;
            var backgroundSlashPrefab =
                _weaponSystem?.CurrentWeapon?.BackgroundSlashPrefab;

            if (_camera == null ||
                (_decalProjectorPrefab == null &&
                 backgroundSlashPrefab == null))
            {
                return false;
            }

            var delta = endWorldPosition - startWorldPosition;
            delta.z = 0f;

            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            if (!TryFindHit(
                    startWorldPosition,
                    endWorldPosition,
                    out var decalPoint,
                    out var followParent,
                    out var hitRoot,
                    out slashObject))
            {
                return false;
            }

            var height = GetProjectorHeight(delta.magnitude);

            if (slashObject == null || !slashObject.IsSlashable)
            {
                return false;
            }

            TrySpawnBackgroundPrefabs(
                startWorldPosition,
                endWorldPosition,
                delta,
                slashObject,
                streak);

            Spawn(
                _decalProjectorPrefab,
                decalPoint,
                followParent,
                GetSpawnPosition(decalPoint, startWorldPosition, endWorldPosition),
                GetSlashZRotation(delta),
                height,
                slashObject.Data.SlashTexture);

            SpawnSlashStreakEffect(
                GetSpawnPosition(decalPoint, startWorldPosition, endWorldPosition),
                GetSlashZRotation(delta),
                streak);

            SpawnSlashObjectHitEffect(
                startWorldPosition,
                endWorldPosition,
                slashObject);

            PlayHitEffect(slashObject.RotationTarget, hitRoot, delta, height, streak);
            PlayScreenShake(streak);
            return true;
        }

        public void Dispose()
        {
            StopScreenShake();

            foreach (var material in _decalMaterials.Values)
            {
                if (material != null)
                {
                    UnityEngine.Object.Destroy(material);
                }
            }

            _decalMaterials.Clear();
            _activeBackgroundSlashSprites.Clear();
        }

        private bool TryFindHit(
            Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            out Transform decalPoint,
            out Transform followParent,
            out Transform hitRoot,
            out SlashObject slashObject)
        {
            var startScreenPosition = _camera.WorldToScreenPoint(startWorldPosition);
            var endScreenPosition = _camera.WorldToScreenPoint(endWorldPosition);
            Transform slashDecalPoint = null;
            Transform slashFollowParent = null;
            Transform slashHitRoot = null;
            SlashObject slashCandidate = null;

            for (var i = 0; i < _hitSampleCount; i++)
            {
                var t = i / (_hitSampleCount - 1f);
                var screenPosition = Vector3.Lerp(startScreenPosition, endScreenPosition, t);
                var ray = _camera.ScreenPointToRay(screenPosition);
                var hits = Physics.RaycastAll(ray, _camera.farClipPlane, _hitLayerMask, QueryTriggerInteraction.Ignore);

                if (TryGetHit(
                        hits,
                        out decalPoint,
                        out followParent,
                        out hitRoot,
                        out slashObject))
                {
                    if (slashCandidate == null && slashObject != null)
                    {
                        slashDecalPoint = decalPoint;
                        slashFollowParent = followParent;
                        slashHitRoot = hitRoot;
                        slashCandidate = slashObject;
                    }
                }
            }

            if (slashCandidate != null)
            {
                decalPoint = slashDecalPoint;
                followParent = slashFollowParent;
                hitRoot = slashHitRoot;
                slashObject = slashCandidate;
                return true;
            }

            decalPoint = null;
            followParent = null;
            hitRoot = null;
            slashObject = null;
            return false;
        }

        private static bool TryGetHit(
            RaycastHit[] hits,
            out Transform decalPoint,
            out Transform followParent,
            out Transform hitRoot,
            out SlashObject slashObject)
        {
            if (hits == null || hits.Length == 0)
            {
                decalPoint = null;
                followParent = null;
                hitRoot = null;
                slashObject = null;
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            Transform slashDecalPoint = null;
            Transform slashFollowParent = null;
            Transform slashHitRoot = null;
            SlashObject slashCandidate = null;

            for (var i = 0; i < hits.Length; i++)
            {
                if (TryFindDecalPoint(
                        hits[i].collider.transform,
                        out decalPoint,
                        out followParent,
                        out hitRoot,
                        out slashObject))
                {
                    if (slashCandidate == null && slashObject != null)
                    {
                        slashDecalPoint = decalPoint;
                        slashFollowParent = followParent;
                        slashHitRoot = hitRoot;
                        slashCandidate = slashObject;
                    }
                }
            }

            if (slashCandidate != null)
            {
                decalPoint = slashDecalPoint;
                followParent = slashFollowParent;
                hitRoot = slashHitRoot;
                slashObject = slashCandidate;
                return true;
            }

            decalPoint = null;
            followParent = null;
            hitRoot = null;
            slashObject = null;
            return false;
        }

        private static bool TryFindDecalPoint(
            Transform hitTransform,
            out Transform decalPoint,
            out Transform followParent,
            out Transform hitRoot,
            out SlashObject slashObject)
        {
            for (var current = hitTransform; current != null; current = current.parent)
            {
                if (current.GetComponent<ObstacleObject>() != null)
                {
                    continue;
                }

                decalPoint = current.Find(DecalProjectorPointName);

                if (decalPoint != null)
                {
                    hitRoot = current;
                    slashObject = current.GetComponent<SlashObject>();

                    if (slashObject == null)
                    {
                        continue;
                    }

                    followParent = slashObject != null
                        ? slashObject.DecalFollowTarget
                        : FindFollowParent(current);
                    return true;
                }
            }

            decalPoint = null;
            followParent = null;
            hitRoot = null;
            slashObject = null;
            return false;
        }

        private static Transform FindFollowParent(Transform hitRoot)
        {
            var mesh = hitRoot.Find(MeshObjectName);
            return mesh != null ? mesh : hitRoot;
        }

        private bool TrySpawnBackgroundPrefabs(
            Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            Vector3 slashDelta,
            SlashObject slashObject,
            int streak)
        {
            var weapon = _weaponSystem?.CurrentWeapon;
            var backgroundSlashPrefab = weapon?.BackgroundSlashPrefab;

            if (backgroundSlashPrefab == null ||
                _pools == null ||
                slashObject == null ||
                (_config != null && !_config.BackgroundSlashEnabled))
            {
                return false;
            }

            if (!TryFindBackgroundPoint(
                    startWorldPosition,
                    endWorldPosition,
                    out var backgroundPoint,
                    out var backgroundRoot))
            {
                return false;
            }

            var center = GetSpawnPosition(
                backgroundPoint,
                startWorldPosition,
                endWorldPosition);
            var projectedStart = ProjectToPlane(backgroundPoint, startWorldPosition);
            var projectedEnd = ProjectToPlane(backgroundPoint, endWorldPosition);
            var direction = (projectedEnd - projectedStart).normalized;
            var scaleMultiplier = _config != null
                ? _config.BackgroundSlashScaleMultiplier
                : Vector2.one;
            var baseHeight = GetBackgroundSlashHeight(slashDelta.magnitude);
            var visualHeight = baseHeight * scaleMultiplier.y;

            if (direction.sqrMagnitude <= Mathf.Epsilon || visualHeight <= Mathf.Epsilon)
            {
                return false;
            }

            var visualStart = center - direction * (visualHeight * 0.5f);
            var visualEnd = center + direction * (visualHeight * 0.5f);

            if (!TryFindSlashOcclusion(
                    visualStart,
                    visualEnd,
                    slashObject,
                    out var entryT,
                    out var exitT))
            {
                entryT = 0.5f;
                exitT = 0.5f;
            }
            else if (entryT <= Mathf.Epsilon && exitT >= 1f - Mathf.Epsilon)
            {
                entryT = 0.5f;
                exitT = 0.5f;
            }

            var zRotation = GetSlashZRotation(slashDelta);
            var playBackgroundEffect =
                _streakConfig != null &&
                _streakConfig.ShouldPlaySlashStreakBackground(streak);
            var spawnedBefore = TrySpawnBackgroundSegment(
                backgroundSlashPrefab,
                backgroundPoint,
                backgroundRoot,
                visualStart,
                Vector3.Lerp(visualStart, visualEnd, entryT),
                zRotation,
                scaleMultiplier.y,
                true,
                playBackgroundEffect);
            var spawnedAfter = TrySpawnBackgroundSegment(
                backgroundSlashPrefab,
                backgroundPoint,
                backgroundRoot,
                Vector3.Lerp(visualStart, visualEnd, exitT),
                visualEnd,
                zRotation,
                scaleMultiplier.y,
                false,
                playBackgroundEffect);

            return spawnedBefore || spawnedAfter;
        }

        private bool TrySpawnBackgroundSegment(
            SpriteRenderer prefab,
            Transform backgroundPoint,
            Transform backgroundRoot,
            Vector3 segmentStart,
            Vector3 segmentEnd,
            float zRotation,
            float heightScale,
            bool flipY,
            bool playBackgroundEffect)
        {
            var renderedHeight = Vector3.Distance(segmentStart, segmentEnd);

            if (prefab == null || renderedHeight <= Mathf.Epsilon)
            {
                return false;
            }

            var baseHeight = heightScale > Mathf.Epsilon
                ? renderedHeight / heightScale
                : renderedHeight;

            return SpawnBackgroundPrefab(
                prefab,
                backgroundPoint,
                backgroundRoot,
                Vector3.Lerp(segmentStart, segmentEnd, 0.5f),
                zRotation,
                baseHeight,
                flipY,
                playBackgroundEffect);
        }

        private bool SpawnBackgroundPrefab(
            SpriteRenderer prefab,
            Transform backgroundPoint,
            Transform backgroundRoot,
            Vector3 worldPosition,
            float zRotation,
            float baseHeight,
            bool flipY,
            bool playBackgroundEffect)
        {
            var spriteObject = _pools.Spawn(prefab.gameObject, backgroundPoint);

            if (spriteObject == null)
            {
                return false;
            }

            var spriteTransform = spriteObject.transform;
            var spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                _pools.Despawn(spriteObject);
                return false;
            }

            var backgroundRenderer = backgroundRoot != null
                ? backgroundRoot.GetComponent<SpriteRenderer>()
                : null;

            spriteRenderer.color = _config != null
                ? _config.BackgroundSlashColor
                : Color.white;
            spriteRenderer.flipY = false;

            if (backgroundRenderer != null)
            {
                spriteRenderer.sortingLayerID = backgroundRenderer.sortingLayerID;
            }

            _backgroundSlashBaseSortingOrder =
                (backgroundRenderer != null ? backgroundRenderer.sortingOrder : 0) +
                (_config != null ? _config.BackgroundSlashSortingOrderOffset : 1);
            RemoveMissingBackgroundSlashSprites();
            RefreshBackgroundSlashSortingOrders();
            spriteRenderer.sortingOrder =
                _backgroundSlashBaseSortingOrder + _activeBackgroundSlashSprites.Count;
            _activeBackgroundSlashSprites.Add(spriteRenderer);

            spriteTransform.SetPositionAndRotation(
                worldPosition,
                backgroundPoint.rotation *
                Quaternion.AngleAxis(
                    zRotation + (flipY ? 180f : 0f),
                    Vector3.forward));

            var spriteHeight = spriteRenderer.sprite.bounds.size.y;
            var uniformScale = spriteHeight > Mathf.Epsilon
                ? baseHeight / spriteHeight
                : 1f;
            var scaleMultiplier = _config != null
                ? _config.BackgroundSlashScaleMultiplier
                : Vector2.one;
            spriteTransform.localScale = new Vector3(
                uniformScale * scaleMultiplier.x,
                uniformScale * scaleMultiplier.y,
                1f);

            var backgroundEffect =
                spriteObject.GetComponentInChildren<EffectBase>(true);

            if (backgroundEffect != null)
            {
                backgroundEffect.gameObject.SetActive(true);
                backgroundEffect.Stop(true);

                if (playBackgroundEffect)
                {
                    backgroundEffect.Play();
                }
            }

            PlayFadeAndDespawn(spriteObject, spriteRenderer);
            return true;
        }

        private bool TryFindBackgroundPoint(
            Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            out Transform backgroundPoint,
            out Transform backgroundRoot)
        {
            if (_background != null)
            {
                backgroundPoint = _background.Find(SlashParentName);

                if (backgroundPoint != null)
                {
                    backgroundRoot = _background;
                    return true;
                }
            }

            var startScreenPosition = _camera.WorldToScreenPoint(startWorldPosition);
            var endScreenPosition = _camera.WorldToScreenPoint(endWorldPosition);

            if (TryFindBackgroundPointOnRay(
                    _camera.ScreenPointToRay(
                        Vector3.Lerp(startScreenPosition, endScreenPosition, 0.5f)),
                    out backgroundPoint,
                    out backgroundRoot))
            {
                return true;
            }

            for (var i = 0; i < _hitSampleCount; i++)
            {
                var t = i / (_hitSampleCount - 1f);
                var screenPosition = Vector3.Lerp(startScreenPosition, endScreenPosition, t);

                if (TryFindBackgroundPointOnRay(
                        _camera.ScreenPointToRay(screenPosition),
                        out backgroundPoint,
                        out backgroundRoot))
                {
                    return true;
                }
            }

            backgroundPoint = null;
            backgroundRoot = null;
            return false;
        }

        private bool TryFindBackgroundPointOnRay(
            Ray ray,
            out Transform backgroundPoint,
            out Transform backgroundRoot)
        {
            var hits = Physics.RaycastAll(
                ray,
                _camera.farClipPlane,
                _hitLayerMask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hits.Length; i++)
            {
                for (var current = hits[i].collider.transform;
                     current != null;
                     current = current.parent)
                {
                    backgroundPoint = current.Find(SlashParentName);

                    if (backgroundPoint != null)
                    {
                        backgroundRoot = current;
                        return true;
                    }
                }
            }

            backgroundPoint = null;
            backgroundRoot = null;
            return false;
        }

        private bool TryFindSlashOcclusion(
            Vector3 lineStart,
            Vector3 lineEnd,
            SlashObject slashObject,
            out float entryT,
            out float exitT)
        {
            var firstHitIndex = -1;
            var lastHitIndex = -1;

            for (var i = 0; i < _hitSampleCount; i++)
            {
                var t = i / (_hitSampleCount - 1f);

                if (!IsOverSlashObject(Vector3.Lerp(lineStart, lineEnd, t), slashObject))
                {
                    continue;
                }

                if (firstHitIndex < 0)
                {
                    firstHitIndex = i;
                }

                lastHitIndex = i;
            }

            if (firstHitIndex < 0)
            {
                entryT = 0f;
                exitT = 0f;
                return false;
            }

            var firstHitT = firstHitIndex / (_hitSampleCount - 1f);
            var lastHitT = lastHitIndex / (_hitSampleCount - 1f);

            entryT = firstHitIndex == 0
                ? 0f
                : RefineOcclusionEntry(
                    lineStart,
                    lineEnd,
                    slashObject,
                    (firstHitIndex - 1) / (_hitSampleCount - 1f),
                    firstHitT);
            exitT = lastHitIndex == _hitSampleCount - 1
                ? 1f
                : RefineOcclusionExit(
                    lineStart,
                    lineEnd,
                    slashObject,
                    lastHitT,
                    (lastHitIndex + 1) / (_hitSampleCount - 1f));

            return exitT > entryT;
        }

        private float RefineOcclusionEntry(
            Vector3 lineStart,
            Vector3 lineEnd,
            SlashObject slashObject,
            float missT,
            float hitT)
        {
            for (var i = 0; i < OcclusionRefinementIterations; i++)
            {
                var midpointT = (missT + hitT) * 0.5f;

                if (IsOverSlashObject(
                        Vector3.Lerp(lineStart, lineEnd, midpointT),
                        slashObject))
                {
                    hitT = midpointT;
                }
                else
                {
                    missT = midpointT;
                }
            }

            return hitT;
        }

        private float RefineOcclusionExit(
            Vector3 lineStart,
            Vector3 lineEnd,
            SlashObject slashObject,
            float hitT,
            float missT)
        {
            for (var i = 0; i < OcclusionRefinementIterations; i++)
            {
                var midpointT = (hitT + missT) * 0.5f;

                if (IsOverSlashObject(
                        Vector3.Lerp(lineStart, lineEnd, midpointT),
                        slashObject))
                {
                    hitT = midpointT;
                }
                else
                {
                    missT = midpointT;
                }
            }

            return hitT;
        }

        private bool IsOverSlashObject(Vector3 worldPosition, SlashObject slashObject)
        {
            var ray = _camera.ScreenPointToRay(_camera.WorldToScreenPoint(worldPosition));
            var hits = Physics.RaycastAll(
                ray,
                _camera.farClipPlane,
                _hitLayerMask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.GetComponentInParent<SlashObject>() == slashObject)
                {
                    return true;
                }
            }

            return false;
        }

        private void SpawnSlashObjectHitEffect(
            Vector3 lineStart,
            Vector3 lineEnd,
            SlashObject slashObject)
        {
            var effectPrefab = slashObject?.Data?.HitEffect;

            if (_pools == null ||
                effectPrefab == null ||
                !TryGetSlashHitMidpoint(
                    lineStart,
                    lineEnd,
                    slashObject,
                    out var hitPoint))
            {
                return;
            }

            var effect = _pools.Spawn(effectPrefab, _effectsRoot);

            if (effect == null)
            {
                return;
            }

            effect.Stop(true);
            effect.transform.SetPositionAndRotation(
                hitPoint,
                effectPrefab.transform.rotation);
            effect.transform.localScale = effectPrefab.transform.localScale;
            effect.Play();

            _pools.Despawn(effect, Mathf.Max(0.01f, effect.TotalLifetime));
        }

        private bool TryGetSlashHitMidpoint(
            Vector3 lineStart,
            Vector3 lineEnd,
            SlashObject slashObject,
            out Vector3 hitPoint)
        {
            if (!TryFindSlashOcclusion(
                    lineStart,
                    lineEnd,
                    slashObject,
                    out var entryT,
                    out var exitT))
            {
                hitPoint = default;
                return false;
            }

            var midpoint = Vector3.Lerp(
                lineStart,
                lineEnd,
                (entryT + exitT) * 0.5f);
            var ray = _camera.ScreenPointToRay(
                _camera.WorldToScreenPoint(midpoint));
            var hits = Physics.RaycastAll(
                ray,
                _camera.farClipPlane,
                _hitLayerMask,
                QueryTriggerInteraction.Ignore);
            var closestDistance = float.PositiveInfinity;
            var found = false;
            hitPoint = default;

            for (var i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.GetComponentInParent<SlashObject>() != slashObject ||
                    hits[i].distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hits[i].distance;
                hitPoint = hits[i].point;
                found = true;
            }

            return found;
        }

        private void Spawn(
            GameObject decalProjectorPrefab,
            Transform decalPoint,
            Transform followParent,
            Vector3 worldPosition,
            float zRotation,
            float height,
            Texture2D slashTexture)
        {
            if (decalProjectorPrefab == null)
            {
                return;
            }

            var decal = UnityEngine.Object.Instantiate(decalProjectorPrefab);
            var decalTransform = decal.transform;
            var baseLocalRotation = decalTransform.localRotation;
            var decalProjector = decal.GetComponent<DecalProjector>();

            decalTransform.SetPositionAndRotation(
                worldPosition,
                decalPoint.rotation * baseLocalRotation * Quaternion.AngleAxis(zRotation, Vector3.forward));

            decalTransform.SetParent(followParent != null ? followParent : decalPoint, true);

            ApplySlashTexture(decalProjector, slashTexture);
            ApplyHeight(decalProjector, height);
            PlayFadeAndDestroy(decal, decalProjector);
        }

        private void ApplySlashTexture(DecalProjector decalProjector, Texture2D slashTexture)
        {
            if (decalProjector == null || slashTexture == null || decalProjector.material == null)
            {
                return;
            }

            if (!_decalMaterials.TryGetValue(slashTexture, out var material) || material == null)
            {
                material = new Material(decalProjector.material)
                {
                    name = $"{decalProjector.material.name}_{slashTexture.name}"
                };

                SetTexture(material, SlashTextureId, slashTexture);
                SetTexture(material, BaseMapId, slashTexture);
                SetTexture(material, MainTextureId, slashTexture);
                _decalMaterials[slashTexture] = material;
            }

            decalProjector.material = material;
        }

        private static void SetTexture(Material material, int propertyId, Texture texture)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetTexture(propertyId, texture);
            }
        }

        private static void ApplyHeight(DecalProjector decalProjector, float height)
        {
            if (decalProjector == null)
            {
                return;
            }

            var size = decalProjector.size;
            size.y = height;
            decalProjector.size = size;
        }

        private void PlayFadeAndDestroy(GameObject decal, DecalProjector decalProjector)
        {
            var lifetime = _config != null ? Mathf.Max(0f, _config.DecalProjectorLifetime) : 0f;
            var fadeTime = _config != null ? Mathf.Max(0f, _config.DecalProjectorFadeTime) : 0f;

            if (decalProjector == null)
            {
                UnityEngine.Object.Destroy(decal, lifetime + fadeTime);
                return;
            }

            var sequence = DOTween.Sequence()
                .SetLink(decal)
                .AppendInterval(lifetime);

            if (fadeTime <= 0f)
            {
                sequence.AppendCallback(() => decalProjector.fadeFactor = 0f);
            }
            else
            {
                sequence.Append(DOVirtual
                    .Float(decalProjector.fadeFactor, 0f, fadeTime, value => decalProjector.fadeFactor = value)
                    .SetEase(Ease.Linear));
            }

            sequence.AppendCallback(() => UnityEngine.Object.Destroy(decal));

            if (_config != null && _config.UseUnscaledTime)
            {
                sequence.SetUpdate(true);
            }
        }

        private void PlayFadeAndDespawn(
            GameObject spriteObject,
            SpriteRenderer spriteRenderer)
        {
            var lifetime = _config != null ? _config.BackgroundSlashLifetime : 0f;
            var fadeTime = _config != null ? _config.BackgroundSlashFadeTime : 0f;
            var sequence = DOTween.Sequence()
                .SetLink(spriteObject)
                .AppendInterval(lifetime);

            if (fadeTime <= 0f)
            {
                sequence.AppendCallback(() =>
                {
                    var color = spriteRenderer.color;
                    color.a = 0f;
                    spriteRenderer.color = color;
                });
            }
            else
            {
                sequence.Append(spriteRenderer.DOFade(0f, fadeTime).SetEase(Ease.Linear));
            }

            sequence.AppendCallback(() =>
            {
                RemoveBackgroundSlashSprite(spriteRenderer);
                _pools.Despawn(spriteObject);
            });

            if (_config != null && _config.UseUnscaledTime)
            {
                sequence.SetUpdate(true);
            }
        }

        private void RemoveBackgroundSlashSprite(SpriteRenderer spriteRenderer)
        {
            _activeBackgroundSlashSprites.Remove(spriteRenderer);
            RemoveMissingBackgroundSlashSprites();
            RefreshBackgroundSlashSortingOrders();
        }

        private void RemoveMissingBackgroundSlashSprites()
        {
            for (var i = _activeBackgroundSlashSprites.Count - 1; i >= 0; i--)
            {
                if (_activeBackgroundSlashSprites[i] == null)
                {
                    _activeBackgroundSlashSprites.RemoveAt(i);
                }
            }
        }

        private void RefreshBackgroundSlashSortingOrders()
        {
            for (var i = 0; i < _activeBackgroundSlashSprites.Count; i++)
            {
                _activeBackgroundSlashSprites[i].sortingOrder =
                    _backgroundSlashBaseSortingOrder + i;
            }
        }

        private void SpawnSlashStreakEffect(
            Vector3 slashWorldPosition,
            float slashZRotation,
            int streak)
        {
            var effectPrefab = _weaponSystem?.CurrentWeapon?.SlashStreakEffect;

            if (_streakConfig == null ||
                _positionConfig == null ||
                effectPrefab == null ||
                !_streakConfig.ShouldSpawnSlashStreakEffect(streak))
            {
                return;
            }

            if (_pools == null)
            {
                return;
            }

            var effect = _pools.Spawn(effectPrefab, _effectsRoot);

            if (effect == null)
            {
                return;
            }

            var localOffset = _streakConfig.SlashStreakEffectLocalOffset;
            var slashWorldRotation = Quaternion.Euler(0f, 0f, slashZRotation);
            var worldOffset = slashWorldRotation *
                new Vector3(localOffset.x, localOffset.y, 0f);
            slashWorldPosition.z = _positionConfig.SlashStreakEffectZ;

            effect.Stop(true);
            effect.transform.SetPositionAndRotation(
                slashWorldPosition + worldOffset,
                slashWorldRotation);

            effect.Play();
            _pools.Despawn(effect, _streakConfig.SlashStreakEffectLifetime);
        }

        private void PlayHitEffect(
            Transform rotationTarget,
            Transform hitRoot,
            Vector3 slashDelta,
            float projectorHeight,
            int streak)
        {
            if (rotationTarget == null ||
                rotationTarget == hitRoot ||
                _hitEffectConfig == null ||
                !_hitEffectConfig.RotationEnabled ||
                _hitEffectConfig.RotationDuration <= 0f)
            {
                return;
            }

            var slashDirection = slashDelta.normalized;

            if (slashDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var worldRotationAxis = Vector3.Cross(slashDirection, _camera.transform.forward).normalized;
            var rotationAngle =
                GetHitRotationAngle(projectorHeight) *
                (_streakConfig != null
                    ? _streakConfig.GetRotationStrengthMultiplier(streak)
                    : 1f);
            var appliedAngle = 0f;
            var rotationTween = DOVirtual
                .Float(0f, rotationAngle, _hitEffectConfig.RotationDuration, angle =>
                {
                    if (rotationTarget == null)
                    {
                        return;
                    }

                    var deltaAngle = angle - appliedAngle;
                    appliedAngle = angle;
                    rotationTarget.Rotate(worldRotationAxis, deltaAngle, Space.World);
                })
                .SetEase(_hitEffectConfig.RotationEase)
                .SetLink(rotationTarget.gameObject);

            if (_hitEffectConfig.UseUnscaledTime)
            {
                rotationTween.SetUpdate(true);
            }
        }

        private void PlayScreenShake(int streak)
        {
            if (_camera == null ||
                _hitEffectConfig == null ||
                !_hitEffectConfig.ScreenShakeEnabled ||
                _hitEffectConfig.ScreenShakeDuration <= 0f)
            {
                return;
            }

            var strength = _streakConfig != null
                ? _streakConfig.GetScreenShakeStrength(
                    streak,
                    _hitEffectConfig.ScreenShakeBaseStrength,
                    _hitEffectConfig.ScreenShakeMaxStrength)
                : _hitEffectConfig.ScreenShakeBaseStrength;

            if (strength <= 0f)
            {
                return;
            }

            StopScreenShake();
            _cameraStartLocalPosition = _camera.transform.localPosition;

            _screenShakeTween = _camera.transform
                .DOShakePosition(
                    _hitEffectConfig.ScreenShakeDuration,
                    new Vector3(strength, strength, 0f),
                    _hitEffectConfig.ScreenShakeVibrato,
                    _hitEffectConfig.ScreenShakeRandomness,
                    false,
                    true)
                .SetLink(_camera.gameObject)
                .OnKill(RestoreCameraPosition);

            if (_hitEffectConfig.UseUnscaledTime)
            {
                _screenShakeTween.SetUpdate(true);
            }
        }

        private void StopScreenShake()
        {
            if (_screenShakeTween != null && _screenShakeTween.IsActive())
            {
                _screenShakeTween.Kill(false);
                return;
            }

            RestoreCameraPosition();
        }

        private void RestoreCameraPosition()
        {
            if (_camera != null)
            {
                _camera.transform.localPosition = _cameraStartLocalPosition;
            }

            _screenShakeTween = null;
        }

        private float GetHitRotationAngle(float projectorHeight)
        {
            var minHeight = _config != null ? Mathf.Max(0f, _config.DecalProjectorMinHeight) : 3f;
            var maxHeight = _config != null ? Mathf.Max(minHeight, _config.DecalProjectorMaxHeight) : 8f;
            var heightProgress = maxHeight > minHeight
                ? Mathf.InverseLerp(minHeight, maxHeight, projectorHeight)
                : 1f;

            return Mathf.Lerp(
                _hitEffectConfig.MinRotationAngle,
                _hitEffectConfig.MaxRotationAngle,
                heightProgress);
        }

        private static float GetSlashZRotation(Vector3 delta)
        {
            return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
        }

        private float GetProjectorHeight(float slashLength)
        {
            var minHeight = _config != null ? Mathf.Max(0f, _config.DecalProjectorMinHeight) : 3f;
            var maxHeight = _config != null ? Mathf.Max(minHeight, _config.DecalProjectorMaxHeight) : 8f;
            var minSlashLength = _config != null ? Mathf.Max(0f, _config.MinDragWorldDistance) : 0f;

            if (minSlashLength <= Mathf.Epsilon)
            {
                return Mathf.Clamp(slashLength, minHeight, maxHeight);
            }

            var height = minHeight * (slashLength / minSlashLength);
            return Mathf.Clamp(height, minHeight, maxHeight);
        }

        private float GetBackgroundSlashHeight(float slashLength)
        {
            var minHeight = _config != null ? _config.BackgroundSlashMinHeight : 2.5f;
            var maxHeight = _config != null ? _config.BackgroundSlashMaxHeight : 10f;
            var minSlashLength = _config != null ? Mathf.Max(0f, _config.MinDragWorldDistance) : 0f;

            if (minSlashLength <= Mathf.Epsilon)
            {
                return Mathf.Clamp(slashLength, minHeight, maxHeight);
            }

            var height = minHeight * (slashLength / minSlashLength);
            return Mathf.Clamp(height, minHeight, maxHeight);
        }

        private Vector3 GetSpawnPosition(Transform decalPoint, Vector3 startWorldPosition, Vector3 endWorldPosition)
        {
            var slashMidpoint = Vector3.Lerp(startWorldPosition, endWorldPosition, 0.5f);
            return ProjectToPlane(decalPoint, slashMidpoint);
        }

        private Vector3 ProjectToPlane(Transform planePoint, Vector3 worldPosition)
        {
            var screenPosition = _camera.WorldToScreenPoint(worldPosition);
            var screenRay = _camera.ScreenPointToRay(screenPosition);
            var projectorPlane = new Plane(_camera.transform.forward, planePoint.position);

            if (projectorPlane.Raycast(screenRay, out var distance))
            {
                return screenRay.GetPoint(distance);
            }

            var planeOffset = Vector3.ProjectOnPlane(
                worldPosition - planePoint.position,
                _camera.transform.forward);
            return planePoint.position + planeOffset;
        }
    }
}
