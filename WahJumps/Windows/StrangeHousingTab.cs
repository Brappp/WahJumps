// File: WahJumps/Windows/StrangeHousingTab.cs
// Status: UPDATED - Using filled heart symbol

using ImGuiNET;
using WahJumps.Utilities;
using WahJumps.Windows.Components;
using System.Numerics;

namespace WahJumps.Windows
{
    public class StrangeHousingTab
    {
        public void Draw()
        {
            // Fixed TabItem usage with NoCloseWithMiddleMouseButton flag
            using var tabItem = new ImRaii.TabItem("Strange Housing");
            if (!tabItem.Success) return;

            // Thank you message in a nice card
            using (var thanksCard = new ImRaii.Child("ThankYouSection", new Vector2(-1, -1)))
            {
                float windowWidth = ImGui.GetWindowWidth();
                float textWidth;

                // Header
                using (var headerColor = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Secondary))
                {
                    textWidth = ImGui.CalcTextSize("Strange Housing Community").X;
                    ImGui.SetCursorPosX((windowWidth - textWidth) / 2);
                    ImGui.TextUnformatted("Strange Housing Community");
                }

                ImGui.Spacing();

                // Description
                textWidth = ImGui.CalcTextSize("Find and explore creative jump puzzles built by the community").X;
                ImGui.SetCursorPosX((windowWidth - textWidth) / 2);
                ImGui.TextUnformatted("Find and explore creative jump puzzles built by the community");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Thank you message (new)
                using (var thankYouColor = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
                {
                    ImGui.PushTextWrapPos(ImGui.GetWindowWidth() * 0.9f);
                    UiTheme.CenteredText("A huge thank you to the Strange Housing staff and community for their efforts in building such a wonderful community!");
                    ImGui.PopTextWrapPos();
                }

                ImGui.Spacing();
                ImGui.Spacing();

                // Links Section
                UiTheme.CenteredText("Useful Links", UiTheme.Primary);
                ImGui.Spacing();

                // Two-column layout for links
                float buttonWidth = windowWidth * 0.4f;
                float leftCol = (windowWidth - (buttonWidth * 2 + 20)) / 2;

                // Row 1
                ImGui.SetCursorPosX(leftCol);
                DrawLinkButton("Visit ffxiv.ju.mp", "https://ffxiv.ju.mp/", buttonWidth);

                ImGui.SameLine(leftCol + buttonWidth + 20);
                DrawDiscordButton("Join Discord", "https://discord.gg/6agVYe6xYk", buttonWidth);

                ImGui.Spacing();
                ImGui.Spacing();

                // Row 2
                ImGui.SetCursorPosX(leftCol);
                DrawLinkButton("Jumping Guide",
                    "https://docs.google.com/document/d/1CrO9doADJAP1BbYq8uPAyFqzGU1fS4cemXat_YACtJI/edit",
                    buttonWidth);

                ImGui.SameLine(leftCol + buttonWidth + 20);
                DrawLinkButton("Jump Puzzle Spreadsheet",
                    "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/edit?gid=1921920879#gid=1921920879",
                    buttonWidth);

                ImGui.Spacing();
                ImGui.Spacing();

                // LifeStream Notice - Updated without symbols
                ImGui.PushTextWrapPos(ImGui.GetWindowWidth() * 0.8f);
                UiTheme.CenteredText("The LifeStream plugin is required for travel buttons to work", UiTheme.Warning);
                ImGui.PopTextWrapPos();

                ImGui.Spacing();

                // Direct GitHub button for LifeStream
                ImGui.SetCursorPosX((windowWidth - buttonWidth) / 2);
                DrawLinkButton("Download LifeStream Plugin", "https://github.com/NightmareXIV/Lifestream", buttonWidth);

                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();

                // Signature with filled heart symbol (♥) - same as used in favorites
                using (var signatureColor = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Accent))
                {
                    // "Made with ♥" line
                    textWidth = ImGui.CalcTextSize("Made with ♥").X;
                    ImGui.SetCursorPosX((windowWidth - textWidth) / 2);
                    ImGui.TextUnformatted("Made with ♥");

                    // "wah" on next line
                    textWidth = ImGui.CalcTextSize("wah").X;
                    ImGui.SetCursorPosX((windowWidth - textWidth) / 2);
                    ImGui.TextUnformatted("wah");
                }
            }
        }

        private void DrawLinkButton(string label, string url, float width)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.12f, 0.15f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.17f, 0.21f, 0.28f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.25f, 0.30f, 0.4f, 1.0f));

            bool clicked = ImGui.Button($"{label}##link", new Vector2(width, 0));

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();

            if (clicked)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(url);
            }
        }

        private void DrawDiscordButton(string label, string url, float width)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);
            ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.DiscordPrimary);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiTheme.DiscordHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, UiTheme.DiscordActive);

            bool clicked = ImGui.Button($"{label}##discord", new Vector2(width, 0));

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();

            if (clicked)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(url);
            }
        }
    }
}
