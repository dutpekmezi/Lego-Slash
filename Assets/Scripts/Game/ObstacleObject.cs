using UnityEngine;

namespace TrashRush.Game
{
    public sealed class ObstacleObject : MonoBehaviour
    {
        [SerializeField] private Transform _rotationTarget;
        [SerializeField] private Transform _mesh;

        private ObstacleObjectData _data;
        private bool _isResolved;

        public ObstacleObjectData Data => _data;
        public bool IsHittable => _data != null && !_isResolved;
        public Transform RotationTarget =>
            _rotationTarget != null ? _rotationTarget : transform;

        public void Initialize(ObstacleObjectData data)
        {
            _data = data;
            _isResolved = false;

            if (_data != null && !string.IsNullOrWhiteSpace(_data.DisplayName))
            {
                gameObject.name = _data.DisplayName;
            }
        }

        public bool TryExplode()
        {
            if (!IsHittable)
            {
                return false;
            }

            _isResolved = true;
            StopObjectMotion();
            DisableOriginalMesh();

            if (_data == null || _data.CellFractured == null)
            {
                Destroy(gameObject);
                return true;
            }

            var fracturedObject = Instantiate(_data.CellFractured, transform.parent);
            var fracturedTransform = fracturedObject.transform;
            fracturedTransform.localScale = _mesh != null
                ? _mesh.localScale
                : Vector3.one;
            fracturedTransform.SetPositionAndRotation(
                RotationTarget.position,
                RotationTarget.rotation);

            ApplyFractureForce(fracturedObject);

            Destroy(fracturedObject, _data.CellFracturedDestroyDelay);
            Destroy(gameObject, _data.CellFractureDestroyDelay);
            return true;
        }

        private void StopObjectMotion()
        {
            var floatingAnimators =
                GetComponentsInChildren<FloatingMeshAnimator>(true);

            for (var i = 0; i < floatingAnimators.Length; i++)
            {
                floatingAnimators[i].Stop();
            }
        }

        private void DisableOriginalMesh()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);

            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }

            var colliders = GetComponentsInChildren<Collider>(true);

            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private void ApplyFractureForce(GameObject fracturedObject)
        {
            var renderers =
                fracturedObject.GetComponentsInChildren<MeshRenderer>(true);

            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = renderers[0].bounds;

            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var explosionCenter = bounds.center;
            var force = _data != null ? _data.CellFractureForce : 0f;

            for (var i = 0; i < renderers.Length; i++)
            {
                var fragment = renderers[i].gameObject;
                var rigidbody = fragment.GetComponent<Rigidbody>();

                if (rigidbody == null)
                {
                    rigidbody = fragment.AddComponent<Rigidbody>();
                }

                rigidbody.isKinematic = false;
                rigidbody.useGravity = false;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;

                var direction = rigidbody.worldCenterOfMass - explosionCenter;

                if (direction.sqrMagnitude <= Mathf.Epsilon)
                {
                    direction = Random.onUnitSphere;
                }

                rigidbody.AddForce(
                    direction.normalized * force,
                    ForceMode.Impulse);
            }
        }
    }
}
