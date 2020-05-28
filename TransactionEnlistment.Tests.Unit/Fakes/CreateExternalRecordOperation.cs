using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TransactionEnlistment.Tests.Unit.Fakes
{
    class CreateExternalRecordOperation : IRollbackableOperation<FakeExternalRecord>, IDisposable
    {
        private readonly object _record;
        private readonly HttpClient _httpClient;
        public CreateExternalRecordOperation(HttpClient httpClient, object record)
        {
            _httpClient = httpClient;
            _record = record;
        }

        public FakeExternalRecord Result { get; set; }

        public async Task Execute()
        {
            if (_record == null)
            {
                throw new Exception("Record cannot be null");
            }
            var result = await _httpClient.PostAsync("/record", new StringContent(JsonConvert.SerializeObject(_record)));
            Result = JsonConvert.DeserializeObject<FakeExternalRecord>(await result.Content.ReadAsStringAsync());
        }

        public async Task Rollback()
        {
            if (Result != null)
            {
                await _httpClient.DeleteAsync($"/record/{Result.Id}");
                Console.WriteLine($"Rolled back {Result.Id}");
            }
        }

        public void Dispose()
        {
            //Don't dispose the HttpClient here, as the caller maybe rolling back multiple operations    
        }
    }
}