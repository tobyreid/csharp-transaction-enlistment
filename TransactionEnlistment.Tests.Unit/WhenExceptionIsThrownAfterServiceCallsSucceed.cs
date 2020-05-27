using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Moq;
using Moq.Protected;
using TransactionEnlistment.Tests.Unit.Fakes;
using Xunit;

namespace TransactionEnlistment.Tests.Unit
{
    public class WhenExceptionIsThrownAfterServiceCallsSucceed
    {
        [Fact]
        public async Task ItShouldRollbackExecutedTasks()
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                // Setup the PROTECTED method to mock
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // prepare the expected response of the mocked http call
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{'id':1,'value':'1'}"),
                })
                .Verifiable();


            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com/")
            };
            var service = new FakeExternalTransactionService(httpClient);

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    await service.CreateContact(new object());
                    await service.CreateContact(new object());
                    throw new Exception("an exception");
                    scope.Complete();
                }
            });

            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2), // we expected a single external request
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post  // we expected a GET request
                        && req.RequestUri == new Uri("http://test.com/record") // to this uri
                ),
                ItExpr.IsAny<CancellationToken>()
            );

            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2), // we expected a single external request
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Delete  // we expected a GET request
                        && req.RequestUri == new Uri("http://test.com/record/1") // to this uri
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
