using System;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Configuration;
using NuGetPackageManagerWebApp.AppCode.Models.CustomNuGetModels;
using NuGetPackageManagerWebApp.AppCode.Models.FileModels;
using NuGet.Common;

namespace NuGetPackageManagerWebApp.AppCode
{
    public class OutsideCalls
    {
        private readonly SourceRepository source;
        private readonly NuGet.Common.ILogger _nugetLogger;

        public OutsideCalls(ILogger<OutsideCalls> logger)
        {
            source = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            _nugetLogger = new NuGetLoggerAdapter(logger);
        }

        public async Task<List<NuGetNodeInformation>> PullMetadataForPackagesFromApi(List<FoundNuGetPackageRow> packages)
        {
            var metadataResource = await source.GetResourceAsync<PackageMetadataResource>();

            List<NuGetNodeInformation> returnData = new List<NuGetNodeInformation>();
            if (packages == null || packages.Count == 0)
            {
                return returnData;
            }
            foreach (var package in packages)
            {
                var metadata = await metadataResource.GetMetadataAsync(
                    package.PackageName,
                    includePrerelease: true,
                    includeUnlisted: false,
                    sourceCacheContext: new SourceCacheContext(),
                    log: _nugetLogger,
                    token: System.Threading.CancellationToken.None
                );

                var foundPackage = metadata.FirstOrDefault(m => m.Identity.Version.ToNormalizedString() == package.PackageVersion);

                if (foundPackage != null)
                {
                    NuGetNodeInformation node = new NuGetNodeInformation
                    {
                        packageId = foundPackage.Identity.Id,
                        packageVersion = foundPackage.Identity.Version.ToNormalizedString(),
                        dependencies = foundPackage.DependencySets.SelectMany(ds => ds.Packages).ToList()
                    };

                    returnData.Add(node);
                }
            }
            return returnData;
        }
    }
}
