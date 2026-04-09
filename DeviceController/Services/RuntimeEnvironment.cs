using System.IO;

namespace DeviceController.Services;

public static class RuntimeEnvironment
{
    public static string ConfigureProcessPaths()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var previousCurrentDirectory = Environment.CurrentDirectory;

        if (!string.Equals(previousCurrentDirectory, baseDirectory, StringComparison.OrdinalIgnoreCase))
            Directory.SetCurrentDirectory(baseDirectory);

        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        if (!PathContains(currentPath, baseDirectory))
        {
            var updatedPath = string.IsNullOrWhiteSpace(currentPath)
                ? baseDirectory
                : $"{baseDirectory};{currentPath}";
            Environment.SetEnvironmentVariable("PATH", updatedPath);
        }

        return $"BaseDirectory={baseDirectory}{Environment.NewLine}CurrentDirectory={Environment.CurrentDirectory}";
    }

    private static bool PathContains(string pathValue, string candidate)
    {
        foreach (var part in pathValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(part, candidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
