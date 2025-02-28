// File: WahJumps/Data/SplitCheckpoint.cs
using System;

namespace WahJumps.Data
{
    public class SplitCheckpoint
    {
        public string Name { get; set; }
        public TimeSpan Time { get; set; } // Total time when split was reached
        public TimeSpan? SplitTime { get; set; } // Time since previous split
        public bool IsCompleted { get; set; }
        public int Order { get; set; } // To maintain the correct order of splits

        public SplitCheckpoint()
        {
            Name = "New Split";
            Time = TimeSpan.Zero;
            SplitTime = null;
            IsCompleted = false;
            Order = 0;
        }

        public SplitCheckpoint(string name, int order)
        {
            Name = name;
            Time = TimeSpan.Zero;
            SplitTime = null;
            IsCompleted = false;
            Order = order;
        }

        // Clone method for creating templates
        public SplitCheckpoint Clone()
        {
            return new SplitCheckpoint
            {
                Name = this.Name,
                Order = this.Order,
                // Don't clone timing data
                Time = TimeSpan.Zero,
                SplitTime = null,
                IsCompleted = false
            };
        }
    }
}
