using DAL.Interfaces;
using System.Linq;
using System.Data.Entity;

namespace DAL.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        public DAL.Users Authenticate(string login, string password)
        {
            using (var ctx = new BusStationEntities())
            {
                // Просто проверяем логин и пароль
                return ctx.Users
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.Login == login && u.Password == password);
            }
        }

        public bool LoginExists(string login)
        {
            using (var ctx = new BusStationEntities())
                {
                    return ctx.Users.Any(u => u.Login == login);
                }
        }
    }
}