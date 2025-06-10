using System.Collections.Generic;
using WahJumps.Models;

namespace WahJumps.Data
{
    public static class StaticInfoData
    {
        public static List<InfoData> GetInfoData()
        {
            return new List<InfoData>
            {
                // Empty rows and section headers
                new InfoData { Section = "", Key = "", Value1 = "", Value2 = "", Value3 = "" },
                
                // Difficulty Ratings section
                new InfoData { Section = "", Key = "Difficulty Ratings: Setting General Expectations", Value1 = "", Value2 = "", Value3 = "" },
                new InfoData { Section = "", Key = "Ratings are designed to give the challenger a general idea of what to expect, and won't always coincide with personal experience: sometimes it will feel harder and sometimes it will feel easier.", Value1 = "", Value2 = "", Value3 = "" },
                new InfoData { Section = "", Key = "Rating", Value1 = "Original System Rating", Value2 = "Star Diagram and Explanation", Value3 = "Square-Enix Equivalent" },
                new InfoData { Section = "", Key = "1★", Value1 = "Beginner", Value2 = "★☆☆☆☆ - Designed to be as easy as they come, with pure Skill focus and light application of other Sub-types; mostly everyone can clear", Value3 = "Cliffhanger GATEs; Fall of Belah'dia & Falling City of Nym GATEs" },
                new InfoData { Section = "", Key = "2★", Value1 = "Medium", Value2 = "★★☆☆☆ - Offers a solid challenge to most players, possibly simple for veteran jumpers; more complex mechanics and jumps present", Value3 = "Kugane Tower; Sylphstep GATE" },
                new InfoData { Section = "", Key = "3★", Value1 = "Hard", Value2 = "★★★☆☆ - \"Party Finder Bounty\" difficulty, extremely tough for most players with full range of mechanics possible; still a challenge for vets", Value3 = "Second half of Moonfire Tower (Only available during event)" },
                new InfoData { Section = "", Key = "4★", Value1 = "Satan", Value2 = "★★★★☆ - An extremely broad difficulty that encompasses the most difficult courses; veterans will find a vast range of difficulties here", Value3 = "Very top of Moonfire Tower (\"toothpick\" section) is borderline 4★ (without using bomb platforms near the top)" },
                new InfoData { Section = "", Key = "5★", Value1 = "God", Value2 = "★★★★★ - Reserved for the hardest of the hard; designed to obliterate challengers", Value3 = "Square-Enix isn't sadistic enough to make these" },
                new InfoData { Section = "", Key = "P", Value1 = "Training", Value2 = "☆☆☆☆☆ - A puzzle meant to instruct or introduce certain techniques, or provide a space to practice these techniques", Value3 = "" },
                new InfoData { Section = "", Key = "E", Value1 = "Event", Value2 = "☆☆☆☆☆ - A puzzle built specifically for an event, usually only open a few days at a time", Value3 = "" },
                new InfoData { Section = "", Key = "T", Value1 = "Temp", Value2 = "☆☆☆☆☆ - A puzzle that is temporary or only open for a limited time; may close permanently or be replaced with another build eventually", Value3 = "" },
                new InfoData { Section = "", Key = "F", Value1 = "In Flux", Value2 = "☆☆☆☆☆ - A puzzle that undergoes considerable, gradual change rather than remaining static like most puzzles", Value3 = "" },
                
                // Empty rows
                new InfoData { Section = "", Key = "", Value1 = "", Value2 = "", Value3 = "" },
                
                // Sub-type Keys section
                new InfoData { Section = "", Key = "Sub-type Keys: Know What Skillset to Bring", Value1 = "", Value2 = "", Value3 = "" },
                new InfoData { Section = "", Key = "Sub-types can seem overwhelming at first, but they can provide information about what skillset you'll need to bring for success. Please note that higher difficulty puzzles may not always reveal needed techniques!", Value1 = "", Value2 = "", Value3 = "" },
                new InfoData { Section = "", Key = "Code", Value1 = "Element", Value2 = "Means the puzzle:", Value3 = "More Info" },
                new InfoData { Section = "", Key = "M", Value1 = "Mystery", Value2 = "has hard-to-find or maze-like paths, furniture combinations with hard-to-solve interactions; tricky, misleading, or deceptive elements", Value3 = "M+ can signify Mystery complex enough to greatly affect the difficulty rating" },
                new InfoData { Section = "", Key = "E", Value1 = "Emote", Value2 = "has points where /sit or /doze interactions with furnishings are required to cross gaps or walls; these points can be hidden", Value3 = "E+ indicates that emotes will trigger furniture movement (\"slides\"), potentially locking you out and forcing a re-entry to reset" },
                new InfoData { Section = "", Key = "S", Value1 = "Speed", Value2 = "makes you wait on Sprint cooldown due to early or multiple long jumps, only applies when jumps are near/beyond max Peloton range", Value3 = "Combat this by bringing extra speed buffs such as Expedient (Scholar) or Moon Flute (Blue Mage)" },
                new InfoData { Section = "", Key = "P", Value1 = "Phasing", Value2 = "uses furnishings you need to run straight into until they let you pass through them; Phasing is often banned so check puzzle rules!", Value3 = "P+ requires Jump Phasing (\"Forbidden Tech\") to prevent phasing through what you're standing on" },
                new InfoData { Section = "", Key = "V", Value1 = "Void Jump", Value2 = "has vertical gaps that can only be crossed using a void jump and are required to complete the puzzle", Value3 = "A void jump is performed by running straight over the void for a second before jumping underneath your desired landing" },
                new InfoData { Section = "", Key = "J", Value1 = "Job Gate", Value2 = "has gaps that can only be crossed using specific job abilities, i.e. En Avant, Elusive Jump, Aetherial Manipulation, Hell's Ingress/Egress, etc", Value3 = "J+ requires a specific job such as Blue Mage for Co-op puzzles, or when only certain job abilities will navigate gaps" },
                new InfoData { Section = "", Key = "G", Value1 = "Ghost", Value2 = "has furnishings that disappear and reset in a predetermined order", Value3 = "G+ has multiple stages of Ghost that will disappear and reset in predetermined orders" },
                new InfoData { Section = "", Key = "L", Value1 = "Logic", Value2 = "employs the above mechanics in unconventional ways that redefine what a jump puzzle truly is; can be viewed as a modifier for the other tags", Value3 = "L+ requires deep understanding of techniques that may be used together to create significant obstacles to solve" },
                new InfoData { Section = "", Key = "X", Value1 = "No Media", Value2 = "is subject to a no streaming/recording request from the builder for any number of reasons", Value3 = "Some builders prefer to keep a low profile or keep their puzzles mysterious, please respect their wishes!" },
                
                // Empty rows
                new InfoData { Section = "", Key = "", Value1 = "", Value2 = "", Value3 = "" },
                
                // Other Information section
                new InfoData { Section = "", Key = "Other Information", Value1 = "", Value2 = "", Value3 = "" },
                new InfoData { Section = "", Key = "Some terms may sound familiar to community veterans, but can often be unknown or misunderstood by newcomers.", Value1 = "", Value2 = "", Value3 = "" },
                new InfoData { Section = "", Key = "", Value1 = "Term", Value2 = "Explanation", Value3 = "More Info" },
                new InfoData { Section = "", Key = "", Value1 = "No media tag", Value2 = "Some builders prefer to not have their puzzles streamed or have videos posted", Value3 = "Notated by a red X on addresses, ask for more info available in our Discord" },
                new InfoData { Section = "", Key = "", Value1 = "Return to Entrance", Value2 = "In main menu: Social > Housing > Return to Front Door/Chamber Door", Value3 = "Useful for when you get stuck or lost" },
                new InfoData { Section = "", Key = "", Value1 = "Goals/Rules", Value2 = "Some puzzles have conditions or rules to provide the builder's intended experience", Value3 = "Check the room greeting or the puzzle directory listing" },
                new InfoData { Section = "", Key = "", Value1 = "Bonus Stages", Value2 = "Some puzzles have extra conditions to alter the primary experience or add difficulty", Value3 = "Check the message book, room greeting, or puzzle directory listing" },
                new InfoData { Section = "", Key = "", Value1 = "Friend Teleport", Value2 = "Adding a builder or FC member to your friend list to use Estate Teleportation command when right-clicking that player's name", Value3 = "Useful for gaining access to Shirogane/Empyreum, bypasses MSQ requirements; restricted to ward you teleported to" },
                new InfoData { Section = "", Key = "", Value1 = "Housing cube", Value2 = "What a house or room looks like from outside once you break through the walls or ceiling", Value3 = "" },
                new InfoData { Section = "", Key = "", Value1 = "The Void", Value2 = "The black space outside of the housing cube", Value3 = "" },
                new InfoData { Section = "", Key = "", Value1 = "\"Over the Void\"", Value2 = "When there are no surfaces (furnishing or housing cube) below you or the furnishing you're standing on", Value3 = "You must be over the void to perform a void jump" },
                new InfoData { Section = "", Key = "", Value1 = "Slides", Value2 = "Client-side furnishing movement caused by /sit and /doze, as well as returning to chamber door and being raised", Value3 = "Can also be triggered by builders upon exiting the furnishing placement menu" },
                new InfoData { Section = "", Key = "", Value1 = "Ghosting", Value2 = "Client-side furnishing disappearance, LOD setting must be off to cause this to function properly (console works by default)", Value3 = "System Config > Graphics Settings > General > un-check \"use low-detail models on distant objects\" (off by default on PS4/PS5)" },
                
                // Empty rows
                new InfoData { Section = "", Key = "", Value1 = "", Value2 = "", Value3 = "" },
                
                // Puzzle Accessibility section
                new InfoData { Section = "", Key = "Puzzle Accessibility", Value1 = "", Value2 = "", Value3 = "" },
                new InfoData { Section = "Having a huge list of addresses is no good without if you don't know how to find them!", Key = "", Value1 = "", Value2 = "", Value3 = "" },
                new InfoData { Section = "", Key = "", Value1 = "District", Value2 = "Main City Aethernet Access Conditions", Value3 = "More Info" },
                new InfoData { Section = "", Key = "", Value1 = "Goblet", Value2 = "\"Where the Heart Is (The Goblet)\" found in Western Thanalan (Lv 5), Accessible on foot from level 2", Value3 = "Exit Ul'dah Steps of Nald via Gate of the Sultana (Western Thanalan), Head south from Scorpion Crossing" },
                new InfoData { Section = "", Key = "", Value1 = "Lavender Beds", Value2 = "\"Where the Heart Is (The Lavender Beds)\" found in Central Shroud (Lv 5), Accessible on foot from level 2", Value3 = "Exit New Gridania via Blue Badger Gate (Central Shroud), Head south from Bentbranch Meadows to the ferry" },
                new InfoData { Section = "", Key = "", Value1 = "Mist", Value2 = "\"Where the Heart Is (Mist)\" found in Lower La Noscea (Lv 5), Accessible on foot from level 2", Value3 = "Exit Limsa Upper Decks via Tempest Gate (Lower La Noscea), Head northeast towards Red Rooster Stead" },
                new InfoData { Section = "", Key = "", Value1 = "Shirogane", Value2 = "\"I Dream of Shirogane\" found in Kugane (Lv 61)", Value3 = "Requires completion of \"By the Grace of Lord Lolorito\" (Patch 4.0) or friend teleport" },
                new InfoData { Section = "", Key = "", Value1 = "Empyreum", Value2 = "\"Ascending to Empyreum\" found in Ishgard (Lv 60)", Value3 = "Requires completion of \"Litany of Peace\" (Patch 3.3) or friend teleport" },
                new InfoData { Section = "", Key = "", Value1 = "Apartment Wing", Value2 = "Wing 1: apartments in same instance as plots 1-30; Wing 2: apartments in same instance as plots 31-60 (subdivision)", Value3 = "Example: Take aetheryte to Topmast for wing 1, to Topmast Subdivision for wing 2" }
            };
        }
    }
} 