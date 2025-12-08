using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Models
{
    public class UserDTO
    {
        public int UserID { get; set; }
        public string Login { get; set; }
        public string FullName { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }

        public bool IsAdmin =>
            !string.IsNullOrWhiteSpace(RoleName) &&
            RoleName.Trim().Equals("Administrator", StringComparison.OrdinalIgnoreCase);
    }
}
