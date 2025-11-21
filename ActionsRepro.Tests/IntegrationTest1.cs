using Microsoft.Extensions.Logging;

namespace ActionsRepro.Tests
{
    [Collection(nameof(TestCollection))]
    public class IntegrationTest1
    {
        private readonly TestFixture _fixture;

        public IntegrationTest1(TestFixture fixture) 
        { 
            _fixture = fixture;
        }

        [Fact]
        public async Task GetWebResourceRootReturnsOkStatusCode()
        { 
            
            // Act
            using var response = await _fixture.ApiClient.GetAsync("/", TestContext.Current.CancellationToken);
            using var response2 = await _fixture.FrontendClient.GetAsync("/", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        }
    }
}
