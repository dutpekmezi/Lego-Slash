using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Collections;

namespace TrashRush.Game
{
    public sealed class SlashObject : MonoBehaviour
    {
        [SerializeField] private Transform _rotationTarget;
        [SerializeField] private Transform _decalFollowTarget;
        [SerializeField] private Transform _mesh;

        private SlashObjectData _data;
        private int _currentSlash;
        private bool _isResolved;
        private bool _isFractured;
        private bool _completedCut;
        private bool _usesVoxelBricks;
        private VoxelBrick[] _initialBricks;
        private int _splitMinimum;
        private Rigidbody _rotationBody;
        private Vector3 _fallPosition;
        private Vector3 _pivotOffset;
        private Vector3 _splitOffset;
        private Vector3 _splitVelocity;
        private float _splitDamping = 4f;
        public Vector3 FallPosition => _rotationBody != null ? _fallPosition : transform.position;
        private Transform BrickRoot => _mesh != null ? _mesh : DecalFollowTarget;
        private Quaternion _initialDecalFollowLocalRotation = Quaternion.identity;
        private Vector3 _initialDecalFollowLocalPosition;
        private bool _hasFractureReferencePose;

        public event Action<SlashObject> Exploded;
        public event Action<SlashObject, SlashObject> SplitCreated;

        public SlashObjectData Data => _data;
        public int CurrentSlash => _currentSlash;
        public int TotalBricks => _initialBricks != null ? _initialBricks.Length : 0;
        public int ReleasedBricks => _usesVoxelBricks ? TotalBricks - RemainingBrickCount() : 0;
        public bool IsSlashable => _data != null && !_isResolved;
        public Transform RotationTarget => _rotationTarget != null ? _rotationTarget : transform;
        public Transform DecalFollowTarget =>
            _decalFollowTarget != null ? _decalFollowTarget : RotationTarget;

        public void Initialize(SlashObjectData data)
        {
            _data = data;
            _currentSlash = 0;
            _isResolved = false;
            _isFractured = false;
            _completedCut = false;
            _splitMinimum = 0;
            _splitOffset = Vector3.zero;
            _splitVelocity = Vector3.zero;
            _initialBricks = BrickRoot.GetComponentsInChildren<VoxelBrick>(true);
            _usesVoxelBricks = _initialBricks.Length > 0;
            if (_data != null && _data.Slashable)
            {
                foreach (var brick in _initialBricks)
                {
                    foreach (var collider in brick.GetComponentsInChildren<Collider>(true))
                    {
                        if (collider is MeshCollider meshCollider) meshCollider.convex = true;
                        collider.isTrigger = false;
                    }
                    // Attached bricks must share the model body, rather than each
                    // becoming an isolated kinematic body inside the compound shape.
                    foreach (var body in brick.GetComponentsInChildren<Rigidbody>(true))
                    {
                        body.isKinematic = true;
                        Destroy(body);
                    }
                }
            }

            if (_data != null && !string.IsNullOrWhiteSpace(_data.DisplayName))
            {
                gameObject.name = _data.DisplayName;
            }

            CaptureFractureReferencePose();
            // Keep prefab pivot bodies passive while floating controls the pose.
            var pivot = BrickRoot.parent != null && BrickRoot.parent.name == "RotationPivot"
                ? BrickRoot.parent : RotationTarget;
            foreach (var animator in GetComponentsInChildren<FloatingMeshAnimator>(true)) animator.Stop();
            _rotationTarget = pivot;
            _rotationBody = pivot.GetComponent<Rigidbody>();
            if (_rotationBody == null) _rotationBody = pivot.gameObject.AddComponent<Rigidbody>();
            _rotationBody.isKinematic = false;
            _rotationBody.useGravity = false;
            _rotationBody.constraints = RigidbodyConstraints.None;
            _rotationBody.linearDamping = 0f;
            _rotationBody.interpolation = RigidbodyInterpolation.Interpolate;
            _rotationBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            _rotationBody.maxAngularVelocity = 5f;
            _fallPosition = transform.position;
            RefreshRotationShape();
        }

