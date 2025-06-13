using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QwQ.Avalonia.Utilities.MessageBus;

/// <summary>
/// 消息总线核心类，提供消息发布和订阅功能
/// </summary>
public static class MessageBus
{
    // 使用 ConcurrentDictionary 存储订阅者，减少锁竞争
    private static readonly ConcurrentDictionary<Type, ConcurrentBag<Subscription>> _subscriptions =
        new();
    private static readonly ConcurrentDictionary<string, HashSet<Type>> _messageGroups = new();
    private static readonly Timer _cleanupTimer = new(
        _ => CleanupDeadSubscriptions(),
        null,
        Timeout.Infinite,
        Timeout.Infinite
    );

    // 使用对象池来减少内存分配
    private static readonly ObjectPool<List<Subscription>> _subscriptionListPool = new(
        () => new List<Subscription>(32),
        list => list.Clear()
    );
    
    static MessageBus()
    {
        // 默认启用自动清理
        EnableAutoCleanup = true;
    }

    /// <summary>
    /// 是否启用消息追踪
    /// </summary>
    public static bool EnableTracing { get; set; } = false;

    /// <summary>
    /// 是否启用异常处理
    /// </summary>
    public static bool EnableExceptionHandling { get; set; } = true;

    /// <summary>
    /// 异常处理器
    /// </summary>
    public static Action<Exception, string>? ExceptionHandler { get; set; }

    /// <summary>
    /// 是否启用自动清理
    /// </summary>
    public static bool EnableAutoCleanup
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            if (value)
            {
                _cleanupTimer.Change(AutoCleanupInterval, AutoCleanupInterval);
            }
            else
            {
                _cleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }

    /// <summary>
    /// 自动清理间隔（毫秒）
    /// </summary>
    public static TimeSpan AutoCleanupInterval
    {
        get;
        set
        {
            if (value.TotalSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "清理间隔不能小于0秒");

            field = value;
            if (EnableAutoCleanup)
            {
                _cleanupTimer.Change(value, value);
            }
        }
    } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// 立即执行一次清理
    /// </summary>
    public static void CleanupNow()
    {
        CleanupDeadSubscriptions();
    }

    /// <summary>
    /// 创建消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="content">消息内容</param>
    /// <returns>消息构建器</returns>
    public static MessageBuilder<TMessage> CreateMessage<TMessage>(TMessage content)
    {
        return new MessageBuilder<TMessage>(content);
    }

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="receiver">接收者对象</param>
    /// <returns>消息接收者构建器</returns>
    public static MessageReceiverBuilder<TMessage> ReceiveMessage<TMessage>(object receiver)
    {
        return new MessageReceiverBuilder<TMessage>(receiver);
    }

    /// <summary>
    /// 注册消息组
    /// </summary>
    /// <param name="groupName">组名称</param>
    /// <param name="messageTypes">消息类型列表</param>
    public static void RegisterMessageGroup(string groupName, params Type[] messageTypes)
    {
        if (string.IsNullOrEmpty(groupName))
            throw new ArgumentException("组名称不能为空", nameof(groupName));
        
        groupName = groupName.Trim().ToLowerInvariant(); // 规范化组名
        
        foreach (var type in messageTypes)
        {
            if (!type.IsClass || type.IsAbstract)
                throw new ArgumentException($"消息类型 {type.Name} 必须是具体的类", nameof(messageTypes));
        }
        
        _messageGroups.AddOrUpdate(
            groupName,
            _ => messageTypes.ToHashSet(),
            (_, existingTypes) =>
            {
                lock (existingTypes)
                {
                    existingTypes.UnionWith(messageTypes.Where(t => !existingTypes.Contains(t)));
                }
                return existingTypes;
            }
        );
    }

    /// <summary>
    /// 获取消息组中的所有消息类型
    /// </summary>
    /// <param name="groupName">组名称</param>
    /// <returns>消息类型列表</returns>
    public static IEnumerable<Type> GetMessageTypesInGroup(string groupName)
    {
        return _messageGroups.TryGetValue(groupName, out var types)
            ? types
            : Enumerable.Empty<Type>();
    }

