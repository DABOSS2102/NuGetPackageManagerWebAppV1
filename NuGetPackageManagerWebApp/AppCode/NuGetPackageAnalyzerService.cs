using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NuGet.ProjectModel;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Configuration;
using NuGet.Versioning;

namespace NuGetPackageManagerApp.AppCode
{
    public class NugetPackageAnalyzerService
    {
        private readonly NugetFileService _nugetFileService;

        public NugetPackageAnalyzerService(NugetFileService nugetFileService)
        {
            _nugetFileService = nugetFileService;
        }

        /// <summary>
        /// Finds all NuGet packages referenced by the files related to a specific project.
        /// </summary>
        public async Task<List<(string PackageId, string Version)>> GetNugetPackagesForProjectAsync(string projectPath)
        {
            // Read file paths associated with the provided project from the saved file
            var files = _nugetFileService.GetFilesForProject(projectPath);

            if (files == null || !files.Any())
            {
                Console.WriteLine($"No files found for the project: {projectPath}");
                return new List<(string PackageId, string Version)>();
            }

            // Analyze the files for package references
            var packages = new List<(string PackageId, string Version)>();
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);
                if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".vbproj", StringComparison.OrdinalIgnoreCase))
                {
                    packages.AddRange(GetPackagesFromProjectFile(file));
                }
                else if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase) &&
                         Path.GetFileName(file).Equals("packages.json", StringComparison.OrdinalIgnoreCase))
                {
                    packages.AddRange(GetPackagesFromJsonFile(file));
                }
                else if (extension.Equals(".config", StringComparison.OrdinalIgnoreCase) &&
                         Path.GetFileName(file).Equals("packages.config", StringComparison.OrdinalIgnoreCase))
                {
                    packages.AddRange(GetPackagesFromConfigFile(file));
                }
            }

            // Get all dependencies for each package
            var allPackages = new HashSet<(string PackageId, string Version)>(packages);
            foreach (var package in packages)
            {
                //var dependencies = await GetPackageDependenciesAsync(package.PackageId, package.Version);
                //allPackages.UnionWith(dependencies);
            }

            // Deduplicate and return
            return allPackages.ToList();
        }

        /// <summary>
        /// Extracts NuGet packages from a project file (.csproj, .vbproj).
        /// </summary>
        private List<(string PackageId, string Version)> GetPackagesFromProjectFile(string projectFilePath)
        {
            var packages = new List<(string PackageId, string Version)>();

            try
            {
                var projectXml = XDocument.Load(projectFilePath);
                var packageReferences = projectXml.Descendants("PackageReference");

                foreach (var reference in packageReferences)
                {
                    var id = reference.Attribute("Include")?.Value ?? string.Empty;
                    var version = reference.Attribute("Version")?.Value ?? string.Empty;

                    if (!string.IsNullOrEmpty(id))
                        packages.Add((id, version));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading project file {projectFilePath}: {ex.Message}");
            }

            return packages;
        }

        /// <summary>
        /// Extracts NuGet packages from a JSON file (e.g., packages.json).
        /// </summary>
        private List<(string PackageId, string Version)> GetPackagesFromJsonFile(string jsonFilePath)
        {
            var packages = new List<(string PackageId, string Version)>();

            try
            {
                var lockFile = LockFileUtilities.GetLockFile(jsonFilePath, NullLogger.Instance);
                if (lockFile != null)
                {
                    packages.AddRange(
                        lockFile
                            .Libraries
                            .Select(lib => (lib.Name, lib.Version.ToNormalizedString()))
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading JSON file {jsonFilePath}: {ex.Message}");
            }

            return packages;
        }

        /// <summary>
        /// Extracts NuGet packages from a packages.config file.
        /// </summary>
        private List<(string PackageId, string Version)> GetPackagesFromConfigFile(string configFilePath)
        {
            var packages = new List<(string PackageId, string Version)>();

            try
            {
                var configXml = XDocument.Load(configFilePath);
                var packageElements = configXml.Descendants("package");

                foreach (var packageElement in packageElements)
                {
                    var id = packageElement.Attribute("id")?.Value ?? string.Empty;
                    var version = packageElement.Attribute("version")?.Value ?? string.Empty;

                    if (!string.IsNullOrEmpty(id))
                        packages.Add((id, version));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading config file {configFilePath}: {ex.Message}");
            }

            return packages;
        }

        /// <summary>
        /// Recursively gets all dependencies for a given package.
        /// </summary>
        //private async Task<List<(string PackageId, string Version)>> GetPackageDependenciesAsync(string packageId, string version)
        //{
        //    var dependencies = new List<(string PackageId, string Version)>();

        //    try
        //    {
        //        var cache = new SourceCacheContext();
        //        var repositories = Repository.Provider.GetCoreV3();
        //        var packageMetadataResource = await repositories.First().GetResourceAsync<PackageMetadataResource>();
        //        var packageMetadata = await packageMetadataResource.GetMetadataAsync(packageId, true, true, cache, NullLogger.Instance, CancellationToken.None);

        //        var package = packageMetadata.FirstOrDefault(p => p.Identity.Version.ToString() == version);
        //        if (package != null)
        //        {
        //            var dependencyGroups = package.DependencySets;
        //            foreach (var group in dependencyGroups)
        //            {
        //                foreach (var dependency in group.Packages)
        //                {
        //                    dependencies.Add((dependency.Id, dependency.VersionRange.MinVersion.ToString()));
        //                    var subDependencies = await GetPackageDependenciesAsync(dependency.Id, dependency.VersionRange.MinVersion.ToString());
        //                    dependencies.AddRange(subDependencies);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error getting dependencies for package {packageId} {version}: {ex.Message}");
        //    }

        //    return dependencies;
        //}
    }
}
