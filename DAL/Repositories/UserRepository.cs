using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace DAL.Repositories
{
    /// <summary>
    /// Репозиторий для работы с пользователями. Замените BusStationEntities на ваш реальный EF-контекст, если имя другое.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private Func<DbContext> _contextFactory = () => new BusStationEntities();

        public UserRepository() { }

        public UserRepository(Func<DbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public List<Users> GetAll()
        {
            using (var ctx = _contextFactory())
            {
                return ctx.Set<Users>().Include(u => u.Roles).ToList();
            }
        }

        public Users GetById(int id)
        {
            using (var ctx = _contextFactory())
            {
                return ctx.Set<Users>().Include(u => u.Roles).FirstOrDefault(u => u.UserID == id);
            }
        }

        public Users GetByLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) return null;
            using (var ctx = _contextFactory())
            {
                return ctx.Set<Users>().Include(u => u.Roles)
                          .FirstOrDefault(u => u.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
            }
        }

        public int Create(Users user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            using (var ctx = _contextFactory())
            {
                ctx.Set<Users>().Add(user);
                ctx.SaveChanges();
                return user.UserID;
            }
        }

        public void Update(Users user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            using (var ctx = _contextFactory())
            {
                var db = ctx.Set<Users>().FirstOrDefault(u => u.UserID == user.UserID);
                if (db == null) throw new InvalidOperationException("Пользователь не найден.");

                db.Login = user.Login;
                db.Password = user.Password;
                db.FullName = user.FullName;
                db.RoleID = user.RoleID;

                ctx.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var ctx = _contextFactory())
            {
                var db = ctx.Set<Users>().FirstOrDefault(u => u.UserID == id);
                if (db == null) throw new InvalidOperationException("Пользователь не найден.");
                ctx.Set<Users>().Remove(db);
                ctx.SaveChanges();
            }
        }

        public Roles GetRoleById(int roleId)
        {
            using (var ctx = _contextFactory())
            {
                return ctx.Set<Roles>().FirstOrDefault(r => r.RoleID == roleId);
            }
        }

        public Roles GetRoleByName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return null;
            using (var ctx = _contextFactory())
            {
                return ctx.Set<Roles>().FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}