    /// <summary>
    /// 清理无效的订阅（接收者已被垃圾回收）
    /// </summary>
    private static void CleanupDeadSubscriptions()
    {
        foreach (var messageType in _subscriptions.Keys)
        {
            if (!_subscriptions.TryGetValue(messageType, out var subscriptions))
                continue;

            var newSubscriptions = new ConcurrentBag<Subscription>();
            foreach (var subscription in subscriptions)
            {
                if (
                    subscription
                        is
                        {
                            IsWeakReference:false,
                            IsActive: true,
                            CancellationToken.IsCancellationRequested: false,
                        } && subscription.CheckReceiverAlive()
                )
                {
                    newSubscriptions.Add(subscription);
                }
            }

            if (newSubscriptions.IsEmpty)
            {
                _subscriptions.TryRemove(messageType, out _);
            }
            else
            {
                _subscriptions[messageType] = newSubscriptions;
            }
        }
    }

    internal static void AddSubscription<TMessage>(
        object receiver,
        Func<TMessage, object, bool> filter,
        Action<TMessage, object> handler,
        bool useWeakReference = false,
        int priority = 5,
        string? tag = null,
        CancellationToken cancellationToken = default
    )
    {
        var messageType = typeof(TMessage);
        var subscription = new Subscription(
            receiver,
            message => filter((TMessage)message, receiver),
            (message, sender) => handler((TMessage)message, sender),
            useWeakReference,
            priority,
            tag,
            cancellationToken
        );

        var subscriptions = _subscriptions.GetOrAdd(messageType, _ => []);

        subscriptions.Add(subscription);

        TraceMessage(
            $"添加订阅: {messageType.Name} -> {receiver.GetType().Name}{(tag != null ? $" (标签: {tag})" : "")}"
        );
    }

    internal static bool RemoveSubscription<TMessage>(object receiver, string? tag = null)
    {
        var messageType = typeof(TMessage);
        if (!_subscriptions.TryGetValue(messageType, out var subscriptions))
            return false;

        int count = 0;
        var newSubscriptions = new ConcurrentBag<Subscription>();

        foreach (var subscription in subscriptions)
        {
            if (
                tag == null && subscription.Receiver == receiver
                || tag != null && subscription.Receiver == receiver && subscription.Tag == tag
            )
            {
                count++;
            }
            else
            {
                newSubscriptions.Add(subscription);
            }
        }

        if (count > 0)
        {
            _subscriptions[messageType] = newSubscriptions;
            TraceMessage(
                $"移除订阅: {messageType.Name} -> {receiver.GetType().Name}{(tag != null ? $" (标签: {tag})" : "")} (数量: {count})"
            );
        }

        return count > 0;
    }

    /// <summary>
    /// 移除指定组的所有订阅
    /// </summary>
    /// <param name="groupName">组名称</param>
    /// <param name="receiver">接收者对象（可选）</param>
    /// <returns>是否成功移除</returns>
    public static bool RemoveGroupSubscriptions(string groupName, object? receiver = null)
    {
        if (!_messageGroups.TryGetValue(groupName, out var messageTypes))
            return false;

        bool anyRemoved = false;

        foreach (var messageType in messageTypes)
        {
            if (!_subscriptions.TryGetValue(messageType, out var subscriptions))
                continue;

            var newSubscriptions = new ConcurrentBag<Subscription>();
            foreach (var subscription in subscriptions)
            {
                if (receiver == null || subscription.Receiver == receiver)
                {
                    anyRemoved = true;
                }
                else
                {
                    newSubscriptions.Add(subscription);
                }
            }

            if (anyRemoved)
            {
                _subscriptions[messageType] = newSubscriptions;
            }
        }

        if (anyRemoved)
        {
            TraceMessage(
                $"移除组订阅: {groupName}{(receiver != null ? $" -> {receiver.GetType().Name}" : "")}"
            );
        }

        return anyRemoved;
    }

    internal static async Task PublishAsync<TMessage>(
        TMessage message,
        object sender,
        IEnumerable<object>? receivers = null,
        bool oneTime = false,
        int priority = 5,
        bool waitForCompletion = false,
        TimeSpan? timeout = null,
        string? tag = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sender);

