using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QwQ.Avalonia.Utilities.TaskManager;

namespace Sample.UtilitiesExamples;

/// <summary>
/// TaskManager使用示例演示
/// </summary>
public static class TaskManagerExample
{
    /// <summary>
    /// 运行TaskManager示例
    /// </summary>
    public static async Task RunExamples()
    {
        Console.WriteLine("=== TaskManager 使用示例 ===");

        await BasicTaskExample();
        await TaskWithControllerExample();
        await AsyncTaskExample();
        await ErrorHandlingExample();
        await TimeoutExample();
        await BackgroundVsForegroundExample();
        await MultiProjectParallelExample();
        await MultiProjectQueueExample();

        Console.WriteLine("\n=== 所有示例执行完成 ===");
    }

    /// <summary>
    /// 基本任务示例
    /// </summary>
    private static async Task BasicTaskExample()
    {
        Console.WriteLine("\n--- 基本任务示例 ---");

        var result = await TaskManager
            .CreateTask(() =>
            {
                Console.WriteLine("正在执行计算任务...");
                Thread.Sleep(2000);
                return 42;
            })
            .SetDelay(500) // 延迟500ms开始
            .SetCompleteCallback(result => Console.WriteLine($"任务完成，结果: {result}"))
            .RunAsync();

        Console.WriteLine(
            $"任务执行结果: {result.Result}, 耗时: {result.ExecutionTime.TotalMilliseconds}ms"
        );
    }

