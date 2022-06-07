using KaneBlake.Build.Core;
using System;
using System.Threading.Tasks;

namespace KaneBlake.Build
{
    class Program
    {
        //https://github.com/dotnet/command-line-api
        //dotnet-KaneBlake LocaleGenerate
        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0) 
            {
                return 1;
            }
            var task = args[0];
            if (task.Equals("LocaleGenerate")) 
            {
                var projectPath = string.Empty;
                if (args.Length > 1) 
                {
                    projectPath = args[1];
                }
                var service = new BuildService();
                await service.Execute(projectPath);
            }

            return 0;
        }
    }
}
