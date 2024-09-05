using System.Diagnostics;
using ImGuiNET;
using System.Numerics;

namespace WahJumps.Windows
{
    public class StrangeHousingTab
    {
        public void Draw()
        {
            if (ImGui.BeginTabItem("Strange Housing"))
            {
                // Add some spacing at the top
                ImGui.Spacing();

                // Center-align the welcome message
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.85f, 0.75f, 0.9f, 1.0f)); // Light purple color for the text
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("A huge thank you to the Strange Housing staff and community for their efforts in building such a wonderful community!").X) / 2);
                ImGui.Text("A huge thank you to the Strange Housing staff and community for their efforts in building such a wonderful community!");
                ImGui.PopStyleColor();

                // Add a separator for visual separation
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Center-align and display the "Useful Links" section header
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.9f, 0.9f, 1.0f)); // Slightly lighter gray for section headers
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Useful Links:").X) / 2);
                ImGui.Text("Useful Links:");
                ImGui.PopStyleColor();

                // Add some more spacing
                ImGui.Spacing();

                // Center-align the ffxiv.ju.mp button
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Visit ffxiv.ju.mp").X) / 2);
                if (ImGui.Button("Visit ffxiv.ju.mp"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://ffxiv.ju.mp/",
                        UseShellExecute = true
                    });
                }

                // Add space between buttons
                ImGui.Spacing();

                // Center-align the Discord button with Discord colors
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.29f, 0.33f, 0.86f, 1.0f));  // Discord blue
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.39f, 0.43f, 0.96f, 1.0f));  // Lighter blue on hover
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.19f, 0.23f, 0.76f, 1.0f));  // Darker blue on active/clicked

                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Join Discord Community").X) / 2);
                if (ImGui.Button("Join Discord Community"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://discord.gg/6agVYe6xYk",
                        UseShellExecute = true
                    });
                }

                ImGui.PopStyleColor(3);  // Reset the button colors

                // Add space between buttons
                ImGui.Spacing();

                // Center-align the Jumping Guide button
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Jumping Guide").X) / 2);
                if (ImGui.Button("Jumping Guide"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://docs.google.com/document/d/1CrO9doADJAP1BbYq8uPAyFqzGU1fS4cemXat_YACtJI/edit",
                        UseShellExecute = true
                    });
                }

                // Add space between buttons
                ImGui.Spacing();

                // Center-align the Jump Puzzle Address Spreadsheet button
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Jump Puzzle Address Spreadsheet").X) / 2);
                if (ImGui.Button("Jump Puzzle Address Spreadsheet"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/edit?gid=1921920879#gid=1921920879",
                        UseShellExecute = true
                    });
                }

                // End the tab item
                ImGui.EndTabItem();
            }
        }
    }
}
