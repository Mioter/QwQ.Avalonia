namespace QwQ.Avalonia.Utilities.TaskManager;

/// <summary>
/// 任务执行状态枚举
/// </summary>
/// <remarks>
/// 表示任务在其生命周期中可能处于的各种状态
/// 状态转换通常遵循以下规则：
/// - NotStarted -> Running (通过StartAsync)
/// - Running -> Paused (通过PauseAsync)
/// - Paused -> Running (通过StartAsync)
/// - Running/Paused -> Stopped (通过StopAsync)
/// - Running/Paused -> Cancelled (通过CancelAsync)
/// - Running -> Completed (任务正常完成)
/// - Running -> Error (任务执行出错)
/// - Running -> Timeout (任务执行超时)
/// </remarks>
public enum TaskExecutionState
{
    /// <summary>
    /// 未开始 - 任务已创建但尚未开始执行
    /// </summary>
    NotStarted,

    /// <summary>
    /// 运行中 - 任务正在执行中
    /// </summary>
    Running,

    /// <summary>
    /// 已暂停 - 任务已暂停执行，可以恢复
    /// </summary>
    Paused,

    /// <summary>
    /// 已停止 - 任务已被用户手动停止
    /// </summary>
    /// <remarks>
    /// 与Cancelled不同，Stopped通常表示用户主动停止，而非因错误或其他原因取消
    /// </remarks>
    Stopped,

    /// <summary>
    /// 已取消 - 任务已被取消执行
    /// </summary>
    Cancelled,

    /// <summary>
    /// 已完成 - 任务已成功完成
    /// </summary>
    Completed,

    /// <summary>
    /// 发生错误 - 任务执行过程中发生异常
    /// </summary>
    Error,

    /// <summary>
    /// 超时 - 任务执行时间超过预设的超时时间
    /// </summary>
    Timeout,
}
