using System.Runtime.CompilerServices;

namespace QwQ.Avalonia.Utilities.MessageBus;

/// <summary>
/// 表示一个消息订阅
/// </summary>
internal class Subscription
{
    private readonly WeakReference? _weakReceiver;
    private readonly object? _strongReceiver;

    /// <summary>
    /// 创建一个新的订阅
    /// </summary>
    /// <param name="receiver">接收者对象</param>
    /// <param name="filter">消息过滤器</param>
    /// <param name="handler">消息处理程序</param>
    /// <param name="useWeakReference">是否使用弱引用</param>
    /// <param name="priority">优先级</param>
    /// <param name="tag">标签</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Subscription(
        object receiver,
        Func<object, bool> filter,
        Action<object, object> handler,
        bool useWeakReference = false,
        int priority = 5,
        string? tag = null,
        CancellationToken cancellationToken = default
    )
    {
        if (useWeakReference)
        {
            _weakReceiver = new WeakReference(receiver);
            _strongReceiver = null;
        }
        else
        {
            _weakReceiver = null;
            _strongReceiver = receiver;
        }

        Filter = filter;
        Handler = handler;
        Priority = priority;
        Tag = tag;
        IsWeakReference = useWeakReference;
        CancellationToken = cancellationToken;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 接收者对象
    /// </summary>
    public object Receiver =>
        _strongReceiver
        ?? (_weakReceiver?.Target ?? throw new InvalidOperationException("接收者已被垃圾回收"));

    /// <summary>
    /// 检查接收者是否仍然存活（优化版本）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckReceiverAlive() =>
        _strongReceiver != null || _weakReceiver is { IsAlive: true, Target: not null };

    /// <summary>
    /// 消息过滤器
    /// </summary>
    public Func<object, bool> Filter { get; }

    /// <summary>
    /// 消息处理程序
    /// </summary>
    public Action<object, object> Handler { get; }

    /// <summary>
    /// 订阅是否有效
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 优先级（0-9，9为最高）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 是否使用弱引用
    /// </summary>
    public bool IsWeakReference { get; }

    /// <summary>
    /// 订阅标签，可用于分组和筛选
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// 取消令牌
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// 订阅创建时间
    /// </summary>
    public DateTime CreatedAt { get; }
}
