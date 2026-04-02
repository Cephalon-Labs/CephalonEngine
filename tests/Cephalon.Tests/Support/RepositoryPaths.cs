namespace Cephalon.Tests.Support;

internal static class RepositoryPaths
{
    private const string RootMarkerFileName = "Directory.Build.props";

    public static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var markerPath = Path.Combine(directory.FullName, RootMarkerFileName);
            if (File.Exists(markerPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find repository root from '{AppContext.BaseDirectory}'.");
    }

    public static string GetDirectory(params string[] relativeSegments)
    {
        var fullPath = Path.Combine([GetRepositoryRoot(), .. relativeSegments]);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Could not find '{fullPath}'.");
        }

        return fullPath;
    }

    public static string GetFile(params string[] relativeSegments)
    {
        var fullPath = Path.Combine([GetRepositoryRoot(), .. relativeSegments]);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Could not find '{fullPath}'.");
        }

        return fullPath;
    }
}
