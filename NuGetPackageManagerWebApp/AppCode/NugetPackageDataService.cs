using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System.Xml.Linq;
using NuGet.Configuration;

namespace NuGetPackageManagerApp.AppCode
{
    public class NugetPackageDataService
    {
        private readonly SourceRepository _repository;

        public NugetPackageDataService(string nugetFeedUrl = "https://api.nuget.org/v3/index.json")
        {
            // Initialize NuGet repository
            var packageSource = new PackageSource(nugetFeedUrl);
            _repository = Repository.Factory.GetCoreV3(packageSource);
        }

        /// <summary>
        /// Retrieves metadata for a specific package.
        /// </summary>
        public async Task<PackageMetadata> GetPackageMetadataAsync(string packageId, string version, string projectFilePath)
        {
            try
            {
                // Determine the target framework from the project file
                var targetFramework = GetTargetFramework(projectFilePath);

                var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>();
                var packageMetadata = await metadataResource.GetMetadataAsync(
                    packageId,
                    includePrerelease: true,
                    includeUnlisted: false,
                    sourceCacheContext: new SourceCacheContext(),
                    log: NullLogger.Instance,
                    token: System.Threading.CancellationToken.None);

                // Ensure we are working with a PackageSearchMetadata, not the interface
                var specificVersion = packageMetadata
                    .Where(p => p.Identity.Version.ToNormalizedString() == version)
                    .FirstOrDefault();

                if (specificVersion == null)
                    throw new Exception($"Package {packageId} version {version} not found.");

                // Filter dependencies based on the target framework
                var packageInfo = new PackageMetadata
                {
                    PackageId = specificVersion.Identity.Id,
                    Version = specificVersion.Identity.Version.ToNormalizedString(),
                    Description = specificVersion.Description,
                    Authors = specificVersion.Authors,
                    LicenseUrl = specificVersion.LicenseUrl?.ToString(),
                    ProjectUrl = specificVersion.ProjectUrl?.ToString(),
                    Dependencies = GetDependencies((PackageSearchMetadata)specificVersion, targetFramework)
                };

                return packageInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving metadata for package {packageId} version {version}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves metadata for multiple packages.
        /// </summary>
        public async Task<List<PackageMetadata>> GetPackagesMetadataAsync(List<(string PackageId, string Version, string ProjectFilePath)> packages)
        {
            var results = new List<PackageMetadata>();

            foreach (var package in packages)
            {
                var metadata = await GetPackageMetadataAsync(package.PackageId, package.Version, package.ProjectFilePath);
                if (metadata != null)
                {
                    results.Add(metadata);
                }
            }

            return results;
        }

        /// <summary>
        /// Extracts package dependencies and filters them by the target framework.
        /// </summary>
        private List<string> GetDependencies(PackageSearchMetadata packageMetadata, string targetFramework)
        {
            var dependencies = new List<string>();

            foreach (var dependencySet in packageMetadata.DependencySets)
            {
                // Filter dependencies based on the target framework
                if (dependencySet.TargetFramework.Framework.Equals(targetFramework, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var packageDependency in dependencySet.Packages)
                    {
                        dependencies.Add($"{packageDependency.Id} {packageDependency.VersionRange}");
                    }
                }
            }

            return dependencies;
        }

        /// <summary>
        /// Gets the target framework of the project by parsing the .csproj file.
        /// </summary>
        private string GetTargetFramework(string projectFilePath)
        {
            try
            {
                // Load the .csproj file
                var xDocument = XDocument.Load(projectFilePath);

                // Find the TargetFramework or TargetFrameworks element
                var frameworkElement = xDocument.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "TargetFramework" || e.Name.LocalName == "TargetFrameworks");

                if (frameworkElement != null)
                {
                    return frameworkElement.Value;
                }

                // If no framework is found, default to an empty string or throw exception
                throw new Exception("No target framework found in the project file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading project file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Fetches all NuGet packages used within shared files related to the project.
        /// </summary>
        public async Task<List<PackageMetadata>> GetPackagesFromSharedFilesAsync(string projectFilePath, List<string> sharedFilePaths)
        {
            var sharedPackages = new List<(string PackageId, string Version)>();

            foreach (var sharedFilePath in sharedFilePaths)
            {
                // Extract the packages from the shared file
                var packages = ExtractPackagesFromFile(sharedFilePath);
                sharedPackages.AddRange(packages);
            }

            // Get metadata for all shared packages
            var results = new List<PackageMetadata>();

            foreach (var package in sharedPackages)
            {
                var metadata = await GetPackageMetadataAsync(package.PackageId, package.Version, projectFilePath);
                if (metadata != null)
                {
                    results.Add(metadata);
                }
            }

            return results;
        }

        /// <summary>
        /// Extracts the package references from a file.
        /// </summary>
        private List<(string PackageId, string Version)> ExtractPackagesFromFile(string filePath)
        {
            var packageReferences = new List<(string PackageId, string Version)>();

            try
            {
                // Read the file (this could be a .csproj, .config, or any other file containing package references)
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    // Look for patterns matching package references, e.g., <PackageReference Include="PackageId" Version="1.0.0" />
                    if (line.Contains("<PackageReference"))
                    {
                        var packageId = ExtractAttributeValue(line, "Include");
                        var version = ExtractAttributeValue(line, "Version");

                        if (!string.IsNullOrEmpty(packageId) && !string.IsNullOrEmpty(version))
                        {
                            packageReferences.Add((packageId, version));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting packages from file {filePath}: {ex.Message}");
            }

            return packageReferences;
        }

        /// <summary>
        /// Extracts the value of an attribute from an XML-like string.
        /// </summary>
        private string ExtractAttributeValue(string line, string attributeName)
        {
            var startIndex = line.IndexOf(attributeName + "=\"");

            if (startIndex == -1)
            {
                return null;
            }

            startIndex += attributeName.Length + 2; // Skip the attribute name and the opening quote
            var endIndex = line.IndexOf("\"", startIndex);

            return line.Substring(startIndex, endIndex - startIndex);
        }
    }

    /// <summary>
    /// Represents detailed metadata about a NuGet package.
    /// </summary>
    public class PackageMetadata
    {
        public string PackageId { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Authors { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
    }
}