    /// <summary>
    /// 带控制器的任务示例
    /// </summary>
    private static async Task TaskWithControllerExample()
    {
        Console.WriteLine("\n--- 带控制器的任务示例 ---");

        TaskController controller = new();

        // 在另一个线程中控制任务
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            Console.WriteLine("[控制器] 暂停任务");
            await controller.PauseAsync();

            await Task.Delay(3000);
            Console.WriteLine("[控制器] 恢复任务");
            await controller.StartAsync();
        });

        var result = await TaskManager
            .CreateTask(token =>
            {
                for (int i = 1; i <= 5; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Console.WriteLine($"处理步骤 {i}/5");
                    Thread.Sleep(1000);

                    // 检查暂停状态
                    if (!controller.IsPaused)
                        continue;

                    Console.WriteLine("[任务] 检测到暂停，等待恢复...");
                    controller.PauseEvent.Wait(token);
                    Console.WriteLine("[任务] 已恢复执行");
                }
                return "任务完成";
            })
            .SetController(controller)
            .SetCompleteCallback(result => Console.WriteLine($"控制器任务完成: {result}"))
            .RunAsync();

        Console.WriteLine(
            $"控制器任务状态: {result.FinalState}, 耗时: {result.ExecutionTime.TotalSeconds:F1}秒"
        );
    }

    /// <summary>
    /// 异步任务示例
    /// </summary>
    private static async Task AsyncTaskExample()
    {
        Console.WriteLine("\n--- 异步任务示例 ---");

        var result = await TaskManager
            .CreateTask(async () =>
            {
                Console.WriteLine("开始异步网络请求模拟...");
                await Task.Delay(1500); // 模拟网络请求
                return $"数据获取完成 - {DateTime.Now:HH:mm:ss}";
            })
            .SetTimeout(
                3000,
                elapsed => Console.WriteLine($"请求超时，已用时: {elapsed.TotalSeconds:F1}秒")
            )
            .SetCompleteCallback(data => Console.WriteLine($"接收到数据: {data}"))
            .RunAsync();

        if (result.IsSuccess)
        {
            Console.WriteLine($"异步任务成功: {result.Result}");
        }
    }

    /// <summary>
    /// 错误处理示例
    /// </summary>
    private static async Task ErrorHandlingExample()
    {
        Console.WriteLine("\n--- 错误处理示例 ---");

        var result = await TaskManager
            .CreateTask(() =>
            {
                Console.WriteLine("模拟任务执行中...");
                Thread.Sleep(1000);
                throw new InvalidOperationException("模拟的业务异常");
            })
            .SetErrorHandler(ex => Console.WriteLine($"[错误处理器] 捕获异常: {ex.Message}"))
            .SetCompleteCallback(_ => Console.WriteLine("这个回调不会被执行"))
            .RunAsync();

        Console.WriteLine(
            $"错误任务状态: {result.FinalState}, 异常类型: {result.Exception?.GetType().Name}"
        );
    }

    /// <summary>
    /// 超时处理示例
    /// </summary>
    private static async Task TimeoutExample()
    {
        Console.WriteLine("\n--- 超时处理示例 ---");

        var result = await TaskManager
            .CreateTask(() =>
            {
                Console.WriteLine("开始长时间运行的任务...");
                Thread.Sleep(5000); // 睡眠5秒
                return "任务完成";
            })
            .SetTimeout(
                2000,
                elapsed =>
                    Console.WriteLine($"[超时处理器] 任务超时，已运行 {elapsed.TotalSeconds:F1} 秒")
            )
            .SetErrorHandler(ex => Console.WriteLine($"任务异常: {ex.Message}"))
            .RunAsync();

        Console.WriteLine(
            $"超时任务状态: {result.FinalState}, 实际耗时: {result.ExecutionTime.TotalSeconds:F1}秒"
        );
    }

    /// <summary>
    /// 前台与后台任务对比示例
    /// </summary>
    private static async Task BackgroundVsForegroundExample()
    {
        Console.WriteLine("\n--- 前台与后台任务对比示例 ---");

        // 后台任务
        var backgroundTask = TaskManager
            .CreateTask(
                () =>
                {
                    Console.WriteLine($"[后台任务] 线程优先级: {Thread.CurrentThread.Priority}");
                    Thread.Sleep(1000);
                    return "后台任务完成";
                },
                isBackground: true
            )
            .SetCompleteCallback(result => Console.WriteLine($"[后台] {result}"));

        // 前台任务
        var foregroundTask = TaskManager
            .CreateTask(
                () =>
                {
                    Console.WriteLine($"[前台任务] 线程优先级: {Thread.CurrentThread.Priority}");
                    Thread.Sleep(1000);
                    return "前台任务完成";
                },
                isBackground: false
            )
            .SetCompleteCallback(result => Console.WriteLine($"[前台] {result}"));

        // 并行执行
        var tasks = new[] { backgroundTask.RunAsync(), foregroundTask.RunAsync() };
        var results = await Task.WhenAll(tasks);

        Console.WriteLine(
            $"两个任务都已完成，后台任务: {results[0].IsSuccess}, 前台任务: {results[1].IsSuccess}"
        );
    }

    /// <summary>
    /// 无返回值任务示例
    /// </summary>
    public static async Task VoidTaskExample()
    {
        Console.WriteLine("\n--- 无返回值任务示例 ---");

        var result = await TaskManager
            .CreateTask(() =>
            {
                Console.WriteLine("执行清理操作...");
                Thread.Sleep(800);
                Console.WriteLine("清理完成");
            })
            .SetDelay(200)
            .SetCompleteCallback(_ => Console.WriteLine("清理任务回调执行"))
            .RunAsync();

        Console.WriteLine($"无返回值任务完成: {result.IsSuccess}");
    }

    /// <summary>
    /// 链式调用顺序测试
    /// </summary>
    public static async Task ChainOrderTest()
    {
        Console.WriteLine("\n--- 链式调用顺序测试 ---");

        // 测试不同的链式调用顺序
        var result = await TaskManager
            .CreateTask(() => "测试结果")
            .SetCompleteCallback(r => Console.WriteLine($"完成: {r}"))
            .SetTimeout(5000)
            .SetErrorHandler(ex => Console.WriteLine($"错误: {ex.Message}"))
            .SetDelay(100)
            .RunAsync();

        Console.WriteLine($"链式调用测试完成: {result.IsSuccess}");
    }

    /// <summary>
    /// 多项目并行执行示例
    /// </summary>
    private static async Task MultiProjectParallelExample()
    {
        Console.WriteLine("\n--- 多项目并行执行示例 ---");

        // 创建测试数据
        var items = Enumerable.Range(1, 30).Select(i => new TestItem($"Item {i}", i * 100));
        var progress = new Progress<double>(p => Console.WriteLine($"进度: {p:P2}"));

        // 创建任务控制器用于演示暂停/继续功能
        var controller = new TaskController();

        // 在另一个线程中控制任务
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            Console.WriteLine("[控制器] 暂停任务");
            await controller.PauseAsync();

            await Task.Delay(3000);
            Console.WriteLine("[控制器] 取消任务");
            await controller.CancelAsync();
        });

        // 创建并行执行的多项目任务
        var result = await TaskManager
            .CreateMultiTask(
                items: items,
                itemWork: async item =>
                {
                    Console.WriteLine($"开始处理 {item.Name}...");
                    await Task.Delay(item.ProcessingTime); // 模拟处理时间
                    Console.WriteLine($"{item.Name} 处理完成");
                    return $"处理结果: {item.Name}";
                }
            )
            .SetMaxConcurrentTasks(3) // 设置最大并发数为3
            .SetController(controller)
            .SetProgress(progress)
            .SetErrorHandler(ex => Console.WriteLine($"处理出错: {ex.Message}"))
            .SetCompleteCallback(results =>
                Console.WriteLine($"所有项目处理完成，共 {results.Count()} 个结果")
            )
            .RunAsync();

        Console.WriteLine($"并行任务执行状态: {result.FinalState}");
        Console.WriteLine($"总耗时: {result.ExecutionTime.TotalSeconds:F1}秒");
        Console.WriteLine("处理结果:");

        if (result.Results != null)
            foreach (string itemResult in result.Results)
            {
                Console.WriteLine($"- {itemResult}");
            }
    }

    /// <summary>
    /// 多项目队列执行示例
    /// </summary>
    private static async Task MultiProjectQueueExample()
    {
        Console.WriteLine("\n--- 多项目队列执行示例 ---");

        // 创建测试数据
        var items = Enumerable.Range(1, 5).Select(i => new TestItem($"Queue Item {i}", i * 200));
        var progress = new Progress<double>(p => Console.WriteLine($"进度: {p:P2}"));

        // 创建任务控制器用于演示暂停/继续功能
        var controller = new TaskController();

        // 在另一个线程中控制任务
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            Console.WriteLine("[控制器] 暂停任务");
            await controller.PauseAsync();

            await Task.Delay(3000);
            Console.WriteLine("[控制器] 恢复任务");
            await controller.StartAsync();
        });

        // 创建队列执行的多项目任务
        var result = await TaskManager
            .CreateMultiTask(
                items: items,
                itemWork: async item =>
                {
                    Console.WriteLine($"开始处理 {item.Name}...");
                    await Task.Delay(item.ProcessingTime); // 模拟处理时间
                    return $"处理结果: {item.Name}";
                },
                isParallel: false
            )
            .SetController(controller)
            .SetProgress(progress)
            .SetErrorHandler(ex => Console.WriteLine($"处理出错: {ex.Message}"))
            .SetCompleteCallback(results =>
                Console.WriteLine($"所有项目处理完成，共 {results.Count()} 个结果")
            )
            .RunAsync();

        Console.WriteLine($"队列任务执行状态: {result.FinalState}");
        Console.WriteLine($"总耗时: {result.ExecutionTime.TotalSeconds:F1}秒");
        Console.WriteLine("处理结果:");

        if (result.Results == null)
            return;
        
        foreach (string itemResult in result.Results)
        {
            Console.WriteLine($"- {itemResult}");
        }
    }

    /// <summary>
    /// 测试项目类
    /// </summary>
    private class TestItem(string name, int processingTime)
    {
        public string Name { get; } = name;
        public int ProcessingTime { get; } = processingTime;
    }
}
