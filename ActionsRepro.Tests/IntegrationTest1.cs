using System.Diagnostics.CodeAnalysis;
using Xunit.v3;

namespace ActionsRepro.Tests
{
    public class IntegrationTest1
    {
        private readonly TestFixture _fixture = new TestFixture(new TestOutputHelper());


        [Fact]
        [Experimental("EXTEXP0001")]
        public async Task GetWebResourceRootReturnsOkStatusCode()
        {
            await _fixture.InitializeAsync();

            // Act
            using var response = await _fixture.ApiClient.GetAsync("/", TestContext.Current.CancellationToken);
            using var response2 = await _fixture.FrontendClient.GetAsync("/", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        }


    }
}