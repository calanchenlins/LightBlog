using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Services.InDto
{
    public class UserSingUpInDto
    {
        public string Name { get; set; }

        public string Password { get; set; }

        public string InvalidInfo { get; set; } = "";
    }
}
