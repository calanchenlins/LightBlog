using K.Basis.Domain.Entities;
using KaneBlake.Infrastruct.EntityFrameworkCore;
using LightBlog.Infrastruct.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Infrastruct.Context
{
    //Add-Migration -Context PostDbContext -OutputDir Infrastruct/Migrations/PostMigration Initial
    //Remove-Migration -context PostDbContext
    //Update-Database -Migration Initial -Context  PostDbContext
    //Drop-Database -context PostDbContext -WhatIf
    //Update-Database -Context  PostDbContext
    public class PostDbContext : TEntityContext<Entity<int>, PostDbContext>
    {
        public PostDbContext(DbContextOptions<PostDbContext> options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Post>()
                .Property(ci => ci.Id)
                .UseHiLo("blog_hilo")
                .IsRequired();

            modelBuilder.Entity<Comment>()
                .Property(ci => ci.Id)
                .UseHiLo("comment_hilo")
                .IsRequired();

            modelBuilder.Entity<Post>().HasMany(b => b.Comments).WithOne(c => c.Post).OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
