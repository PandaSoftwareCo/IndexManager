using System.Threading.Tasks;
using System.Windows.Input;

namespace IndexManager.Framework
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object parameter);
    }
}
