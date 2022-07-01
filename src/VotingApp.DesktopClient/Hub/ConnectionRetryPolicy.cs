using Microsoft.AspNetCore.SignalR.Client;
using System;

namespace VotingApp.DesktopClient.Hub
{
    public class ConnectionRetryPolicy : IRetryPolicy
    {
        public event EventHandler RetryLimitExceeded;
        public event EventHandler ConnectionLost;

        public ConnectionRetryPolicy(EventHandler connectionLostHandler, EventHandler retryLimitExceededHandler)
        {
            ConnectionLost += connectionLostHandler;
            RetryLimitExceeded += retryLimitExceededHandler;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            if (retryContext.PreviousRetryCount == 6)
            {
                if (RetryLimitExceeded != null)
                {
                    RetryLimitExceeded(this, null);
                    return null;
                }
            }

            if (ConnectionLost != null)
            {
                ConnectionLost(this, null);
            }

            return TimeSpan.FromSeconds(2 ^ (int)retryContext.PreviousRetryCount);
        }
    }
}
