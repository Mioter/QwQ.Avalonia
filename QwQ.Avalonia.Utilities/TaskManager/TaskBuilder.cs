using System.Diagnostics;
using QwQ.Avalonia.Helper;

namespace QwQ.Avalonia.Utilities.TaskManager;

/// <summary>
/// 任务构建器，提供任务的创建和管理功能
/// 支持设置延迟、超时、控制器、错误处理、完成回调和进度报告等高级功能
/// 通过链式调用方式提供流畅的API，使任务配置更加直观和简洁
/// </summary>
/// <typeparam name="TResult">任务返回类型，表示任务执行的结果类型</typeparam>
public class TaskBuilder<TResult>
{
    private readonly Func<CancellationToken, Task<TResult>> _work;
    private readonly bool _isBackground;

    private TimeSpan? _delay;
    private TimeSpan? _timeout;
    private Action<TimeSpan>? _timeoutHandler;
    private TaskController? _controller;
    private Action<Exception>? _errorHandler;
    private Action<TResult>? _completeCallback;
    private IProgress<double>? _progress;
    private ThreadPriority _priority = ThreadPriority.Normal;

    /// <summary>
    /// 初始化任务构建器
    /// </summary>
    /// <param name="work">任务工作委托</param>
    /// <param name="isBackground">是否为后台任务</param>
    internal TaskBuilder(Func<CancellationToken, Task<TResult>> work, bool isBackground)
    {
        _work = work;
        _isBackground = isBackground;
    }

    /// <summary>
    /// 设置任务延迟时间，任务将在指定的延迟时间后开始执行
    /// </summary>
    /// <param name="delay">延迟时间</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    /// <exception cref="ArgumentOutOfRangeException">当延迟时间为负数时抛出</exception>
    public TaskBuilder<TResult> SetDelay(TimeSpan delay)
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
    public TaskBuilder<TResult> SetDelay(int delayMilliseconds)
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
    public TaskBuilder<TResult> SetTimeout(TimeSpan timeout, Action<TimeSpan>? timeoutHandler = null)
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
    public TaskBuilder<TResult> SetTimeout(int timeoutMilliseconds, Action<TimeSpan>? timeoutHandler = null)
    {
        return SetTimeout(TimeSpan.FromMilliseconds(timeoutMilliseconds), timeoutHandler);
    }

    /// <summary>
    /// 设置任务控制器，用于控制任务的执行状态（暂停、恢复、取消等）
    /// </summary>
    /// <param name="controller">任务控制器实例</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    /// <exception cref="ArgumentNullException">当controller为null时抛出</exception>
    public TaskBuilder<TResult> SetController(TaskController controller)
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
    public TaskBuilder<TResult> SetErrorHandler(Action<Exception> errorHandler)
    {
        ArgumentNullException.ThrowIfNull(errorHandler);
        
        _errorHandler = errorHandler;
        return this;
    }

    /// <summary>
    /// 设置完成回调方法，在任务成功完成时调用
    /// </summary>
    /// <param name="completeCallback">完成回调方法，接收任务结果作为参数</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    /// <exception cref="ArgumentNullException">当completeCallback为null时抛出</exception>
    public TaskBuilder<TResult> SetCompleteCallback(Action<TResult> completeCallback)
    {
        ArgumentNullException.ThrowIfNull(completeCallback);
        
        _completeCallback = completeCallback;
        return this;
    }

    /// <summary>
    /// 设置任务优先级，影响任务在线程池中的执行优先级
    /// </summary>
    /// <param name="priority">任务优先级</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    public TaskBuilder<TResult> SetPriority(ThreadPriority priority)
    {
        _priority = priority;
        return this;
    }
    
    /// <summary>
    /// 设置进度报告器，用于报告任务执行进度
    /// </summary>
    /// <param name="progress">进度报告器，接收0到1之间的值表示进度百分比</param>
    /// <returns>当前构建器实例，用于链式调用</returns>
    public TaskBuilder<TResult> SetProgress(IProgress<double> progress)
    {
        _progress = progress;
        return this;
    }

    /// <summary>
    /// 运行任务并返回执行结果
    /// </summary>
    /// <returns>任务执行结果，包含执行状态、返回值和执行时间等信息</returns>
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
            // 延迟执行
            if (_delay.HasValue)
            {
                await Task.Delay(_delay.Value, controller.CancellationToken);
            }

            // 启动任务
            await controller.StartAsync();
            
            // 报告初始进度
            _progress?.Report(0.0);

            // 创建任务
            var mainTask = ExecuteTask(controller);

            // 处理超时
            if (_timeout.HasValue)
            {
                using var timeoutCts = new CancellationTokenSource(_timeout.Value);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(controller.CancellationToken, timeoutCts.Token);
                
                try
                {
                    var result = await mainTask.WaitAsync(linkedCts.Token);
                    await controller.SetCompletedAsync();

                    // 报告完成进度
                    _progress?.Report(1.0);
                    
                    // 执行完成回调
                    if (result.IsSuccess && result.Result != null)
                    {
                        _completeCallback?.Invoke(result.Result);
                    }

                    return result;
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    await controller.SetTimeoutAsync();
                    _timeoutHandler?.Invoke(stopwatch.Elapsed);
                    return TaskExecutionResult<TResult>.Timeout(stopwatch.Elapsed);
                }
            }
            else
            {
                var result = await mainTask;
                await controller.SetCompletedAsync();

                // 报告完成进度
                _progress?.Report(1.0);
                
                // 执行完成回调
                if (result.IsSuccess && result.Result != null)
                {
                    _completeCallback?.Invoke(result.Result);
                }

                return result;
            }
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
    /// 执行任务的核心方法
    /// </summary>
    /// <param name="controller">任务控制器，用于控制任务执行状态</param>
    /// <returns>任务执行结果，包含执行状态和返回值</returns>
    /// <remarks>
    /// 此方法处理任务的实际执行逻辑，包括：
    /// - 检查任务是否被暂停，如果是则等待恢复
    /// - 根据配置决定是在线程池中执行还是在当前线程执行
    /// - 捕获并处理执行过程中的异常
    /// </remarks>
    private async Task<TaskExecutionResult<TResult>> ExecuteTask(TaskController controller)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 检查暂停状态
            if (controller.IsPaused)
            {
                // 使用异步等待而不是同步等待，避免阻塞UI线程
                await controller.PauseEvent.WaitOneAsync(controller.CancellationToken);
            }

            // 根据配置决定执行方式
            TResult result;
            if (_isBackground)
            {
                result = await Task.Run(() => _work(controller.CancellationToken), controller.CancellationToken);
            }
            else
            {
                result = await _work(controller.CancellationToken);
            }

            stopwatch.Stop();
            return TaskExecutionResult<TResult>.Success(result, stopwatch.Elapsed);
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
}