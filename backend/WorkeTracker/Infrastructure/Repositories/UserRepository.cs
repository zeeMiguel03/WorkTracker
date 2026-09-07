using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        } 

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.users.AddAsync(user, cancellationToken);
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            return _context.users.AnyAsync(
               user => user.Email == normalizedEmail,
               cancellationToken);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return _context.users.FirstOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);
        }

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.users.FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
        }

        public void Remove(User user)
        {
            _context.users.Remove(user);
        }

        public void Update(User user)
        {
            _context.users.Update(user);
        }
    }
}