        private void RefreshRotationShape()
        {
            if (_rotationBody == null) return;
            _pivotOffset = _rotationBody.position - _fallPosition - _splitOffset;
            _rotationBody.ResetCenterOfMass();
            _rotationBody.ResetInertiaTensor();
            foreach (var collider in BrickRoot.GetComponentsInChildren<Collider>(true))
                collider.hasModifiableContacts = true;
            DividedObjectContacts.SetBudget(_rotationBody.GetInstanceID(), 1f);
        }

        internal void SetFallPosition(Vector3 position, bool snap = false)
        {
            _fallPosition = position;
            if (_rotationBody == null) transform.position = position;
            else if (snap) _rotationBody.position = position + _pivotOffset + _splitOffset;
        }

        private void FixedUpdate()
        {
            if (_rotationBody == null || _isResolved) return;
            // Only the fall target is animated. Never tween a dynamic body's
            // transform or its parent while the solver owns its rotation.
            var decay = Mathf.Exp(-_splitDamping * Time.fixedDeltaTime);
            _splitOffset += _splitVelocity * ((1f - decay) / _splitDamping);
            _splitVelocity *= decay;
            _rotationBody.linearVelocity = (_fallPosition + _pivotOffset + _splitOffset - _rotationBody.position) / Time.fixedDeltaTime;
            _rotationBody.WakeUp();
        }

        internal void ApplyHitRotation(Camera camera, Vector3 start, Vector3 end)
        {
            if (_rotationBody == null || _isResolved || _data == null) return;
            var axis = Vector3.Cross(camera != null ? camera.transform.forward : Vector3.forward, end - start).normalized;
            _rotationBody.AddTorque(axis * _data.HitRotationForce, ForceMode.VelocityChange);
        }

        private void OnDestroy()
        {
            if (_rotationBody != null) DividedObjectContacts.Remove(_rotationBody.GetInstanceID());
        }

        public bool TryRegisterSlash(out bool completed)
        {
            completed = false;

            if (!IsSlashable)
            {
                return false;
            }

            _currentSlash++;
            completed = _completedCut || (_usesVoxelBricks
                ? (_splitMinimum > 0 ? RemainingBrickCount() < _splitMinimum :
                    RemainingBrickCount() * 3L < _initialBricks.Length)
                : _data.Hardness < 10f);

            if (completed)
            {
                _isResolved = true;
            }

            return true;
        }

        public void MarkEscaped()
        {
            _isResolved = true;
        }

        private int RemainingBrickCount()
        {
            var remaining = 0;
            foreach (var brick in _initialBricks)
            {
                if (brick != null && !brick.IsDetached && brick.transform.IsChildOf(BrickRoot))
                    remaining++;
            }
            return remaining;
        }

        internal void ReleaseAllBricks(SlashConfig config)
        {
            foreach (var brick in _initialBricks)
            {
                if (brick != null && !brick.IsDetached && brick.transform.IsChildOf(BrickRoot))
                    brick.Launch(config, _data);
            }
        }

        internal bool CanSplit(SlashConfig config)
        {
            // Split-enabled models must reach the split threshold before the old
            // one-third cleanup rule can prematurely resolve the whole model.
            if (_data != null && _data.Slashable)
                _splitMinimum = _data.MinSlashablePieceCount;
            return IsSlashable && _usesVoxelBricks && _data.Slashable &&
                _data.Prefab != null;
        }

