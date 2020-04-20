using KaneBlake.Basis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Infrastruct.Entities
{
    public class User : Entity<int>
    {
        public User(string username, string password)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            CreatedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        public string Username { get; private set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; private set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        public string Password { get; private set; }
    }
}
