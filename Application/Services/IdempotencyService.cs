using Application.Interfaces;
using Core.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Application.Services
{
    public class IdempotencyService : IIdempotencyService
    {
        private readonly ApplicationDbContext _db;

        public IdempotencyService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IdempotencyKey?> GetByKeyAsync(Guid userId, string idempotencyKey)
        {
            return await _db.IdempotencyKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.UserId == userId && k.IdempotencyKeyValue == idempotencyKey);
        }

        public async Task<IdempotencyKey> RegisterIdempotencyKeyAsync(Guid userId, string transactionId, string idempotencyKey, string requestHash, TimeSpan? ttl = null)
        {
            var existing = await _db.IdempotencyKeys.FirstOrDefaultAsync(k => k.UserId == userId && k.IdempotencyKeyValue == idempotencyKey);
            if (existing != null)
            {
                return existing;
            }

            var expiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromHours(24));
            var key = new IdempotencyKey
            {
                UserId = userId,
                TransactionId = transactionId,
                IdempotencyKeyValue = idempotencyKey,
                RequestHash = requestHash,
                Status = "registered",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            _db.IdempotencyKeys.Add(key);
            await _db.SaveChangesAsync();
            return key;
        }

        public async Task StoreResponseAsync(long idempotencyKeyId, string responseHash, string responseData)
        {
            var key = await _db.IdempotencyKeys.FindAsync(idempotencyKeyId);
            if (key == null) return;
            key.ResponseHash = responseHash;
            key.ResponseData = responseData;
            key.Status = "completed";
            await _db.SaveChangesAsync();
        }
    }
}