        internal void SplitAlongCut(Camera camera, Vector3 start, Vector3 end, SlashConfig config)
        {
            _splitMinimum = _data.MinSlashablePieceCount;
            var normal = Vector3.Cross(camera.transform.forward, end - start).normalized;
            if (normal.sqrMagnitude <= Mathf.Epsilon) return;
            var sides = new[] { new List<VoxelBrick>(), new List<VoxelBrick>() };
            foreach (var brick in _initialBricks)
            {
                if (brick == null || brick.IsDetached || !brick.transform.IsChildOf(BrickRoot)) continue;
                var renderer = brick.GetComponentInChildren<Renderer>();
                var center = renderer != null ? renderer.bounds.center : brick.transform.position;
                sides[Vector3.Dot(center - start, normal) < 0f ? 0 : 1].Add(brick);
            }
            if (sides[0].Count == 0 || sides[1].Count == 0) return;
            // Keep the larger side in the existing object. Tiny sides never create a prefab.
            var retained = sides[0].Count >= sides[1].Count ? sides[0] : sides[1];
            var separated = retained == sides[0] ? sides[1] : sides[0];
            SlashObject fragment = null;
            if (separated.Count < _splitMinimum)
            {
                foreach (var brick in separated) brick.Launch(config, _data);
            }
            else
            {
                var staging = new GameObject("SplitPrefabStaging");
                staging.SetActive(false);
                fragment = Instantiate(_data.Prefab, staging.transform);
                while (fragment.BrickRoot.childCount > 0)
                    fragment.BrickRoot.GetChild(0).SetParent(staging.transform, false);
                fragment.transform.SetParent(transform.parent, false);
                fragment.transform.SetPositionAndRotation(FallPosition, transform.rotation);
                fragment.transform.localScale = transform.localScale;
                foreach (var brick in separated) brick.transform.SetParent(fragment.BrickRoot, true);
                fragment.Initialize(_data);
                fragment._splitMinimum = _splitMinimum;
                fragment.RecenterPivot();
                fragment.ApplyHitRotation(camera, start, end);
                Destroy(staging);
            }
            _initialBricks = retained.ToArray();
            DestroyDecalProjectors();
            RecenterPivot();
            if (fragment != null)
            {
                // Only the slash introduces a positional impulse. Contact
                // response remains angular, and the configured fall is preserved.
                var retainedDirection = normal * (retained == sides[0] ? -1f : 1f);
                var push = _data.SplitPushForce;
                _splitDamping = fragment._splitDamping = config != null ? config.SplitPushDamping : 4f;
                _splitVelocity += retainedDirection * push;
                fragment._splitVelocity -= retainedDirection * push;
                fragment.gameObject.name = gameObject.name + "_Split";
                SplitCreated?.Invoke(this, fragment);
            }
        }

        internal void CutAndComplete(Camera camera, Vector3 start, Vector3 end, SlashConfig config)
        {
            if (_completedCut || camera == null) return;
            var normal = Vector3.Cross(camera.transform.forward, end - start).normalized;
            if (normal.sqrMagnitude <= Mathf.Epsilon) return;
            var pieces = new[] { new List<VoxelBrick>(), new List<VoxelBrick>() };
            foreach (var brick in _initialBricks)
            {
                if (brick == null || brick.IsDetached || !brick.transform.IsChildOf(BrickRoot)) continue;
                var renderer = brick.GetComponentInChildren<Renderer>();
                var center = renderer != null ? renderer.bounds.center : brick.transform.position;
                pieces[Vector3.Dot(center - start, normal) < 0f ? 0 : 1].Add(brick);
            }
            var velocity = _rotationBody != null ? _rotationBody.linearVelocity : Vector3.zero;
            var spin = _rotationBody != null ? _rotationBody.angularVelocity : Vector3.zero;
            for (int side = 0; side < pieces.Length; side++)
            {
                if (pieces[side].Count == 0) continue;
                // Visual debris only: no SlashObject, progress registration or
                // recursively slashable prefab is created for these halves.
                var group = new GameObject(gameObject.name + "_CutHalf_" + side);
                group.layer = gameObject.layer;
                var center = Vector3.zero;
                foreach (var brick in pieces[side]) center += brick.transform.position;
                group.transform.position = center / pieces[side].Count;
                foreach (var brick in pieces[side]) brick.DetachForExplosion(group.transform);
                var body = group.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.isKinematic = false;
                body.constraints = RigidbodyConstraints.None;
                body.linearDamping = 0f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.angularVelocity = spin;
                group.AddComponent<CompletedCutHalfMotion>().Initialize(body, velocity,
                    normal * (side == 0 ? -1f : 1f) * _data.SplitPushForce,
                    config != null ? config.SplitPushDamping : 4f);
                Destroy(group, config != null ? config.BrickLifetime : 5f);
            }
            _completedCut = true;
        }

