using System;
using VContainer.Unity;

namespace TrashRush.Game
{
    public sealed class ScoreSystem : IStartable
    {
        private readonly ScoreConfig _config;

        public event Action<int> ScoreChanged;

        public int CurrentScore { get; private set; }

        public ScoreSystem(ScoreConfig config)
        {
            _config = config;
        }

        public void Start()
        {
            ResetScore();
        }

        public int RegisterSlash(int streak)
        {
            var requestedScore = _config != null
                ? _config.GetSlashScore(streak)
                : 0;
            var nextScore = Math.Min(
                (long)CurrentScore + requestedScore,
                int.MaxValue);
            var awardedScore = (int)(nextScore - CurrentScore);

            CurrentScore = (int)nextScore;
            ScoreChanged?.Invoke(CurrentScore);
            return awardedScore;
        }

        private void ResetScore()
        {
            CurrentScore = 0;
            ScoreChanged?.Invoke(CurrentScore);
        }
    }
}
