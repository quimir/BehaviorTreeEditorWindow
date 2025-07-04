using System;
using System.Collections.Generic;
using UnityEngine;

namespace Editor.View.BtWindows.Core
{
    public enum TreePersistenceType
    {
        kPersistent,
        kTemporary
    }

    /// <summary>
    /// Represents the state of a window in the Behavior Tree Editor.
    /// Contains properties for the unique identifier of the window, associated tree information,
    /// position of the window, persistence type, and serialized tree data.
    /// </summary>
    [Serializable]
    public class WindowState
    {
        public string WindowId;
        public string AssociatedTreeId;
        public TreePersistenceType PersistenceType;

        [TextArea(3, 10)] public string SerializedTreeData;
    }

    [Serializable]
    public class WindowStateList
    {
        public List<WindowState> States;
    }
}