namespace QwQ.Avalonia.Utilities.MessageBus;

/// <summary>
/// 消息接收者构建器，用于创建和订阅消息
/// </summary>
public class MessageReceiverBuilder<TMessage>
{
    private readonly object _receiver;
    private Func<TMessage, object, bool> _filter = (_, _) => true;
    private Action<TMessage, object>? _handler;
    private bool _useWeakReference;
    private int _priority = 5;
    private string? _tag;
    private CancellationToken _cancellationToken = CancellationToken.None;

    internal MessageReceiverBuilder(object receiver, Action<TMessage, object>? handler = null)
    {
        _receiver = receiver;
        _handler = handler;
    }

    /// <summary>
    /// 设置消息过滤条件
    /// </summary>
    /// <param name="filter">过滤条件</param>
    /// <returns>消息接收者构建器</returns>
    public MessageReceiverBuilder<TMessage> WithFilter(Func<TMessage, object, bool> filter)
    {
        _filter = filter;
        return this;
    }

    /// <summary>
    /// 设置只接收来自特定发送者类型的消息
    /// </summary>
    /// <typeparam name="TSender">发送者类型</typeparam>
    /// <returns>消息接收者构建器</returns>
    public MessageReceiverBuilder<TMessage> FromSender<TSender>()
        where TSender : class
    {
        _filter = (_, sender) => sender is TSender;
        return this;
    }

    /// <summary>
    /// 设置消息优先级（0-9，9为最高）
    /// </summary>
    /// <param name="priority">优先级值</param>
    /// <returns>消息接收者构建器</returns>
    public MessageReceiverBuilder<TMessage> WithPriority(int priority)
    {
        if (priority < 0)
            priority = 0;
        if (priority > 9)
            priority = 9;
        _priority = priority;
        return this;
    }

    /// <summary>
    /// 设置使用弱引用，防止内存泄漏
    /// </summary>
    /// <returns>消息接收者构建器</returns>
    public MessageReceiverBuilder<TMessage> AsWeakReference()
    {
        _useWeakReference = true;
        return this;
    }

    /// <summary>
    /// 设置订阅标签，用于分组和筛选
    /// </summary>
    /// <param name="tag">标签</param>
    /// <returns>消息接收者构建器</returns>
    public MessageReceiverBuilder<TMessage> WithTag(string tag)
    {
        _tag = tag;
        return this;
    }

    /// <summary>
    /// 设置取消令牌
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息接收者构建器</returns>
    public MessageReceiverBuilder<TMessage> WithCancellationToken(
        CancellationToken cancellationToken
    )
    {
        _cancellationToken = cancellationToken;
        return this;
    }

    /// <summary>
    /// 设置消息处理程序
    /// </summary>
    public MessageReceiverBuilder<TMessage> WithHandler(Action<TMessage, object> handler)
    {
        _handler = handler;
        return this;
    }

    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <returns>是否成功订阅</returns>
    public bool Subscribe()
    {
        if (_handler == null)
        {
            throw new InvalidOperationException("必须先设置消息处理程序");
        }

        MessageBus.AddSubscription(
            _receiver,
            _filter,
            _handler,
            _useWeakReference,
            _priority,
            _tag,
            _cancellationToken
        );

        return true;
    }

    /// <summary>
    /// 异步订阅消息
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    public Task<bool> SubscribeAsync()
    {
        return Task.FromResult(Subscribe());
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    /// <returns>是否成功取消订阅</returns>
    public bool Unsubscribe()
    {
        return MessageBus.RemoveSubscription<TMessage>(_receiver, _tag);
    }

    /// <summary>
    /// 取消特定标签的订阅
    /// </summary>
    /// <param name="tag">标签</param>
    /// <returns>是否成功取消订阅</returns>
    public bool UnsubscribeByTag(string tag)
    {
        return MessageBus.RemoveSubscription<TMessage>(_receiver, tag);
    }
}
