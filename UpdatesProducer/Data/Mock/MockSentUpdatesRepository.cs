using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UpdatesProducer.Mock
{
    public class MockSentUpdatesRepository : ISentUpdatesRepository
    {
        private readonly ILogger<MockSentUpdatesRepository> _logger;

        public MockSentUpdatesRepository(
            ILogger<MockSentUpdatesRepository> logger)
        {
            _logger = logger;
        }

        public Task<bool> ExistsAsync(string url)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(string url)
        {
            return Task.CompletedTask;
        }
    }
}