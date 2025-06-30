using System;
using System.Windows.Input;

public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    /// <summary>
    /// Конструктор для команд без параметрів
    /// </summary>
    public RelayCommand(Action execute) : this(_ => execute()) { }

    /// <summary>
    /// Конструктор для команд з параметрами
    /// </summary>
    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? (_ => true);
    }

    /// <summary>
    /// Можливість виконання команди
    /// </summary>
    public bool CanExecute(object parameter) => _canExecute(parameter);

    /// <summary>
    /// Виконання команди
    /// </summary>
    public void Execute(object parameter) => _execute(parameter);

    /// <summary>
    /// Подія зміни стану можливості виконання команди
    /// </summary>
    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Метод для примусового виклику оновлення стану команди
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}