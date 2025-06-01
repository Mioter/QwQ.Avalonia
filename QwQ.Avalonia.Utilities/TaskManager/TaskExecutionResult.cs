namespace QwQ.Avalonia.Utilities.TaskManager;

/// <summary>
/// 任务执行结果，封装了任务执行的状态、结果和相关信息
/// </summary>
/// <typeparam name="T">结果类型，表示任务返回的数据类型</typeparam>
/// <remarks>
/// 此类设计为不可变类型，一旦创建就不能修改其属性，确保线程安全
/// 通过静态工厂方法创建不同状态的结果实例
/// </remarks>
public class TaskExecutionResult<T>
{
    /// <summary>
    /// 获取任务是否成功完成
    /// </summary>
    /// <value>如果任务最终状态为Completed，则为true；否则为false</value>
    public bool IsSuccess => FinalState == TaskExecutionState.Completed;

    /// <summary>
    /// 获取任务执行结果（单任务）
    /// </summary>
    /// <value>任务执行的返回值，如果任务未成功完成或是多任务，可能为null</value>
    public T? Result { get; private init; }

    /// <summary>
    /// 获取任务执行结果集合（多任务）
    /// </summary>
    /// <value>多任务执行的返回值集合，如果是单任务或任务未成功完成，可能为null</value>
    public IReadOnlyList<T>? Results { get; private init; }

    /// <summary>
    /// 获取错误信息
    /// </summary>
    /// <value>任务执行过程中发生的异常，如果任务成功完成，则为null</value>
    public Exception? Exception { get; private init; }

    /// <summary>
    /// 获取任务最终状态
    /// </summary>
    /// <value>表示任务执行完成后的最终状态</value>
    public TaskExecutionState FinalState { get; private init; }

    /// <summary>
    /// 获取任务执行时长
    /// </summary>
    /// <value>从任务开始到结束的时间跨度</value>
    public TimeSpan ExecutionTime { get; private init; }

    /// <summary>
    /// 获取任务是否已取消
    /// </summary>
    /// <value>如果任务最终状态为Cancelled，则为true；否则为false</value>
    public bool IsCancelled => FinalState == TaskExecutionState.Cancelled;

    /// <summary>
    /// 获取任务是否超时
    /// </summary>
    /// <value>如果任务最终状态为Timeout，则为true；否则为false</value>
    public bool IsTimeout => FinalState == TaskExecutionState.Timeout;

    /// <summary>
    /// 获取任务是否出错
    /// </summary>
    /// <value>如果任务最终状态为Error，则为true；否则为false</value>
    public bool IsError => FinalState == TaskExecutionState.Error;

    /// <summary>
    /// 获取任务是否已停止
    /// </summary>
    /// <value>如果任务最终状态为Stopped，则为true；否则为false</value>
    public bool IsStopped => FinalState == TaskExecutionState.Stopped;

    /// <summary>
    /// 创建单任务成功结果
    /// </summary>
    /// <param name="result">任务执行的返回值</param>
    /// <param name="executionTime">任务执行的时长</param>
    /// <returns>表示成功完成的任务执行结果</returns>
    /// <remarks>
    /// 用于单个任务成功完成时创建结果对象
    /// </remarks>
    public static TaskExecutionResult<T> Success(T result, TimeSpan executionTime)
    {
        return new TaskExecutionResult<T>
        {
            Result = result,
            FinalState = TaskExecutionState.Completed,
            ExecutionTime = executionTime,
        };
    }

    /// <summary>
    /// 创建多任务成功结果
    /// </summary>
    /// <param name="results">多任务执行的返回值集合</param>
    /// <param name="executionTime">任务执行的时长</param>
    /// <returns>表示成功完成的多任务执行结果</returns>
    /// <remarks>
    /// 用于多个任务成功完成时创建结果对象
    /// </remarks>
    public static TaskExecutionResult<T> Success(IReadOnlyList<T> results, TimeSpan executionTime)
    {
        ArgumentNullException.ThrowIfNull(results);

        return new TaskExecutionResult<T>
        {
            Results = results,
            FinalState = TaskExecutionState.Completed,
            ExecutionTime = executionTime,
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="exception">任务执行过程中发生的异常</param>
    /// <param name="state">任务的最终状态，通常为Error</param>
    /// <param name="executionTime">任务执行的时长</param>
    /// <returns>表示失败的任务执行结果</returns>
    /// <remarks>
    /// 用于任务执行失败时创建结果对象
    /// </remarks>
    public static TaskExecutionResult<T> Failure(
        Exception exception,
        TaskExecutionState state,
        TimeSpan executionTime
    )
    {
        ArgumentNullException.ThrowIfNull(exception);

        // 验证状态是否为失败状态
        if (
            state != TaskExecutionState.Error
            && state != TaskExecutionState.Cancelled
            && state != TaskExecutionState.Timeout
            && state != TaskExecutionState.Stopped
        )
        {
            throw new ArgumentException(
                "状态必须是失败状态（Error、Cancelled、Timeout或Stopped）",
                nameof(state)
            );
        }

        return new TaskExecutionResult<T>
        {
            Exception = exception,
            FinalState = state,
            ExecutionTime = executionTime,
        };
    }

    /// <summary>
    /// 创建取消结果
    /// </summary>
    /// <param name="executionTime">任务执行的时长</param>
    /// <returns>表示被取消的任务执行结果</returns>
    /// <remarks>
    /// 用于任务被取消时创建结果对象
    /// </remarks>
    public static TaskExecutionResult<T> Cancelled(TimeSpan executionTime)
    {
        return new TaskExecutionResult<T>
        {
            FinalState = TaskExecutionState.Cancelled,
            ExecutionTime = executionTime,
        };
    }

    /// <summary>
    /// 创建超时结果
    /// </summary>
    /// <param name="executionTime">任务执行的时长</param>
    /// <returns>表示超时的任务执行结果</returns>
    /// <remarks>
    /// 用于任务执行超时时创建结果对象
    /// </remarks>
    public static TaskExecutionResult<T> Timeout(TimeSpan executionTime)
    {
        return new TaskExecutionResult<T>
        {
            FinalState = TaskExecutionState.Timeout,
            ExecutionTime = executionTime,
        };
    }

    /// <summary>
    /// 创建已停止结果
    /// </summary>
    /// <param name="executionTime">任务执行的时长</param>
    /// <returns>表示已停止的任务执行结果</returns>
    /// <remarks>
    /// 用于任务被手动停止时创建结果对象
    /// </remarks>
    public static TaskExecutionResult<T> Stopped(TimeSpan executionTime)
    {
        return new TaskExecutionResult<T>
        {
            FinalState = TaskExecutionState.Stopped,
            ExecutionTime = executionTime,
        };
    }
}
