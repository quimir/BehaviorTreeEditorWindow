using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using BehaviorTree.BehaviorTrees;
using BehaviorTree.Core;
using ExTools.Singleton;
using ExTools.Utillties;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Factory;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ExTools
{
    public class ExTool:SingletonWithLazy<ExTool>
    {
        // 缓冲字典，存储MemberInfo到LabelText的映射
        private static readonly Dictionary<MemberInfo, string> label_text_cache_ = new();

        /// <summary>
        /// 通过反射机制获取其的所有子类
        /// </summary>
        /// <param name="type">基类</param>
        /// <returns>所有的子类</returns>
        public List<Type> GetDerivedClasses(Type type)
        {
            var derived_classes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var t in assembly.GetTypes())
                if (t.IsClass && !t.IsAbstract && type.IsAssignableFrom(t)&&t.GetCustomAttribute<HideInDerivedClass>()==null)
                    derived_classes.Add(t);

            return derived_classes;
        }

        /// <summary>
        /// 获取树ID的辅助方法(通过反射获取私有字段)
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public string GetTreeId(IBehaviorTrees tree)
        {
            if (tree is ExtendableBehaviorTree base_bt)
            {
                // 使用反射获取私有的TreeId字段
                var field = typeof(ExtendableBehaviorTree).GetField("tree_id_",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null) return field.GetValue(base_bt) as string;
            }

            return null;
        }

        /// <summary>
        /// 解析LabelText的文本信息，使用Lambda表达式来准确捕获需求的成员所定义的LabelText文本。
        /// </summary>
        /// <param name="field_selector">需要捕获的成员，如果在该类当中没有定义该成员则会报错。</param>
        /// <typeparam name="T">成员类</typeparam>
        /// <typeparam name="TField">捕获成员的类型</typeparam>
        /// <returns>如果有该成员定义有LabelText那么则会返回其的文本，否则返回空</returns>
        /// <exception cref="ArgumentException">当Lambda表达式不是成员访问表达式的时候则抛出该错误</exception>
        public string GetLabelText<T, TField>(Expression<Func<T, TField>> field_selector)
        {
            if (field_selector.Body is not MemberExpression member_expr) throw new ArgumentException("表达式必须是成员访问表达式");

            var member_info = member_expr.Member;

            // 检查缓冲
            lock (label_text_cache_)
            {
                if (label_text_cache_.TryGetValue(member_info, out var cached_text)) return cached_text;

                // 如果缓冲中没有，则获取特性值并存储
                var label_attr = member_info.GetCustomAttribute<LabelTextAttribute>();
                var label_text = label_attr?.Text;
                label_text_cache_[member_info] = label_text;

                return label_text;
            }
        }

        // 预加载所有类型的LabelText到缓冲，可在应用启动时调用
        public void PreloadLabelText(Type type)
        {
            lock (label_text_cache_)
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                     BindingFlags.Instance))
                {
                    if (label_text_cache_.ContainsKey(field)) continue;
                    var attr = field.GetCustomAttribute<LabelTextAttribute>();
                    label_text_cache_[field] = attr?.Text;
                }

                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Instance))
                {
                    if (label_text_cache_.ContainsKey(prop)) continue;
                    var attr = prop.GetCustomAttribute<LabelTextAttribute>();
                    label_text_cache_[prop] = attr?.Text;
                }
            }
        }

        public bool CompareByValue<T>(T a, T b)
        {
            var serializer=SerializerCreator.Instance.Create(SerializerType.kJson, new SerializationSettings
            {
                PreserveReferences = true,
                TypeNameHandling = SerializationTypeNameHandling.kAuto
            });
            var bytes_1 = serializer.Serialize(a);
            var bytes_2 = serializer.Serialize(b);
            return bytes_1.Equals(bytes_2);
        }
    }

    public static class UnityThread
    {
        private static int main_thread_id_;

        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == main_thread_id_;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            main_thread_id_ = Thread.CurrentThread.ManagedThreadId;
        }
    }
}