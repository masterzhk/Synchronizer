using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Synchronizer
{
    /// <summary>
    /// 同步上下文
    /// </summary>
    /// <typeparam name="TRequestInput">请求参数</typeparam>
    /// <typeparam name="TRequestOutput">请求结果</typeparam>
    /// <typeparam name="TResponse">异步结果</typeparam>
    public class SyncContext<TRequestInput, TRequestOutput, TResponse>
    {
        /// <summary>
        /// 请求参数
        /// </summary>
        public TRequestInput RequestInput { get; set; }

        /// <summary>
        /// 请求结果
        /// </summary>
        public TRequestOutput RequestOutput { get; set; }

        /// <summary>
        /// 异步结果
        /// </summary>
        public TResponse Response { get; set; }

        /// <summary>
        /// 同步事件
        /// </summary>
        public ManualResetEvent ManualResetEvent { get; set; } = new ManualResetEvent(false);
    }
}
