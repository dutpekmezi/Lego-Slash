using UnityEngine;

namespace TrashRush.Game
{
    [DisallowMultipleComponent]
    public sealed class VoxelBrick : MonoBehaviour
    {
        public bool IsDetached { get; private set; }

        internal void DetachForExplosion(Transform parent)
        {
            IsDetached = true;
            transform.SetParent(parent, true);
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                collider.isTrigger = false;
        }

        public bool Launch(SlashConfig config, SlashObjectData data = null, float? lifetime = null)
        {
            if (IsDetached) return false;
            IsDetached = true;
            // Keep the current world pose when the rotating/falling model releases it.
            transform.SetParent(null, true);
            var body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = true;
            body.detectCollisions = true;
            body.constraints = RigidbodyConstraints.None;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            var force = data != null ? data.FragmentationForce : 5f;
            if (force > 0f)
            {
                // Every random direction is in the world -Z hemisphere, including
                // upward, downward, sideways and diagonal launches.
                var direction = Random.onUnitSphere;
                direction.z = -Mathf.Max(0.2f, Mathf.Abs(direction.z));
                body.AddForce(direction.normalized * force * 4f, ForceMode.VelocityChange);
                var spin = config != null ? config.BrickSpinSpeed : 4f;
                body.AddTorque(Random.insideUnitSphere * spin * (force / 5f),
                    ForceMode.VelocityChange);
            }
            Destroy(gameObject, lifetime ?? (config != null ? config.BrickLifetime : 5f));
            return true;
        }
    }
}
