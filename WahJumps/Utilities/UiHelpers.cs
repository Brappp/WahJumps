using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace WahJumps.Utilities
{
    public static class UiHelpers
    {
        private static readonly Vector4 CodeColor = new Vector4(0.55f, 0.70f, 0.86f, 1.0f);

        private static readonly Dictionary<string, string> CodeDescriptions = new()
        {
            { "M", "Mystery - Hard-to-find or maze-like paths" },
            { "E", "Emote - Requires emote interaction" },
            { "S", "Speed - Sprinting and time-based actions" },
            { "P", "Phasing - Furniture interactions that phase you" },
            { "V", "Void Jump - Requires jumping into void" },
            { "J", "Job Gate - Requires specific jobs" },
            { "G", "Ghost - Disappearances of furnishings" },
            { "L", "Logic - Logic-based puzzle solving" },
            { "X", "No Media - No streaming/recording allowed" }
        };

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Warning($"Failed to open URL '{url}': {ex.Message}");
            }
        }

        public static string CombineCodes(params string[] codes)
        {
            return string.Join(", ", codes.Where(c => !string.IsNullOrEmpty(c)));
        }

        public static int CountStars(string rating)
        {
            return string.IsNullOrEmpty(rating) ? 0 : rating.Count(c => c == '★');
        }

        public static int ConvertRatingToInt(string rating)
        {
            if (string.IsNullOrEmpty(rating)) return 0;

            int stars = CountStars(rating);
            if (stars > 0) return stars;

            switch (rating)
            {
                case "E": return 2;
                case "T": return 2;
                case "F": return 2;
                default: return 0;
            }
        }

        public static void RenderCodesWithTooltips(string codes)
        {
            if (string.IsNullOrEmpty(codes))
            {
                ImGui.TextColored(new Vector4(0.35f, 0.37f, 0.41f, 1.0f), "—");
                return;
            }

            ImGui.TextColored(CodeColor, codes);

            if (ImGui.IsItemHovered())
            {
                using var tooltip = new ImRaii.Tooltip();
                ImGui.Text("Puzzle Types:");
                ImGui.Separator();
                DrawCodeDescriptions(codes);
            }
        }

        public static void DrawCodeDescriptions(string codes)
        {
            string[] codeParts = codes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string code in codeParts)
            {
                string trimmedCode = code.Trim();
                if (CodeDescriptions.TryGetValue(trimmedCode, out string? description))
                {
                    ImGui.BulletText($"{trimmedCode}: {description}");
                }
            }
        }
    }
}
