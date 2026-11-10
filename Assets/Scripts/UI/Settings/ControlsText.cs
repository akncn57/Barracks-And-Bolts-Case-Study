namespace BarracksAndBolts.UI.Settings
{
    /// <summary>The list on the Controls page. Kept in code next to the input bindings it describes, not in a prefab.</summary>
    public static class ControlsText
    {
        // Keys in the left column, what they do from the tab stop on. Wide enough for the longest key.
        private const string Tab = "<pos=44%>";
        private const string KeyColor = "#FFE2A8";
        private const string HeaderColor = "#6B4E33";

        public static readonly string Body = string.Join("\n",
            Header("Camera"),
            Row("W A S D, arrows", "Move the camera"),
            Row("Mouse at the edge", "Move the camera"),
            Row("Middle mouse drag", "Move the camera"),
            Row("Mouse wheel", "Zoom"),
            Row("Home", "Look at the Castle"),
            Gap(),
            Header("Selection"),
            Row("Left click", "Select a unit or building"),
            Row("Left drag", "Select your units in a box"),
            Row("Ctrl + 1-9", "Save the selection as a group"),
            Row("1-9", "Select the group"),
            Row("1-9 twice", "Look at the group"),
            Row(".", "Next idle villager"),
            Gap(),
            Header("Orders"),
            Row("Right click", "Move, attack, gather, build"),
            Row("F, then left click", "Attack Move"),
            Row("X", "Stop"),
            Row("H", "Hold Position"),
            Row("Delete", "Remove what is selected"),
            Gap(),
            Header("Building"),
            Row("Left click", "Place"),
            Row("Shift + left click", "Place and keep placing"),
            Row("Right click, Esc", "Cancel"),
            Gap(),
            Header("Game"),
            Row("P", "Pause"),
            Row("+ / -", "Game speed"),
            Row("Esc", "Close a window, open settings"),
            Row("F1", "This list"));

        private static string Header(string text)
        {
            return $"<size=115%><b><color={HeaderColor}>{text}</color></b></size>";
        }

        private static string Row(string keys, string action)
        {
            return $"<color={KeyColor}>{keys}</color>{Tab}{action}";
        }

        // A short empty line between groups, smaller than a full one.
        private static string Gap()
        {
            return "<size=50%> </size>";
        }
    }
}
