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
                ImGui.Spacing(); 

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.85f, 0.75f, 0.9f, 1.0f)); 
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("A huge thank you to the Strange Housing staff and community for their efforts in building such a wonderful community!").X) / 2); // Center the text
                ImGui.Text("A huge thank you to the Strange Housing staff and community for their efforts in building such a wonderful community!");
                ImGui.PopStyleColor();

                ImGui.Spacing(); 

                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Useful Links:").X) / 2);
                ImGui.Text("Useful Links:");

                ImGui.Spacing(); 

                // ffxiv.ju.mp link
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Visit ffxiv.ju.mp").X) / 2);
                if (ImGui.Button("Visit ffxiv.ju.mp"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://ffxiv.ju.mp/",
                        UseShellExecute = true
                    });
                }

                ImGui.Spacing(); 

                // Discord Button with Discord colors
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.29f, 0.33f, 0.86f, 1.0f));  
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.39f, 0.43f, 0.96f, 1.0f));  
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.19f, 0.23f, 0.76f, 1.0f));  

                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Join Discord Community").X) / 2);
                if (ImGui.Button("Join Discord Community"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://discord.gg/6agVYe6xYk",
                        UseShellExecute = true
                    });
                }

                ImGui.PopStyleColor(3);  

                ImGui.Spacing(); 

                // Jumping Guide Google Doc link
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Jumping Guide").X) / 2);
                if (ImGui.Button("Jumping Guide"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://docs.google.com/document/d/1CrO9doADJAP1BbYq8uPAyFqzGU1fS4cemXat_YACtJI/edit",
                        UseShellExecute = true
                    });
                }

                ImGui.Spacing(); 

                // Jump Puzzle Address Spreadsheet link
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Jump Puzzle Address Spreadsheet").X) / 2);
                if (ImGui.Button("Jump Puzzle Address Spreadsheet"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/edit?gid=1921920879#gid=1921920879",
                        UseShellExecute = true
                    });
                }

                ImGui.Spacing(); 

                // Add LifeStream plugin requirement notice
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.8f, 0.4f, 1.0f)); // Use a yellowish color to highlight the note
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("The LifeStream plugin is required for the travel button to work").X) / 2);
                ImGui.TextWrapped("⚠️ The LifeStream plugin is required for the travel button to work");
                ImGui.PopStyleColor();

                ImGui.Spacing(); 

                // LifeStream GitHub repository link
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("LifeStream Plugin GitHub").X) / 2);
                if (ImGui.Button("LifeStream Plugin GitHub"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/NightmareXIV/Lifestream",
                        UseShellExecute = true
                    });
                }

                ImGui.Spacing(); 

                ImGui.EndTabItem();
            }
        }
    }
}
