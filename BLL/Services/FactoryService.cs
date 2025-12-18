using BLL.Interfaces;
using DAL.Repositories;

namespace BLL.Services
{
    public static class FactoryService
    {
        public static IUserService CreateUserService()
        {
            var repo = new UserRepository();
            return new UserService(repo);
        }
    }
}