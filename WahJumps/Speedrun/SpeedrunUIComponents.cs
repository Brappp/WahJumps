// File: WahJumps/Utilities/SpeedrunUiComponents.cs
using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using WahJumps.Data;

namespace WahJumps.Utilities
{
    // Renamed to avoid conflict with WahJumps.Windows.Components.UiComponents
    public static class SpeedrunUiComponents
    {
        // Helper method to draw a time display in mm:ss.ms format
        public static void DrawTimeDisplay(TimeSpan time, Vector4 color, float scale = 2.0f)
        {
            string timeText = $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";

            ImGui.PushStyleColor(ImGuiCol.Text, color);

            var textSize = ImGui.CalcTextSize(timeText);
            float windowWidth = ImGui.GetWindowWidth();

            ImGui.SetCursorPosX((windowWidth - textSize.X * scale) / 2);

            ImGui.SetWindowFontScale(scale);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);

            ImGui.PopStyleColor();
        }

        // Draw a time comparison between two times (for split comparisons)
        // Added 'ref' keyword to the comparison parameter to fix CS1620
        public static void DrawTimeComparison(TimeSpan current, ref TimeSpan? comparison, Vector4 aheadColor, Vector4 behindColor)
        {
            if (!comparison.HasValue) return;

            TimeSpan diff = current - comparison.Value;
            bool isAhead = diff.TotalMilliseconds < 0; // Negative means ahead (faster)

            // Format the time difference with a +/- sign
            string sign = isAhead ? "-" : "+";
            TimeSpan absDiff = isAhead ? diff.Negate() : diff;
            string diffText = $"{sign}{(int)absDiff.TotalMinutes:D2}:{absDiff.Seconds:D2}.{absDiff.Milliseconds / 10:D2}";

            // Use appropriate color based on ahead/behind
            ImGui.PushStyleColor(ImGuiCol.Text, isAhead ? aheadColor : behindColor);
            ImGui.Text(diffText);
            ImGui.PopStyleColor();
        }

