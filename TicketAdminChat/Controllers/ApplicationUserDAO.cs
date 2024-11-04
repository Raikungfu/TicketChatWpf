using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketAdminChat.Data;
using TicketApplication.Models;

namespace TicketAdminChat.Controllers
{
    public class AdminLoginResult
    {
        public SignInResult SignInResult { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class ApplicationUserDAO
    {
        private readonly ApplicationDbContext dbContext;
        private readonly PasswordHasher<User> passwordHasher;

        public ApplicationUserDAO(ApplicationDbContext _dbContext)
        {
            dbContext = _dbContext;
            passwordHasher = new PasswordHasher<User>();
        }

        public async Task<User> GetApplicationUserById(string userId)
        {
            return await dbContext.Users.FindAsync(userId);
        }

        public async Task<List<User>> GetApplicationUsers()
        {
            return await dbContext.Users.Where(x => x.Role == "Customer").ToListAsync();
        }

        public async Task<AdminLoginResult> LoginAdmin(string username, string password)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == username && u.Password == password);
            if (user == null)
            {
                return new AdminLoginResult { SignInResult = SignInResult.Failed };
            }

            return new AdminLoginResult
            {
                SignInResult = SignInResult.Success,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = "https://static.vecteezy.com/system/resources/previews/043/900/708/non_2x/user-profile-icon-illustration-vector.jpg"
            };
        }
    }
}
