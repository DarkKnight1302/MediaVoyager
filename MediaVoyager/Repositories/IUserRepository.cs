using MediaVoyager.Entities;

namespace MediaVoyager.Repositories
{
    public interface IUserRepository
    {
        public Task<User> CreateUser(string id, string name, bool isGoogleLogin, string email, string passwordHash);

        public Task<User> GetUser(string id);

        public Task UpdateUser(User user);
    }
}
