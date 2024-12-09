using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NuGet.Configuration;

namespace NuGetPackageManagerApp.AppCode
{
    public class NugetFileService
    {
        public readonly string _outputFilePath;
        public readonly string _globalPackageFolder;

        public NugetFileService(string relativeOutputFilePath)
        {
            _outputFilePath = Path.Combine(AppContext.BaseDirectory, relativeOutputFilePath);

            // Determine the default global packages folder based on the OS
            _globalPackageFolder = GetDefaultGlobalPackagesFolder();

            Console.WriteLine($"Global NuGet Package Folder: {_globalPackageFolder}");

            // Add all packages from the global package folder
            AddPackagesFromGlobalFolder();
        }

        /// <summary>
        /// Gets the default global package folder for the system.
        /// </summary>
        private string GetDefaultGlobalPackagesFolder()
        {
            // Check for Windows environment using RuntimeInformation
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Get the user's profile directory
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                // Ensure the path structure is correct for Windows
                string globalPackagesFolder = Path.Combine(userProfile, ".nuget", "packages");

                // Verify if the directory exists, otherwise return a fallback
                if (Directory.Exists(globalPackagesFolder))
                {
                    return globalPackagesFolder;
                }
                else
                {
                    // Fallback to a known default if the directory does not exist
                    string fallbackFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NuGet", "packages");
                    return fallbackFolder;
                }
            }

            // Check for Linux or macOS environment
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userHome, ".nuget", "packages");
            }

            // Default case if platform is not identified
            throw new InvalidOperationException($"Unsupported platform for NuGet global package folder. {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture} {RuntimeInformation.ProcessArchitecture} {RuntimeInformation.FrameworkDescription}");
        }

        /// <summary>
        /// Adds new project/file mappings to the save file.
        /// Ensures that existing data is preserved, and duplicates are avoided.
        /// </summary>
        public void AddMappings(List<(string ProjectOrSolutionPath, string FilePath)> newMappings)
        {
            // Ensure the directory exists
            string directoryPath = Path.GetDirectoryName(_outputFilePath) ?? AppContext.BaseDirectory;
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"Created directory: {directoryPath}");
            }

            // Load existing data
            var existingData = LoadMappings();

            // Merge new data with existing data, avoiding duplicates
            var updatedData = existingData
                .Concat(newMappings)
                .Distinct()
                .ToList();

            // Save the combined data
            SaveMappings(updatedData);

            Console.WriteLine($"Updated NuGet package info saved to {_outputFilePath}");
        }

        /// <summary>
        /// Scans a folder for files that could contain NuGet package references
        /// and adds them to the saved file.
        /// </summary>
        public void AddFilesFromFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"Folder does not exist: {folderPath}");
                return;
            }

            // Get all relevant files from the folder
            var relevantFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(IsRelevantFile)
                .ToList();

            // Map files to projects based on folder structure
            var mappings = relevantFiles.Select(filePath =>
            {
                string folderName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? "UnknownProject");
                return (ProjectOrSolutionPath: folderName, FilePath: filePath);
            }).ToList();

            // Add mappings to the saved file
            AddMappings(mappings);
        }

        /// <summary>
        /// Determines if a file is relevant for NuGet package references.
        /// </summary>
        private static bool IsRelevantFile(string filePath)
        {
            var relevantExtensions = new[] { ".sln", ".csproj", ".vbproj", ".props", ".targets", ".config" };
            return relevantExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Removes specific files from the saved data.
        /// </summary>
        public void RemoveFiles(List<string> filePaths)
        {
            var existingData = LoadMappings();
            var updatedData = existingData
                .Where(mapping => !filePaths.Contains(mapping.FilePath))
                .ToList();
            SaveMappings(updatedData);
        }

        /// <summary>
        /// Removes specific projects/solutions from the saved data.
        /// </summary>
        public void RemoveProjects(List<string> projectPaths)
        {
            var existingData = LoadMappings();
            var updatedData = existingData
                .Where(mapping => !projectPaths.Contains(mapping.ProjectOrSolutionPath))
                .ToList();
            SaveMappings(updatedData);
        }

        /// <summary>
        /// Retrieves all files associated with a specific project/solution from the saved file.
        /// </summary>
        public List<string> GetFilesForProject(string projectPath)
        {
            return LoadMappings()
                .Where(mapping => mapping.ProjectOrSolutionPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                .Select(mapping => mapping.FilePath)
                .ToList();
        }

        /// <summary>
        /// Retrieves all projects/solutions from the saved file.
        /// </summary>
        public List<string> GetAllProjects()
        {
            return LoadMappings()
                .Select(mapping => mapping.ProjectOrSolutionPath)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Loads all saved mappings from the file.
        /// </summary>
        public List<(string ProjectOrSolutionPath, string FilePath)> LoadMappings()
        {
            if (!File.Exists(_outputFilePath))
                return new List<(string ProjectOrSolutionPath, string FilePath)>();

            return File.ReadAllLines(_outputFilePath)
                .Select(line => line.Split(','))
                .Where(parts => parts.Length == 2)
                .Select(parts => (ProjectOrSolutionPath: parts[0], FilePath: parts[1]))
                .ToList();
        }

        /// <summary>
        /// Saves the mappings to the file.
        /// </summary>
        private void SaveMappings(List<(string ProjectOrSolutionPath, string FilePath)> data)
        {
            var lines = data.Select(entry => $"{entry.ProjectOrSolutionPath},{entry.FilePath}");
            File.WriteAllLines(_outputFilePath, lines);
        }

        /// <summary>
        /// Initializes the saved file with the global packages folder if it doesn't exist.
        /// </summary>
        public void InitializeWithGlobalPackagesFolder()
        {
            if (!File.Exists(_outputFilePath))
            {
                AddMappings(new List<(string ProjectOrSolutionPath, string FilePath)>
                {
                    ("GlobalPackages", _globalPackageFolder)
                });
            }
        }

        /// <summary>
        /// Adds all packages from the global package folder to the mappings.
        /// </summary>
        private void AddPackagesFromGlobalFolder()
        {
            if (Directory.Exists(_globalPackageFolder))
            {
                var packageFiles = Directory.GetFiles(_globalPackageFolder, "*.*", SearchOption.AllDirectories)
                    .Where(IsRelevantFile)
                    .ToList();

                var mappings = packageFiles.Select(filePath =>
                {
                    string folderName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? "UnknownProject");
                    return (ProjectOrSolutionPath: folderName, FilePath: filePath);
                }).ToList();

                AddMappings(mappings);
            }
        }
    }
}
