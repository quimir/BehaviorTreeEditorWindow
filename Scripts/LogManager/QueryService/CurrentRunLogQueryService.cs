using System;
using System.Collections.Generic;
using LiteDB;
using LogManager.Core;
using LogManager.QueryService;

namespace Script.LogManager.QueryService
{
    public class CurrentRunLogQueryService : LogQueryService
    {
        private readonly Guid current_run_id_;
        private readonly BsonExpression run_id_filter_;
        
        public CurrentRunLogQueryService(string db_path, Guid current_run_id,string collection_name = "logs") 
            : base(db_path, collection_name)
        {
            if (current_run_id==Guid.Empty)
            {
                // 这个类的核心就是 RunId，所以必须提供
                throw new ArgumentException("Current Run ID cannot be empty.", nameof(current_run_id));
            }

            current_run_id_ = current_run_id;
            
            // 预先构建 RunId 的查询表达式 (假设字段是 Properties.RunId)
            // 使用 BsonValue 确保类型正确匹配
            run_id_filter_=Query.EQ("Properties.RunId",new BsonValue(current_run_id));
        }

        public override IEnumerable<BsonDocument> FindRaw(BsonExpression query = null, int skip = 0, int limit = Int32.MaxValue,
            string order_by_field = "Timestamp", bool ascending = false)
        {
            BsonExpression final_query = CombineWithRunIdFilter(query);
            
            return base.FindRaw(final_query, skip, limit, order_by_field, ascending);
        }

        public IEnumerable<LogEntry> GetAllCurrentRunLogs(int skip = 0, int limit = Int32.MaxValue,
            string order_by_field = "Timestamp", bool ascending = false)
        {
            return Find(null,skip,limit,order_by_field,ascending);
        }

        private BsonExpression CombineWithRunIdFilter(BsonExpression additional_query)
        {
            if (additional_query==null)
            {
                return run_id_filter_;
            }

            else
            {
                return Query.And(run_id_filter_, additional_query);
            }
        }
        
        /// <summary>
        /// 不支持基于当前 RunId 的任意删除。请使用基类的 Delete 方法进行全局删除。
        /// </summary>
        [Obsolete("Deleting arbitrary logs scoped to the current run is not supported. Use the base LogQueryService for global deletions.", true)]
        public new int Delete(BsonExpression query)
        {
            throw new NotSupportedException("Deleting arbitrary logs scoped to the current run is not supported.");
        }
        
        /// <summary>
        /// 删除旧日志是全局操作，不限于当前运行。请使用基类的 DeleteOlderThan 方法。
        /// </summary>
        [Obsolete("Retention policy applies globally, not just to the current run. Use the base LogQueryService.DeleteOlderThan.", true)]
        public new int DeleteOlderThan(DateTime cutoffDateUtc)
        {
            throw new NotSupportedException("Retention policy applies globally, not just to the current run.");
        }
    }
}
