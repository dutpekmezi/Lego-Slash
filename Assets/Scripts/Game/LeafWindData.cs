using UnityEngine;

namespace TrashRush.Game
{
    public readonly struct LeafWindData
    {
        public static readonly LeafWindData None = new(0f, 0f, 0f);

        public LeafWindData(
            float radius,
            float velocityImpulse,
            float maxVelocity)
        {
            Radius = Mathf.Max(0f, radius);
            VelocityImpulse = Mathf.Max(0f, velocityImpulse);
            MaxVelocity = Mathf.Max(0f, maxVelocity);
        }

        public float Radius { get; }
        public float VelocityImpulse { get; }
        public float MaxVelocity { get; }

        public bool IsEnabled =>
            Radius > Mathf.Epsilon &&
            VelocityImpulse > Mathf.Epsilon;

        public LeafWindData Scale(float multiplier)
        {
            multiplier = Mathf.Max(0f, multiplier);

            return new LeafWindData(
                Radius * multiplier,
                VelocityImpulse * multiplier,
                MaxVelocity * multiplier);
        }
    }
}
