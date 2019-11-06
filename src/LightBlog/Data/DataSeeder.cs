using LightBlog.Infrastruct.Context;
using LightBlog.Infrastruct.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Data
{
    public class DataSeeder
    {
        public static void UserDbSeeder(UserDbContext context)
        {
            var userTable = context.Set<User>();
            if (!userTable.Any())
            {
                userTable.Add(new User("demo", "demo"));
                userTable.Add(new User("alan", "alan"));
            }
            context.SaveChanges();
        }

        public static void PostDbSeeder(PostDbContext context)
        {
            var postTable = context.Set<Post>();
            if (!postTable.Any())
            {
                var post1 = new Post(
                    @"Asp.NetCore源码学习[1-1]：配置[Configuration]", 
                    @"",
                    @"初始数据", 
                    @"",
                    true);
                post1.SetAuthor(1, "demo");
                postTable.Add(post1);
                var post2 = new Post(
                    @"Asp.NetCore源码学习[1-2]：配置[Configuration]",
                    @"",
                    @"初始数据",
                    @"",
                    true);
                post2.SetAuthor(1, "demo");
                postTable.Add(post1);
                var post3 = new Post(
                    @"alan发布的博客",
                    @"",
                    @"alan发布的博客",
                    @"",
                    true);
                post2.SetAuthor(2, "alan");
                postTable.Add(post1);
                postTable.Add(post2);
                context.SaveChanges();
            }
            context.SaveChanges();
        }
    }
}
