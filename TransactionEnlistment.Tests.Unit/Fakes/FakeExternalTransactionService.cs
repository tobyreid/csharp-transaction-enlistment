using System.Net.Http;
using System.Threading.Tasks;

namespace TransactionEnlistment.Tests.Unit.Fakes
{
    public class FakeExternalTransactionService : TransactionServiceBase
    {
        private readonly HttpClient _httpClient;

        public FakeExternalTransactionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<object> CreateContact(object record)
        {
            var createConactOperation = new CreateExternalRecordOperation(_httpClient, record);
            await ExecuteOperation(createConactOperation);
            return createConactOperation.Result;
        }
    }
    
}