using System.Collections.Generic;
using BLL.Models;

namespace BLL.Interfaces
{
    public interface IUserService
    {
        List<UserDTO> GetAll();
        UserDTO GetById(int id);
        UserDTO GetByLogin(string login);

        // Создать пользователя. plainPassword хранится без хеширования (по вашей просьбе).
        // Будет запрещено создавать пользователей с ролью Administrator.
        int Create(UserDTO userDto, string plainPassword);

        // Обновить пользователя. Изменение роли запрещено через этот метод.
        // Если newPlainPassword == null или пустой — пароль не меняется.
        void Update(UserDTO userDto, string newPlainPassword);

        // Удалить. currentUserId — id текущего администратора, чтобы запретить удаление самого себя.
        void Delete(int id, int currentUserId);
    }
}