using DG.Tweening;
using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(
        fileName = "ObstacleObjectData",
        menuName = "Trash Rush/Game/Obstacle Object Data")]
    public sealed class ObstacleObjectData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private string _id;
        [SerializeField] private ObstacleObject _prefab;

        [Header("Fall")]
        [SerializeField, Min(0.01f)] private float _fallDuration = 9f;
        [SerializeField] private Ease _fallEase = Ease.Linear;

        [Header("Cell Fracture")]
        [SerializeField] private GameObject _cellFractured;
        [SerializeField, Min(0f)] private float _cellFractureForce = 5f;
        [SerializeField, Min(0f)] private float _cellFractureDestroyDelay = 1f;
        [SerializeField, Min(0f)] private float _cellFracturedDestroyDelay = 5f;

        public string DisplayName => _displayName;
        public string Id => _id;
        public ObstacleObject Prefab => _prefab;
        public float FallDuration => Mathf.Max(0.01f, _fallDuration);
        public Ease FallEase => _fallEase;
        public GameObject CellFractured => _cellFractured;
        public float CellFractureForce => Mathf.Max(0f, _cellFractureForce);
        public float CellFractureDestroyDelay =>
            Mathf.Max(0f, _cellFractureDestroyDelay);
        public float CellFracturedDestroyDelay =>
            Mathf.Max(0f, _cellFracturedDestroyDelay);
    }
}
