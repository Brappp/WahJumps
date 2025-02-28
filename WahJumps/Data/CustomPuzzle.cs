// File: WahJumps/Data/CustomPuzzle.cs
using System;

namespace WahJumps.Data
{
    public class CustomPuzzle
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }
        public string Creator { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;

        // Optional fields to match JumpPuzzleData structure
        public string World { get; set; } = "Custom";
        public string Address { get; set; } = "N/A";
        public string Rating { get; set; } = "Custom";

        public CustomPuzzle()
        {
            Name = "New Custom Puzzle";
            Description = "";
            Creator = "";
        }

        public CustomPuzzle(string name, string description = "", string creator = "")
        {
            Name = name;
            Description = description;
            Creator = creator;
        }

        // Convert to JumpPuzzleData for compatibility with existing code
        public JumpPuzzleData ToJumpPuzzleData()
        {
            return new JumpPuzzleData
            {
                Id = -1, // Use negative ID to identify as custom
                PuzzleName = Name,
                Builder = Creator,
                World = World,
                Address = Address,
                Rating = Rating,
                GoalsOrRules = Description,
                CustomId = Id // Store the actual GUID in the CustomId field
            };
        }
    }
}
