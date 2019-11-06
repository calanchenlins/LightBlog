public class BuildParameters
{
	public BuildParameters(ICakeContext context)
	{
		Context = context;
	}

	public ICakeContext Context { get; }

	public string Configuration { get; private set; }
	public DirectoryPathCollection Projects { get; set; }
	public DirectoryPathCollection TestProjects { get; set; }
	public FilePathCollection ProjectFiles { get; set; }
	public FilePathCollection TestProjectFiles { get; set; }

	public static BuildParameters Create(ICakeContext context)
	{
		var buildParameters = new BuildParameters(context);
		buildParameters.Initialize();
		return buildParameters;
	}


	private void Initialize()
	{
		InitializeCore();
	}

	private void InitializeCore()
	{
		Projects = Context.GetDirectories("./src/*");
		TestProjects = Context.GetDirectories("./test/*");
		ProjectFiles = Context.GetFiles("./src/*/*/*.csproj");
		TestProjectFiles = Context.GetFiles("./test/*/*.csproj");
        Configuration = "Release";
	}
}