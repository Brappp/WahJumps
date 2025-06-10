using System;

namespace WahJumps.Data
{
    // Extend the existing JumpPuzzleData class with properties needed for speedrunning
    public partial class JumpPuzzleData
    {
        // Additional properties for custom puzzles
        public Guid CustomId { get; set; } = Guid.Empty;
        public bool IsCustomPuzzle => Id < 0 && CustomId != Guid.Empty;
    }
}
