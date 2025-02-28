// File: WahJumps/Data/SpeedrunRecord.cs
using System;
using System.Collections.Generic;

namespace WahJumps.Data
{
    public class SpeedrunRecord
    {
        public int PuzzleId { get; set; }
        public string PuzzleName { get; set; }
        public string World { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();

        // Unique identifier for each record
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
