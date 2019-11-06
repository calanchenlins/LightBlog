using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Services
{
    public interface IAuthService<T>
    {
        T GetAuthenticatedUser();
    }
}