        // Helper for drawing a split row in the splits display
        public static void DrawSplitRow(SplitCheckpoint split, int index, int currentSplitIndex, Vector4 completedColor, Vector4 pendingColor)
        {
            if (index < currentSplitIndex)
            {
                // Completed split
                ImGui.PushStyleColor(ImGuiCol.Text, completedColor);
                ImGui.Text(split.Name);
                ImGui.PopStyleColor();
            }
            else if (index == currentSplitIndex + 1) // Next split
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
                ImGui.Text($"► {split.Name}");
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, pendingColor);
                ImGui.Text(split.Name);
                ImGui.PopStyleColor();
            }
        }

        // Format a TimeSpan for display
        public static string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
        }

        // Draw a confirmation dialog
        public static bool DrawConfirmationDialog(string title, string message, string confirmText = "Yes", string cancelText = "No")
        {
            bool confirmed = false;

            // Create a dummy bool variable for the p_open parameter
            bool dummy = true;

            if (ImGui.BeginPopupModal(title, ref dummy, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text(message);
                ImGui.Separator();

                if (ImGui.Button(confirmText, new Vector2(120, 0)))
                {
                    confirmed = true;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button(cancelText, new Vector2(120, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            return confirmed;
        }

        // Draw a key-value table for record details or settings
        public static void DrawKeyValueTable(string id, Dictionary<string, string> items)
        {
            if (ImGui.BeginTable(id, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var item in items)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(item.Key);

                    ImGui.TableNextColumn();
                    ImGui.Text(item.Value);
                }

                ImGui.EndTable();
            }
        }

        // Draw a split template preview in a compact format
        public static void DrawTemplatePreview(SplitTemplate template, bool isSelected = false)
        {
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.2f, 0.8f, 0.2f, 1.0f));
            }

            ImGui.Text(template.Name);

            if (isSelected)
            {
                ImGui.PopStyleColor();
            }

            ImGui.SameLine();
            ImGui.TextDisabled($"({template.Splits.Count} splits)");

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"Template: {template.Name}");
                ImGui.Text($"Puzzle: {template.PuzzleName ?? "Generic"}");

                if (template.Splits.Count > 0)
                {
                    ImGui.Separator();
                    ImGui.Text("Splits:");

                    foreach (var split in template.Splits)
                    {
                        ImGui.BulletText(split.Name);
                    }
                }

                ImGui.EndTooltip();
            }
        }

        // Draw a button with tooltip
        public static bool DrawButtonWithTooltip(string label, string tooltip, Vector2? size = null)
        {
            bool clicked = size.HasValue ?
                ImGui.Button(label, size.Value) :
                ImGui.Button(label);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }

            return clicked;
        }

        // Draw a minimal mode toggle button
        public static bool DrawMinimalModeToggle(bool isMinimal)
        {
            bool result = isMinimal;

            if (isMinimal)
            {
                // Button to exit minimal mode
                if (ImGui.Button("▢"))
                {
                    result = false;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Exit minimal mode");
                }
            }
            else
            {
                // Button to enter minimal mode
                if (ImGui.Button("_"))
                {
                    result = true;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Enter minimal mode");
                }
            }

            return result;
        }

        // Draw a heading with a horizontal line
        public static void DrawHeading(string text, Vector4? color = null)
        {
            if (color.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, color.Value);
            }

            ImGui.Text(text);

            if (color.HasValue)
            {
                ImGui.PopStyleColor();
            }

            ImGui.Separator();
        }

        // Helper for combining puzzle type codes (moved from UiComponents to avoid conflict)
        public static string CombineCodes(string m, string e, string s, string p, string v, string j, string g, string l, string x)
        {
            List<string> codes = new List<string>();

            if (!string.IsNullOrEmpty(m)) codes.Add("M");
            if (!string.IsNullOrEmpty(e)) codes.Add("E");
            if (!string.IsNullOrEmpty(s)) codes.Add("S");
            if (!string.IsNullOrEmpty(p)) codes.Add("P");
            if (!string.IsNullOrEmpty(v)) codes.Add("V");
            if (!string.IsNullOrEmpty(j)) codes.Add("J");
            if (!string.IsNullOrEmpty(g)) codes.Add("G");
            if (!string.IsNullOrEmpty(l)) codes.Add("L");
            if (!string.IsNullOrEmpty(x)) codes.Add("X");

            return string.Join(", ", codes);
        }
    }

    // ImRaii helper class - moved from UiComponents to SpeedrunUiComponents
    public static class SpeedrunImRaii
    {
        public readonly struct Child : IDisposable
        {
            private readonly bool success;

            public Child(string id, Vector2 size, bool border = false, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
            {
                success = ImGui.BeginChild(id, size, border, flags);
            }

            public bool Success => success;

            public void Dispose()
            {
                ImGui.EndChild();
            }
        }

        public readonly struct TabBar : IDisposable
        {
            private readonly bool success;

            public TabBar(string id, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
            {
                success = ImGui.BeginTabBar(id, flags);
            }

            public bool Success => success;

            public void Dispose()
            {
                if (success)
                    ImGui.EndTabBar();
            }
        }

        public readonly struct StyleColor : IDisposable
        {
            private readonly int count;

            public StyleColor(params (ImGuiCol Idx, Vector4 Col)[] colors)
            {
                count = colors.Length;
                foreach (var (idx, col) in colors)
                    ImGui.PushStyleColor(idx, col);
            }

            public void Dispose()
            {
                ImGui.PopStyleColor(count);
            }
        }

        public readonly struct StyleVar : IDisposable
        {
            private readonly int count;

            public StyleVar(params (ImGuiStyleVar Idx, float Val)[] vars)
            {
                count = vars.Length;
                foreach (var (idx, val) in vars)
                    ImGui.PushStyleVar(idx, val);
            }

            public StyleVar(params (ImGuiStyleVar Idx, Vector2 Val)[] vars)
            {
                count = vars.Length;
                foreach (var (idx, val) in vars)
                    ImGui.PushStyleVar(idx, val);
            }

            public void Dispose()
            {
                ImGui.PopStyleVar(count);
            }
        }

        public readonly struct TreeNode : IDisposable
        {
            private readonly bool success;

            public TreeNode(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
            {
                success = ImGui.TreeNodeEx(label, flags);
            }

            public bool Success => success;

            public void Dispose()
            {
                if (success)
                    ImGui.TreePop();
            }
        }

        public readonly struct Group : IDisposable
        {
            public Group()
            {
                ImGui.BeginGroup();
            }

            public void Dispose()
            {
                ImGui.EndGroup();
            }
        }

        public readonly struct Combo : IDisposable
        {
            private readonly bool success;

            public Combo(string label, string previewValue, ImGuiComboFlags flags = ImGuiComboFlags.None)
            {
                success = ImGui.BeginCombo(label, previewValue, flags);
            }

            public bool Success => success;

            public void Dispose()
            {
                if (success)
                    ImGui.EndCombo();
            }
        }
    }
}
