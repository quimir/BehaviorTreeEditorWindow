using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Script.LogManager;
using Script.Utillties;

namespace LogManager.Core
{
    /// <summary>
    /// Represents a hierarchical logging node that organizes log entries into a structured space.
    /// Each node can hold recent logs, manage child nodes, and define display properties.
    /// </summary>
    public class LogSpaceNode
    {
        /// <summary>
        /// 当前空间节点的名字
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 是否显示当前控制面板当中显示
        /// </summary>
        public bool CanDisplay;

        private readonly LogSpaceNodeList children_;

        /// <summary>
        /// 父节点
        /// </summary>
        public LogSpaceNode Parent { get; internal set; }

        /// <summary>
        /// 当前空间节点的子节点
        /// </summary>
        public LogSpaceNodeList Children => children_;

        /// <summary>
        /// 用于存储近期日志的有界并发队列.
        /// </summary>
        private readonly ConcurrentQueue<LogEntry> recent_logs_ = new();

        /// <summary>
        /// 节点别名，用于支持多路径访问同一个节点
        /// </summary>
        private readonly HashSet<string> aliases_ = new();

        /// <summary>
        /// 全局节点注册表，用于按路径快速查找节点
        /// </summary>
        private static readonly ConcurrentDictionary<string, LogSpaceNode> global_node_registry_ = new();

        /// <summary>
        /// 构建日志空间
        /// </summary>
        /// <param name="name">日志空间名称</param>
        public LogSpaceNode(string name = "")
        {
            Name = name;
            CanDisplay = true;

            // 立即初始化children_,解决集合初始化问题
            children_ = new LogSpaceNodeList(this);
        }

        /// <summary>
        /// 为节点添加别名，允许通过多个路径访问同一个节点
        /// </summary>
        /// <param name="alias">别名</param>
        public void AddAlias(string alias)
        {
            if (!string.IsNullOrWhiteSpace(alias)) aliases_.Add(alias);
        }
        
        /// <summary>
        /// 移除对应的别名（如果存在）
        /// </summary>
        /// <param name="alias">日志空间的别名</param>
        /// <returns>如果别名存在并且成功移除返回true否则false</returns>
        public bool RemoveAlias(string alias)
        {
            return aliases_.Remove(alias);
        }

        /// <summary>
        /// 检查节点是否匹配指定的名称（包括别名）
        /// </summary>
        /// <param name="name">要匹配的名称</param>
        /// <returns>是否匹配</returns>
        public bool MatchesName(string name)
        {
            return Name == name || aliases_.Contains(name);
        }

