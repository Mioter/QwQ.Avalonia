namespace QwQ.Avalonia.Utilities.MessageBus;

/// <summary>
/// 消息构建器，用于创建和发送消息
/// </summary>
public class MessageBuilder<TMessage>
{
    private readonly TMessage _content;
    private readonly HashSet<object> _receivers = []; // 可以存储Type类型或object实例
    private bool _oneTime;
    private bool _waitForCompletion;
    private int _priority = 5;
    private object _sender = typeof(MessageBus); // 默认为MessageBus类型
    private TimeSpan? _timeout;
    private string? _tag;
    private CancellationToken _cancellationToken = CancellationToken.None;

    internal MessageBuilder(TMessage content)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// 设置消息发送者（带类型约束）
    /// </summary>
    /// <typeparam name="TSender">发送者类型</typeparam>
    /// <param name="sender">发送者实例</param>
    /// <returns>消息构建器</returns>
    public MessageBuilder<TMessage> FromSender<TSender>(TSender sender)
        where TSender : class
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        return this;
    }

    /// <summary>
    /// 添加消息接收者类型
    /// </summary>
    /// <typeparam name="TReceiver">接收者类型</typeparam>
    /// <returns>消息构建器</returns>
    public MessageBuilder<TMessage> AddReceivers<TReceiver>()
        where TReceiver : class
    {
        _receivers.Add(typeof(TReceiver));
        return this;
    }

    /// <summary>
    /// 添加多个消息接收者类型
    /// </summary>
    /// <param name="receiverTypes">接收者类型</param>
    /// <returns>消息构建器</returns>
    public MessageBuilder<TMessage> AddReceivers(params Type[] receiverTypes)
    {
        if (receiverTypes.Length == 0)
            return this;

        foreach (var receiverType in receiverTypes)
        {
            _receivers.Add(receiverType);
        }
        return this;
    }

    /// <summary>
    /// 设置为一次性消息
    /// </summary>
    public MessageBuilder<TMessage> SetAsOneTime()
    {
        _oneTime = true;
        return this;
    }

    /// <summary>
    /// 等待接收者处理完成
    /// </summary>
    public MessageBuilder<TMessage> WaitForCompletion()
    {
        _waitForCompletion = true;
        return this;
    }

    /// <summary>
    /// 设置消息优先级（0-9，9为最高）
    /// </summary>
    /// <param name="priority">优先级值</param>
    /// <returns>消息构建器</returns>
    public MessageBuilder<TMessage> SetPriority(int priority)
    {
        _priority = Math.Clamp(priority, 0, 9);
        return this;
    }

    /// <summary>
    /// 设置消息标签，用于分组和筛选
    /// </summary>
    /// <param name="tag">标签</param>
    /// <returns>消息构建器</returns>
    public MessageBuilder<TMessage> WithTag(string tag)
    {
        _tag = tag;
        return this;
    }

    /// <summary>
    /// 设置消息处理超时时间
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <returns>消息构建器</returns>
    public MessageBuilder<TMessage> WithTimeout(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "超时时间不能为负数");

        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// 设置消息处理超时时间（毫秒）
    /// </summary>
    /// <param name="milliseconds">超时时间（毫秒）</param>
    /// <returns>消息构建器</returns>
    public MessageBuilder<TMessage> WithTimeoutMilliseconds(int milliseconds)
    {
        if (milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "超时时间不能为负数");

        _timeout = TimeSpan.FromMilliseconds(milliseconds);
        return this;
    }

    /// <summary>
    /// 设置取消令牌
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息构建器</returns>
    public MessageBuilder<TMessage> WithCancellationToken(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        return this;
    }

    /// <summary>
    /// 发布消息
    /// </summary>
    /// <returns>是否成功发布</returns>
    public bool Publish()
    {
        // 检查是否有订阅者
        if (!MessageBus.HasSubscribers<TMessage>())
        {
            return false;
        }

        MessageBus.Publish(
            _content,
            _sender,
            _receivers.Count != 0 ? _receivers : null,
            _oneTime,
            _priority,
            _tag,
            _cancellationToken
        );

        return true;
    }

    /// <summary>
    /// 异步发布消息
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    public async Task<bool> PublishAsync()
    {
        // 检查是否有订阅者
        if (!MessageBus.HasSubscribers<TMessage>())
        {
            return false;
        }

        await MessageBus.PublishAsync(
            _content,
            _sender,
            _receivers.Count != 0 ? _receivers : null,
            _oneTime,
            _priority,
            _waitForCompletion,
            _timeout,
            _tag,
            _cancellationToken
        );

        return true;
    }
}
