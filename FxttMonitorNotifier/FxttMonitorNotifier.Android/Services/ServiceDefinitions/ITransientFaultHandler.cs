using FxttMonitorNotifier.Droid.Services.Implementations;

using Xamarin.Forms;

[assembly: Dependency(typeof(TransientFaultHandler))]
namespace FxttMonitorNotifier.Droid.Services.ServiceDefinitions
{
    using System;

    using static FxttMonitorNotifier.Droid.Models.GlobalConstants;

    public interface ITransientFaultHandler
    { 
        void Do(Action action, TimeSpan retryInterval, uint maxAttemptCount = TransientFaultHandlerConstants.MaxAtteptsCount);

        T Do<T>(Func<T> action, TimeSpan retryInterval, uint maxAttemptCount = TransientFaultHandlerConstants.MaxAtteptsCount);
    }
}