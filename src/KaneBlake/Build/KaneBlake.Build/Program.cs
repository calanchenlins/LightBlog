using KaneBlake.Build.Core;
using System;
using System.Threading.Tasks;

namespace KaneBlake.Build
{
    class Program
    {
        //dotnet-KaneBlake 
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var projectPath = @"C:\Users\Administrator\Desktop\SampleSolution\DNC.RazorClassLibrary";
            //projectPath = @"C:\WorkStation\Code\GitHubCode\LightBlog\src\KaneBlake\STS.Identity\KaneBlake.STS.Identity";

            var service = new BuildService();
            await service.Execute(projectPath);
            return 0;
        }
    }
}
