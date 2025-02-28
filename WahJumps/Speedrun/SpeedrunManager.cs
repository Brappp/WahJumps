// File: WahJumps/Data/SpeedrunManager.cs
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using CsvHelper;
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

        private readonly string recordsFilePath;
        private List<SpeedrunRecord> records = new List<SpeedrunRecord>();
        private SpeedrunState currentState = SpeedrunState.Idle;
        private DateTime startTime;
        private TimeSpan currentTime;
        private int countdownSeconds;
        private int countdownRemaining;
        private SpeedrunRecord currentRun;
        private JumpPuzzleData currentPuzzle;

        // Settings
        public int DefaultCountdown { get; set; } = 3;
        public bool PlaySoundOnCountdown { get; set; } = true;
        public bool AutoSaveRecords { get; set; } = true;

        // Properties
        public string RecordsPath => recordsFilePath;

        // Events
        public event Action<SpeedrunState> StateChanged;
        public event Action<TimeSpan> TimeUpdated;
        public event Action<int> CountdownTick;
        public event Action<SpeedrunRecord> RunCompleted;

        public SpeedrunManager(string configDirectory)
        {
            recordsFilePath = Path.Combine(configDirectory, "speedrun_records.csv");
            LoadRecords();
        }

        public void SetPuzzle(JumpPuzzleData puzzle)
        {
            currentPuzzle = puzzle;
        }

        public JumpPuzzleData GetCurrentPuzzle()
        {
            return currentPuzzle;
        }

        public void StartCountdown(Dictionary<string, string> customFields = null)
        {
            if (currentPuzzle == null) return;

            countdownSeconds = DefaultCountdown;
            countdownRemaining = countdownSeconds;
            startTime = DateTime.Now;

            currentRun = new SpeedrunRecord
            {
                PuzzleId = currentPuzzle.Id,
                PuzzleName = currentPuzzle.PuzzleName,
                World = currentPuzzle.World,
                Date = DateTime.Now,
                CustomFields = customFields ?? new Dictionary<string, string>()
            };

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

            currentRun.Time = currentTime;
            records.Add(currentRun);

            currentState = SpeedrunState.Finished;
            StateChanged?.Invoke(currentState);
            RunCompleted?.Invoke(currentRun);

            if (AutoSaveRecords)
                SaveRecords();
        }

        public void ResetTimer()
        {
            currentState = SpeedrunState.Idle;
            currentTime = TimeSpan.Zero;
            StateChanged?.Invoke(currentState);
        }

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

        // CSV handling methods
        public void SaveRecords()
        {
            try
            {
                using (var writer = new StreamWriter(recordsFilePath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    // Write headers
                    csv.WriteField("Id");
                    csv.WriteField("PuzzleId");
                    csv.WriteField("PuzzleName");
                    csv.WriteField("World");
                    csv.WriteField("TimeInSeconds");
                    csv.WriteField("Date");

                    // Get all unique custom fields
                    var customFields = records
                        .SelectMany(r => r.CustomFields.Keys)
                        .Distinct()
                        .ToList();

                    foreach (var field in customFields)
                    {
                        csv.WriteField($"Custom_{field}");
                    }

                    csv.NextRecord();

                    // Write data
                    foreach (var record in records)
                    {
                        csv.WriteField(record.Id);
                        csv.WriteField(record.PuzzleId);
                        csv.WriteField(record.PuzzleName);
                        csv.WriteField(record.World);
                        csv.WriteField(record.Time.TotalSeconds);
                        csv.WriteField(record.Date);

                        foreach (var field in customFields)
                        {
                            if (record.CustomFields.TryGetValue(field, out string value))
                                csv.WriteField(value);
                            else
                                csv.WriteField(string.Empty);
                        }

                        csv.NextRecord();
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error saving speedrun records: {ex.Message}");
            }
        }

        // File: WahJumps/Data/SpeedrunManager.cs - Fixed header record access

        public void LoadRecords()
        {
            if (!File.Exists(recordsFilePath))
                return;

            try
            {
                records.Clear();

                using (var reader = new StreamReader(recordsFilePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    // Read headers
                    csv.Read();
                    csv.ReadHeader();

                    // Get custom field headers - fix for HeaderRecord access
                    var customFields = new List<string>();

                    // Different ways to access headers depending on CsvHelper version
                    if (csv.HeaderRecord != null)
                    {
                        customFields = csv.HeaderRecord
                            .Where(h => h.StartsWith("Custom_"))
                            .Select(h => h.Substring(7))
                            .ToList();
                    }
                    else if (csv.Context.Reader.HeaderRecord != null)
                    {
                        customFields = csv.Context.Reader.HeaderRecord
                            .Where(h => h.StartsWith("Custom_"))
                            .Select(h => h.Substring(7))
                            .ToList();
                    }

                    // Read records
                    while (csv.Read())
                    {
                        var record = new SpeedrunRecord
                        {
                            Id = csv.GetField<Guid>("Id"),
                            PuzzleId = csv.GetField<int>("PuzzleId"),
                            PuzzleName = csv.GetField<string>("PuzzleName"),
                            World = csv.GetField<string>("World"),
                            Time = TimeSpan.FromSeconds(csv.GetField<double>("TimeInSeconds")),
                            Date = csv.GetField<DateTime>("Date")
                        };

                        foreach (var field in customFields)
                        {
                            string value = csv.GetField<string>($"Custom_{field}");
                            if (!string.IsNullOrEmpty(value))
                                record.CustomFields[field] = value;
                        }

                        records.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error loading speedrun records: {ex.Message}");
            }
        }

        public List<SpeedrunRecord> GetRecords() => records.ToList();

        public void RemoveRecord(Guid id)
        {
            records.RemoveAll(r => r.Id == id);
            SaveRecords();
        }

        public SpeedrunState GetState() => currentState;

        public TimeSpan GetCurrentTime() => currentTime;

        public int GetCountdownRemaining() => countdownRemaining;
    }
}
