using KaneBlake.Infrastruct.EntityFrameworkCore;
using LightBlog.Infrastruct.Context;
using LightBlog.Infrastruct.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Infrastruct.Repository
{
    public class UserRepository : EFRepository<User>
    {
        public UserRepository(UserDbContext dbContext) : base(dbContext)
        {
        }
    }
}
