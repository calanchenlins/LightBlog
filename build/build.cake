#load "./build/index.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var build = BuildParameters.Create(Context);
var target = Argument("target", "Default");//需要执行的目标任务

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    // Executed BEFORE the first task.
    Information("Running tasks...");
});

Teardown(ctx =>
{
    // Executed AFTER the last task.
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
	.Does(() =>
{
	if (DirectoryExists("./artifacts"))
	{
		DeleteDirectory("./artifacts", new DeleteDirectorySettings(){
            Force = true,
            Recursive = true
        });
	}
});

Task("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{
	DotNetCoreRestore();
});

Task("Build")
.IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
         Configuration = build.Configuration
         //OutputDirectory = "./artifacts/"
    };

    // DotNetCoreBuild("./Kane.Blake.sln", settings);
    // DotNetCoreBuild("./src/utils/Kane.Blake.Utils.Logging/Kane.Blake.Utils.Logging.csproj", settings);
    foreach (var project in build.ProjectFiles)
	{
		DotNetCoreBuild(project.FullPath, settings);
	}
    
});

Task("Test")
	.IsDependentOn("Build")
	.Does(() =>
{
	var settings = new DotNetCoreTestSettings
    {
         Configuration = build.Configuration,
		 NoBuild = false
    };

	foreach (var testProject in build.TestProjectFiles)
	{
		DotNetCoreTest(testProject.FullPath, settings);
	}
});

Task("Pack")
	.Does(() =>
{
	var settings = new DotNetCorePackSettings
	{
		Configuration = build.Configuration,
		IncludeSymbols = true,
		OutputDirectory = "./artifacts/packages"
	};
	foreach (var project in build.ProjectFiles)
	{
		DotNetCorePack(project.FullPath, settings);
	}
});


Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack")
    .Does(() =>
{

});

RunTarget(target);