using Microsoft.Extensions.Logging;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Common.Profiling
{
    /// <summary>
    /// An wrapped <see cref="IAsyncStorage"/> with Logging.
    /// </summary>
    public class LogStorage : IAsyncStorage
    {
        private readonly IAsyncStorage _storage;

        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="LogStorage"/> with the specified <see cref="IAsyncStorage" /> and <see cref="ILoggerFactory"/>.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="loggerFactory"></param>
        public LogStorage(IAsyncStorage storage, ILoggerFactory loggerFactory)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = loggerFactory?.CreateLogger<LogStorage>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <inheritdoc/>
        public List<Guid> GetUnviewedIds(string user) => _storage.GetUnviewedIds(user);

        /// <inheritdoc/>
        public Task<List<Guid>> GetUnviewedIdsAsync(string user) => _storage.GetUnviewedIdsAsync(user);

        /// <inheritdoc/>
        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
            => _storage.List(maxResults, start, finish, orderBy);

        /// <inheritdoc/>
        public Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
            => _storage.ListAsync(maxResults, start, finish, orderBy);

        /// <inheritdoc/>
        public MiniProfiler Load(Guid id) => _storage.Load(id);

        /// <inheritdoc/>
        public Task<MiniProfiler> LoadAsync(Guid id) => _storage.LoadAsync(id);

        /// <inheritdoc/>
        public void Save(MiniProfiler profiler)
        {
            _logger.LogInformation("Storing Profiler:{ProfilerData}", profiler);
            _storage.Save(profiler);
        }

        /// <inheritdoc/>
        public Task SaveAsync(MiniProfiler profiler)
        {
            _logger.LogInformation("Storing Profiler:{ProfilerData}", profiler);
            return _storage.SaveAsync(profiler);
        }

        /// <inheritdoc/>
        public void SetUnviewed(string user, Guid id) => _storage.SetUnviewed(user, id);

        /// <inheritdoc/>
        public Task SetUnviewedAsync(string user, Guid id) => _storage.SetUnviewedAsync(user, id);

        /// <inheritdoc/>
        public void SetViewed(string user, Guid id) => _storage.SetViewed(user, id);

        /// <inheritdoc/>
        public Task SetViewedAsync(string user, Guid id) => _storage.SetViewedAsync(user, id);
    }

}
