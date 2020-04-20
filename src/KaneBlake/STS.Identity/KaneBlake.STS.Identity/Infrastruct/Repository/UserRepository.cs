using KaneBlake.Basis.Infrastruct.EntityFrameworkCore;
using KaneBlake.STS.Identity.Infrastruct.Context;
using KaneBlake.STS.Identity.Infrastruct.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Infrastruct.Repository
{
    public class UserRepository : EFRepository<User>
    {
        public UserRepository(UserDbContext dbContext) : base(dbContext)
        {
        }
    }
}
