using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http.Json;
using BootstrapBlazor.Components;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;

namespace NuGetPackageManagerApp.AppCode.NuGet
{
    public class NuGetService
    {
        private readonly HttpClient _httpClient;
        private readonly string NuGetApiBase1 = "https://api.nuget.org/";
        private readonly string NuGetApiBase2 = "https://azuresearch-usnc.nuget.org/";

        public NuGetService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> GetAutoCompleteSuggestions(string query)
        {
            var requestUri = NuGetApiBase2 + $"autocomplete?q={Uri.EscapeDataString(query)}&take=5";
            var response = await _httpClient.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NuGetAutoCompleteResponse>();
                return result?.Data ?? new List<string>();
            }
            
            return new List<string>();
        }

        public async Task<List<NuGetPackageSearchInfo>> GetSearchQueryResults(string PackageName)
        {
            var requestUri = NuGetApiBase2 + $"query?q={Uri.EscapeDataString(PackageName)}&take=5";

            var response = await _httpClient.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NuGetSearchResponse>();
                return result?.Data ?? new List<NuGetPackageSearchInfo>();
            }

            return new List<NuGetPackageSearchInfo>();
        }

        public async Task<NuGetPackageInfoFromCatalog?> GetPackageInfoFromCatalogAsync(string packageName, string version)
        {
            NuGetPackageInfoFromCatalog? returnPackage = null;

            string catalogURL = await GetNuGetPackageCatalogURLAsync(packageName, version);

            if(String.IsNullOrWhiteSpace(catalogURL))
            {
                return null;
            }

            try
            {
                var response = await _httpClient.GetAsync(catalogURL);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<NuGetPackageInfoFromCatalog>();
                    returnPackage = result;
                }
            }
            catch (Exception e)
            {
                return null;
            }

            return returnPackage;
        }
        public async Task<string> GetNuGetPackageCatalogURLAsync(string packageName, string version)
        {
            string catalogURL = "";

            var requestUri = NuGetApiBase1 + $"v3/registration5-semver1/{packageName.ToLower()}/{version.ToLower()}.json";

            try
            {
                var response = await _httpClient.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Registration5Semver1ReturnModel>();
                    catalogURL = result.CatalogEntry;
                }
            }
            catch (Exception e)
            {
                return "";
            }

            return catalogURL;
        }
    }
    public class Registration5Semver1ReturnModel
    {
        // URL to the Catalog Entry
        // This has all the data about the package
        [JsonPropertyName("catalogEntry")]
        public string CatalogEntry { get; set; } = "";

        [JsonPropertyName("listed")]
        public bool IsListed { get; set; }

        // URL to the Package Content
        [JsonPropertyName("packageContent")]
        public string PackageContent { get; set; } = "";

        // Date of when this version of the package was published
        [JsonPropertyName("published")]
        public DateTime Published { get; set; }

        // URL to the Package Details
        [JsonPropertyName("registration")]
        public string Registration { get; set; } = "";
    }

    public class NuGetPackageInfoFromCatalog
    {
        [JsonPropertyName("authors")]
        public string Authors { get; set; } = "";

        [JsonPropertyName("catalog:commitId")]
        public string CatalogCommitId { get; set; } = "";

        [JsonPropertyName("catalog:commitTimeStamp")]
        public DateTime CatalogCommitTimeStamp { get; set; }

        [JsonPropertyName("copyright")]
        public string CopyRight { get; set; } = "";

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("iconFile")]
        public string IconFile { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("isPrerelease")]
        public bool IsPrerelease { get; set; }

        [JsonPropertyName("lastEdited")]
        public DateTime LastEdited { get; set; }

        [JsonPropertyName("licenseExpression")]
        public string LicenseExpression { get; set; } = "";

        [JsonPropertyName("licenseUrl")]
        public string LicenseUrl { get; set; } = "";

        [JsonPropertyName("listed")]
        public bool IsListed { get; set; }

        [JsonPropertyName("packageHash")]
        public string PackageHash { get; set; } = "";

        [JsonPropertyName("packageHashAlgorithm")]
        public string PackageHashAlgorithm { get; set; } = "";

        [JsonPropertyName("packageSize")]
        public long PackageSize { get; set; }

        [JsonPropertyName("projectUrl")]
        public string ProjectUrl { get; set; } = "";

        [JsonPropertyName("published")]
        public DateTime Published { get; set; }

        [JsonPropertyName("readmeFile")]
        public string ReadmeFile { get; set; } = "";

        [JsonPropertyName("releaseNotes")]
        public string ReleaseNotes { get; set; } = "";

        [JsonPropertyName("repository")]
        public string Repository { get; set; } = "";

        [JsonPropertyName("verbatimVersion")]
        public string VerbatimVersion { get; set; } = "";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        public List<DependencyGroup> DependencyGroups { get; set; } = new List<DependencyGroup>();

        // Just in case there are no Dependency Groups
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();
        public List<packageEntry> PackageEntries { get; set; } = new List<packageEntry>();
        public List<string> Tags { get; set; } = new List<string>();
    }
    public class DependencyGroup
    {
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();

        [JsonPropertyName("targetFramework")]
        public string TargetFramework { get; set; } = "";
    }
    public class Dependency
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("range")]
        public string Range { get; set; } = "";
    }
    public class packageEntry
    {
        [JsonPropertyName("compressedLength")]
        public long CompressedLength { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = "";

        [JsonPropertyName("length")]
        public long Length { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    public class NuGetAutoCompleteResponse
    {
        public List<string> Data { get; set; }
    }

    public class NuGetSearchResponse
    {
        public List<NuGetPackageSearchInfo> Data { get; set; }
    }

    public class NuGetPackageSearchInfo
    {
        [JsonPropertyName("id")]
        public string id { get; set; }
        [JsonPropertyName("version")]
        public string version { get; set; }
        [JsonPropertyName("description")]
        public string description { get; set; }
        [JsonPropertyName("authors")]
        public List<string> Authors { get; set; }
        [JsonPropertyName("versions")]
        public List<NuGetPackageVersion> Versions { get; set; }
    }
    public class NuGetPackageVersion
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }
    }
    


}
