using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Services
{
    public interface IUserService<T>
    {
        Task<bool> ValidateCredentials(T user, string password);

        Task<T> FindByUsername(string username);

        Task<T> SignUp(string userName, string password);


        bool IsAuthenticated();
        Task<bool> UserNameExists(string userName);

        Task SignInAsync(T user,bool rememberLogin);
    }
}
