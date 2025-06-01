using System.Collections.Concurrent;
using System.Diagnostics;
using QwQ.Avalonia.Helper;

namespace QwQ.Avalonia.Utilities.TaskManager;

/// <summary>
/// 多项目任务构建器，提供多项目任务的创建和管理功能。
/// 支持并行和队列两种执行模式，可以灵活控制任务的执行方式。
/// 适用于需要处理大量数据项的场景，如批量文件处理、数据分析等。
/// </summary>
/// <typeparam name="TItem">项目类型，表示要处理的数据项</typeparam>
/// <typeparam name="TResult">任务返回类型，表示处理结果</typeparam>
public class MultiTaskBuilder<TItem, TResult>
{
    private readonly IEnumerable<TItem> _items;
    private readonly Func<TItem, CancellationToken, Task<TResult>> _itemWork;
    private readonly bool _isParallel;
    private readonly bool _isBackground;

    private TimeSpan? _delay;
    private TimeSpan? _timeout;
    private Action<TimeSpan>? _timeoutHandler;
    private TaskController? _controller;
    private Action<Exception>? _errorHandler;
    private Action<IEnumerable<TResult>>? _completeCallback;
    private IProgress<double>? _progress;
    private ThreadPriority _priority = ThreadPriority.Normal;
    private int _maxConcurrentTasks = Environment.ProcessorCount;

    /// <summary>
    /// 初始化多项目任务构建器
    /// </summary>
    /// <param name="items">要处理的项目集合</param>
    /// <param name="itemWork">处理单个项目的工作委托</param>
    /// <param name="isParallel">是否并行执行（true：并行执行，false：队列执行）</param>
    /// <param name="isBackground">是否为后台任务</param>
    internal MultiTaskBuilder(
        IEnumerable<TItem> items,
        Func<TItem, Task<TResult>> itemWork,
        bool isParallel,
        bool isBackground)
    {
        _items = items;
        _itemWork = (item, _) => itemWork(item);
        _isParallel = isParallel;
        _isBackground = isBackground;
    }
    
    /// <summary>
    /// 初始化多项目任务构建器（支持取消令牌）
    /// </summary>
    /// <param name="items">要处理的项目集合</param>
    /// <param name="itemWork">处理单个项目的工作委托，支持取消令牌</param>
    /// <param name="isParallel">是否并行执行（true：并行执行，false：队列执行）</param>
    /// <param name="isBackground">是否为后台任务</param>
    internal MultiTaskBuilder(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task<TResult>> itemWork,
        bool isParallel,
        bool isBackground)
    {
        _items = items;
        _itemWork = itemWork;
        _isParallel = isParallel;
        _isBackground = isBackground;
    }

    /// <summary>
    /// 设置任务延迟时间，任务将在指定的延迟时间后开始执行
    /// </summary>
    /// <param name="delay">延迟时间</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    /// <exception cref="ArgumentOutOfRangeException">当延迟时间为负数时抛出</exception>
    public MultiTaskBuilder<TItem, TResult> SetDelay(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay time cannot be negative");
            
        _delay = delay;
        return this;
    }

    /// <summary>
    /// 设置任务延迟时间（毫秒）
    /// </summary>
    /// <param name="delayMilliseconds">延迟毫秒数</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    public MultiTaskBuilder<TItem, TResult> SetDelay(int delayMilliseconds)
    {
        return SetDelay(TimeSpan.FromMilliseconds(delayMilliseconds));
    }

    /// <summary>
    /// 设置任务超时时间和超时处理程序
    /// </summary>
    /// <param name="timeout">超时时间，任务执行时间超过此值将被视为超时</param>
    /// <param name="timeoutHandler">超时处理程序，在任务超时时调用，参数为任务已执行的时间</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    /// <exception cref="ArgumentOutOfRangeException">当超时时间为负数或零时抛出</exception>
    public MultiTaskBuilder<TItem, TResult> SetTimeout(TimeSpan timeout, Action<TimeSpan>? timeoutHandler = null)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");
            
