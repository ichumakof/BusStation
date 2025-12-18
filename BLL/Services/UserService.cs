using BLL.Interfaces;
using BLL.Models;
using DAL;
using DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private const string AdminRoleName = "Administrator";

        public UserService(IUserRepository repo = null)
        {
            _repo = repo ?? new UserRepository();
        }

        public List<UserDTO> GetAll()
        {
            var list = _repo.GetAll();
            return list.Select(MapToDTO).ToList();
        }

        public UserDTO GetById(int id)
        {
            var u = _repo.GetById(id);
            return u == null ? null : MapToDTO(u);
        }

        public UserDTO GetByLogin(string login)
        {
            var u = _repo.GetByLogin(login);
            return u == null ? null : MapToDTO(u);
        }

        public int Create(UserDTO userDto, string plainPassword)
        {
            if (userDto == null) throw new ArgumentNullException(nameof(userDto));
            if (string.IsNullOrWhiteSpace(userDto.Login)) throw new ArgumentException("Login required.");
            if (plainPassword == null) plainPassword = string.Empty; // разрешаем пустой пароль, если это надо

            // Бизнес-правило: запрещаем создавать новых администраторов
            var role = _repo.GetRoleById(userDto.RoleID);
            if (role != null && role.Name != null && role.Name.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Нельзя создавать пользователей с ролью Administrator через это приложение.");

            // уникальность логина
            var exists = _repo.GetByLogin(userDto.Login);
            if (exists != null)
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");

            var entity = new Users
            {
                Login = userDto.Login.Trim(),
                Password = plainPassword, // без хеширования по вашему требованию
                FullName = userDto.FullName,
                RoleID = userDto.RoleID
            };

            return _repo.Create(entity);
        }

        public void Update(UserDTO userDto, string newPlainPassword)
        {
            if (userDto == null) throw new ArgumentNullException(nameof(userDto));

            var target = _repo.GetById(userDto.UserID);
            if (target == null) throw new InvalidOperationException("Пользователь не найден.");

            // Запрет менять роль через этот метод
            if (userDto.RoleID != target.RoleID)
                throw new InvalidOperationException("Изменение роли запрещено через этот интерфейс.");

            // Проверка уникальности логина при изменении
            if (!string.Equals(target.Login, userDto.Login, StringComparison.OrdinalIgnoreCase))
            {
                var byLogin = _repo.GetByLogin(userDto.Login);
                if (byLogin != null && byLogin.UserID != target.UserID)
                    throw new InvalidOperationException("Другой пользователь уже использует этот логин.");
            }

            // Обновляем поля
            target.Login = userDto.Login.Trim();
            target.FullName = userDto.FullName;

            if (!string.IsNullOrEmpty(newPlainPassword))
            {
                target.Password = newPlainPassword; // сохраняем без хеширования по требованию
            }

            _repo.Update(target);
        }

        public void Delete(int id, int currentUserId)
        {
            if (id == currentUserId)
                throw new InvalidOperationException("Нельзя удалить самого себя.");

            var target = _repo.GetById(id);
            if (target == null) throw new InvalidOperationException("Пользователь не найден.");

            // Дополнительно можно запретить удалять админов (если нужно)
            var targetRole = _repo.GetRoleById(target.RoleID);
            if (targetRole != null && targetRole.Name != null && targetRole.Name.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Удаление администратора запрещено.");
            }

            _repo.Delete(id);
        }

        private UserDTO MapToDTO(Users u)
        {
            return new UserDTO
            {
                UserID = u.UserID,
                Login = u.Login,
                FullName = u.FullName,
                RoleID = u.RoleID,
                RoleName = u.Roles?.Name
            };
        }
    }
}