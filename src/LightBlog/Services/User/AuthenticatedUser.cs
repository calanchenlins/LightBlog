using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Services.User
{
    public class AuthenticatedUser
    {
        public bool IsAuthenticated { get; set; } = false;

        public int UserId { get; set; }

        public string UserName { get; set; }
    }
}
