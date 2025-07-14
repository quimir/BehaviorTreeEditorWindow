using System;
using System.Collections.Generic;
using Save.Serialization.Core.TypeConverter;
using UnityEngine;

namespace Save.Serialization.Storage.TypeConverter
{
    public class AnimatorTypeConverter : ComponentTypeConverter<Animator>
    {
        public override object Serialize(Animator value, ISerializationContext context)
        {
            var base_data=base.Serialize(value,context) as Dictionary<string, object>;
            if (base_data==null)
            {
                return null;
            }

            if (value.runtimeAnimatorController!=null)
            {
                base_data["controller"] = value.runtimeAnimatorController.name;
            }

            return base_data;
        }

        public override Animator Deserialize(object data, ISerializationContext context)
        {
            var result=base.Deserialize(data,context);
            if (result==null)
            {
                return null;
            }

            if (data is Dictionary<string, object> dict && dict.TryGetValue("controller", out var controllerObj))
            {
                var controller_name=controllerObj as string;
                if (!string.IsNullOrEmpty(controller_name) && result.runtimeAnimatorController!=null)
                {
                    // 验证控制器名称是否匹配
                    if (result.runtimeAnimatorController.name!=controller_name)
                    {
                        Debug.LogWarning($"Animator控制器名称不匹配：期望: {controller_name},实际{result.runtimeAnimatorController.name}");
                    }
                }
            }

            return result;
        }
    }
}
