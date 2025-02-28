// File: WahJumps/Data/SpeedrunManager.cs
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using CsvHelper;
using Newtonsoft.Json;
using CsvHelper.Configuration;
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

        // File paths
        private readonly string recordsFilePath;
        private readonly string templatesFilePath;
        private readonly string customPuzzlesFilePath;

        // Data collections
        private List<SpeedrunRecord> records = new List<SpeedrunRecord>();
        private List<SplitTemplate> splitTemplates = new List<SplitTemplate>();
        private List<CustomPuzzle> customPuzzles = new List<CustomPuzzle>();

        // Current state
        private SpeedrunState currentState = SpeedrunState.Idle;
        private DateTime startTime;
        private TimeSpan currentTime;
        private int countdownSeconds;
        private int countdownRemaining;
        private SpeedrunRecord currentRun;
        private JumpPuzzleData currentPuzzle;
        private SplitTemplate currentTemplate;
        private int currentSplitIndex = -1;
        private List<TimeSpan> previousSplitTimes = new List<TimeSpan>(); // For comparison

        // Settings
        public int DefaultCountdown { get; set; } = 3;
        public bool PlaySoundOnCountdown { get; set; } = true;
        public bool AutoSaveRecords { get; set; } = true;
        public bool ShowSplitComparison { get; set; } = true;

        // Properties
        public string RecordsPath => recordsFilePath;
        public string TemplatesPath => templatesFilePath;
        public string CustomPuzzlesPath => customPuzzlesFilePath;

        // Events
        public event Action<SpeedrunState> StateChanged;
        public event Action<TimeSpan> TimeUpdated;
        public event Action<int> CountdownTick;
        public event Action<SpeedrunRecord> RunCompleted;
        public event Action<SplitCheckpoint> SplitCompleted;
        public event Action<SplitTemplate> TemplateChanged;
        public event Action<CustomPuzzle> CustomPuzzleAdded;

        public SpeedrunManager(string configDirectory)
        {
            recordsFilePath = Path.Combine(configDirectory, "speedrun_records.csv");
            templatesFilePath = Path.Combine(configDirectory, "split_templates.json");
            customPuzzlesFilePath = Path.Combine(configDirectory, "custom_puzzles.json");

            LoadRecords();
            LoadTemplates();
            LoadCustomPuzzles();
        }

        #region Puzzle Management

        public void SetPuzzle(JumpPuzzleData puzzle)
        {
            currentPuzzle = puzzle;

            // Look for an existing template for this puzzle
            if (puzzle != null)
            {
                var puzzleId = puzzle.Id;
                var existingTemplate = splitTemplates.FirstOrDefault(t => t.PuzzleId == puzzleId);

                if (existingTemplate != null)
                {
                    SetTemplate(existingTemplate);
                }
                else
                {
                    // Create a default template
                    CreateDefaultTemplate(puzzle);
                }
            }
        }

        public JumpPuzzleData GetCurrentPuzzle()
        {
            return currentPuzzle;
        }

        public CustomPuzzle CreateCustomPuzzle(string name, string description = "", string creator = "")
        {
            var puzzle = new CustomPuzzle(name, description, creator);
            customPuzzles.Add(puzzle);
            SaveCustomPuzzles();
            CustomPuzzleAdded?.Invoke(puzzle);
            return puzzle;
        }

        public List<CustomPuzzle> GetCustomPuzzles()
        {
            return customPuzzles.ToList();
        }

        public void SetCustomPuzzle(CustomPuzzle puzzle)
        {
            if (puzzle != null)
            {
                currentPuzzle = puzzle.ToJumpPuzzleData();

                // Look for an existing template for this custom puzzle
                var existingTemplate = splitTemplates.FirstOrDefault(t =>
                    t.IsCustomPuzzle && t.PuzzleName == puzzle.Name);

                if (existingTemplate != null)
                {
                    SetTemplate(existingTemplate);
                }
                else
                {
                    // Create a default template
                    var template = new SplitTemplate(puzzle.Name + " Template")
                    {
                        PuzzleId = null,
                        PuzzleName = puzzle.Name,
                        IsCustomPuzzle = true
                    };

                    // Add a default finish split
                    template.Splits.Add(new SplitCheckpoint("Finish", 0));

                    SetTemplate(template);
                    splitTemplates.Add(template);
                    SaveTemplates();
                }
            }
        }

        public void UpdateCustomPuzzle(CustomPuzzle puzzle)
        {
            var existing = customPuzzles.FirstOrDefault(p => p.Id == puzzle.Id);
            if (existing != null)
            {
                int index = customPuzzles.IndexOf(existing);
                customPuzzles[index] = puzzle;
                SaveCustomPuzzles();
            }
        }

        public void RemoveCustomPuzzle(Guid id)
        {
            customPuzzles.RemoveAll(p => p.Id == id);
            SaveCustomPuzzles();

            // Also remove any templates for this custom puzzle
            splitTemplates.RemoveAll(t => t.IsCustomPuzzle && t.PuzzleName == customPuzzles.Find(p => p.Id == id)?.Name);
            SaveTemplates();
        }

        #endregion

        #region Template Management

        public void SetTemplate(SplitTemplate template)
        {
            currentTemplate = template;
            currentSplitIndex = -1;
            TemplateChanged?.Invoke(template);
        }

        public SplitTemplate GetCurrentTemplate()
        {
            return currentTemplate;
        }

        private void CreateDefaultTemplate(JumpPuzzleData puzzle)
        {
            var template = new SplitTemplate(puzzle.PuzzleName + " Template")
            {
                PuzzleId = puzzle.Id,
                PuzzleName = puzzle.PuzzleName
            };

            // Add a default finish split
            template.Splits.Add(new SplitCheckpoint("Finish", 0));

            SetTemplate(template);
            splitTemplates.Add(template);
            SaveTemplates();
        }

        public List<SplitTemplate> GetTemplates()
        {
            return splitTemplates.ToList();
        }

        public List<SplitTemplate> GetTemplatesForPuzzle(int puzzleId)
        {
            return splitTemplates.Where(t => t.PuzzleId == puzzleId).ToList();
        }

        public List<SplitTemplate> GetTemplatesForCustomPuzzle(string puzzleName)
        {
            return splitTemplates.Where(t => t.IsCustomPuzzle && t.PuzzleName == puzzleName).ToList();
        }

        public SplitTemplate CreateTemplate(string name, int? puzzleId = null, string puzzleName = null)
        {
            var template = new SplitTemplate(name, puzzleId, puzzleName);
            splitTemplates.Add(template);
            SaveTemplates();
            return template;
        }

        public void UpdateTemplate(SplitTemplate template)
        {
            var existing = splitTemplates.FirstOrDefault(t => t.Id == template.Id);
            if (existing != null)
            {
                int index = splitTemplates.IndexOf(existing);
                template.LastModified = DateTime.Now;
                splitTemplates[index] = template;

                if (currentTemplate?.Id == template.Id)
                {
                    currentTemplate = template;
                    TemplateChanged?.Invoke(template);
                }

                SaveTemplates();
            }
        }

        public void RemoveTemplate(Guid id)
        {
            splitTemplates.RemoveAll(t => t.Id == id);
            SaveTemplates();

            if (currentTemplate?.Id == id)
            {
                currentTemplate = null;
            }
        }

        public SplitTemplate DuplicateTemplate(Guid id)
        {
            var original = splitTemplates.FirstOrDefault(t => t.Id == id);
            if (original != null)
            {
                var copy = original.Clone();
                splitTemplates.Add(copy);
                SaveTemplates();
                return copy;
            }
            return null;
        }

        #endregion

        #region Speedrun Timer Control

        public void StartCountdown(Dictionary<string, string> customFields = null)
        {
            if (currentPuzzle == null) return;

            countdownSeconds = DefaultCountdown;
            countdownRemaining = countdownSeconds;
            startTime = DateTime.Now;

            // Initialize new run with current puzzle info
            currentRun = new SpeedrunRecord
            {
                PuzzleId = currentPuzzle.Id,
                PuzzleName = currentPuzzle.PuzzleName,
                World = currentPuzzle.World,
                Date = DateTime.Now,
                CustomFields = customFields ?? new Dictionary<string, string>()
            };

            // Setup for custom puzzles
            if (currentPuzzle.Id < 0 && currentPuzzle.CustomId != Guid.Empty)
            {
                currentRun.IsCustomPuzzle = true;
                currentRun.CustomPuzzleId = currentPuzzle.CustomId;
            }

            // Initialize splits from template if available
            if (currentTemplate != null)
            {
                currentRun.TemplateId = currentTemplate.Id;

                // Create clones of the template splits
                foreach (var templateSplit in currentTemplate.Splits.OrderBy(s => s.Order))
                {
                    currentRun.Splits.Add(new SplitCheckpoint
                    {
                        Name = templateSplit.Name,
                        Order = templateSplit.Order,
                        Time = TimeSpan.Zero,
                        SplitTime = null,
                        IsCompleted = false
                    });
                }
            }

            // Reset current split index
            currentSplitIndex = -1;

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
            currentSplitIndex = -1;
            StateChanged?.Invoke(currentState);
        }

        public void MarkSplit()
        {
            if (currentState != SpeedrunState.Running || currentRun == null || currentRun.Splits.Count == 0)
                return;

            // Find the next incomplete split
            currentSplitIndex++;
            if (currentSplitIndex >= currentRun.Splits.Count)
            {
                // All splits completed, stop the timer
                StopTimer();
                return;
            }

            var split = currentRun.Splits[currentSplitIndex];
            split.Time = currentTime;
            split.IsCompleted = true;

            // Calculate split time (difference from previous split)
            if (currentSplitIndex > 0)
            {
                var prevSplit = currentRun.Splits[currentSplitIndex - 1];
                split.SplitTime = currentTime - prevSplit.Time;
            }
            else
            {
                split.SplitTime = currentTime;
            }

            SplitCompleted?.Invoke(split);

            // If this was the last split, stop the timer
            if (currentSplitIndex == currentRun.Splits.Count - 1)
            {
                StopTimer();
            }
        }

        public SplitCheckpoint GetCurrentSplit()
        {
            if (currentRun == null || currentSplitIndex < 0 || currentSplitIndex >= currentRun.Splits.Count)
                return null;

            return currentRun.Splits[currentSplitIndex];
        }

        public SplitCheckpoint GetNextSplit()
        {
            if (currentRun == null || currentSplitIndex + 1 >= currentRun.Splits.Count)
                return null;

            return currentRun.Splits[currentSplitIndex + 1];
        }

        public List<SplitCheckpoint> GetCurrentSplits()
        {
            return currentRun?.Splits ?? new List<SplitCheckpoint>();
        }

        #endregion

        #region Timer Update Methods

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

        #region Record Management

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
                    csv.WriteField("IsCustomPuzzle");
                    csv.WriteField("CustomPuzzleId");
                    csv.WriteField("TemplateId");

                    // Split count for dynamic columns
                    int maxSplits = records.Any() ? records.Max(r => r.Splits.Count) : 0;
                    for (int i = 0; i < maxSplits; i++)
                    {
                        csv.WriteField($"Split_{i}_Name");
                        csv.WriteField($"Split_{i}_TimeSeconds");
                    }

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
                        csv.WriteField(record.IsCustomPuzzle);
                        csv.WriteField(record.CustomPuzzleId);
                        csv.WriteField(record.TemplateId);

                        // Write split data
                        var orderedSplits = record.Splits.OrderBy(s => s.Order).ToList();
                        for (int i = 0; i < maxSplits; i++)
                        {
                            if (i < orderedSplits.Count)
                            {
                                csv.WriteField(orderedSplits[i].Name);
                                csv.WriteField(orderedSplits[i].Time.TotalSeconds);
                            }
                            else
                            {
                                csv.WriteField(string.Empty);
                                csv.WriteField(string.Empty);
                            }
                        }

                        // Write custom fields
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

        public void LoadRecords()
        {
            if (!File.Exists(recordsFilePath))
                return;

            try
            {
                records.Clear();

                using (var reader = new StreamReader(recordsFilePath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null
                }))
                {
                    // Read headers
                    csv.Read();
                    csv.ReadHeader();

                    var headerRecord = csv.Context.Reader.HeaderRecord;

                    // Find split columns
                    var splitColumns = headerRecord
                        .Where(h => h.StartsWith("Split_"))
                        .ToList();

                    // Find custom field headers
                    var customFields = headerRecord
                        .Where(h => h.StartsWith("Custom_"))
                        .Select(h => h.Substring(7))
                        .ToList();

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

                        // Read IsCustomPuzzle and CustomPuzzleId if they exist
                        if (headerRecord.Contains("IsCustomPuzzle"))
                        {
                            record.IsCustomPuzzle = csv.GetField<bool>("IsCustomPuzzle");
                        }

                        if (headerRecord.Contains("CustomPuzzleId"))
                        {
                            string customIdStr = csv.GetField<string>("CustomPuzzleId");
                            if (Guid.TryParse(customIdStr, out Guid customId))
                            {
                                record.CustomPuzzleId = customId;
                            }
                        }

                        if (headerRecord.Contains("TemplateId"))
                        {
                            string templateIdStr = csv.GetField<string>("TemplateId");
                            if (Guid.TryParse(templateIdStr, out Guid templateId))
                            {
                                record.TemplateId = templateId;
                            }
                        }

                        // Process split data
                        var splitNames = splitColumns.Where(c => c.Contains("_Name")).OrderBy(c => c).ToList();
                        var splitTimes = splitColumns.Where(c => c.Contains("_TimeSeconds")).OrderBy(c => c).ToList();

                        for (int i = 0; i < splitNames.Count; i++)
                        {
                            string name = csv.GetField<string>(splitNames[i]);

                            if (!string.IsNullOrEmpty(name) && i < splitTimes.Count)
                            {
                                double timeSeconds = 0;
                                Double.TryParse(csv.GetField<string>(splitTimes[i]), out timeSeconds);

                                record.Splits.Add(new SplitCheckpoint
                                {
                                    Name = name,
                                    Time = TimeSpan.FromSeconds(timeSeconds),
                                    IsCompleted = true,
                                    Order = i
                                });
                            }
                        }

                        // Calculate split times
                        for (int i = 0; i < record.Splits.Count; i++)
                        {
                            if (i == 0)
                            {
                                record.Splits[i].SplitTime = record.Splits[i].Time;
                            }
                            else
                            {
                                record.Splits[i].SplitTime = record.Splits[i].Time - record.Splits[i - 1].Time;
                            }
                        }

                        // Read custom fields
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

        public List<SpeedrunRecord> GetRecordsForPuzzle(int puzzleId)
        {
            return records.Where(r => r.PuzzleId == puzzleId && !r.IsCustomPuzzle).ToList();
        }

        public List<SpeedrunRecord> GetRecordsForCustomPuzzle(Guid customPuzzleId)
        {
            return records.Where(r => r.IsCustomPuzzle && r.CustomPuzzleId == customPuzzleId).ToList();
        }

        public SpeedrunRecord GetPersonalBest(int puzzleId)
        {
            return records
                .Where(r => r.PuzzleId == puzzleId && !r.IsCustomPuzzle)
                .OrderBy(r => r.Time)
                .FirstOrDefault();
        }

        public SpeedrunRecord GetPersonalBestForCustomPuzzle(Guid customPuzzleId)
        {
            return records
                .Where(r => r.IsCustomPuzzle && r.CustomPuzzleId == customPuzzleId)
                .OrderBy(r => r.Time)
                .FirstOrDefault();
        }

        public void RemoveRecord(Guid id)
        {
            records.RemoveAll(r => r.Id == id);
            SaveRecords();
        }

        #endregion

        #region Templates & Custom Puzzles File Management

        private void SaveTemplates()
        {
            try
            {
                var json = JsonConvert.SerializeObject(splitTemplates, Formatting.Indented);
                File.WriteAllText(templatesFilePath, json);
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error saving split templates: {ex.Message}");
            }
        }

        private void LoadTemplates()
        {
            if (!File.Exists(templatesFilePath))
                return;

            try
            {
                var json = File.ReadAllText(templatesFilePath);
                splitTemplates = JsonConvert.DeserializeObject<List<SplitTemplate>>(json) ?? new List<SplitTemplate>();
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error loading split templates: {ex.Message}");
                splitTemplates = new List<SplitTemplate>();
            }
        }

        private void SaveCustomPuzzles()
        {
            try
            {
                var json = JsonConvert.SerializeObject(customPuzzles, Formatting.Indented);
                File.WriteAllText(customPuzzlesFilePath, json);
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error saving custom puzzles: {ex.Message}");
            }
        }

        private void LoadCustomPuzzles()
        {
            if (!File.Exists(customPuzzlesFilePath))
                return;

            try
            {
                var json = File.ReadAllText(customPuzzlesFilePath);
                customPuzzles = JsonConvert.DeserializeObject<List<CustomPuzzle>>(json) ?? new List<CustomPuzzle>();
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error loading custom puzzles: {ex.Message}");
                customPuzzles = new List<CustomPuzzle>();
            }
        }

        #endregion

        #region State Getters

        public SpeedrunState GetState() => currentState;

        public TimeSpan GetCurrentTime() => currentTime;

        public int GetCountdownRemaining() => countdownRemaining;

        public int GetCurrentSplitIndex() => currentSplitIndex;

        #endregion
    }
}
