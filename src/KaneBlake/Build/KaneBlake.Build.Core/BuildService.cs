using KaneBlake.Build.Core.Localization;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.Build.Core
{
    public class BuildService
    {
        public async Task<int> Execute(string ProjectDirectory) 
        {
            if (string.IsNullOrEmpty(ProjectDirectory)) 
            {
                ProjectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            if (!MSBuildLoader.TryLoad(out var msBuildPath))
            {
                Console.WriteLine("This Tool depend on MsbuildCore. Please Install it by .NET Core SDKs.");
                return 1;
            }
            Console.WriteLine($"Find MsbuildCore in: {msBuildPath} ");
            var data = new DataStructure(ProjectDirectory);
            data.AcceptVisitor(new ProjectPreProcessor());
            data.AcceptVisitor(new LocalizableEntryGenerator());
            data.AcceptVisitor(new LocalizableEntryWriter());
            await data.ExecuteAsync();
            return 0;
        }
    }
}
