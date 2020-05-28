using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;

namespace TransactionEnlistment
{
    public class TransactionEnlistment : IEnlistmentNotification
    {
        private readonly List<IRollbackableOperation> _journal = new List<IRollbackableOperation>();

        /// <summary>Initializes a new instance of the <see cref="TransactionEnlistment"/> class.</summary>
        /// <param name="tx">The Transaction.</param>
        public TransactionEnlistment(Transaction tx)
        {
            tx.EnlistVolatile(this, EnlistmentOptions.None);
        }
        /// <summary>
        /// Enlists <paramref name="operation"/> in its journal, so it will be committed or rolled
        /// together with the other enlisted operations.
        /// </summary>
        /// <param name="operation"></param>
        public async Task EnlistOperation(IRollbackableOperation operation)
        {
            await operation.Execute();
            _journal.Add(operation);
        }
        public void Commit(Enlistment enlistment)
        {
            DisposeJournal();

            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            Rollback(enlistment);
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Rollback(Enlistment enlistment)
        {
            try
            {
                // Roll back journal items in reverse order
                for (var i = _journal.Count - 1; i >= 0; i--)
                {
                    _journal[i].Rollback().ConfigureAwait(false);
                }

                DisposeJournal();
            }
            catch (Exception e)
            {
                throw new TransactionException("Failed to roll back.", e);
            }

            enlistment.Done();
        }
        private void DisposeJournal()
        {
            // Dispose journal items in reverse order
            for (var i = _journal.Count - 1; i >= 0; i--)
            {
                var disposable = _journal[i] as IDisposable;
                disposable?.Dispose();
                _journal.RemoveAt(i);
            }
        }
    }
}