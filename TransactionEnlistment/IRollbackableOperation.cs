using System.Threading.Tasks;

namespace TransactionEnlistment
{
    public interface IRollbackableOperation<TResult> :IRollbackableOperation
    {
        /// <summary>
        /// The Result, available once operation has been executed
        /// </summary>
        public TResult Result { get; set; }
    }

    public interface IRollbackableOperation
    {
        /// <summary>
        /// Executes the operation.
        /// </summary>
        Task Execute();

        /// <summary>
        /// Rolls back the operation, restores the original state.
        /// </summary>
        Task Rollback();
    }
}