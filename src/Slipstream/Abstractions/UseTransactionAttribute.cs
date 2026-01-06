using System;
using System.Transactions;

namespace Slipstream.Abstractions
{
    /// <summary>
    /// Apply to a request type to indicate the request should be executed inside a <see cref="TransactionScope"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class UseTransactionAttribute : Attribute
    {
        /// <summary>
        /// Isolation level to use for the transaction. Defaults to <see cref="IsolationLevel.ReadCommitted"/>.
        /// </summary>
        public IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// Optional timeout for the transaction scope. If null the default system timeout is used.
        /// </summary>
        public TimeSpan? Timeout { get; }

        public UseTransactionAttribute(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            IsolationLevel = isolationLevel;
            Timeout = null;
        }

        public UseTransactionAttribute(int timeoutSeconds)
        {
            IsolationLevel = IsolationLevel.ReadCommitted;
            Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        public UseTransactionAttribute(IsolationLevel isolationLevel, int timeoutSeconds)
        {
            IsolationLevel = isolationLevel;
            Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }
    }
}
