using Microsoft.EntityFrameworkCore;
using TRManager.Api.Data;
using TRManager.Api.Features.User.Dtos;

namespace TRManager.Api.Features.User
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(Guid id);
        Task UpdateAsync(Guid id, UpdateUserRequest req);
        Task DeleteSoftAsync(Guid id);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        public UserService(ApplicationDbContext db) => _db = db;

        public async Task<List<UserDto>> GetAllAsync() =>
            await _db.Users
                .Select(u => new UserDto(u.Id, u.Email, u.UserName, u.FullName, u.Phone, u.IsActive, u.CreatedAt))
                .ToListAsync();

        public async Task<UserDto?> GetByIdAsync(Guid id) =>
            await _db.Users.Where(u => u.Id == id)
                .Select(u => new UserDto(u.Id, u.Email, u.UserName, u.FullName, u.Phone, u.IsActive, u.CreatedAt))
                .FirstOrDefaultAsync();

        public async Task UpdateAsync(Guid id, UpdateUserRequest req)
        {
            var u = await _db.Users.FindAsync(id) ?? throw new KeyNotFoundException("User not found");
            if (req.FullName is not null) u.FullName = req.FullName;
            if (req.Phone is not null) u.Phone = req.Phone;
            if (req.IsActive is not null) u.IsActive = req.IsActive.Value;
            u.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteSoftAsync(Guid id)
        {
            var u = await _db.Users.FindAsync(id) ?? throw new KeyNotFoundException("User not found");
            u.IsActive = false;
            u.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
