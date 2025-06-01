namespace QwQ.Avalonia.Utilities.TaskManager;

/// <summary>
/// 任务管理器，提供灵活的任务创建和管理功能
/// </summary>
/// <remarks>
/// TaskManager是一个静态工厂类，用于创建和管理各种类型的任务。<br/>
/// 支持创建同步/异步、可取消/不可取消、单任务/多任务等多种类型的任务。<br/>
/// 所有创建的任务都支持暂停、恢复、取消、超时等高级控制功能。
/// </remarks>
public static class TaskManager
{
    /// <summary>
    /// 创建一个新的同步任务
    /// </summary>
    /// <typeparam name="T">任务返回类型，表示任务执行结果的数据类型</typeparam>
    /// <param name="taskFactory">任务工厂方法，用于创建并执行任务</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>任务构建器，用于进一步配置和执行任务</returns>
    /// <remarks>
    /// 此方法创建一个同步执行的任务，不支持取消操作。
    /// 适用于简单、快速的操作，不需要取消功能的场景。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当taskFactory为null时抛出</exception>
    public static TaskBuilder<T> CreateTask<T>(Func<T> taskFactory, bool isBackground = true)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        return new TaskBuilder<T>(_ => Task.FromResult(taskFactory()), isBackground);
    }

    /// <summary>
    /// 创建一个新的异步任务
    /// </summary>
    /// <typeparam name="T">任务返回类型，表示任务执行结果的数据类型</typeparam>
    /// <param name="taskFactory">异步任务工厂方法，用于创建并执行异步任务</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>任务构建器，用于进一步配置和执行任务</returns>
    /// <remarks>
    /// 此方法创建一个异步执行的任务，不支持取消操作。<br/>
    /// 适用于需要异步执行但不需要取消功能的场景。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当taskFactory为null时抛出</exception>
    public static TaskBuilder<T> CreateTask<T>(Func<Task<T>> taskFactory, bool isBackground = true)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        return new TaskBuilder<T>(_ => taskFactory(), isBackground);
    }

    /// <summary>
    /// 创建一个新的可取消异步任务
    /// </summary>
    /// <typeparam name="T">任务返回类型，表示任务执行结果的数据类型</typeparam>
    /// <param name="taskFactory">可取消异步任务工厂方法，接受取消令牌作为参数</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>任务构建器，用于进一步配置和执行任务</returns>
    /// <remarks>
    /// 此方法创建一个支持取消操作的异步任务。<br/>
    /// 适用于需要长时间运行且可能需要中途取消的异步操作。<br/>
    /// 任务工厂方法接收一个CancellationToken参数，可以用于检测取消请求。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当taskFactory为null时抛出</exception>
    public static TaskBuilder<T> CreateTask<T>(
        Func<CancellationToken, Task<T>> taskFactory,
        bool isBackground = true
    )
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        return new TaskBuilder<T>(taskFactory, isBackground);
    }

    /// <summary>
    /// 创建一个新的可取消同步任务
    /// </summary>
    /// <typeparam name="T">任务返回类型，表示任务执行结果的数据类型</typeparam>
    /// <param name="taskFactory">可取消同步任务工厂方法，接受取消令牌作为参数</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>任务构建器，用于进一步配置和执行任务</returns>
    /// <remarks>
    /// 此方法创建一个支持取消操作的同步任务。<br/>
    /// 适用于需要支持取消功能的同步操作。<br/>
    /// 任务工厂方法接收一个CancellationToken参数，可以用于检测取消请求。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当taskFactory为null时抛出</exception>
    public static TaskBuilder<T> CreateTask<T>(
        Func<CancellationToken, T> taskFactory,
        bool isBackground = true
    )
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        return new TaskBuilder<T>(token => Task.FromResult(taskFactory(token)), isBackground);
    }

    /// <summary>
    /// 创建一个无返回值的同步任务
    /// </summary>
    /// <param name="action">任务动作，表示要执行的操作</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>任务构建器，用于进一步配置和执行任务</returns>
    /// <remarks>
    /// 此方法创建一个无返回值的同步任务，不支持取消操作。<br/>
    /// 适用于不需要返回结果且不需要取消功能的简单操作。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当action为null时抛出</exception>
    public static TaskBuilder<object?> CreateTask(Action action, bool isBackground = true)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new TaskBuilder<object?>(
            _ =>
            {
                action();
                return Task.FromResult<object?>(null);
            },
            isBackground
        );
    }

    /// <summary>
    /// 创建一个无返回值的异步任务
    /// </summary>
    /// <param name="asyncAction">异步任务动作，表示要执行的异步操作</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>任务构建器，用于进一步配置和执行任务</returns>
    /// <remarks>
    /// 此方法创建一个无返回值的异步任务，不支持取消操作。<br/>
    /// 适用于不需要返回结果且不需要取消功能的异步操作。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当asyncAction为null时抛出</exception>
    public static TaskBuilder<object?> CreateTask(Func<Task> asyncAction, bool isBackground = true)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);

        return new TaskBuilder<object?>(
            async _ =>
            {
                await asyncAction();
                return null;
            },
            isBackground
        );
    }

    /// <summary>
    /// 创建一个无返回值的可取消同步任务
    /// </summary>
    /// <param name="cancellableAction">可取消任务动作，接受取消令牌作为参数</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>任务构建器，用于进一步配置和执行任务</returns>
    /// <remarks>
    /// 此方法创建一个无返回值且支持取消操作的同步任务。<br/>
    /// 适用于不需要返回结果但需要支持取消功能的操作。<br/>
    /// 任务动作接收一个CancellationToken参数，可以用于检测取消请求。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当cancellableAction为null时抛出</exception>
    public static TaskBuilder<object?> CreateTask(
        Action<CancellationToken> cancellableAction,
        bool isBackground = true
    )
    {
        ArgumentNullException.ThrowIfNull(cancellableAction);

        return new TaskBuilder<object?>(
            token =>
            {
                cancellableAction(token);
                return Task.FromResult<object?>(null);
            },
            isBackground
        );
    }

    /// <summary>
    /// 创建一个无返回值的可取消异步任务
    /// </summary>
    /// <param name="cancellableAsyncAction">可取消异步任务动作，接受取消令牌作为参数</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>任务构建器，用于进一步配置和执行任务</returns>
    /// <remarks>
    /// 此方法创建一个无返回值且支持取消操作的异步任务。<br/>
    /// 适用于不需要返回结果但需要支持取消功能的异步操作。<br/>
    /// 任务动作接收一个CancellationToken参数，可以用于检测取消请求。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当cancellableAsyncAction为null时抛出</exception>
    public static TaskBuilder<object?> CreateTask(
        Func<CancellationToken, Task> cancellableAsyncAction,
        bool isBackground = true
    )
    {
        ArgumentNullException.ThrowIfNull(cancellableAsyncAction);

        return new TaskBuilder<object?>(
            async token =>
            {
                await cancellableAsyncAction(token);
                return null;
            },
            isBackground
        );
    }

    /// <summary>
    /// 创建一个多项目异步任务
    /// </summary>
    /// <typeparam name="TItem">项目类型，表示要处理的数据项类型</typeparam>
    /// <typeparam name="TResult">任务返回类型，表示处理结果的数据类型</typeparam>
    /// <param name="items">项目集合，要处理的数据项集合</param>
    /// <param name="itemWork">项目工作委托，处理单个数据项的异步方法</param>
    /// <param name="isParallel">执行模式（true 并行执行/ false 队列执行），默认为并行执行</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>多项目任务构建器，用于进一步配置和执行多项目任务</returns>
    /// <remarks>
    /// 此方法创建一个处理多个数据项的异步任务。<br/>
    /// 可以选择并行或队列方式执行，并行方式适合相互独立的任务，队列方式适合需要按顺序处理的任务。<br/>
    /// 每个数据项的处理都是异步的，适合IO密集型操作。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当items或itemWork为null时抛出</exception>
    public static MultiTaskBuilder<TItem, TResult> CreateMultiTask<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, Task<TResult>> itemWork,
        bool isParallel = true,
        bool isBackground = true
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemWork);

        return new MultiTaskBuilder<TItem, TResult>(items, itemWork, isParallel, isBackground);
    }

    /// <summary>
    /// 创建一个多项目同步任务
    /// </summary>
    /// <typeparam name="TItem">项目类型，表示要处理的数据项类型</typeparam>
    /// <typeparam name="TResult">任务返回类型，表示处理结果的数据类型</typeparam>
    /// <param name="items">项目集合，要处理的数据项集合</param>
    /// <param name="itemWork">项目工作委托，处理单个数据项的同步方法</param>
    /// <param name="isParallel">执行模式（true 并行执行/ false 队列执行），默认为并行执行</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>多项目任务构建器，用于进一步配置和执行多项目任务</returns>
    /// <remarks>
    /// 此方法创建一个处理多个数据项的同步任务。<br/>
    /// 可以选择并行或队列方式执行，并行方式适合相互独立的任务，队列方式适合需要按顺序处理的任务。<br/>
    /// 每个数据项的处理都是同步的，适合CPU密集型操作。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当items或itemWork为null时抛出</exception>
    public static MultiTaskBuilder<TItem, TResult> CreateMultiTask<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, TResult> itemWork,
        bool isParallel = true,
        bool isBackground = true
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemWork);

        return new MultiTaskBuilder<TItem, TResult>(
            items,
            item => Task.FromResult(itemWork(item)),
            isParallel,
            isBackground
        );
    }

    /// <summary>
    /// 创建一个无返回值的多项目异步任务
    /// </summary>
    /// <typeparam name="TItem">项目类型，表示要处理的数据项类型</typeparam>
    /// <param name="items">项目集合，要处理的数据项集合</param>
    /// <param name="itemWork">项目工作委托，处理单个数据项的异步方法</param>
    /// <param name="isParallel">执行模式（true 并行执行/ false 队列执行），默认为并行执行</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>多项目任务构建器，用于进一步配置和执行多项目任务</returns>
    /// <remarks>
    /// 此方法创建一个处理多个数据项且无返回值的异步任务。<br/>
    /// 可以选择并行或队列方式执行，并行方式适合相互独立的任务，队列方式适合需要按顺序处理的任务。<br/>
    /// 每个数据项的处理都是异步的，适合IO密集型操作。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当items或itemWork为null时抛出</exception>
    public static MultiTaskBuilder<TItem, object?> CreateMultiTask<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, Task> itemWork,
        bool isParallel = true,
        bool isBackground = true
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemWork);

        return new MultiTaskBuilder<TItem, object?>(
            items,
            async item =>
            {
                await itemWork(item);
                return null;
            },
            isParallel,
            isBackground
        );
    }

    /// <summary>
    /// 创建一个无返回值的多项目同步任务
    /// </summary>
    /// <typeparam name="TItem">项目类型，表示要处理的数据项类型</typeparam>
    /// <param name="items">项目集合，要处理的数据项集合</param>
    /// <param name="itemWork">项目工作委托，处理单个数据项的同步方法</param>
    /// <param name="isParallel">执行模式（true 并行执行/ false 队列执行），默认为并行执行</param>
    /// <param name="isBackground">是否为后台任务，默认为true。设为true时将在线程池中执行</param>
    /// <returns>多项目任务构建器，用于进一步配置和执行多项目任务</returns>
    /// <remarks>
    /// 此方法创建一个处理多个数据项且无返回值的同步任务。<br/>
    /// 可以选择并行或队列方式执行，并行方式适合相互独立的任务，队列方式适合需要按顺序处理的任务。<br/>
    /// 每个数据项的处理都是同步的，适合CPU密集型操作。
    /// </remarks>
    /// <exception cref="ArgumentNullException">当items或itemWork为null时抛出</exception>
    public static MultiTaskBuilder<TItem, object?> CreateMultiTask<TItem>(
        IEnumerable<TItem> items,
        Action<TItem> itemWork,
        bool isParallel = true,
        bool isBackground = true
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemWork);

        return new MultiTaskBuilder<TItem, object?>(
            items,
            item =>
            {
                itemWork(item);
                return Task.FromResult<object?>(null);
            },
            isParallel,
            isBackground
        );
    }
}
