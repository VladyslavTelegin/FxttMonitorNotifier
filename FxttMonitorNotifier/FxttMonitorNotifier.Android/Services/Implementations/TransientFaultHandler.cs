namespace FxttMonitorNotifier.Droid.Services.Implementations
{
    using FxttMonitorNotifier.Droid.Models;
    using FxttMonitorNotifier.Droid.Services.ServiceDefinitions;

    using System;
    using System.Collections.Generic;
    using System.Threading;

    using static FxttMonitorNotifier.Droid.Models.GlobalConstants;

    public sealed class TransientFaultHandler : ITransientFaultHandler
    {
        public void Do(Action action, TimeSpan retryInterval, uint maxAttemptCount = TransientFaultHandlerConstants.MaxAtteptsCount)
        {
            this.Do<object>(() =>
            {
                action();
                return null;
            }, 
            retryInterval, maxAttemptCount);
        }

        public T Do<T>(Func<T> action, TimeSpan retryInterval, uint maxAttemptCount = TransientFaultHandlerConstants.MaxAtteptsCount)
        { 
            var thrownExceptions = new List<Exception>();

            for (var attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }

                    return action.Invoke();
                }
                catch (Exception ex)
                {
                    thrownExceptions.Add(ex);
                }
            }

            var baseExceptionMessage = $"{GlobalConstants.ExceptionConstants.ExceptionThrownFromStringSlice} {nameof(TransientFaultHandler)}";

            var aggregateException = new AggregateException(baseExceptionMessage, thrownExceptions)
            {
                Source = nameof(TransientFaultHandler)
            };

            throw aggregateException;
        }
    }
}