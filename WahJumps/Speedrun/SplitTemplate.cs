// File: WahJumps/Data/SplitTemplate.cs
using System;
using System.Collections.Generic;

namespace WahJumps.Data
{
    public class SplitTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public int? PuzzleId { get; set; } // Can be null for generic templates
        public string PuzzleName { get; set; } // For reference
        public List<SplitCheckpoint> Splits { get; set; } = new List<SplitCheckpoint>();
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public bool IsCustomPuzzle { get; set; } = false;

        public SplitTemplate()
        {
            Name = "New Template";
        }

        public SplitTemplate(string name, int? puzzleId = null, string puzzleName = null)
        {
            Name = name;
            PuzzleId = puzzleId;
            PuzzleName = puzzleName;
        }

        // Clone method for creating a copy of this template
        public SplitTemplate Clone()
        {
            var clone = new SplitTemplate
            {
                Name = this.Name + " (Copy)",
                PuzzleId = this.PuzzleId,
                PuzzleName = this.PuzzleName,
                Created = DateTime.Now,
                LastModified = DateTime.Now,
                IsCustomPuzzle = this.IsCustomPuzzle
            };

            // Clone each split
            foreach (var split in this.Splits)
            {
                clone.Splits.Add(split.Clone());
            }

            return clone;
        }
    }
}
