using System.Diagnostics.CodeAnalysis;

namespace ActionsRepro.Tests
{
    public class IntegrationTest1
    {
        private readonly ITestOutputHelper _outputHelper;

        public IntegrationTest1(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _fixture = new(outputHelper);
        }

        private readonly TestFixture _fixture;


        [Fact]
        [Experimental("EXTEXP0001")]
        public async Task GetWebResourceRootReturnsOkStatusCode()
        {
            _outputHelper.WriteLine("Initializing...");
            await _fixture.InitializeAsync();

            // Act
            using var response = await _fixture.ApiClient.GetAsync("/");
            using var response2 = await _fixture.FrontendClient.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        }


    }
}