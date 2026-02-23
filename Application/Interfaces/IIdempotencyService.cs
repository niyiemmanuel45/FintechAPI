using Core.Entities;
using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IIdempotencyService
    {
        Task<IdempotencyKey?> GetByKeyAsync(Guid userId, string idempotencyKey);
        Task<IdempotencyKey> RegisterIdempotencyKeyAsync(Guid userId, string transactionId, string idempotencyKey, string requestHash, TimeSpan? ttl = null);
        Task StoreResponseAsync(long idempotencyKeyId, string responseHash, string responseData);
    }
}
