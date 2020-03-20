using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Synchronizer
{
    /// <summary>
    /// 同步器
    /// </summary>
    /// <typeparam name="TParam">请求参数类型</typeparam>
    /// <typeparam name="TResult">返回参数类型</typeparam>
    public class Synchronizer<TParam, TResult>
    {
        /// <summary>
        /// 请求方法
        /// </summary>
        private Action<TParam> m_action;

        /// <summary>
        /// 请求参数特征选择器
        /// </summary>
        private Func<TParam, string> m_paramKeySelector;

        /// <summary>
        /// 响应结果特征选择器
        /// </summary>
        private Func<TResult, string> m_resultKeySelector;

        /// <summary>
        /// 同步事件
        /// </summary>
        private ConcurrentDictionary<string, ManualResetEvent> m_syncEvents = new ConcurrentDictionary<string, ManualResetEvent>();

        /// <summary>
        /// 响应参数
        /// </summary>
        private ConcurrentDictionary<string, TResult> m_results = new ConcurrentDictionary<string, TResult>();

        /// <summary>
        /// 创建一个同步器
        /// </summary>
        /// <param name="action">请求方法</param>
        /// <param name="paramKeySelector">请求参数特征选择器</param>
        /// <param name="resultKeySelector">响应结果特征选择器</param>
        /// <remarks>相同特征的请求参数和响应结果将被同步</remarks>
        public Synchronizer(Action<TParam> action, Func<TParam, string> paramKeySelector, Func<TResult, string> resultKeySelector)
        {
            m_action = action;
            m_paramKeySelector = paramKeySelector;
            m_resultKeySelector = resultKeySelector;
        }

        /// <summary>
        /// 同步等待请求响应
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public SyncStatus GetResult(TParam param, out TResult result, int millisecondsTimeout)
        {
            SyncStatus syncStatus = SyncStatus.Failed;

            result = default(TResult);

            var key = m_paramKeySelector(param);

            var syncEvent = new ManualResetEvent(false);
            if (m_syncEvents.TryAdd(key, syncEvent))
            {
                m_action(param);
                if (syncEvent.WaitOne(millisecondsTimeout))
                {
                    if (m_results.TryRemove(key, out result))
                    {
                        syncStatus = SyncStatus.Successful;
                    }
                }
                else
                {
                    syncStatus = SyncStatus.Timeout;
                }

                m_syncEvents.TryRemove(key, out _);
            }

            return syncStatus;
        }

        /// <summary>
        /// 请求响应通知
        /// </summary>
        /// <param name="result"></param>
        public void SetResult(TResult result)
        {
            string key = m_resultKeySelector(result);
            if (m_syncEvents.TryGetValue(key, out ManualResetEvent syncEvent))
            {
                m_results.TryAdd(key, result);
                syncEvent.Set();
            }
        }
    }
}