        private void RecenterPivot()
        {
            var renderers = BrickRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var pivot = BrickRoot.parent != null && BrickRoot.parent.name == "RotationPivot"
                ? BrickRoot.parent : RotationTarget;
            RotationTarget.DOKill(false);
            foreach (var animator in GetComponentsInChildren<FloatingMeshAnimator>()) animator.Stop();
            _rotationTarget = pivot;
            pivot.DOKill(false);
            var children = new Transform[pivot.childCount];
            var positions = new Vector3[children.Length];
            for (int i = 0; i < children.Length; i++)
            {
                children[i] = pivot.GetChild(i);
                positions[i] = children[i].position;
            }
            pivot.position = bounds.center;
            for (int i = 0; i < children.Length; i++) children[i].position = positions[i];
            CaptureFractureReferencePose();
            if (_rotationBody != null)
            {
                _rotationBody.position = pivot.position;
                RefreshRotationShape();
            }
            foreach (var animator in GetComponentsInChildren<FloatingMeshAnimator>()) animator.Stop();
        }

        public void Explode(Camera gameplayCamera, Vector2 screenSlashDirection)
        {
            if (gameObject == null || _isFractured)
            {
                return;
            }

            _isResolved = true;
            _isFractured = true;
            StopObjectMotion();
            var modelBody = GetComponent<Rigidbody>();
            if (modelBody != null)
            {
                modelBody.isKinematic = true;
                modelBody.detectCollisions = false;
            }
            Exploded?.Invoke(this);

            if (_completedCut)
            {
                // The cut already released two intact debris groups. Do not
                // explode them again when the service advances to the next item.
                Destroy(gameObject);
                return;
            }

            DestroyDecalProjectors();
            if (_usesVoxelBricks)
            {
                foreach (var brick in _initialBricks)
                {
                    if (brick == null || brick.IsDetached || !brick.transform.IsChildOf(BrickRoot))
                        continue;
                    brick.Launch(null, _data, _data.CellFracturedDestroyDelay);
                }
                DisableOriginalMesh();
                Destroy(gameObject, _data.CellFractureDestroyDelay);
                return;
            }
            DisableOriginalMesh();

            if (_data == null || _data.CellFractured == null)
            {
                Destroy(gameObject);
                return;
            }

            var fracturedObject = Instantiate(_data.CellFractured, transform.parent);
            var fracturedTransform = fracturedObject.transform;
            GetFractureRootPose(out var fracturePosition, out var fractureRotation);
            fracturedTransform.SetPositionAndRotation(fracturePosition, fractureRotation);

            ApplyFractureForce(fracturedObject, gameplayCamera, screenSlashDirection);

            Destroy(fracturedObject, _data.CellFracturedDestroyDelay);
        }

        private void CaptureFractureReferencePose()
        {
            var followTarget = DecalFollowTarget;
            _hasFractureReferencePose =
                followTarget != null &&
                followTarget != transform &&
                followTarget.IsChildOf(transform);

            if (!_hasFractureReferencePose)
            {
                _initialDecalFollowLocalRotation = Quaternion.identity;
                _initialDecalFollowLocalPosition = Vector3.zero;
                return;
            }

            _initialDecalFollowLocalRotation = followTarget.localRotation;
            _initialDecalFollowLocalPosition = followTarget.localPosition;
        }

        private void GetFractureRootPose(
            out Vector3 position,
            out Quaternion rotation)
        {
            var followTarget = DecalFollowTarget;

            if (followTarget == null)
            {
                position = transform.position;
                rotation = transform.rotation;
                return;
            }

            if (!_hasFractureReferencePose)
            {
                position = followTarget.position;
                rotation = followTarget.rotation;
                return;
            }

            rotation = followTarget.rotation * Quaternion.Inverse(_initialDecalFollowLocalRotation);
            position = followTarget.position - rotation * _initialDecalFollowLocalPosition;
        }

