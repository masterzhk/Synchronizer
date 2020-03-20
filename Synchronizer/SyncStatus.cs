using System;
using System.Collections.Generic;
using System.Text;

namespace Synchronizer
{
    /// <summary>
    /// 同步状态
    /// </summary>
    public enum SyncStatus
    {
        /// <summary>
        /// 失败
        /// </summary>
        Failed,

        /// <summary>
        /// 成功
        /// </summary>
        Successful,

        /// <summary>
        /// 超时
        /// </summary>
        Timeout,
    }
}
