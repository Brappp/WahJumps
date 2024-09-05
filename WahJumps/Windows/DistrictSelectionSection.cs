using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace WahJumps.Windows
{
    public class DistrictSelectionSection
    {
        private readonly Dictionary<string, bool> selectedDistricts;

        public DistrictSelectionSection(Dictionary<string, bool> selectedDistricts)
        {
            this.selectedDistricts = selectedDistricts;
        }

        public void Draw()
        {
            if (ImGui.CollapsingHeader("Select Districts"))
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.15f, 0.15f, 0.15f, 1.0f));

                ImGui.BeginChild("DistrictSelectionSection", new Vector2(ImGui.GetContentRegionAvail().X, 60), false);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.6f, 0.8f, 1.0f));
                ImGui.Text("Select Districts");
                ImGui.PopStyleColor();
                ImGui.Separator();

                foreach (var district in GetDistricts())
                {
                    bool selected = selectedDistricts[district];
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 1.0f));
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 5);
                    if (ImGui.Checkbox(district, ref selected))
                    {
                        selectedDistricts[district] = selected;
                    }
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                }

                ImGui.EndChild();
                ImGui.PopStyleColor();
            }
        }

        private List<string> GetDistricts()
        {
            return new List<string> { "Mist", "The Goblet", "The Lavender Beds", "Empyreum", "Shirogane" };
        }
    }
}
