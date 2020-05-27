using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace TransactionEnlistment
{
    public abstract class TransactionServiceBase
    {
        /// <summary>
        /// Dictionary of transaction enlistment objects for the current thread.
        /// </summary>
        [ThreadStatic]
        private static Dictionary<string, TransactionEnlistment> _enlistments;

        private static readonly SemaphoreSlim EnlistmentsLock = new SemaphoreSlim(1, 1);

   

        private static bool IsInTransaction()
        {
            return Transaction.Current != null;
        }
        protected static async Task<TResult> ExecuteOperation<TResult>(IRollbackableOperation<TResult> operation)
        {
            if (IsInTransaction())
                await EnlistOperation(operation);
            else
                await operation.Execute();
            return operation.Result;
        }
        private static async Task EnlistOperation(IRollbackableOperation operation)
        {
            var transaction = Transaction.Current;
            await EnlistmentsLock.WaitAsync();
            try
            {
                if (_enlistments == null)
                {
                    _enlistments = new Dictionary<string, TransactionEnlistment>();
                }

                if (!_enlistments.TryGetValue(transaction.TransactionInformation.LocalIdentifier, out var enlistment))
                {
                    enlistment = new TransactionEnlistment(transaction);
                    _enlistments.Add(transaction.TransactionInformation.LocalIdentifier, enlistment);
                }

                await enlistment.EnlistOperation(operation);
            }
            finally
            {
                EnlistmentsLock.Release();
            }
        }
    }
}
