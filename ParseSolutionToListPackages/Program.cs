using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Please enter the path to your directory containing the solution:");
        var directoryPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            Console.WriteLine("Invalid directory path. Please make sure the path is correct and try again.");
            return;
        }

        var solutionFile = Directory.GetFiles(directoryPath, "*.sln").FirstOrDefault();
        if (solutionFile == null)
        {
            Console.WriteLine("No solution (.sln) file found in the directory.");
            return;
        }

        Console.WriteLine($"Using solution file: {solutionFile}");

        try
        {
            var projects = ParseSolution(solutionFile);
            var packages = new List<(string Project, string Package, string Version)>();

            foreach (var projectPath in projects)
            {
                ProcessProjectPath(projectPath, packages);
            }

            var csvPath = Path.Combine(Path.GetDirectoryName(solutionFile), "packages.csv");
            WritePackagesToCsv(packages, csvPath);

            Console.WriteLine($"CSV file generated successfully at: {csvPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static List<string> ParseSolution(string solutionPath)
    {
        var lines = File.ReadAllLines(solutionPath);
        var projectPaths = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("Project("))
            {
                var parts = line.Split('"');
                if (parts.Length > 3)
                {
                    var relativePath = parts[3]; // Assuming this is the relative path
                    var fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solutionPath), relativePath));
                    projectPaths.Add(fullPath);
                }
            }
        }

        return projectPaths;
    }

    static void ProcessProjectPath(string projectPath, List<(string Project, string Package, string Version)> packages)
    {
        if (File.Exists(projectPath) && projectPath.EndsWith(".csproj"))
        {
            var packageReferences = ParseProjectFile(projectPath);
            packages.AddRange(packageReferences.Select(pr => (Path.GetFileName(projectPath), pr.Package, pr.Version)));
        }
        else if (Directory.Exists(projectPath))
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
            foreach (var csprojFile in csprojFiles)
            {
                Console.WriteLine($"Found project file: {csprojFile}");
                var packageReferences = ParseProjectFile(csprojFile);
                packages.AddRange(packageReferences.Select(pr => (Path.GetFileName(csprojFile), pr.Package, pr.Version)));
            }
        }
        else
        {
            Console.WriteLine($"Resolved path is not a valid project: {projectPath}");
        }
    }

    static List<(string Package, string Version)> ParseProjectFile(string projectPath)
    {
        var xdoc = XDocument.Load(projectPath);
        var packageReferences = xdoc.Descendants("PackageReference")
            .Select(pr => (Package: pr.Attribute("Include")?.Value, Version: pr.Attribute("Version")?.Value))
            .Where(pr => !string.IsNullOrEmpty(pr.Package) && !string.IsNullOrEmpty(pr.Version))
            .ToList();

        return packageReferences;
    }

    static void WritePackagesToCsv(List<(string Project, string Package, string Version)> packages, string csvPath)
    {
        using var writer = new StreamWriter(csvPath);
        writer.WriteLine("Project,Package,Version");

        foreach (var package in packages)
        {
            writer.WriteLine($"\"{package.Project}\",\"{package.Package}\",\"{package.Version}\"");
        }
    }
}
