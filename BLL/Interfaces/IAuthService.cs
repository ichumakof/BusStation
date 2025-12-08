using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Models;

namespace BLL.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Аутентификация пользователя (вход в систему)
        /// </summary>
        UserDTO Authenticate(string login, string password);
    }
}