        private void StopObjectMotion()
        {
            if (_rotationBody != null)
            {
                DividedObjectContacts.Remove(_rotationBody.GetInstanceID());
                _rotationBody.isKinematic = true;
                _rotationBody.detectCollisions = false;
            }
            transform.DOKill(false);
            var floatingAnimators = GetComponentsInChildren<FloatingMeshAnimator>(true);

            for (var i = 0; i < floatingAnimators.Length; i++)
            {
                floatingAnimators[i].Stop();
            }
        }

        private void DisableOriginalMesh()
        {
            var renderers = DecalFollowTarget.GetComponentsInChildren<Renderer>(true);

            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }

            var colliders = DecalFollowTarget.GetComponentsInChildren<Collider>(true);

            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private void DestroyDecalProjectors()
        {
            var decalProjectors = GetComponentsInChildren<DecalProjector>(true);

            for (var i = 0; i < decalProjectors.Length; i++)
            {
                if (decalProjectors[i] != null)
                {
                    Destroy(decalProjectors[i].gameObject);
                }
            }
        }

        private void ApplyFractureForce(
            GameObject fracturedObject, Camera gameplayCamera, Vector2 screenSlashDirection)
        {
            if (_data == null)
            {
                return;
            }

            StartCoroutine(SeparateFractureGroups(fracturedObject, gameplayCamera, screenSlashDirection));
        }

        private IEnumerator SeparateFractureGroups(
            GameObject fracturedObject, Camera gameplayCamera, Vector2 screenSlashDirection)
        {
            var renderers = fracturedObject.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0 || gameplayCamera == null ||
                screenSlashDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                Destroy(gameObject, _data.CellFractureDestroyDelay);
                yield break;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var screenDirection = screenSlashDirection.normalized;
            var cameraRight = gameplayCamera.transform.right;
            var cameraUp = gameplayCamera.transform.up;
            var worldSlashDirection =
                cameraRight * screenDirection.x + cameraUp * screenDirection.y;
            var separationDirection = Vector3.Cross(
                gameplayCamera.transform.forward, worldSlashDirection).normalized;

            // Freeze existing physics before changing hierarchy. Destroy completes at frame end.
            var existingBodies = fracturedObject.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < existingBodies.Length; i++)
            {
                existingBodies[i].isKinematic = true;
                existingBodies[i].detectCollisions = false;
                Destroy(existingBodies[i]);
            }

            var groups = new Transform[2];
            var groupBounds = new Bounds[2];
            var counts = new int[2];
            for (var i = 0; i < groups.Length; i++)
            {
                groups[i] = new GameObject(i == 0 ? "CutSideNegative" : "CutSidePositive").transform;
                groups[i].SetParent(fracturedObject.transform, false);
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                var pieceBounds = renderers[i].bounds;
                var side = Vector3.Dot(pieceBounds.center - bounds.center, separationDirection);
                var groupIndex = side < 0f ? 0 : 1;
                if (counts[groupIndex]++ == 0)
                {
                    groupBounds[groupIndex] = pieceBounds;
                }
                else
                {
                    groupBounds[groupIndex].Encapsulate(pieceBounds);
                }
                renderers[i].transform.SetParent(groups[groupIndex], true);
            }

            // Put each parent pivot at its visual center while retaining the assembled pose.
            for (var i = 0; i < groups.Length; i++)
            {
                var offset = groupBounds[i].center - groups[i].position;
                groups[i].position += offset;
                for (var j = 0; j < groups[i].childCount; j++)
                {
                    groups[i].GetChild(j).position -= offset;
                }
            }

            if (existingBodies.Length > 0)
            {
                yield return null;
            }

