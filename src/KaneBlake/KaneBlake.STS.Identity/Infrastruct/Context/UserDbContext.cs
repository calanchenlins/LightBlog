using K.Basis.Domain.Entities;
using KaneBlake.Infrastruct.EntityFrameworkCore;
using KaneBlake.STS.Identity.Infrastruct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Infrastruct.Context
{
    public class UserDbContext : TEntityContext<Entity<int>, UserDbContext>
    {
        public UserDbContext(DbContextOptions<UserDbContext> options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(ci => ci.Id)
                .UseHiLo("user_hilo")
                .IsRequired();
            modelBuilder.Entity<User>().HasAlternateKey(k => k.Username);
            base.OnModelCreating(modelBuilder);
        }
    }
}
