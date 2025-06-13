using System.Runtime.CompilerServices;

namespace QwQ.Avalonia.Utilities.MessageBus;

/// <summary>
/// 消息接收者构建器扩展方法
/// </summary>
public static class MessageReceiverBuilderExtensions
{
    /// <summary>
    /// 使用弱引用订阅消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="builder">消息接收者构建器</param>
    /// <returns>是否成功订阅</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SubscribeAsWeakReference<TMessage>(
        this MessageReceiverBuilder<TMessage> builder
    )
    {
        return builder.AsWeakReference().Subscribe();
    }

    /// <summary>
    /// 使用弱引用异步订阅消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="builder">消息接收者构建器</param>
    /// <returns>表示异步操作的任务</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<bool> SubscribeAsWeakReferenceAsync<TMessage>(
        this MessageReceiverBuilder<TMessage> builder
    )
    {
        return builder.AsWeakReference().SubscribeAsync();
    }
}
