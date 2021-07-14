using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Synchronizer
{
    /// <summary>
    /// 异步接口同步器
    /// </summary>
    /// <typeparam name="TRequestInput">请求参数</typeparam>
    /// <typeparam name="TRequestOutput">请求结果</typeparam>
    /// <typeparam name="TResponse">异步结果</typeparam>
    /// <typeparam name="TKey">同步键类型</typeparam>
    public class RequestSynchronizer<TRequestInput, TRequestOutput, TResponse, TKey>
    {
        /// <summary>
        /// 请求方法
        /// </summary>
        private Func<TRequestInput, TRequestOutput> m_RequestFunc;

        /// <summary>
        /// 请求过滤器
        /// </summary>
        private Func<TRequestInput, TRequestOutput, bool> m_RequestFilter;

        /// <summary>
        /// 请求键选择器
        /// </summary>
        private Func<TRequestInput, TRequestOutput, TKey> m_RequestKeySelectorWithRequestOutput;

        /// <summary>
        /// 请求键选择器
        /// </summary>
        private Func<TRequestInput, TKey> m_RequestKeySelectorWithoutRequestOutput;

        /// <summary>
        /// 异步结果键选择器
        /// </summary>
        private Func<TResponse, TKey> m_ResponseKeySelector;

        /// <summary>
        /// 同步上下文
        /// </summary>
        private ConcurrentDictionary<TKey, SyncContext<TRequestInput, TRequestOutput, TResponse>> m_SyncContexts = new ConcurrentDictionary<TKey, SyncContext<TRequestInput, TRequestOutput, TResponse>>();

        /// <summary>
        /// 同步请求
        /// </summary>
        /// <param name="requestInput">请求参数</param>
        /// <param name="requestOutput">请求结果</param>
        /// <param name="response">异步结果</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>同步结果</returns>
        private delegate SyncStatus SyncRequestDelegate(TRequestInput requestInput, out TRequestOutput requestOutput, out TResponse response, TimeSpan timeout);

        /// <summary>
        /// 同步请求
        /// </summary>
        private SyncRequestDelegate m_SyncRequestDelegate;

        /// <summary>
        /// 请求响应
        /// </summary>
        /// <param name="response"></param>
        private delegate void FeedResponseDelegate(TResponse response);

        /// <summary>
        /// 请求响应
        /// </summary>
        private FeedResponseDelegate m_FeedResponseDelegate;

        /// <summary>
        /// 创建一个同步器
        /// </summary>
        /// <param name="requestFunc">请求方法</param>
        /// <param name="requestFilter">请求过滤器</param>
        /// <param name="requestKeySelector">请求键选择器</param>
        /// <param name="responseKeySelector">异步结果键选择器</param>
        /// <remarks>
        /// 请求过滤器requestFilter返回true表示是有效的请求，否则请求无效，不同步异步结果。
        /// 请求键选择器返回的键与异步结果键选择器返回的键相等表示同步成功，否则同步失败。
        /// </remarks>
        public RequestSynchronizer(
            Func<TRequestInput, TRequestOutput> requestFunc,
            Func<TRequestInput, TRequestOutput, bool> requestFilter,
            Func<TRequestInput, TRequestOutput, TKey> requestKeySelector,
            Func<TResponse, TKey> responseKeySelector
            )
        {
            m_RequestFunc = requestFunc;
            m_RequestFilter = requestFilter;
            m_RequestKeySelectorWithRequestOutput = requestKeySelector;
            m_ResponseKeySelector = responseKeySelector;

            m_SyncRequestDelegate = SyncRequestWithRequestOutput;
            m_FeedResponseDelegate = FeedResponseWithRequestOutput;
        }

        /// <summary>
        /// 创建一个同步器
        /// </summary>
        /// <param name="requestFunc">请求方法</param>
        /// <param name="requestFilter">请求过滤器</param>
        /// <param name="requestKeySelector">请求键选择器</param>
        /// <param name="responseKeySelector">异步结果键选择器</param>
        /// <remarks>
        /// 请求过滤器requestFilter返回true表示是有效的请求，否则请求无效，不同步异步结果。
        /// 请求键选择器返回的键与异步结果键选择器返回的键相等表示同步成功，否则同步失败。
        /// </remarks>
        public RequestSynchronizer(
            Func<TRequestInput, TRequestOutput> requestFunc,
            Func<TRequestInput, TRequestOutput, bool> requestFilter,
            Func<TRequestInput, TKey> requestKeySelector,
            Func<TResponse, TKey> responseKeySelector
            )
        {
            m_RequestFunc = requestFunc;
            m_RequestFilter = requestFilter;
            m_RequestKeySelectorWithoutRequestOutput = requestKeySelector;
            m_ResponseKeySelector = responseKeySelector;

            m_SyncRequestDelegate = SyncRequestWithoutRequestOutput;
            m_FeedResponseDelegate = FeedResponseWithoutRequestOutput;
        }

        /// <summary>
        /// 同步请求
        /// </summary>
        /// <param name="requestInput">请求参数</param>
        /// <param name="requestOutput">请求结果</param>
        /// <param name="response">异步结果</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>同步结果</returns>
        public SyncStatus SyncRequest(
            TRequestInput requestInput,
            out TRequestOutput requestOutput,
            out TResponse response,
            TimeSpan timeout
            )
        {
            return m_SyncRequestDelegate(requestInput, out requestOutput, out response, timeout);
        }

        /// <summary>
        /// 同步请求
        /// </summary>
        /// <param name="requestInput">请求参数</param>
        /// <param name="requestOutput">请求结果</param>
        /// <param name="response">异步结果</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>同步结果</returns>
        private SyncStatus SyncRequestWithRequestOutput(
            TRequestInput requestInput,
            out TRequestOutput requestOutput,
            out TResponse response,
            TimeSpan timeout
            )
        {
            SyncStatus syncStatus = SyncStatus.Failed;

            response = default;

            bool syncContextAdded = false;
            TKey key = default;
            SyncContext<TRequestInput, TRequestOutput, TResponse> syncContext = default;

            lock (m_SyncContexts)
            {
                requestOutput = m_RequestFunc(requestInput);

                if (m_RequestFilter(requestInput, requestOutput))
                {
                    key = m_RequestKeySelectorWithRequestOutput(requestInput, requestOutput);
                    syncContext = new SyncContext<TRequestInput, TRequestOutput, TResponse>()
                    {
                        RequestInput = requestInput,
                        RequestOutput = requestOutput,
                    };

                    if (m_SyncContexts.TryAdd(key, syncContext))
                    {
                        syncContextAdded = true;
                    }
                }
            }

            if (syncContextAdded)
            {
                if (syncContext.ManualResetEvent.WaitOne(timeout))
                {
                    syncStatus = SyncStatus.Successful;
                    response = syncContext.Response;
                }
                else
                {
                    syncStatus = SyncStatus.Timeout;
                }

                m_SyncContexts.TryRemove(key, out _);
            }

            return syncStatus;
        }

        /// <summary>
        /// 同步请求
        /// </summary>
        /// <param name="requestInput">请求参数</param>
        /// <param name="requestOutput">请求结果</param>
        /// <param name="response">异步结果</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>同步结果</returns>
        private SyncStatus SyncRequestWithoutRequestOutput(
            TRequestInput requestInput,
            out TRequestOutput requestOutput,
            out TResponse response,
            TimeSpan timeout
            )
        {
            SyncStatus syncStatus = SyncStatus.Failed;

            response = default;
            requestOutput = default;

            TKey key = m_RequestKeySelectorWithoutRequestOutput(requestInput);
            SyncContext<TRequestInput, TRequestOutput, TResponse> syncContext = new SyncContext<TRequestInput, TRequestOutput, TResponse>()
            {
                RequestInput = requestInput,
                RequestOutput = requestOutput,
            };

            if (m_SyncContexts.TryAdd(key, syncContext))
            {
                requestOutput = m_RequestFunc(requestInput);
                syncContext.RequestOutput = requestOutput;

                if (m_RequestFilter(requestInput, requestOutput))
                {
                    if (syncContext.ManualResetEvent.WaitOne(timeout))
                    {
                        syncStatus = SyncStatus.Successful;
                        response = syncContext.Response;
                    }
                    else
                    {
                        syncStatus = SyncStatus.Timeout;
                    }
                }

                m_SyncContexts.TryRemove(key, out _);
            }

            return syncStatus;
        }

        /// <summary>
        /// 请求响应
        /// </summary>
        /// <param name="response"></param>
        public void FeedResponse(TResponse response)
        {
            m_FeedResponseDelegate(response);

        }

        /// <summary>
        /// 请求响应
        /// </summary>
        /// <param name="response"></param>
        private void FeedResponseWithRequestOutput(TResponse response)
        {
            TKey key = m_ResponseKeySelector(response);
            lock (m_SyncContexts)
            {
                if (m_SyncContexts.TryGetValue(key, out var syncContext))
                {
                    syncContext.Response = response;
                    syncContext.ManualResetEvent.Set();
                }
            }
        }

        /// <summary>
        /// 请求响应
        /// </summary>
        /// <param name="response"></param>
        private void FeedResponseWithoutRequestOutput(TResponse response)
        {
            TKey key = m_ResponseKeySelector(response);
            if (m_SyncContexts.TryGetValue(key, out var syncContext))
            {
                syncContext.Response = response;
                syncContext.ManualResetEvent.Set();
            }
        }
    }
}