            if (fracturedObject != null)
            {
                for (var i = 0; i < groups.Length; i++)
                {
                    if (counts[i] == 0)
                    {
                        continue;
                    }

                    // Child colliders form a compound collider on the parent body.
                    var meshColliders = groups[i].GetComponentsInChildren<MeshCollider>(true);
                    for (var j = 0; j < meshColliders.Length; j++)
                    {
                        meshColliders[j].convex = true;
                    }

                    var body = groups[i].gameObject.AddComponent<Rigidbody>();
                    body.useGravity = false;
                    var force = _data.CellFractureForce;
                    var sideDirection = separationDirection * (i == 0 ? -1f : 1f);
                    var randomOffset = UnityEngine.Random.insideUnitCircle;
                    var impulse = sideDirection * force +
                        cameraUp * Mathf.Min(_data.CellFractureUpwardForce, force * 0.25f) +
                        (cameraRight * randomOffset.x + cameraUp * randomOffset.y) *
                        Mathf.Min(_data.CellFractureRandomForce, force * 0.15f);
                    body.AddForce(impulse, ForceMode.Impulse);
                    body.AddTorque(UnityEngine.Random.insideUnitSphere *
                        _data.CellFractureTorqueForce, ForceMode.Impulse);
                }
            }

            Destroy(gameObject, _data.CellFractureDestroyDelay);
        }
    }

    // Completed halves are visual debris, not new SlashObjects. Their prescribed
    // separation must not be cancelled by contacts along the cut surface.
    internal sealed class CompletedCutHalfMotion : MonoBehaviour
    {
        private Rigidbody _body;
        private Vector3 _target;
        private Vector3 _fallVelocity;
        private Vector3 _pushVelocity;
        private float _damping;

        internal void Initialize(Rigidbody body, Vector3 fallVelocity, Vector3 pushVelocity, float damping)
        {
            _body = body;
            _target = body.position;
            _fallVelocity = fallVelocity;
            _pushVelocity = pushVelocity;
            _damping = Mathf.Max(0.1f, damping);
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.hasModifiableContacts = true;
            DividedObjectContacts.SetBudget(body.GetInstanceID(), 1f);
        }

        private void FixedUpdate()
        {
            if (_body == null) return;
            var dt = Time.fixedDeltaTime;
            var decay = Mathf.Exp(-_damping * dt);
            _fallVelocity += Physics.gravity * dt;
            _target += _fallVelocity * dt + _pushVelocity * ((1f - decay) / _damping);
            _pushVelocity *= decay;
            _body.linearVelocity = (_target - _body.position) / dt;
            _body.WakeUp();
        }

        private void OnDestroy()
        {
            if (_body != null) DividedObjectContacts.Remove(_body.GetInstanceID());
        }
    }

    // Contact modification may execute on physics worker threads. Only managed,
    // thread-safe cached values are accessed here; ScriptableObjects stay on the main thread.
    internal static class DividedObjectContacts
    {
        private static readonly ConcurrentDictionary<int, float> Budgets = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Budgets.Clear();
            Physics.ContactModifyEvent -= ModifyContacts;
            Physics.ContactModifyEventCCD -= ModifyContacts;
            Physics.ContactModifyEvent += ModifyContacts;
            Physics.ContactModifyEventCCD += ModifyContacts;
        }

        public static void SetBudget(int bodyId, float impulse) => Budgets[bodyId] = impulse;
        public static void Remove(int bodyId) => Budgets.TryRemove(bodyId, out _);

        private static void ModifyContacts(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
        {
            for (int i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i];
                var own = Budgets.ContainsKey(pair.bodyInstanceID);
                var other = Budgets.ContainsKey(pair.otherBodyInstanceID);
                if (!own && !other) continue;
                var properties = pair.massProperties;
                // Keep angular contact response in the solver, but eliminate
                // the linear response on each artificially falling model.
                if (own) properties.inverseMassScale = 0f;
                if (other) properties.otherInverseMassScale = 0f;
                pair.massProperties = properties;
                for (int contact = 0; contact < pair.contactCount; contact++)
                {
                    pair.SetBounciness(contact, 0f);
                    // Do not turn initial overlap after a cut into a strong
                    // depenetration spin. Closing angular motion still resolves.
                    pair.SetSeparation(contact, Math.Max(0f, pair.GetSeparation(contact)));
                }
            }
        }
    }
}
