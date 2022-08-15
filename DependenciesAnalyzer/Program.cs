using DependenciesAnalyzer;
using NuGet.ProjectModel;

static LockFile? GetLockFile(string projectPath, string outputPath)
{
    // Run the restore command
    var dotNetRunner = new DotNetRunner();
    string[] arguments = new[] { "restore", $"\"{projectPath}\"" };
    var directoryName = Path.GetDirectoryName(projectPath);
    if (string.IsNullOrEmpty(directoryName))
    {
        Console.WriteLine("Directory can not be determined");
        return null;
    }
    var runStatus = dotNetRunner.Run(directoryName, arguments);

    // Load the lock file
    string lockFilePath = Path.Combine(outputPath, "project.assets.json");
    return LockFileUtilities.GetLockFile(lockFilePath, NuGet.Common.NullLogger.Instance);
}

static void ReportDependency(LockFileTargetLibrary projectLibrary, LockFileTarget lockFileTargetFramework,
    IDictionary<string, IList<Nuget>> nugets, Stack<string> currentPath)
{
    currentPath.Push(projectLibrary.Name);
    nugets.AddNuget(projectLibrary, currentPath);

    Console.Write(new String(' ', currentPath.Count * 2));
    Console.WriteLine($"{projectLibrary.Name}, v{projectLibrary.Version}");

    foreach (var childDependency in projectLibrary.Dependencies)
    {
        var childLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == childDependency.Id);
        if (childLibrary is null)
        {
            Console.WriteLine("Child library is null");
            continue;
        }

        ReportDependency(childLibrary, lockFileTargetFramework, nugets, currentPath);
    }
    currentPath.Pop();
}

static void PrintDependencies(IDictionary<string, IList<Nuget>> nugetsByName, bool conflictsOnly = false)
{
    foreach (var nugetGroup in nugetsByName.OrderBy(x => x.Key))
    {
        var nugetsByVersion = nugetGroup.Value.GroupBy(x => x.Version);
        if(conflictsOnly && nugetsByVersion.Count() == 1)
        {
            continue;
        }

        Console.WriteLine($"Nuget: {nugetGroup.Key}");

        foreach (var versionNuget in nugetsByVersion)
        {
            Console.WriteLine($"-- Version: {versionNuget.Key}");
            foreach (var nuget in versionNuget)
            {
                Console.WriteLine($"---- Path: {nuget.Path.Aggregate((a, b) => $"{a} -> {b}")}");
            }
        }

        Console.WriteLine("----------------");
        Console.WriteLine();
    }
}


var dgOutput = "./graph.dg";

IDictionary<string, IList<Nuget>> nugetsByName = new Dictionary<string, IList<Nuget>>();
var currentPath = new Stack<string>();

string dependencyGraphText = File.ReadAllText(dgOutput);
var dependencyGraph = DependencyGraphSpec.Load(dgOutput);

foreach (var project in dependencyGraph.Projects.Where(p => p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference))
{
    currentPath.Push(project.Name);
    // Generate lock file
    var lockFile = GetLockFile(project.FilePath, project.RestoreMetadata.OutputPath);
    if (lockFile is null)
    {
        Console.WriteLine("Lock file not found");
        continue;
    }

    Console.WriteLine(project.Name);

    foreach (var targetFramework in project.TargetFrameworks)
    {
        Console.WriteLine($"  [{targetFramework.FrameworkName}]");

        var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t => t.TargetFramework.Equals(targetFramework.FrameworkName));
        if (lockFileTargetFramework is null)
        {
            continue;
        }

        foreach (var dependency in targetFramework.Dependencies)
        {
            var projectLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == dependency.Name);
            if (projectLibrary is null)
            {
                Console.WriteLine("Project library is null");
                continue;
            }

            ReportDependency(projectLibrary, lockFileTargetFramework, nugetsByName, currentPath);
        }
    }
    currentPath.Pop();
}

PrintDependencies(nugetsByName, true);
