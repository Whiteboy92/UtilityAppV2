using System.ComponentModel;
using UtilityAppV2.Core;
using Xunit;

namespace UtilityAppV2.Tests.Core;

public class BaseViewModelTests
{
    private sealed class Probe : BaseViewModel
    {
        public int Value
        {
            get;
            set => SetProperty(ref field, value);
        }
    }

    [Fact]
    public void SetProperty_RaisesPropertyChangedOnChange()
    {
        var probe = new Probe();
        var raised = new List<string?>();
        probe.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        probe.Value = 42;

        Assert.Equal(42, probe.Value);
        Assert.Contains(nameof(Probe.Value), raised);
    }

    [Fact]
    public void SetProperty_DoesNotRaiseWhenValueUnchanged()
    {
        var probe = new Probe { Value = 7 };
        int events = 0;
        probe.PropertyChanged += (_, _) => events++;

        probe.Value = 7; // same value

        Assert.Equal(0, events);
    }

    [Fact]
    public void SetProperty_RaisesForEachDistinctChange()
    {
        var probe = new Probe();
        int events = 0;
        probe.PropertyChanged += (_, _) => events++;

        probe.Value = 1;
        probe.Value = 2;

        Assert.Equal(2, events);
    }
}
