using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    /// <summary>
    /// Интерфейс для работы с аутентификацией пользователей
    /// </summary>
    public interface IAuthRepository
    {
        /// <summary>
        /// Проверяет логин и пароль пользователя
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns>Найденный пользователь или null</returns>
        DAL.Users Authenticate(string login, string password);

        /// <summary>
        /// Проверяет существует ли пользователь с таким логином
        /// </summary>
        /// <param name="login">Логин для проверки</param>
        /// <returns>true если пользователь существует</returns>
        bool LoginExists(string login);
    }
}
