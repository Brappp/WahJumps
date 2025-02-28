// File: WahJumps/Data/SpeedrunRecord.cs
using System;
using System.Collections.Generic;

namespace WahJumps.Data
{
    public class SpeedrunRecord
    {
        public int PuzzleId { get; set; } // Can be -1 for custom puzzles
        public string PuzzleName { get; set; }
        public string World { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();
        public List<SplitCheckpoint> Splits { get; set; } = new List<SplitCheckpoint>();
        public bool IsCustomPuzzle { get; set; } = false;
        public Guid CustomPuzzleId { get; set; } = Guid.Empty; // For custom puzzles
        public Guid TemplateId { get; set; } = Guid.Empty; // Link to the template used

        // Unique identifier for each record
        public Guid Id { get; set; } = Guid.NewGuid();

        // Create a template from this record's splits
        public SplitTemplate CreateTemplate(string templateName = null)
        {
            var template = new SplitTemplate
            {
                Name = templateName ?? $"{PuzzleName} Template",
                PuzzleId = IsCustomPuzzle ? null : PuzzleId,
                PuzzleName = PuzzleName,
                IsCustomPuzzle = IsCustomPuzzle
            };

            if (IsCustomPuzzle)
            {
                template.PuzzleId = null;
                template.IsCustomPuzzle = true;
            }

            // Clone the splits (without timing data)
            foreach (var split in Splits)
            {
                template.Splits.Add(split.Clone());
            }

            return template;
        }
    }
}
