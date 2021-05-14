# Synchronizer

Synchronizer旨在把异步调用封装成同步调用。

## RequestSynchronizer

### 使用说明

示例代码只用于表明用法，需要完整可编译的代码例子可以参考单元测试代码。

1. 下载安装[Synchronizer](https://www.nuget.org/packages/Synchronizer/)包
2. 构造一个请求同步器RequestSynchronizer

   ```CSharp
   RequestSynchronizer<RequestInput, RequestOutput, Response, string> synchronizer = new RequestSynchronizer<RequestInput, RequestOutput, Response, string>(
                   input => AsyncRequestMethod(input),  // 异步调用方法
                   (input, output) => output.Succeeded, // 过滤失败的异步调用
                   (input, output) => output.RequestId, // 指定请求标识
                   response => response.RequestId);     // 指定请求标识
   ```

3. 在接收异步返回结果的地方调用FeedResponse方法

   ```CSharp
   synchronizer.FeedResponse(response);
   ```

4. 在需要同步调用的地方调用SyncRequest方法

```CSharp
var syncStatus = synchronizer.SyncRequest(requestInput, out RequestOutput requestOutput, out Response response, TimeSpan.FromSeconds(5));
```

### 注意事项

1. 指定的请求标识来同步关联，如果存在重用请求标识的情况，需要保证错开重用。
2. 通过FeedResponse喂入 请求标识无关联 的结果会一直保存在内存里。
3. 同步调用超时后，通过FeedResponse喂入的关联结果会一直保存在内存里（超时后已无关联）。
