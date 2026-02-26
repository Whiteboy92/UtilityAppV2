using System.Windows.Input;

namespace UtilityAppV2.Commands
{
    public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        : ICommand
    {
        private readonly Action<object?> execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}