using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QwQ.Avalonia.Utilities.MessageBus;

/// <summary>
/// 消息总线核心类，提供消息发布和订阅功能
/// </summary>
public static class MessageBus
{
    private static readonly ConcurrentDictionary<Type, List<Subscription>> _subscriptions = new();
    private static readonly ConcurrentDictionary<string, List<Type>> _messageGroups = new();

    static MessageBus()
    {
        // 默认启用自动清理
        EnableAutoCleanup = true;
        AutoCleanupInterval = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// 清理所有资源
    /// </summary>
    public static void Dispose()
    {
        try
        {
            _cleanupTimer.Dispose();
            _subscriptions.Clear();
            _messageGroups.Clear();
        }
        catch (Exception ex) when (EnableExceptionHandling)
        {
            ExceptionHandler?.Invoke(ex, "清理MessageBus资源时发生异常");
        }
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

    private static readonly Timer _cleanupTimer = new(
        _ => CleanupDeadSubscriptions(),
        null,
        Timeout.Infinite,
        Timeout.Infinite
    );

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
    /// 自动清理间隔
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
    }

    /// <summary>
    /// 立即执行一次清理
    /// </summary>
    public static void CleanupNow() => CleanupDeadSubscriptions();

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

        _messageGroups.AddOrUpdate(
            groupName,
            _ => messageTypes.ToList(),
            (_, existingTypes) =>
            {
                lock (existingTypes)
                {
                    existingTypes.AddRange(messageTypes.Where(t => !existingTypes.Contains(t)));
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
    public static void CleanupDeadSubscriptions()
    {
        var messageTypesToRemove = new List<Type>();

        foreach (var (messageType, subscriptions) in _subscriptions)
        {
            lock (subscriptions)
            {
                var deadSubscriptions = subscriptions
                    .Where(s =>
                        !s.IsActive
                        || s.IsWeakReference && !s.CheckReceiverAlive()
                        || s.CancellationToken.IsCancellationRequested
                    )
                    .ToList();

                foreach (var subscription in deadSubscriptions)
                {
                    subscriptions.Remove(subscription);
                    if (EnableTracing)
                    {
                        Trace.WriteLine(
                            $"[MessageBus] 清理无效订阅: {messageType.Name} -> {subscription.Receiver.GetType().Name}"
                        );
                    }
                }

                // 如果该消息类型没有订阅了，标记为需要移除
                if (subscriptions.Count == 0)
                {
                    messageTypesToRemove.Add(messageType);
                }
            }
        }

        // 移除空的订阅列表
        foreach (var messageType in messageTypesToRemove)
        {
            _subscriptions.TryRemove(messageType, out _);
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
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(handler);

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

        _subscriptions.AddOrUpdate(
            messageType,
            _ => [subscription],
            (_, list) =>
            {
                lock (list)
                {
                    // 检查是否已存在相同的订阅（相同接收者和处理程序）
                    var existingSubscription = list.FirstOrDefault(s =>
                        s.Receiver == receiver && s.Handler.Method == handler.Method
                    );

                    if (existingSubscription != null)
                    {
                        // 更新现有订阅
                        existingSubscription.IsActive = true;
                        existingSubscription.Priority = priority;
                        existingSubscription.Tag = tag;
                        existingSubscription.CancellationToken = cancellationToken;
                    }
                    else
                    {
                        // 添加新订阅
                        list.Add(subscription);
                    }
                }
                return list;
            }
        );

        if (EnableTracing)
        {
            Trace.WriteLine(
                $"[MessageBus] 添加订阅: {messageType.Name} -> {receiver.GetType().Name}{(tag != null ? $" (标签: {tag})" : "")}"
            );
        }
    }

    internal static bool RemoveSubscription<TMessage>(object receiver, string? tag = null)
    {
        var messageType = typeof(TMessage);
        if (!_subscriptions.TryGetValue(messageType, out var subscriptions))
            return false;

        lock (subscriptions)
        {
            int count =
                // 只移除特定标签的订阅
                tag != null
                    ? subscriptions.RemoveAll(s => s.Receiver == receiver && s.Tag == tag) :
                    // 移除所有该接收者的订阅
                    subscriptions.RemoveAll(s => s.Receiver == receiver);

            if (EnableTracing && count > 0)
            {
                Trace.WriteLine(
                    $"[MessageBus] 移除订阅: {messageType.Name} -> {receiver.GetType().Name}{(tag != null ? $" (标签: {tag})" : "")} (数量: {count})"
                );
            }

            return count > 0;
        }
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

            lock (subscriptions)
            {
                if (receiver != null)
                {
                    // 只移除特定接收者的订阅
                    anyRemoved |= subscriptions.RemoveAll(s => s.Receiver == receiver) > 0;
                }
                else
                {
                    // 移除所有订阅
                    anyRemoved |= subscriptions.Count > 0;
                    subscriptions.Clear();
                }
            }
        }

        if (EnableTracing && anyRemoved)
        {
            Trace.WriteLine(
                $"[MessageBus] 移除组订阅: {groupName}{(receiver != null ? $" -> {receiver.GetType().Name}" : "")}"
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
        if (!_subscriptions.TryGetValue(messageType, out var subscriptions))
        {
            if (EnableTracing)
            {
                Trace.WriteLine($"[MessageBus] 没有找到消息类型 {messageType.Name} 的订阅者");
            }
            return;
        }

        List<Subscription> matchingSubscriptions;
        lock (subscriptions)
        {
            matchingSubscriptions = subscriptions
                .Where(s =>
                    s is { IsActive: true, CancellationToken.IsCancellationRequested: false }
                    && (tag == null || s.Tag == tag)
                    && (
                        receivers == null
                        || receivers.Any(r =>
                            r is Type receiverType
                                ? s.Receiver.GetType() == receiverType
                                    || s.Receiver.GetType().IsSubclassOf(receiverType)
                                : r == s.Receiver
                        )
                    )
                )
                .OrderByDescending(s => s.Priority)
                .ToList();
        }

        if (matchingSubscriptions.Count == 0)
        {
            if (EnableTracing)
            {
                Trace.WriteLine($"[MessageBus] 没有找到匹配的订阅者: {messageType.Name}");
            }
            return;
        }

        if (EnableTracing)
        {
            Trace.WriteLine(
                $"[MessageBus] 发布消息: {messageType.Name} 从 {sender.GetType().Name} 到 {matchingSubscriptions.Count} 个接收者{(tag != null ? $" (标签: {tag})" : "")}"
            );
        }

        var tasks = new List<Task>(matchingSubscriptions.Count);
        var stopwatch = EnableTracing ? Stopwatch.StartNew() : null;

        foreach (var subscription in matchingSubscriptions)
        {
            if (!subscription.Filter(message))
                continue;

            if (!subscription.IsActive || subscription.CancellationToken.IsCancellationRequested)
                continue;

            if (subscription.IsWeakReference && !subscription.CheckReceiverAlive())
            {
                subscription.IsActive = false;
                continue;
            }

            subscription.Priority = priority;

            var task = Task.Run(
                () =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        subscription.Handler(message, sender);
                    }
                    catch (Exception ex) when (EnableExceptionHandling)
                    {
                        ExceptionHandler?.Invoke(ex, $"处理消息 {messageType.Name} 时发生异常");

                        if (EnableTracing)
                        {
                            Trace.WriteLine(
                                $"[MessageBus] 异常: 处理消息 {messageType.Name} 时发生异常: {ex.Message}"
                            );
                        }
                    }
                },
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
                    // 使用超时等待所有任务完成
                    var completedTask = await Task.WhenAny(
                        Task.WhenAll(tasks),
                        Task.Delay(timeout.Value, cancellationToken)
                    );
                    if (completedTask != Task.WhenAll(tasks))
                    {
                        if (EnableTracing)
                        {
                            Trace.WriteLine($"[MessageBus] 超时: 消息 {messageType.Name} 处理超时");
                        }
                    }
                }
                else
                {
                    // 无超时等待所有任务完成
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex) when (EnableExceptionHandling)
            {
                ExceptionHandler?.Invoke(ex, $"等待消息 {messageType.Name} 处理完成时发生异常");

                if (EnableTracing)
                {
                    Trace.WriteLine(
                        $"[MessageBus] 异常: 等待消息 {messageType.Name} 处理完成时发生异常: {ex.Message}"
                    );
                }
            }
        }

        if (EnableTracing && stopwatch != null)
        {
            stopwatch.Stop();
            Trace.WriteLine(
                $"[MessageBus] 完成: 消息 {messageType.Name} 处理耗时 {stopwatch.ElapsedMilliseconds}ms"
            );
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
        ArgumentNullException.ThrowIfNull(sender);

        var messageType = typeof(TMessage);
        if (!_subscriptions.TryGetValue(messageType, out var subscriptions))
        {
            if (EnableTracing)
            {
                Trace.WriteLine($"[MessageBus] 没有找到消息类型 {messageType.Name} 的订阅者");
            }
            return;
        }

        List<Subscription> matchingSubscriptions;
        lock (subscriptions)
        {
            matchingSubscriptions = subscriptions
                .Where(s =>
                    s is { IsActive: true, CancellationToken.IsCancellationRequested: false }
                    && (tag == null || s.Tag == tag)
                    && (
                        receivers == null
                        || receivers.Any(r =>
                            r is Type receiverType
                                ? s.Receiver.GetType() == receiverType
                                    || s.Receiver.GetType().IsSubclassOf(receiverType)
                                : r == s.Receiver
                        )
                    )
                )
                .OrderByDescending(s => s.Priority)
                .ToList();
        }

        if (matchingSubscriptions.Count == 0)
        {
            if (EnableTracing)
            {
                Trace.WriteLine($"[MessageBus] 没有找到匹配的订阅者: {messageType.Name}");
            }
            return;
        }

        if (EnableTracing)
        {
            Trace.WriteLine(
                $"[MessageBus] 发布消息: {messageType.Name} 从 {sender.GetType().Name} 到 {matchingSubscriptions.Count} 个接收者{(tag != null ? $" (标签: {tag})" : "")}"
            );
        }

        var stopwatch = EnableTracing ? Stopwatch.StartNew() : null;

        foreach (var subscription in matchingSubscriptions)
        {
            if (!subscription.Filter(message))
                continue;

            if (!subscription.IsActive || subscription.CancellationToken.IsCancellationRequested)
                continue;

            if (subscription.IsWeakReference && !subscription.CheckReceiverAlive())
            {
                subscription.IsActive = false;
                continue;
            }

            subscription.Priority = priority;

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                subscription.Handler(message, sender);
            }
            catch (Exception ex) when (EnableExceptionHandling)
            {
                ExceptionHandler?.Invoke(ex, $"处理消息 {messageType.Name} 时发生异常");

                if (EnableTracing)
                {
                    Trace.WriteLine(
                        $"[MessageBus] 异常: 处理消息 {messageType.Name} 时发生异常: {ex.Message}"
                    );
                }
            }

            if (oneTime)
            {
                subscription.IsActive = false;
            }
        }

        if (EnableTracing && stopwatch != null)
        {
            stopwatch.Stop();
            Trace.WriteLine(
                $"[MessageBus] 完成: 消息 {messageType.Name} 处理耗时 {stopwatch.ElapsedMilliseconds}ms"
            );
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
    /// 获取所有消息类型的统计信息
    /// </summary>
    /// <returns>消息类型和订阅者数量的字典</returns>
    public static Dictionary<Type, int> GetSubscriberStatistics()
    {
        var result = new Dictionary<Type, int>();

        foreach (var kvp in _subscriptions)
        {
            result[kvp.Key] = kvp.Value.Count;
        }

        return result;
    }

    /// <summary>
    /// 获取消息组统计信息
    /// </summary>
    /// <returns>消息组和消息类型数量的字典</returns>
    public static Dictionary<string, int> GetMessageGroupStatistics()
    {
        var result = new Dictionary<string, int>();

        foreach (var kvp in _messageGroups)
        {
            result[kvp.Key] = kvp.Value.Count;
        }

        return result;
    }
}
