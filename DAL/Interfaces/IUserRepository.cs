using System.Collections.Generic;

namespace DAL.Repositories
{
    public interface IUserRepository
    {
        List<Users> GetAll();
        Users GetById(int id);
        Users GetByLogin(string login);
        int Create(Users user);
        void Update(Users user);
        void Delete(int id);

        Roles GetRoleById(int roleId);
        Roles GetRoleByName(string roleName);
    }
}