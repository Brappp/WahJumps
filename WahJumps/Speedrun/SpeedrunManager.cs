// File: WahJumps/Data/SpeedrunManager.cs
using System;
using WahJumps.Data;

namespace WahJumps.Data
{
    public class SpeedrunManager
    {
        public enum SpeedrunState
        {
            Idle,
            Countdown,
            Running,
            Finished
        }

        // Current state
        private SpeedrunState currentState = SpeedrunState.Idle;
        private DateTime startTime;
        private TimeSpan currentTime;
        private int countdownSeconds;
        private int countdownRemaining;
        private JumpPuzzleData currentPuzzle;

        // Settings
        public int DefaultCountdown { get; set; } = 3;

        // Events
        public event Action<SpeedrunState> StateChanged;
        public event Action<TimeSpan> TimeUpdated;
        public event Action<int> CountdownTick;

        public SpeedrunManager(string configDirectory)
        {
            // Simple constructor, no complex initialization needed
        }

        #region Puzzle Management

        public void SetPuzzle(JumpPuzzleData puzzle)
        {
            currentPuzzle = puzzle;
        }

        public JumpPuzzleData GetCurrentPuzzle()
        {
            return currentPuzzle;
        }

        #endregion

        #region Timer Control

        public void StartCountdown()
        {
            countdownSeconds = DefaultCountdown;
            countdownRemaining = countdownSeconds;
            startTime = DateTime.Now;

            // Change state to countdown
            currentState = SpeedrunState.Countdown;
            StateChanged?.Invoke(currentState);
        }

        public void SkipCountdown()
        {
            StartTimer();
        }

        private void StartTimer()
        {
            startTime = DateTime.Now;
            currentTime = TimeSpan.Zero;
            currentState = SpeedrunState.Running;
            StateChanged?.Invoke(currentState);
        }

        public void StopTimer()
        {
            if (currentState != SpeedrunState.Running) return;

            currentState = SpeedrunState.Finished;
            StateChanged?.Invoke(currentState);
        }

        public void ResetTimer()
        {
            currentState = SpeedrunState.Idle;
            currentTime = TimeSpan.Zero;
            StateChanged?.Invoke(currentState);
        }

        #endregion

        #region Update Methods

        public void Update()
        {
            switch (currentState)
            {
                case SpeedrunState.Countdown:
                    UpdateCountdown();
                    break;
                case SpeedrunState.Running:
                    UpdateTimer();
                    break;
            }
        }

        private void UpdateCountdown()
        {
            int previousRemaining = countdownRemaining;
            countdownRemaining = countdownSeconds - (int)(DateTime.Now - startTime).TotalSeconds;

            if (countdownRemaining <= 0)
            {
                StartTimer();
                return;
            }

            if (countdownRemaining != previousRemaining)
            {
                CountdownTick?.Invoke(countdownRemaining);
            }
        }

        private void UpdateTimer()
        {
            currentTime = DateTime.Now - startTime;
            TimeUpdated?.Invoke(currentTime);
        }

        #endregion

        #region State Getters

        public SpeedrunState GetState() => currentState;

        public TimeSpan GetCurrentTime() => currentTime;

        public int GetCountdownRemaining() => countdownRemaining;

        #endregion
    }
}
