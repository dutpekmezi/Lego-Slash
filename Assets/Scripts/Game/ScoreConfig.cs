using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "Trash Rush/Game/Score Config")]
    public sealed class ScoreConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int _normalSlashScore = 10;
        [SerializeField, Min(0)] private int _minStreakScore = 20;
        [SerializeField, Min(0)] private int _maxStreakScore = 100;
        [SerializeField, Min(0)] private int _streakScoreIncrease = 5;

        public int NormalSlashScore => Mathf.Max(0, _normalSlashScore);
        public int MinStreakScore => Mathf.Max(0, _minStreakScore);
        public int MaxStreakScore => Mathf.Max(MinStreakScore, _maxStreakScore);
        public int StreakScoreIncrease => Mathf.Max(0, _streakScoreIncrease);

        public int GetSlashScore(int streak)
        {
            if (streak <= 1)
            {
                return NormalSlashScore;
            }

            var streakIndex = streak - 2;
            var score = (long)MinStreakScore +
                (long)StreakScoreIncrease * streakIndex;
            return score >= MaxStreakScore
                ? MaxStreakScore
                : (int)score;
        }
    }
}
