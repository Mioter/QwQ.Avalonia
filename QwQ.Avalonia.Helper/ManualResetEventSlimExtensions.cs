namespace QwQ.Avalonia.Helper;


/// <summary>
/// 为 ManualResetEventSlim 提供异步等待扩展方法
/// </summary>
public static class ManualResetEventSlimExtensions
{
    /// <summary>
    /// 异步等待 ManualResetEventSlim 被设置
    /// </summary>
    /// <param name="manualResetEvent">要等待的事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步等待的任务</returns>
    public static Task WaitOneAsync(this ManualResetEventSlim manualResetEvent, CancellationToken cancellationToken = default)
    {
        return WaitOneAsync(manualResetEvent.WaitHandle, cancellationToken);
    }

    /// <summary>
    /// 异步等待 WaitHandle 被设置
    /// </summary>
    /// <param name="waitHandle">要等待的句柄</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步等待的任务</returns>
    public static Task WaitOneAsync(this WaitHandle waitHandle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(waitHandle);

        // 如果已经设置，则立即返回完成的任务
        if (waitHandle.WaitOne(0))
            return Task.CompletedTask;

        // 如果已经取消，则立即返回已取消的任务
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var tcs = new TaskCompletionSource<bool>();
        var rwh = ThreadPool.RegisterWaitForSingleObject(
            waitHandle,
            (state, _) => (state as TaskCompletionSource<bool>)?.TrySetResult(true),
            tcs,
            Timeout.InfiniteTimeSpan,
            true);

        var cancellationTokenRegistration = default(CancellationTokenRegistration);

        if (cancellationToken.CanBeCanceled)
        {
            cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                rwh.Unregister(null);
                tcs.TrySetCanceled(cancellationToken);
            });
        }

        tcs.Task.ContinueWith(_ =>
        {
            rwh.Unregister(null);
            cancellationTokenRegistration.Dispose();
        }, TaskContinuationOptions.ExecuteSynchronously);

        return tcs.Task;
    }
}

