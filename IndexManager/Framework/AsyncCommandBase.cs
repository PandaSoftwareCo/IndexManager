using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IndexManager.Framework
{
    public abstract class AsyncCommandBase : IAsyncCommand
    {
        public abstract bool CanExecute(object parameter);
        public abstract Task ExecuteAsync(object parameter);

        public async void Execute(object parameter)
        {
            //Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            var window = Application.Current.MainWindow;
            window.Cursor = Cursors.Wait;
#if DEBUG
            await ExecuteAsync(parameter);
#else
            try
            {
                await ExecuteAsync(parameter);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(e);
                Debug.WriteLine(e.ToString());
            }
#endif
            window.Cursor = Cursors.Arrow;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        protected void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
