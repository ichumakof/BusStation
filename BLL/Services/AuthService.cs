using BLL.Interfaces;
using BLL.Models;
using DAL.Repositories;
using System;
using DAL.Interfaces;

namespace BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;

        public AuthService() : this(new AuthRepository()) { }
        public AuthService(IAuthRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public UserDTO Authenticate(string login, string password)
        {
            // 1. Проверка пустых полей — можно кидать аргументную ошибку или возвращать null.
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Введите логин и пароль");

            // 2. Проверяем в БД
            var userEntity = _repo.Authenticate(login, password);

            // 3. Если не нашли - возвращаем null (не кидаем исключение)
            if (userEntity == null)
                return null;

            // 4. Создаем DTO и возвращаем — НИКАКИХ проверок роли здесь
            var userDto = new UserDTO
            {
                UserID = userEntity.UserID,
                Login = userEntity.Login,
                FullName = userEntity.FullName,
                RoleID = userEntity.RoleID,
                RoleName = userEntity.Roles?.Name
            };

            return userDto;
        }
    }
}