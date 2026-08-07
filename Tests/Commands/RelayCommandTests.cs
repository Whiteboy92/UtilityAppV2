using UtilityAppV2.Commands;
using Xunit;

namespace UtilityAppV2.Tests.Commands;

public class RelayCommandTests
{
    [Fact]
    public void CanExecute_TrueByDefaultWhenNoPredicate()
    {
        var command = new RelayCommand(_ => { });

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_HonorsPredicate()
    {
        var command = new RelayCommand(_ => { }, _ => false);

        Assert.False(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReceivesParameter()
    {
        var command = new RelayCommand(_ => { }, p => (int)p! > 5);

        Assert.True(command.CanExecute(10));
        Assert.False(command.CanExecute(1));
    }

    [Fact]
    public void Execute_InvokesActionWithParameter()
    {
        object? received = null;
        var command = new RelayCommand(p => received = p);

        command.Execute("payload");

        Assert.Equal("payload", received);
    }

    [Fact]
    public void Constructor_ThrowsOnNullExecute()
    {
        Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
    }
}
