using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace GameShell;

public static class ShellCommands
{
    /// <summary>Runs a command if it can execute, awaiting it when it is asynchronous.</summary>
    public static async Task RunAsync(ICommand command)
    {
        if (!command.CanExecute(null))
        {
            return;
        }

        if (command is IAsyncRelayCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(null);
        }
        else
        {
            command.Execute(null);
        }
    }
}
