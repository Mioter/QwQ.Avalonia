namespace QwQ.Avalonia.Helper;

/// <summary>
///     扩展方法，用于转换
///     <see cref="Action" />  委托为
///     <see cref="System.IObservable{T}" /> 类型以满足
///     <see cref="System.IObservable{T}.Subscribe"></see> 参数类型要求。
/// </summary>
public static class ObservableExtensions
{
    public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext)
    {
        return observable.Subscribe(new ActionObserver<T>(onNext));
    }

    private class ActionObserver<T>(Action<T> onNext) : IObserver<T>
    {

        public void OnNext(T value)
        {
            onNext(value);
        }

        public void OnError(Exception error)
        {
            // 可选：处理错误
        }

        public void OnCompleted()
        {
            // 可选：处理完成事件
        }
    }
}
