using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace WahJumps.Windows
{
    public class WorldSelectionSection
    {
        private readonly Dictionary<string, bool> selectedWorlds;

        public WorldSelectionSection(Dictionary<string, bool> selectedWorlds)
        {
            this.selectedWorlds = selectedWorlds;
        }

        public void Draw()
        {
            if (ImGui.CollapsingHeader("Select Worlds"))
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.15f, 0.15f, 0.15f, 1.0f));

                ImGui.BeginChild("WorldSelectionSection", new Vector2(ImGui.GetContentRegionAvail().X, 275), false);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.6f, 0.8f, 1.0f));
                ImGui.Text("Select Worlds");
                ImGui.PopStyleColor();
                ImGui.Separator();

                // Get datacenters information from WorldData
                var datacenters = GetWorldsByDatacenter();
                foreach (var dc in datacenters)
                {
                    ImGui.BeginGroup();
                    ImGui.TextColored(new Vector4(0.1f, 0.6f, 0.8f, 1.0f), dc.Key);
                    foreach (var world in dc.Value)
                    {
                        bool selected = selectedWorlds[world];
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 1.0f));
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        if (ImGui.Checkbox(world, ref selected))
                        {
                            selectedWorlds[world] = selected;
                        }
                        ImGui.PopStyleColor();
                    }
                    ImGui.EndGroup();
                    ImGui.SameLine();
                }

                ImGui.EndChild();
                ImGui.PopStyleColor();
            }
        }

        private Dictionary<string, List<string>> GetWorldsByDatacenter()
        {
            return new Dictionary<string, List<string>>
            {
                // NA
                { "Aether", new List<string> { "Adamantoise", "Cactuar", "Faerie", "Gilgamesh", "Jenova", "Midgardsormr", "Sargatanas", "Siren" }},
                { "Primal", new List<string> { "Behemoth", "Excalibur", "Exodus", "Famfrit", "Hyperion", "Lamia", "Leviathan", "Ultros" }},
                { "Crystal", new List<string> { "Balmung", "Brynhildr", "Coeurl", "Diabolos", "Goblin", "Malboro", "Mateus", "Zalera" }},
                { "Dynamis", new List<string> { "Cuchulainn", "Golem", "Halicarnassus", "Kraken", "Maduin", "Marilith", "Rafflesia", "Seraph" }},

                // EU
                { "Chaos", new List<string> { "Cerberus", "Louisoix", "Moogle", "Omega", "Phantom", "Ragnarok", "Sagittarius", "Spriggan" }},
                { "Light", new List<string> { "Alpha", "Lich", "Odin", "Phoenix", "Raiden", "Shiva", "Twintania", "Zodiark" }},

                // JP
                { "Elemental", new List<string> { "Aegis", "Atomos", "Carbuncle", "Garuda", "Gungnir", "Kujata", "Tonberry", "Typhon" }},
                { "Gaia", new List<string> { "Alexander", "Bahamut", "Durandal", "Fenrir", "Ifrit", "Ridill", "Tiamat", "Ultima" }},
                { "Mana", new List<string> { "Anima", "Asura", "Chocobo", "Hades", "Ixion", "Masamune", "Pandaemonium", "Titan" }},
                { "Meteor", new List<string> { "Belias", "Mandragora", "Ramuh", "Shinryu", "Unicorn", "Valefor", "Yojimbo", "Zeromus" }},

                // OCE
                { "Materia", new List<string> { "Bismarck", "Ravana", "Sephirot", "Sophia", "Zurvan" }}
            };
        }
    }
}
