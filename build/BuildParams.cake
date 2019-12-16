public class BuildParameters
{
	public BuildParameters(ICakeContext context,string rootPath)
	{
		Context = context;
		RootPath = rootPath;
	}

	private readonly string WebSdk = "Microsoft.NET.Sdk.Web";
	public string RootPath { get; }
	public ICakeContext Context { get; }

	public string Configuration { get; private set; }
	public string Runtime { get; private set; }
	public FilePathCollection SolutionFiles { get; set; }
	public DirectoryPathCollection Projects { get; set; }
	public DirectoryPathCollection TestProjects { get; set; }
	public FilePathCollection ProjectFiles { get; set; }
	public FilePathCollection TestProjectFiles { get; set; }
	public FilePathCollection WebProjectFiles { get; set; }

	public static BuildParameters Create(ICakeContext context,string rootPath)
	{
		var buildParameters = new BuildParameters(context,rootPath);
		buildParameters.Initialize();
		return buildParameters;
	}


	private void Initialize()
	{
		InitializeCore();
	}

	private void InitializeCore()
	{
		Projects = Context.GetDirectories($"{RootPath}src/*");
		TestProjects = Context.GetDirectories($"{RootPath}test/*");
		ProjectFiles = Context.GetFiles($"{RootPath}**/*.csproj");
		TestProjectFiles = Context.GetFiles($"{RootPath}test/**/*.csproj");
		WebProjectFiles = new FilePathCollection();
		SolutionFiles = Context.GetFiles($"{RootPath}**/*.sln");

		Configuration = Context.Argument("Configuration", "Release");
		Runtime = Context.Argument("Runtime", "linux-x64");

		foreach (var projectFile in ProjectFiles)
		{
			string SdkValue = Context.XmlPeek(projectFile.FullPath, "/Project[@Sdk]/@Sdk");
			if(WebSdk.Equals(SdkValue))
			{
				WebProjectFiles.Add(new FilePath(projectFile.FullPath));
			}
		}
	}
}