        /// <summary>
        /// 注册节点到全局注册表，支持多路径访问
        /// </summary>
        /// <param name="path">注册路径</param>
        public void RegisterGlobalPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path)) global_node_registry_[path] = this;
        }
        
        /// <summary>
        /// 从全局注册表移除路径
        /// </summary>
        /// <param name="path">注册路径</param>
        /// <returns>成功移除返回true，否则false</returns>
        public static bool UnregisterGlobalPath(string path)
        {
            return global_node_registry_.TryRemove(path, out _);
        }

        /// <summary>
        /// 从全局注册表获取节点
        /// </summary>
        public static LogSpaceNode GetGlobalNode(string path)
        {
            return global_node_registry_.GetValueOrDefault(path);
        }

        /// <summary>
        /// 获取当前内存中保留的近期日志条目数。
        /// </summary>
        public int RecentLogsCount => recent_logs_.Count;

        /// <summary>
        /// 获取此节点内存中保留的近期日志快照（只读副本）。适用于 UI 显示。数量受 _maxRecentLogs 限制。
        /// </summary>
        public IEnumerable<LogEntry> RecentLogs => recent_logs_.ToList();

        /// <summary>
        /// (内部使用) 向此节点的近期日志缓冲区添加一条日志。
        /// 如果超出容量限制，会自动移除最旧的条目。
        /// 此方法应由 BaseViewLogManager 调用。
        /// </summary>
        /// <param name="entry">要添加的日志条目。</param>
        /// <param name="max_recent_logs">最多可以承载的日志条目</param>
        internal void AddRecentLog(LogEntry entry, int max_recent_logs)
        {
            recent_logs_.Enqueue(entry);

            // 当队列超出大小时，移除最旧的条目
            while (recent_logs_.Count > max_recent_logs && recent_logs_.TryDequeue(out _))
            {
                // 已经交给TryDeque进行处理，这里不需要另外添加
            }
        }

        /// <summary>
        /// 清空此节点内存中的近期日志缓冲区。
        /// 不影响已持久化的日志。
        /// </summary>
        public void ClearRecentLogBuffer()
        {
            while (recent_logs_.TryDequeue(out _))
            {
            }
        }

        #region 字符串还原系统

        /// <summary>
        /// 从路径字符串还原或创建LogSpaceNode结构
        /// </summary>
        /// <param name="path_string">路径字符串，如 "BehaviourTreeWindows.TreeView.NodeView"</param>
        /// <param name="root_node">根节点，如果为null则创建新的根节点</param>
        /// <returns>路径末端的节点</returns>
        public static LogSpaceNode FromPathString(string path_string, LogSpaceNode root_node=null)
        {
            if (string.IsNullOrWhiteSpace(path_string))
            {
                return root_node;
            }
            
            // 首先尝试从全局注册获取
            var global_node = GetGlobalNode(path_string);
            if (global_node!=null)
            {
                return global_node;
            }

            var parts = path_string.Split(FixedValues.kPathSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length==0)
            {
                return root_node;
            }

            LogSpaceNode current_node;

            // 如果没有提供根节点，创建一个
            if (root_node==null)
            {
                current_node = new LogSpaceNode(parts[0]);
                if (parts.Length==1)
                {
                    return current_node;
                }
            }
            else
            {
                current_node = root_node;
            }
            
            // 从第一个部分开始(如果rootNode为null)或从头开始(如果rootNode不为null)
            int start_index = root_node == null ? 1 : 0;

            for (int i = start_index; i < parts.Length; i++)
            {
                var part= parts[i];
                var child_node = current_node.Children.FirstOrDefault(c => c.MatchesName(part));

                if (child_node==null)
                {
                    child_node = new LogSpaceNode(part);
                    current_node.Children.Add(child_node);
                }

                current_node = child_node;
            }

            return current_node;
        }

        /// <summary>
        /// 从多个路径字符串批量还原节点结构
        /// </summary>
        /// <param name="pathStrings">路径字符串数组</param>
        /// <param name="sharedRoot">共享的根节点，如果为null则每个路径创建独立的树结构</param>
        /// <returns>所有末端节点的字典，键为路径，值为节点</returns>
        public static Dictionary<string, LogSpaceNode> FromPathStrings(string[] pathStrings, 
            LogSpaceNode sharedRoot = null)
        {
            var result = new Dictionary<string, LogSpaceNode>();

            if (sharedRoot != null)
            {
                // 使用共享根节点 - 所有路径都添加到同一个根节点下
                foreach (var pathString in pathStrings)
                {
                    if (string.IsNullOrWhiteSpace(pathString))
                        continue;

                    var node = sharedRoot.GetOrCreateNodeByPath(pathString);
                    result[pathString] = node;
                }
            }
            else
            {
                // 不使用共享根节点 - 每个路径创建独立的树结构
                foreach (var pathString in pathStrings)
                {
                    if (string.IsNullOrWhiteSpace(pathString))
                        continue;

                    var node = FromPathString(pathString);
                    result[pathString] = node;
                }
            }

            return result;
        }

       /// <summary>
       /// 根据需求的节点返回该节点的根节点
       /// </summary>
       /// <param name="node">需求节点</param>
       /// <returns>需求节点的根节点</returns>
        public static LogSpaceNode GetRootNode(LogSpaceNode node)
        {
            if (node == null) return null;
        
            var current = node;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current;
        }
        
        /// <summary>
        /// 将节点结构序列化为路径字符串数组
        /// </summary>
        /// <param name="rootNode">根节点</param>
        /// <param name="includeIntermediateNodes">是否包含中间节点路径</param>
        /// <returns>所有路径的字符串数组</returns>
        public static string[] ToPathStrings(LogSpaceNode rootNode, bool includeIntermediateNodes = false)
        {
            if (rootNode == null)
                return Array.Empty<string>();

            var paths = new List<string>();
            CollectPaths(rootNode, "", paths, includeIntermediateNodes);
            return paths.ToArray();
        }

        private static void CollectPaths(LogSpaceNode node, string currentPath, List<string> paths, bool includeIntermediate)
        {
            var nodePath = string.IsNullOrEmpty(currentPath) 
                ? node.Name 
                : $"{currentPath}{FixedValues.kPathSeparator}{node.Name}";

            // 如果是叶子节点或者要包含中间节点
            if (node.Children.Count == 0 || includeIntermediate)
            {
                if (!string.IsNullOrEmpty(nodePath))
                    paths.Add(nodePath);
            }

            // 递归处理子节点
            foreach (var child in node.Children)
            {
                CollectPaths(child, nodePath, paths, includeIntermediate);
            }
        }

        #endregion

        #region 转换相关操作

        /// <summary>
        /// 根据指定的路径字符串，在当前节点下查找或创建子节点层级
        /// 支持别名匹配
        /// </summary>
        public LogSpaceNode GetOrCreateNodeByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return this;

            var parts = path.Split(FixedValues.kPathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var currentNode = this;

            foreach (var part in parts)
            {
                var childNode = currentNode.Children.FirstOrDefault(c => c.MatchesName(part));
                if (childNode == null)
                {
                    childNode = new LogSpaceNode(part);
                    currentNode.Children.Add(childNode);
                }

                currentNode = childNode;
            }

            return currentNode;
        }

        /// <summary>
        /// 获取当前节点的完整路径字符串
        /// </summary>
        /// <param name="stopAtParentName">需要截断的日志空间名称.
        /// <code>例如：TreeView.NodeView.Node,stopAtParentName=NodeView则返回TreeView.NodeView</code>
        /// </param>
        /// <returns>当前节点的完整路径字符串</returns>
        public string GetFullPath(string stopAtParentName = null)
        {
            if (Parent == null || (stopAtParentName != null && Parent.Name == stopAtParentName))
                return Name;

            var pathParts = new LinkedList<string>();
            var currentNode = this;

            while (currentNode != null)
            {
                if (stopAtParentName != null && currentNode.Name == stopAtParentName)
                    if (currentNode != this && pathParts.Any())
                        break;

                if (currentNode.Parent == null && string.IsNullOrEmpty(currentNode.Name))
                    if (pathParts.Any())
                        break;

                if (!string.IsNullOrEmpty(currentNode.Name) || currentNode.Parent == null)
                    pathParts.AddFirst(currentNode.Name);

                if (currentNode.Parent == null ||
                    (stopAtParentName != null && currentNode.Parent.Name == stopAtParentName))
                    break;

                currentNode = currentNode.Parent;
            }

            return string.Join(FixedValues.kPathSeparator, pathParts.Where(p => !string.IsNullOrEmpty(p)));
        }

        #endregion

        #region 子节点相关操作

        /// <summary>
        /// 为当前的日志空间添加下一层的日志空间，并将该日志空间进行返回
        /// </summary>
        /// <param name="name">日志空间的名称</param>
        /// <returns>根据日志空间名称所创建的日志空间</returns>
        public LogSpaceNode AddChild(string name)
        {
            var child = new LogSpaceNode(name);
            Children.Add(child);
            return child;
        }

        /// <summary>
        /// 创建节点的链式构建器，返回父节点用于继续构建
        /// </summary>
        /// <param name="name">新的日志空间名称</param>
        /// <returns>父节点日志空间</returns>
        public LogSpaceNode AddChildAndReturn(string name)
        {
            AddChild(name);
            return this;
        }

        #endregion
        
        
        #region 测试部分
        
        /// <summary>
        /// 获取树结构的字符串表示
        /// </summary>
        public string GetTreeStructureString(int indent = 0)
        {
            var sb = new StringBuilder();
            var indentStr = new string(' ', indent * 2);
        
            sb.AppendLine($"{indentStr}{Name} (日志: {RecentLogsCount})");
        
            if (aliases_.Count > 0)
            {
                sb.AppendLine($"{indentStr}  别名: [{string.Join(", ", aliases_)}]");
            }

            foreach (var child in Children)
            {
                sb.Append(child.GetTreeStructureString(indent + 1));
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return $"LogSpaceNode(Name: {Name}, Path: {GetFullPath()}, Children: {Children.Count})";
        }
        
        #endregion
    }

    public class LogSpaceNodeList : List<LogSpaceNode>
    {
        private readonly LogSpaceNode owner_;

        public LogSpaceNodeList(LogSpaceNode owner)
        {
            owner_ = owner ?? throw new ArgumentNullException(nameof(owner), "LogSpaceNodeList owner cannot be null.");
        }

        /// <summary>
        /// 将节点添加到其中并且将添加节点的父节点指向自身。
        /// </summary>
        /// <param name="node">需要添加的节点</param>
        public new void Add(LogSpaceNode node)
        {
            if (node != null)
            {
                // 默认为覆写
                node.Parent = owner_;
                base.Add(node);
            }
        }

        /// <summary>
        /// 将节点数组添加到其中，其会过滤节点为null的可能性。并且将添加节点的父节点指向自身
        /// </summary>
        /// <param name="nodes">需要被添加的节点数组</param>
        public new void AddRange(IEnumerable<LogSpaceNode> nodes)
        {
            if (nodes == null) return;

            var space_nodes = nodes as LogSpaceNode[] ?? nodes.ToArray();
            var log_space_nodes = space_nodes.ToList(); // 转换为List处理
            foreach (var node in log_space_nodes)
                if (node != null)
                    node.Parent = owner_;

            // 过滤掉null节点后天机
            base.AddRange(log_space_nodes.Where(n => n != null));
        }

        /// <summary>
        /// 移除某项节点
        /// </summary>
        /// <param name="node">需要被移除的节点</param>
        /// <returns>如果节点存在，并且移除时候没有发生错误则在清除父节点引用之后返回true，否则返回false</returns>
        public new bool Remove(LogSpaceNode node)
        {
            if (node == null || !base.Remove(node)) return false;
            node.Parent = null; // 清除父节点引用
            return true;
        }
    }
}