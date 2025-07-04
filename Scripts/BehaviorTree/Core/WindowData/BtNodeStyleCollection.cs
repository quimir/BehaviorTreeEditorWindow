using System;
using System.Collections.Generic;
using Save.Serialization;
using Script.BehaviorTree.Save;

namespace BehaviorTree.Core.WindowData
{
    /// <summary>
    /// Represents a collection of node styles associated with a behavior tree.
    /// This collection allows conversion between a dictionary of node GUIDs and their styles and vice versa.
    /// </summary>
    [Serializable]
    public class BtNodeStyleCollection
    {
        [CustomSerialize]
        public class StyleEntry
        {
            public string NodeGuid;
            public BtNodeStyle Style;
        }

        public List<StyleEntry> Entries { get; } = new();

        /// <summary>
        /// Converts a dictionary mapping node GUIDs to their corresponding BtNodeStyle objects into a BtNodeStyleCollection.
        /// </summary>
        /// <param name="dictionary">
        /// A dictionary where the keys represent the node GUIDs (strings) and the values are BtNodeStyle objects representing their associated styles.
        /// </param>
        /// <returns>
        /// A BtNodeStyleCollection containing style entries derived from the specified dictionary.
        /// </returns>
        public static BtNodeStyleCollection FromDictionary(Dictionary<string, BtNodeStyle> dictionary)
        {
            BtNodeStyleCollection collection = new BtNodeStyleCollection();
        
            foreach (var pair in dictionary)
            {
                collection.Entries.Add(new StyleEntry 
                { 
                    NodeGuid = pair.Key, 
                    Style = pair.Value 
                });
            }
        
            return collection;
        }

        /// <summary>
        /// Converts the specified BtNodeStyleCollection to a dictionary mapping node GUIDs to their associated BtNodeStyles.
        /// </summary>
        /// <param name="collection">
        /// The BtNodeStyleCollection containing the node styles to be converted.
        /// </param>
        /// <returns>
        /// A dictionary where the keys are node GUIDs (strings) and the values are BtNodeStyle objects representing the styles associated with those nodes.
        /// </returns>
        public static Dictionary<string, BtNodeStyle> ToDictionary(BtNodeStyleCollection collection)
        {
            Dictionary<string, BtNodeStyle> dictionary = new Dictionary<string, BtNodeStyle>();
        
            if (collection is { Entries: not null })
            {
                foreach (var entry in collection.Entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.NodeGuid) && entry.Style != null)
                    {
                        // 避免重复键
                        if (!dictionary.ContainsKey(entry.NodeGuid))
                        {
                            dictionary.Add(entry.NodeGuid, entry.Style);
                        }
                    }
                }
            }
        
            return dictionary;
        }

        /// <summary>
        /// Converts the specified BtNodeStyleCollection to a dictionary mapping node GUIDs to their associated BtNodeStyles.
        /// </summary>
        /// <returns>
        /// A dictionary where the keys are node GUIDs (strings) and the values are BtNodeStyle objects representing the styles associated with those nodes.
        /// </returns>
        public Dictionary<string, BtNodeStyle> ToDictionary()
        {
            return ToDictionary(this);
        }
    }
}
