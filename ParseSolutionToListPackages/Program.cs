using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CsprojFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter the path to your directory containing the solution:");
            var rootDirectory = Console.ReadLine();
                        
            string outputFile = rootDirectory;

            try
            {
                var csprojFiles = Directory.GetFiles(rootDirectory, "*.csproj", SearchOption.AllDirectories).ToList();

                var projectsWithPackages = new List<string>();

                foreach (var csprojFile in csprojFiles)
                {
                    var packages = ExtractPackageReferences(csprojFile);
                    if (packages.Any())
                    {
                        foreach (var package in packages)
                        {
                            projectsWithPackages.Add($"{csprojFile},{package.Item1},{package.Item2}");
                        }
                    }
                    else
                    {
                        projectsWithPackages.Add($"{csprojFile},,");
                    }
                }

                if (projectsWithPackages.Any())
                {
                    SaveToCsv(projectsWithPackages, $"{outputFile}\\packages.csv");
                    Console.WriteLine($"Found and saved details of {projectsWithPackages.Count} .csproj files to {outputFile}.");
                }
                else
                {
                    Console.WriteLine("No .csproj files found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static List<(string, string)> ExtractPackageReferences(string csprojPath)
        {
            var packageReferences = new List<(string, string)>();
            var doc = XDocument.Load(csprojPath);
            var ns = doc.Root.GetDefaultNamespace();

            var packages = doc.Descendants(ns + "PackageReference");
            foreach (var package in packages)
            {
                var name = package.Attribute("Include")?.Value;
                var version = package.Attribute("Version")?.Value;
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(version))
                {
                    packageReferences.Add((name, version));
                }
            }

            return packageReferences;
        }

        static void SaveToCsv(List<string> dataLines, string outputFile)
        {
            using (var writer = new StreamWriter(outputFile))
            {
                writer.WriteLine("Project Path,Package Name,Package Version"); // Header
                foreach (var line in dataLines)
                {
                    writer.WriteLine(line);
                }
            }
        }
    }
}
