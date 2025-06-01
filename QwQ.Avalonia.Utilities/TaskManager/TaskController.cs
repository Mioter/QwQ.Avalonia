namespace QwQ.Avalonia.Utilities.TaskManager;

/// <summary>
/// 任务控制器实现
/// </summary>
public class TaskController : IDisposable
{
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private volatile TaskExecutionState _state = TaskExecutionState.NotStarted;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// 获取取消令牌
    /// </summary>
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;
    
    /// <summary>
    /// 获取暂停事件
    /// </summary>
    public ManualResetEventSlim PauseEvent { get; } = new(true);

    /// <summary>
    /// 获取或设置任务优先级
    /// </summary>
    public ThreadPriority Priority
    {
        get;
        set
        {
            field = value;
            if (Thread.CurrentThread.IsAlive)
            {
                Thread.CurrentThread.Priority = value;
            }
        }
    } = ThreadPriority.Normal;

    /// <summary>
    /// 任务当前状态
    /// </summary>
    public TaskExecutionState State => _state;
    
    /// <summary>
    /// 任务是否已完成
    /// </summary>
    public bool IsCompleted => State == TaskExecutionState.Completed;
    
    /// <summary>
    /// 任务是否已取消
    /// </summary>
    public bool IsCancelled => State == TaskExecutionState.Cancelled;
    
    /// <summary>
    /// 任务是否正在运行
    /// </summary>
    public bool IsRunning => State == TaskExecutionState.Running;
    
    /// <summary>
    /// 任务是否已暂停
    /// </summary>
    public bool IsPaused => State == TaskExecutionState.Paused;
    
    /// <summary>
    /// 任务是否已停止
    /// </summary>
    public bool IsStopped => State == TaskExecutionState.Stopped;
    
    /// <summary>
    /// 启动或恢复任务
    /// </summary>
    /// <remarks>
    /// 如果任务处于未开始或已暂停状态，将其设置为运行状态并释放暂停事件
    /// </remarks>
    /// <returns>表示异步操作的任务</returns>
    public async Task StartAsync()
    {
        await _stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_state == TaskExecutionState.NotStarted || _state == TaskExecutionState.Paused)
            {
                _state = TaskExecutionState.Running;
                PauseEvent.Set();
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    /// 暂停任务
    /// </summary>
    /// <remarks>
    /// 如果任务处于运行状态，将其设置为暂停状态并重置暂停事件
    /// </remarks>
    /// <returns>表示异步操作的任务</returns>
    public async Task PauseAsync()
    {
        await _stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_state == TaskExecutionState.Running)
            {
                _state = TaskExecutionState.Paused;
                PauseEvent.Reset();
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    /// <summary>
    /// 停止任务
    /// </summary>
    /// <remarks>
    /// 如果任务处于运行或暂停状态，将其设置为停止状态，释放暂停事件并取消任务
    /// </remarks>
    /// <returns>表示异步操作的任务</returns>
    public async Task StopAsync()
    {
        await _stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_state == TaskExecutionState.Running || _state == TaskExecutionState.Paused)
            {
                _state = TaskExecutionState.Stopped;
                PauseEvent.Set();
                _cancellationTokenSource.Cancel();
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    /// <summary>
    /// 取消任务
    /// </summary>
    /// <remarks>
    /// 如果任务未完成且未取消，将其设置为取消状态，释放暂停事件并取消任务
    /// </remarks>
    /// <returns>表示异步操作的任务</returns>
    public async Task CancelAsync()
    {
        await _stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_state != TaskExecutionState.Completed && _state != TaskExecutionState.Cancelled)
            {
                _state = TaskExecutionState.Cancelled;
                PauseEvent.Set();
                _cancellationTokenSource.Cancel();
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    /// <summary>
    /// 设置任务状态为完成
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    internal async Task SetCompletedAsync() => await SetStateAsync(TaskExecutionState.Completed);
    
    /// <summary>
    /// 设置任务状态为错误
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    internal async Task SetErrorAsync() => await SetStateAsync(TaskExecutionState.Error);
    
    /// <summary>
    /// 设置任务状态为超时
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    internal async Task SetTimeoutAsync() => await SetStateAsync(TaskExecutionState.Timeout);
    
    /// <summary>
    /// 异步设置任务状态
    /// </summary>
    private async Task SetStateAsync(TaskExecutionState state)
    {
        await _stateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _state = state;
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    /// <remarks>
    /// 释放暂停事件、取消令牌源和状态锁等非托管资源
    /// </remarks>
    public void Dispose()
    {
        _stateLock.Dispose();
        PauseEvent.Dispose();
        _cancellationTokenSource.Dispose();
        
        GC.SuppressFinalize(this);
    }
}