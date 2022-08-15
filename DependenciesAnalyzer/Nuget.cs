namespace DependenciesAnalyzer;

public record Nuget(string Name, string Version, IEnumerable<string> Path);
