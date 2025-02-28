// File: WahJumps/Utilities/ImRaii.cs
using System;
using ImGuiNET;
using System.Numerics;

namespace WahJumps.Utilities
{
    /// <summary>
    /// Resource management pattern for ImGui to ensure proper Begin/End pairing
    /// </summary>
    public static class ImRaii
    {
        public class Table : IDisposable
        {
            private readonly bool _success;

            public Table(string id, int columns, ImGuiTableFlags flags = ImGuiTableFlags.None)
            {
                _success = ImGui.BeginTable(id, columns, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndTable();
            }
        }

        public class TabBar : IDisposable
        {
            private readonly bool _success;

            public TabBar(string id, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
            {
                _success = ImGui.BeginTabBar(id, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndTabBar();
            }
        }

        public class TabItem : IDisposable
        {
            private readonly bool _success;

            // Simple constructor - just the label - no close button
            public TabItem(string label)
            {
                // Don't provide a boolean reference - this hides the close button
                _success = ImGui.BeginTabItem(label);
            }

            // Constructor with ref parameter - for when tab closability is needed
            public TabItem(string label, ref bool open)
            {
                _success = ImGui.BeginTabItem(label, ref open);
            }

            // Constructor with flags only - for setting the TabItem behavior
            public TabItem(string label, ImGuiTabItemFlags flags)
            {
                bool dummy = true;  // Create a dummy that won't be closed
                _success = ImGui.BeginTabItem(label, ref dummy, flags);
            }

            // Constructor with both ref parameter and flags - for maximum flexibility
            public TabItem(string label, ref bool open, ImGuiTabItemFlags flags)
            {
                _success = ImGui.BeginTabItem(label, ref open, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndTabItem();
            }
        }

        public class Child : IDisposable
        {
            private readonly bool _success;

            public Child(string id, Vector2 size = default, bool border = false, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
            {
                _success = ImGui.BeginChild(id, size, border, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndChild();
            }
        }

        public class Popup : IDisposable
        {
            private readonly bool _success;

            public Popup(string id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
            {
                _success = ImGui.BeginPopup(id, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndPopup();
            }
        }

        public class StyleVar : IDisposable
        {
            private readonly int _count;

            public StyleVar(ImGuiStyleVar styleVar, Vector2 value)
            {
                ImGui.PushStyleVar(styleVar, value);
                _count = 1;
            }

            public StyleVar(ImGuiStyleVar styleVar, float value)
            {
                ImGui.PushStyleVar(styleVar, value);
                _count = 1;
            }

            public StyleVar(params (ImGuiStyleVar Idx, float Val)[] vars)
            {
                _count = vars.Length;
                foreach (var (idx, val) in vars)
                    ImGui.PushStyleVar(idx, val);
            }

            public StyleVar(params (ImGuiStyleVar Idx, Vector2 Val)[] vars)
            {
                _count = vars.Length;
                foreach (var (idx, val) in vars)
                    ImGui.PushStyleVar(idx, val);
            }

            public void Dispose()
            {
                ImGui.PopStyleVar(_count);
            }
        }

        public class StyleColor : IDisposable
        {
            private readonly int _count;

            public StyleColor(ImGuiCol idx, Vector4 color)
            {
                ImGui.PushStyleColor(idx, color);
                _count = 1;
            }

            public StyleColor(params (ImGuiCol, Vector4)[] colors)
            {
                foreach (var (idx, color) in colors)
                    ImGui.PushStyleColor(idx, color);
                _count = colors.Length;
            }

            public void Dispose()
            {
                ImGui.PopStyleColor(_count);
            }
        }

        public class Group : IDisposable
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

        public class Combo : IDisposable
        {
            private readonly bool _success;

            public Combo(string label, string previewValue)
            {
                _success = ImGui.BeginCombo(label, previewValue);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndCombo();
            }
        }

        public class TreeNode : IDisposable
        {
            private readonly bool _success;

            public TreeNode(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
            {
                _success = ImGui.TreeNodeEx(label, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.TreePop();
            }
        }

        public class Tooltip : IDisposable
        {
            public Tooltip()
            {
                ImGui.BeginTooltip();
            }

            public void Dispose()
            {
                ImGui.EndTooltip();
            }
        }
    }
}
