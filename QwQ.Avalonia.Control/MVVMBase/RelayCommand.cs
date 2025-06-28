using System.Windows.Input;

namespace QwQ.Avalonia.Control.MVVMBase;

internal class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => execute((T?)parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}