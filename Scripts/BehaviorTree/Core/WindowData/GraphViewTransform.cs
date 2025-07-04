using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviorTree.Core.WindowData
{
    /// <summary>
    /// Represents a transformation component for a graph view, including position, rotation, and scaling properties.
    /// </summary>
    public class GraphViewTransform : ITransform
    {
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 scale { get; set; }

        private Matrix4x4 stored_matrix_;

        private float[] matrix_elements_;

        public Matrix4x4 matrix
        {
            get
            {
                if (matrix_elements_ is { Length: 16 })
                    stored_matrix_ = new Matrix4x4
                    {
                        m00 = matrix_elements_[0], m01 = matrix_elements_[1], m02 = matrix_elements_[2],
                        m03 = matrix_elements_[3],
                        m10 = matrix_elements_[4], m11 = matrix_elements_[5], m12 = matrix_elements_[6],
                        m13 = matrix_elements_[7],
                        m20 = matrix_elements_[8], m21 = matrix_elements_[9], m22 = matrix_elements_[10],
                        m23 = matrix_elements_[11],
                        m30 = matrix_elements_[12], m31 = matrix_elements_[13], m32 = matrix_elements_[14],
                        m33 = matrix_elements_[15]
                    };

                return stored_matrix_;
            }
        }

        // 序列化前调用
        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            matrix_elements_ = new float[16];
            for (var i = 0; i < 16; i++) matrix_elements_[i] = stored_matrix_[i];
        }

        // 反序列后调用
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (matrix_elements_ is { Length: 16 })
                stored_matrix_ = new Matrix4x4
                {
                    m00 = matrix_elements_[0], m01 = matrix_elements_[1], m02 = matrix_elements_[2],
                    m03 = matrix_elements_[3],
                    m10 = matrix_elements_[4], m11 = matrix_elements_[5], m12 = matrix_elements_[6],
                    m13 = matrix_elements_[7],
                    m20 = matrix_elements_[8], m21 = matrix_elements_[9], m22 = matrix_elements_[10],
                    m23 = matrix_elements_[11],
                    m30 = matrix_elements_[12], m31 = matrix_elements_[13], m32 = matrix_elements_[14],
                    m33 = matrix_elements_[15]
                };
        }
    }
}