        var messageType = typeof(TMessage);
        var matchingSubscriptions = GetMatchingSubscriptions(messageType, receivers, tag);

        if (matchingSubscriptions.Count == 0)
        {
            TraceMessage($"没有找到消息类型 {messageType.Name} 的订阅者");
            return;
        }

        TraceMessage(
            $"发布消息: {messageType.Name} 从 {sender.GetType().Name} 到 {matchingSubscriptions.Count} 个接收者{(tag != null ? $" (标签: {tag})" : "")}"
        );

        var tasks = new List<Task>();
        var stopwatch = EnableTracing ? Stopwatch.StartNew() : null;

        try
        {
            foreach (var subscription in matchingSubscriptions.Where(s => s.Filter(message)))
            {
                subscription.Priority = priority;
                var task = HandleSubscriptionMessageAsync(
                    message,
                    subscription,
                    sender,
                    timeout,
                    cancellationToken
                );
                tasks.Add(task);

                if (oneTime)
                {
                    subscription.IsActive = false;
                }
            }

            if (waitForCompletion && tasks.Count > 0)
            {
                try
                {
                    if (timeout.HasValue)
                    {
                        var cts = new CancellationTokenSource(timeout.Value);
                        try 
                        {
                            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(timeout.Value, cts.Token));
                            if (!cts.IsCancellationRequested)
                            {
                                cts.Cancel(); // 取消所有未完成的任务
                            }
                        }
                        finally 
                        {
                            cts.Dispose();
                        }
                    }
                    else
                    {
                        await Task.WhenAll(tasks);
                    }
                }
                catch (Exception ex) when (EnableExceptionHandling)
                {
                    ExceptionHandler?.Invoke(ex, $"等待消息 {messageType.Name} 处理完成时发生异常");
                    TraceMessage(
                        $"异常: 等待消息 {messageType.Name} 处理完成时发生异常: {ex.Message}"
                    );
                }
            }
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                TraceMessage(
                    $"完成: 消息 {messageType.Name} 处理耗时 {stopwatch.ElapsedMilliseconds}ms"
                );
            }
        }
    }

    internal static void Publish<TMessage>(
        TMessage message,
        object sender,
        IEnumerable<object>? receivers = null,
        bool oneTime = false,
        int priority = 5,
        string? tag = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = typeof(TMessage);
        var matchingSubscriptions = GetMatchingSubscriptions(messageType, receivers, tag);

        if (matchingSubscriptions.Count == 0)
        {
            TraceMessage($"没有找到消息类型 {messageType.Name} 的订阅者");
            return;
        }

        TraceMessage(
            $"发布消息: {messageType.Name} 从 {sender.GetType().Name} 到 {matchingSubscriptions.Count} 个接收者{(tag != null ? $" (标签: {tag})" : "")}"
        );

        var stopwatch = EnableTracing ? Stopwatch.StartNew() : null;

        try
        {
            foreach (var subscription in matchingSubscriptions.Where(s => s.Filter(message)))
            {
                subscription.Priority = priority;
                HandleSubscriptionMessage(message, subscription, sender, cancellationToken);

                if (oneTime)
                {
                    subscription.IsActive = false;
                }
            }
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                TraceMessage(
                    $"完成: 消息 {messageType.Name} 处理耗时 {stopwatch.ElapsedMilliseconds}ms"
                );
            }
        }
    }

    /// <summary>
    /// 获取指定类型消息的订阅者数量
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <returns>订阅者数量</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSubscriberCount<TMessage>()
    {
        var messageType = typeof(TMessage);
        return _subscriptions.TryGetValue(messageType, out var subscriptions)
            ? subscriptions.Count
            : 0;
    }

    /// <summary>
    /// 检查是否有指定类型消息的订阅者
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <returns>是否有订阅者</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasSubscribers<TMessage>()
    {
        return GetSubscriberCount<TMessage>() > 0;
    }

    /// <summary>
    /// 记录消息总线追踪信息
    /// </summary>
    /// <param name="message">追踪消息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TraceMessage(string message)
    {
        if (EnableTracing)
        {
            Trace.WriteLine($"[MessageBus] {message}");
        }
    }

    /// <summary>
    /// 获取匹配的订阅者列表
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static List<Subscription> GetMatchingSubscriptions(
        Type messageType,
        IEnumerable<object>? receivers = null,
        string? tag = null
    )
    {
        if (!_subscriptions.TryGetValue(messageType, out var subscriptions))
            return [];

        var result = _subscriptionListPool.Get();
        try
        {
            // 预先处理 receivers
            var receiverTypes = new HashSet<Type>();
            var receiverObjects = new HashSet<object>();
            if (receivers != null)
            {
                foreach (object receiver in receivers)
                {
                    if (receiver is Type type)
                    {
                        receiverTypes.Add(type);
                    }
                    else
                    {
                        receiverObjects.Add(receiver);
                    }
                }
            }

            foreach (var subscription in subscriptions)
            {
                if (
                    subscription
                        is { IsActive: true, CancellationToken.IsCancellationRequested: false }
                    && (tag == null || subscription.Tag == tag)
                    && (
                        receivers == null
                        || receiverObjects.Contains(subscription.Receiver)
                        || receiverTypes.Any(t =>
                            subscription.Receiver.GetType() == t
                            || subscription.Receiver.GetType().IsSubclassOf(t)
                        )
                    )
                )
                {
                    result.Add(subscription);
                }
            }

            // 按优先级排序
            result.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return result;
        }
        catch
        {
            _subscriptionListPool.Return(result);
            throw;
        }
    }

    /// <summary>
    /// 处理订阅者消息
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HandleSubscriptionMessage<TMessage>(
        TMessage message,
        Subscription subscription,
        object sender,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!subscription.IsActive || subscription.CancellationToken.IsCancellationRequested)
            return;

        if (subscription.IsWeakReference && !subscription.CheckReceiverAlive())
        {
            subscription.IsActive = false;
            return;
        }

        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            subscription.Handler(message, sender);
        }
        catch (Exception ex) when (EnableExceptionHandling)
        {
            ExceptionHandler?.Invoke(ex, $"处理消息 {typeof(TMessage).Name} 时发生异常");
            TraceMessage($"异常: 处理消息 {typeof(TMessage).Name} 时发生异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理订阅者消息（异步）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task HandleSubscriptionMessageAsync<TMessage>(
        TMessage message,
        Subscription subscription,
        object sender,
        TimeSpan? timeout,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!subscription.IsActive || subscription.CancellationToken.IsCancellationRequested)
            return;

        if (subscription.IsWeakReference && !subscription.CheckReceiverAlive())
        {
            subscription.IsActive = false;
            return;
        }

        await Task.Run(
            () =>
            {
                try
                {
                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        subscription.CancellationToken,
                        cancellationToken
                    );

                    using var timeoutCts = timeout.HasValue
                        ? new CancellationTokenSource(timeout.Value)
                        : null;

                    if (timeoutCts != null)
                    {
                        using var finalCts = CancellationTokenSource.CreateLinkedTokenSource(
                            linkedCts.Token,
                            timeoutCts.Token
                        );
                        if (finalCts.Token.IsCancellationRequested)
                            return;

                        subscription.Handler(message, sender);
                    }
                    else
                    {
                        if (linkedCts.Token.IsCancellationRequested)
                            return;

                        subscription.Handler(message, sender);
                    }
                }
                catch (Exception ex) when (EnableExceptionHandling)
                {
                    ExceptionHandler?.Invoke(ex, $"处理消息 {typeof(TMessage).Name} 时发生异常");
                    TraceMessage(
                        $"异常: 处理消息 {typeof(TMessage).Name} 时发生异常: {ex.Message}"
                    );
                }
            },
            cancellationToken
        );
    }
}

/// <summary>
/// 对象池
/// </summary>
internal class ObjectPool<T>(Func<T> factory, Action<T> reset)
{
    private readonly ConcurrentBag<T> _pool = [];

    public T Get()
    {
        return _pool.TryTake(out var item) ? item : factory();
    }

    public void Return(T item)
    {
        reset(item);
        _pool.Add(item);
    }
}
