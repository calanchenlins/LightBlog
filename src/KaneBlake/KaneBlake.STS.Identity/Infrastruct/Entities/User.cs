using K.Basis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BCrypts = BCrypt.Net.BCrypt;

namespace KaneBlake.STS.Identity.Infrastruct.Entities
{
    public class User : Entity<int>
    {
        public User()
        {
        }

        public static User Create(string username, string password) 
        {
            var salt = BCrypts.GenerateSalt();
            var user = new User
            {
                Username = username ?? throw new ArgumentNullException(nameof(username)),
                Password = BCrypts.HashPassword(password ?? throw new ArgumentNullException(nameof(password)), salt),
                Salt = salt,
                CreatedTime = DateTime.UtcNow
            };
            return user;
        }

        public bool ValidateCredentials(string password)
        {
            string passwordHash = BCrypts.HashPassword(password,Salt);
            return Password.Equals(passwordHash);
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

        [Required]
        public string Salt { get; private set; }
    }
}
