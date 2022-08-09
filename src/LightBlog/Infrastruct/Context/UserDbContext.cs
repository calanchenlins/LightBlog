using K.Basis.Domain.Entities;
using K.Infrastruct.EntityFrameworkCore;
using LightBlog.Infrastruct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Infrastruct.Context
{
    //Add-Migration -Context UserDbContext -OutputDir Infrastruct/Migrations/UserMigration Initial
    //Remove-Migration -context UserDbContext
    //Update-Database -Migration Initial -Context  UserDbContext
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
            base.OnModelCreating(modelBuilder);
        }
    }
}
