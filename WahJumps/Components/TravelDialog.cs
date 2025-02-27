// File: WahJumps/Windows/Components/TravelDialog.cs

using ImGuiNET;
using System;
using System.Numerics;
using WahJumps.Data;
using WahJumps.Utilities;

namespace WahJumps.Windows.Components
{
    public class TravelDialog
    {
        private bool isOpen = false;
        private JumpPuzzleData targetPuzzle = null;
        private Action<string> onTravel;
        private Action onCancel;

        // Dialog UI state
        private float dialogOpacity = 0.0f;
        private float dialogWidth = 500;
        private float dialogHeight = 250;
        private float targetOpacity = 0.0f;
        private bool isAnimating = false;

        // Animation timing
        private float fadeSpeed = 5.0f;

        public TravelDialog(Action<string> onTravel, Action onCancel)
        {
            this.onTravel = onTravel;
            this.onCancel = onCancel;
        }

        public void Open(JumpPuzzleData puzzle)
        {
            targetPuzzle = puzzle;
            isOpen = true;
            targetOpacity = 1.0f;
            isAnimating = true;
        }

        public void Close()
        {
            targetOpacity = 0.0f;
            isAnimating = true;
        }

        public void Draw()
        {
            if (!isOpen && !isAnimating) return;

            // Handle animation
            if (isAnimating)
            {
                if (targetOpacity > dialogOpacity)
                {
                    dialogOpacity += ImGui.GetIO().DeltaTime * fadeSpeed;
                    if (dialogOpacity >= targetOpacity)
                    {
                        dialogOpacity = targetOpacity;
                        isAnimating = false;
                    }
                }
                else
                {
                    dialogOpacity -= ImGui.GetIO().DeltaTime * fadeSpeed;
                    if (dialogOpacity <= 0)
                    {
                        dialogOpacity = 0;
                        isAnimating = false;
                        isOpen = false;
                        return;
                    }
                }
            }

            // Darken background
            ImGui.SetNextWindowBgAlpha(0.0f);
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);

            ImGuiWindowFlags overlayFlags =
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoNav |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoInputs;

            if (ImGui.Begin("##overlay", ref isOpen, overlayFlags))
            {
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(
                    Vector2.Zero,
                    ImGui.GetIO().DisplaySize,
                    ImGui.GetColorU32(new Vector4(0, 0, 0, 0.5f * dialogOpacity))
                );
            }
            ImGui.End();

            // Main dialog
            ImGui.SetNextWindowPos(
                new Vector2(
                    (ImGui.GetIO().DisplaySize.X - dialogWidth) * 0.5f,
                    (ImGui.GetIO().DisplaySize.Y - dialogHeight) * 0.5f
                )
            );
            ImGui.SetNextWindowSize(new Vector2(dialogWidth, 0));
            ImGui.SetNextWindowBgAlpha(dialogOpacity);

            ImGuiWindowFlags dialogFlags =
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.AlwaysAutoResize;

            if (ImGui.Begin("Travel Confirmation", ref isOpen, dialogFlags))
            {
                if (targetPuzzle != null)
                {
                    // Title and description
                    using (var titleColor = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
                    {
                        ImGui.SetWindowFontScale(1.2f);
                        UiTheme.CenteredText($"Travel to {targetPuzzle.PuzzleName}?");
                        ImGui.SetWindowFontScale(1.0f);
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Puzzle information
                    ImGui.Text("Destination Details:");
                    ImGui.Spacing();

                    // FIX: Use StyleVar for indentation only, no manual Indent/Unindent
                    using (var indentStyle = new ImRaii.StyleVar(ImGuiStyleVar.IndentSpacing, 20.0f))
                    {
                        // Apply indentation
                        ImGui.Indent();

                        ImGui.Text("World:");
                        ImGui.SameLine();
                        ImGui.TextColored(UiTheme.Primary, targetPuzzle.World);

                        ImGui.Text("Address:");
                        ImGui.SameLine();
                        ImGui.TextColored(UiTheme.Primary, targetPuzzle.Address);

                        ImGui.Text("Builder:");
                        ImGui.SameLine();
                        ImGui.TextColored(UiTheme.Primary, targetPuzzle.Builder);

                        ImGui.Text("Rating:");
                        ImGui.SameLine();
                        ImGui.TextColored(RatingToColor(targetPuzzle.Rating), targetPuzzle.Rating);

                        // Reset indentation
                        ImGui.Unindent();
                    }

                    ImGui.Spacing();

                    // Warning if LifeStream is needed
                    using (var warning = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Warning))
                    {
                        UiTheme.CenteredText("The LifeStream plugin is required for travel to work.");
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Buttons
                    float buttonWidth = 120;
                    float totalButtonWidth = buttonWidth * 2 + 20; // 2 buttons + spacing
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - totalButtonWidth) * 0.5f);

                    using (var colors = new ImRaii.StyleColor(ImGuiCol.Button, UiTheme.Success))
                    {
                        if (ImGui.Button("Travel Now", new Vector2(buttonWidth, 0)) && targetPuzzle != null)
                        {
                            // Format travel command and execute
                            string travelCommand = FormatTravelCommand(targetPuzzle);
                            onTravel?.Invoke(travelCommand);
                            Close();
                        }
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
                    {
                        Close();
                        onCancel?.Invoke();
                    }
                }
            }
            ImGui.End();
        }

        private string FormatTravelCommand(JumpPuzzleData puzzle)
        {
            var world = puzzle.World;
            var address = puzzle.Address;

            if (address.Contains("Room"))
            {
                // Handle FC room, just remove the room information
                address = address.Split("Room")[0].Trim();
            }
            else if (address.Contains("Apartment"))
            {
                // Split the address for apartment cases
                var parts = address.Split("Apartment");
                var apartmentPart = parts[1].Trim();  // Extract the apartment number
                address = parts[0].Trim();  // Keep the ward and wing part

                // Handle Wing logic for subdivisions
                if (address.Contains("Wing 2"))
                {
                    // Replace Wing 2 with "subdivision"
                    address = address.Replace("Wing 2", "subdivision").Trim();
                    address = $"{address} Apartment {apartmentPart}";
                }
                else if (address.Contains("Wing 1"))
                {
                    // Remove "Wing 1" (it can be ignored)
                    address = address.Replace("Wing 1", "").Trim();
                    address = $"{address} Apartment {apartmentPart}";
                }
                else
                {
                    // No wings, keep it simple
                    address = $"{address} Apartment {apartmentPart}";
                }
            }

            // Return the formatted command
            return $"/travel {world} {address}";
        }

        private Vector4 RatingToColor(string rating)
        {
            switch (rating)
            {
                case "1★":
                    return new Vector4(0.0f, 0.8f, 0.0f, 1.0f); // Green
                case "2★":
                    return new Vector4(0.0f, 0.6f, 0.9f, 1.0f); // Blue
                case "3★":
                    return new Vector4(0.9f, 0.8f, 0.0f, 1.0f); // Yellow
                case "4★":
                    return new Vector4(1.0f, 0.5f, 0.0f, 1.0f); // Orange
                case "5★":
                    return new Vector4(0.9f, 0.0f, 0.0f, 1.0f); // Red
                default:
                    return new Vector4(0.8f, 0.8f, 0.8f, 1.0f); // Gray
            }
        }
    }
}
