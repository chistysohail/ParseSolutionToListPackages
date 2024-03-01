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
        Console.WriteLine("Please enter the path to your solution (.sln) file:");
        var solutionPath = Console.ReadLine(); // Get solution path from user input

        // Validate the input path
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
        {
            Console.WriteLine("Invalid path. Please make sure the path is correct and try again.");
            return;
        }

        var projects = ParseSolution(solutionPath);
        var packages = new List<(string Project, string Package, string Version)>();

        foreach (var projectPath in projects)
        {
            var packageReferences = ParseProjectFile(projectPath);
            packages.AddRange(packageReferences.Select(pr => (Project: Path.GetFileName(projectPath), pr.Package, pr.Version)));
        }

        // Assuming the CSV file is to be created in the same directory as the solution file
        var csvPath = Path.Combine(Path.GetDirectoryName(solutionPath), "packages.csv");
        WritePackagesToCsv(packages, csvPath);

        Console.WriteLine($"CSV file generated successfully at: {csvPath}");
    }

    static List<string> ParseSolution(string solutionPath)
    {
        var projectLines = File.ReadAllLines(solutionPath)
            .Where(line => line.StartsWith("Project("))
            .ToList();

        var projectPaths = projectLines
            .Select(line =>
            {
                var matches = Regex.Match(line, "\"(.*?\\.csproj)\"");
                return Path.Combine(Path.GetDirectoryName(solutionPath), matches.Groups[1].Value);
            }).ToList();

        return projectPaths;
    }

    static List<(string Package, string Version)> ParseProjectFile(string projectPath)
    {
        var xdoc = XDocument.Load(projectPath);
        XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
        var packageReferences = xdoc.Descendants(ns + "PackageReference")
            .Select(pr => (Package: pr.Attribute("Include")?.Value, Version: pr.Attribute("Version")?.Value))
            .Where(pr => pr.Package != null && pr.Version != null)
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
