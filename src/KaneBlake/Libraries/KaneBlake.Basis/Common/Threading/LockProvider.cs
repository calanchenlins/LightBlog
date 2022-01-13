using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Common.Threading
{
    /// <summary>
    /// A LockProvider based upon the SemaphoreSlim class to selectively lock objects, resources or statement blocks 
    /// according to given unique IDs in a sync or async way.
    /// <see href="https://github.com/Darkseal/LockProvider/blob/master/LockProvider.cs"/>
    /// </summary>
    public class LockProvider<T>
    {

        private readonly static ConcurrentDictionary<T, SemaphoreSlim> _semaphoreSlims = new ConcurrentDictionary<T, SemaphoreSlim>();


        /// <summary>
        /// Blocks the current thread (according to the given ID) until it can enter the LockProvider
        /// </summary>
        /// <param name="id">the unique ID to perform the lock</param>
        public int Wait(T id)
        {
            var _semaphore = _semaphoreSlims.GetOrAdd(id, new SemaphoreSlim(1, 1));
            _semaphore.Wait();
            return _semaphore.GetHashCode();
        }


        /// <summary>
        /// Asynchronously puts thread to wait (according to the given ID) until it can enter the LockProvider
        /// </summary>
        /// <param name="id">the unique ID to perform the lock</param>
        public async Task<int> WaitAsync(T id)
        {
            var _semaphore = _semaphoreSlims.GetOrAdd(id, new SemaphoreSlim(1, 1));
            await _semaphore.WaitAsync();
            return _semaphore.GetHashCode();
        }


        /// <summary>
        /// Releases the lock (according to the given ID)
        /// </summary>
        /// <param name="id">the unique ID to unlock</param>
        public int Release(T id)
        {
            if (_semaphoreSlims.TryGetValue(id, out var semaphore))
            {
                semaphore.Release();
                return semaphore.GetHashCode();
            }
            throw new KeyNotFoundException($"ResourceId: {id} is not found when release the lock.");
        }
    }
}
