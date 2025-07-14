using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree.Nodes;
using ExTools.Utillties;
using LogManager.Core;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Factory;

namespace BehaviorTree
{
    public static class BtToolEx
    {
        private static LogSpaceNode bt_space_ = new("BtToolEx");

        /// <summary>
        /// Creates a deep clone of the provided list of behavior tree nodes.
        /// </summary>
        /// <param name="nodes">The list of behavior tree nodes to be cloned.</param>
        /// <returns>A new list containing deep clones of the provided behavior tree nodes. Returns an empty list
        /// if the input list is null or empty.</returns>
        public static List<BtNodeBase> CloneData(this List<BtNodeBase> nodes)
        {
            if (nodes == null ||nodes.Count==0)
            {
                return new List<BtNodeBase>();
            }
            var serializer = SerializerCreator.Instance.Create
            (SerializerType.kJson,new SerializationSettings
            {
                PreserveReferences = true,
                TypeNameHandling = SerializationTypeNameHandling.kAuto
            });
            
            var node_json = serializer.Serialize(nodes);
            var data= serializer.Deserialize<List<BtNodeBase>>(node_json);

            if (data!=null)
            {
                var cloned_node_set = new HashSet<BtNodeBase>(data);

                foreach (var clone_data in data.Where(clone_data => clone_data != null))
                {
                    clone_data.Guild = Guid.NewGuid().ToString();

                    switch (clone_data)
                    {
                        case BtComposite composite:
                            composite.PostCloneRelink(cloned_node_set);
                            break;
                        case BtPrecondition precondition:
                            precondition.PostCloneRelink(cloned_node_set);
                            break;
                    }
                }
            }

            return data ?? new List<BtNodeBase>();
        }

        public static bool StringEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }
    }
}

