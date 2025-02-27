// File: WahJumps/Utilities/ImRaii.cs
// Status: FINAL FIX - Fixed ref keyword error

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

            // Simple constructor - just the label
            public TabItem(string label)
            {
                // This overload doesn't support flags directly, so we use a dummy bool
                bool dummyOpen = true;
                _success = ImGui.BeginTabItem(label, ref dummyOpen, ImGuiTabItemFlags.NoCloseWithMiddleMouseButton);
            }

            // Constructor with ref parameter
            public TabItem(string label, ref bool open)
            {
                _success = ImGui.BeginTabItem(label, ref open, ImGuiTabItemFlags.NoCloseWithMiddleMouseButton);
            }

            // Constructor with ref parameter and flags
            public TabItem(string label, ref bool open, ImGuiTabItemFlags flags)
            {
                flags |= ImGuiTabItemFlags.NoCloseWithMiddleMouseButton;
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
    }
}