        _timeout = timeout;
        _timeoutHandler = timeoutHandler;
        return this;
    }

    /// <summary>
    /// 设置任务超时时间和超时处理程序（毫秒）
    /// </summary>
    /// <param name="timeoutMilliseconds">超时毫秒数</param>
    /// <param name="timeoutHandler">超时处理程序，在任务超时时调用</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    public MultiTaskBuilder<TItem, TResult> SetTimeout(int timeoutMilliseconds, Action<TimeSpan>? timeoutHandler = null)
    {
        return SetTimeout(TimeSpan.FromMilliseconds(timeoutMilliseconds), timeoutHandler);
    }

    /// <summary>
    /// 设置任务控制器，用于控制任务的执行状态（暂停、恢复、取消等）
    /// </summary>
    /// <param name="controller">任务控制器实例</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    /// <exception cref="ArgumentNullException">当controller为null时抛出</exception>
    public MultiTaskBuilder<TItem, TResult> SetController(TaskController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        
        _controller = controller;
        return this;
    }

    /// <summary>
    /// 设置错误处理方法，在任务执行出错时调用
    /// </summary>
    /// <param name="errorHandler">错误处理方法，接收异常对象作为参数</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    /// <exception cref="ArgumentNullException">当errorHandler为null时抛出</exception>
    public MultiTaskBuilder<TItem, TResult> SetErrorHandler(Action<Exception> errorHandler)
    {
        ArgumentNullException.ThrowIfNull(errorHandler);
        
        _errorHandler = errorHandler;
        return this;
    }

    /// <summary>
    /// 设置完成回调方法，在任务成功完成时调用
    /// </summary>
    /// <param name="completeCallback">完成回调方法，接收处理结果集合作为参数</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    /// <exception cref="ArgumentNullException">当completeCallback为null时抛出</exception>
    public MultiTaskBuilder<TItem, TResult> SetCompleteCallback(Action<IEnumerable<TResult>> completeCallback)
    {
        ArgumentNullException.ThrowIfNull(completeCallback);
        
        _completeCallback = completeCallback;
        return this;
    }

    /// <summary>
    /// 设置进度报告器，用于报告任务执行进度
    /// </summary>
    /// <param name="progress">进度报告器，接收0到1之间的值表示进度百分比</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    public MultiTaskBuilder<TItem, TResult> SetProgress(IProgress<double> progress)
    {
        _progress = progress;
        return this;
    }

    /// <summary>
    /// 设置任务优先级，影响任务在线程池中的执行优先级
    /// </summary>
    /// <param name="priority">任务优先级</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    public MultiTaskBuilder<TItem, TResult> SetPriority(ThreadPriority priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// 设置最大并发任务数，控制并行执行时的最大同时执行任务数
    /// </summary>
    /// <param name="maxConcurrentTasks">最大并发任务数，必须大于0</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 maxConcurrentTasks 小于或等于 0 时抛出</exception>
    /// <remarks>
    /// 此设置仅在并行执行模式下有效。较大的值可以提高吞吐量，但会消耗更多系统资源。
    /// 默认值为系统处理器数量，通常是一个较为平衡的选择。
    /// </remarks>
    public MultiTaskBuilder<TItem, TResult> SetMaxConcurrentTasks(int maxConcurrentTasks)
    {
        if (maxConcurrentTasks < 1)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrentTasks), "Max concurrent tasks must be greater than zero");

        _maxConcurrentTasks = maxConcurrentTasks;
        return this;
    }

    /// <summary>
    /// 运行任务并返回执行结果
    /// </summary>
    /// <returns>任务执行结果，包含处理结果集合、执行状态和执行时间等信息</returns>
    /// <remarks>
    /// 此方法会异步执行任务，并处理任务的各种状态：
    /// - 成功完成：返回成功结果和执行时间
    /// - 取消：返回取消状态和已执行时间
    /// - 超时：返回超时状态和已执行时间
    /// - 错误：返回错误状态、异常信息和已执行时间
    /// </remarks>
    public async Task<TaskExecutionResult<TResult>> RunAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var controller = _controller ?? new TaskController();
        controller.Priority = _priority;

        try
        {
            // 检查项目集合是否为空
            if (!_items.Any())
            {
                // 如果项目集合为空，直接返回成功结果（空集合）
                return TaskExecutionResult<TResult>.Success(new List<TResult>(), TimeSpan.Zero);
            }
            
            // 报告初始进度
            _progress?.Report(0.0);
            
            // 延迟执行
            if (_delay.HasValue)
            {
                await Task.Delay(_delay.Value, controller.CancellationToken);
            }

            // 启动任务
            await controller.StartAsync();

            // 创建任务
            var mainTask = ExecuteMultiProjectTask(controller);

            // 处理超时
            if (_timeout.HasValue)
            {
                var timeoutTask = Task.Delay(_timeout.Value, controller.CancellationToken);
                var completedTask = await Task.WhenAny(mainTask, timeoutTask);

                if (completedTask == timeoutTask && !mainTask.IsCompleted)
                {
                    await controller.SetTimeoutAsync();
                    _timeoutHandler?.Invoke(stopwatch.Elapsed);
                    return TaskExecutionResult<TResult>.Timeout(stopwatch.Elapsed);
                }
            }

            var result = await mainTask;
            await controller.SetCompletedAsync();
            
            // 报告完成进度
            _progress?.Report(1.0);

            // 执行完成回调
            if (result is { IsSuccess: true, Results: not null })
            {
                _completeCallback?.Invoke(result.Results);
            }

            return result;
        }
        catch (OperationCanceledException) when (controller.IsCancelled)
        {
            return TaskExecutionResult<TResult>.Cancelled(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            await controller.SetErrorAsync();
            _errorHandler?.Invoke(ex);
            return TaskExecutionResult<TResult>.Failure(ex, TaskExecutionState.Error, stopwatch.Elapsed);
        }
        finally
        {
            stopwatch.Stop();
            if (_controller == null) // 如果是内部创建的控制器，需要释放资源
            {
                controller.Dispose();
            }
        }
    }

    /// <summary>
    /// 执行多项目任务的核心方法
    /// </summary>
    /// <param name="controller">任务控制器，用于控制任务执行状态</param>
    /// <returns>任务执行结果，包含处理结果集合和执行时间</returns>
    /// <remarks>
    /// 此方法根据配置选择并行或队列方式执行任务，并收集所有处理结果。
    /// 并行执行适合相互独立的任务，队列执行适合需要按顺序处理的任务。
    /// </remarks>
    private async Task<TaskExecutionResult<TResult>> ExecuteMultiProjectTask(TaskController controller)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrentBag<TResult>();
        int totalItems = _items.Count();
        var completedItems = new AtomicCounter();

        try
        {
            if (_isParallel)
            {
                await ExecuteParallelAsync(controller, results, totalItems, completedItems);
            }
            else
            {
                await ExecuteQueueAsync(controller, results, totalItems, completedItems);
            }

            stopwatch.Stop();
            return TaskExecutionResult<TResult>.Success(results.ToList(), stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (controller.IsCancelled)
        {
            // 任务被取消，重新抛出异常以便上层处理
            stopwatch.Stop();
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return TaskExecutionResult<TResult>.Failure(ex, TaskExecutionState.Error, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// 并行执行任务，同时处理多个数据项
    /// </summary>
    /// <param name="controller">任务控制器</param>
    /// <param name="results">结果集合，用于存储处理结果</param>
    /// <param name="totalItems">总项目数</param>
    /// <param name="completedItems">已完成项目计数器</param>
    /// <remarks>
    /// 此方法使用信号量控制最大并发任务数，避免资源过度消耗。
    /// 每个任务完成后会更新进度并将结果添加到结果集合中。
    /// </remarks>
    private async Task ExecuteParallelAsync(
        TaskController controller,
        ConcurrentBag<TResult> results,
        int totalItems,
        AtomicCounter completedItems)
    {
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(_maxConcurrentTasks);

        foreach (var item in _items)
        {
            if (controller.IsCancelled)
                break;

            await semaphore.WaitAsync(controller.CancellationToken);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // 检查暂停状态
                    if (controller.IsPaused)
                    {
                        // 使用异步等待而不是同步等待，避免潜在的线程阻塞问题
                        await controller.PauseEvent.WaitOneAsync(controller.CancellationToken);
                    }

                    // 在执行实际工作前再次检查取消状态，
                    // 确保即使任务已经启动（例如，从暂停状态恢复后），在执行核心逻辑前也能响应取消请求。
                    controller.CancellationToken.ThrowIfCancellationRequested();
                    
                    // 设置线程优先级（注意：此处的注释 “仅对后台任务有效” 可能需要审阅，
                    // 因为 _priority 在并行模式下似乎无条件应用，与 _isBackground 标志的行为可能不完全一致）
                    if (Thread.CurrentThread.IsThreadPoolThread)
                    {
                        Thread.CurrentThread.Priority = _priority;
                    }

                    var result = await _itemWork(item, controller.CancellationToken);
                    results.Add(result);
                    int currentCount = completedItems.Increment();
                    _progress?.Report((double)currentCount / totalItems);
                }
                finally
                {
                    semaphore.Release();
                }
            }, controller.CancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 队列执行任务，按顺序处理数据项
    /// </summary>
    /// <param name="controller">任务控制器</param>
    /// <param name="results">结果集合，用于存储处理结果</param>
    /// <param name="totalItems">总项目数</param>
    /// <param name="completedItems">已完成项目计数器</param>
    /// <remarks>
    /// 此方法按顺序处理每个数据项，一个接一个地执行。
    /// 适合需要保持处理顺序或者处理过程中有依赖关系的场景。
    /// </remarks>
    private async Task ExecuteQueueAsync(
        TaskController controller,
        ConcurrentBag<TResult> results,
        int totalItems,
        AtomicCounter completedItems)
    {
        foreach (var item in _items)
        {
            if (controller.IsCancelled)
                break;

            // 检查暂停状态
            if (controller.IsPaused)
            {
                // 使用异步等待而不是同步等待，避免阻塞UI线程
                await controller.PauseEvent.WaitOneAsync(controller.CancellationToken);
            }

            var result = _isBackground
                ? await Task.Run(async () => 
                {
                    // 设置线程优先级（仅对后台任务有效）
                    if (Thread.CurrentThread.IsThreadPoolThread)
                    {
                        Thread.CurrentThread.Priority = _priority;
                    }
                    return await _itemWork(item, controller.CancellationToken);
                }, controller.CancellationToken)
                : await _itemWork(item, controller.CancellationToken);

            results.Add(result);
            int currentCount = completedItems.Increment();
            _progress?.Report((double)currentCount / totalItems);
        }
    }

    /// <summary>
    /// 原子计数器，用于线程安全的计数操作
    /// </summary>
    /// <remarks>
    /// 使用Interlocked实现线程安全的计数操作，避免多线程并发访问时的竞争条件。
    /// 适用于并行执行环境中需要共享的计数器。
    /// </remarks>
    private class AtomicCounter
    {
        private int _count;

        /// <summary>
        /// 增加计数并返回当前值
        /// </summary>
        /// <returns>增加后的计数值</returns>
        public int Increment()
        {
            return Interlocked.Increment(ref _count);
        }
        
        /// <summary>
        /// 获取当前计数值
        /// </summary>
        public int Count => Interlocked.CompareExchange(ref _count, 0, 0);
    }
}