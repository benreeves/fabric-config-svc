using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace ConfigService.Common
{
    public class DebugLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Debug.WriteLine(formatter(state, exception));
        }
    }
}
