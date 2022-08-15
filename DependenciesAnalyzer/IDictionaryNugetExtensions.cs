using NuGet.ProjectModel;

namespace DependenciesAnalyzer
{
    public static class IDictionaryNugetExtensions
    {
        public static void AddNuget(this IDictionary<string, IList<Nuget>> dictionary, LockFileTargetLibrary projectLibrary, IEnumerable<string> path)
        {
            if(!dictionary.TryGetValue(projectLibrary.Name, out var nugets))
            {
                nugets = new List<Nuget>();
                dictionary.Add(projectLibrary.Name, nugets);
            }

            var pathForNuget = path.ToList();
            pathForNuget.Reverse();
            nugets.Add(new Nuget(projectLibrary.Name, projectLibrary.Version.ToString(), pathForNuget));
        }
    }
}
