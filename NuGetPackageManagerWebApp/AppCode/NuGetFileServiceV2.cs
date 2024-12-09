using Microsoft.VisualStudio.OLE.Interop;
using NuGet.ProjectModel;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using NuGetPackageManagerWebApp.AppCode.Models.FileModels;
using NuGetPackageManagerWebApp.AppCode.Models.CustomNuGetModels;

namespace NuGetPackageManagerWebApp.AppCode.CustomNuGetV2
{
    public class NuGetFileServiceV2
    {
        private readonly string baseAppDataPath;
        public readonly string foundNuGetPackagesFileName = "FoundNuGetPackages.csv";
        public readonly string assetFilesFileName = "AssetFiles.csv";
        private readonly List<string> saveFileNames = new List<string>();

        public NuGetFileServiceV2()
        {
            // Get the App_Data folder path
            baseAppDataPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");

            // Ensure the folder exists
            if (!Directory.Exists(baseAppDataPath))
            {
                Directory.CreateDirectory(baseAppDataPath);
            }

            saveFileNames.Add(foundNuGetPackagesFileName);
            saveFileNames.Add(assetFilesFileName);

            foreach (var fileName in saveFileNames)
            {
                var filePath = Path.Combine(baseAppDataPath, fileName);
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close();
                }
            }
        }

        public async Task<CombinedSavedData> AddNewAssetsFile(string Name, string ProjectOrSolution, string assetsFilePath, List<string>? projects = null)
        {
            CombinedSavedData combinedSavedData = new CombinedSavedData();

            AssetFilesRow assetEntry = await CheckForAssetInAssetsSaveFiles(assetsFilePath);
            if(assetEntry.Name == "")
            {
                assetEntry.Name = Name;
                assetEntry.ProjectOrSolution = ProjectOrSolution;
                if(projects != null)
                {
                    assetEntry.CustomProjects = projects;
                }
            }
            List<FoundNuGetPackageRow> allPackages = await PullDataFromPackagesSaveFiles();
            List<FoundNuGetPackageRow> foundPackages = await FindPackagesInAssetFile(assetsFilePath);
            foreach(var package in foundPackages)
            {
                FoundNuGetPackageRow? matchPackage = allPackages.Find(x => x.PackageName == package.PackageName);
                if (matchPackage == null)
                {
                    package.CustomPackageId = allPackages.Count.ToString();
                    allPackages.Add(package);
                    assetEntry.PackagesBeingUsed.Add(package.CustomPackageId);
                }
                else
                {
                    if(matchPackage.PackageVersion != package.PackageVersion)
                    {
                        package.CustomPackageId = allPackages.Count.ToString();
                        allPackages.Add(package);
                        assetEntry.PackagesBeingUsed.Add(package.CustomPackageId);
                        if (!assetEntry.PackagesBeingUsed.Contains(matchPackage.CustomPackageId))
                        {
                            assetEntry.PackagesBeingUsed.Remove(matchPackage.CustomPackageId);
                        }
                    }
                    else if(!assetEntry.PackagesBeingUsed.Contains(matchPackage.CustomPackageId))
                    {
                        assetEntry.PackagesBeingUsed.Add(matchPackage.CustomPackageId);
                    }
                    
                }
            }

            await UpdateAssetFile(assetEntry);
            await UpdatePackagesFile(allPackages);

            combinedSavedData.AssetFileInfo = assetEntry;
            combinedSavedData.FoundPackages = allPackages;
            return combinedSavedData;
        }
        public Task<List<FoundNuGetPackageRow>> PullDataFromPackagesSaveFiles()
        {
            List<FoundNuGetPackageRow> foundPackages = new List<FoundNuGetPackageRow>();

            string filePath = Path.Combine(baseAppDataPath, foundNuGetPackagesFileName);
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }
            else
            {
                var lines = File.ReadAllLinesAsync(filePath).Result;
                lines = lines.Skip(1).ToArray();
                foreach (var line in lines)
                {
                    foundPackages.Add(new FoundNuGetPackageRow(line));
                }
            }

            return Task.FromResult(foundPackages);
        }
        public async Task<List<FoundNuGetPackageRow>> GetSavedPackageRowsUsingPackageIds(List<string> packageIds)
        {
            List<FoundNuGetPackageRow> allPackages = await PullDataFromPackagesSaveFiles();
            List<FoundNuGetPackageRow> foundPackages = new List<FoundNuGetPackageRow>();
            FoundNuGetPackageRow? matchPackage = null;
            foreach (var id in packageIds)
            {
                matchPackage = allPackages.Find(x => x.CustomPackageId == id);
                if(matchPackage != null)
                {
                    foundPackages.Add(matchPackage);
                }
            }
            return foundPackages;
        }
        public Task<List<FoundNuGetPackageRow>> FindPackagesInAssetFile(string assetsFilePath) 
        {
            List<FoundNuGetPackageRow> foundNuGetPackages = new List<FoundNuGetPackageRow>();

            if(File.Exists(assetsFilePath))
            {
                var packages = new List<NuGetPackageInAssetFile>();

                // Load project.assets.json
                var lockFile = new LockFileFormat().Read(assetsFilePath);

                // Iterate through the libraries (installed packages)
                foreach (var library in lockFile.Libraries)
                {
                    var package = new NuGetPackageInAssetFile()
                    {
                        Name = library.Name,
                        Version = library.Version.ToString()
                    };

                    packages.Add(package);
                }

                foreach(var package in packages)
                {
                    foundNuGetPackages.Add(new FoundNuGetPackageRow() { PackageName = package.Name, PackageVersion = package.Version });
                }
            }

            return Task.FromResult(foundNuGetPackages);
        }
        public Task<List<AssetFilesRow>> GetAllAssetRows()
        {
            List<AssetFilesRow> assetFilesRows = new List<AssetFilesRow>();
            string filePath = Path.Combine(baseAppDataPath, assetFilesFileName);
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }
            else
            {
                var lines = File.ReadAllLines(filePath);
                if(lines.Length == 0)
                {
                    return Task.FromResult(assetFilesRows);
                }
                lines = lines.Skip(1).ToArray();
                foreach (var line in lines)
                {
                    assetFilesRows.Add(new AssetFilesRow(line));
                }
            }
            return Task.FromResult(assetFilesRows);
        }
        private async Task<AssetFilesRow> CheckForAssetInAssetsSaveFiles(string assetsFilePath)
        {
            List<AssetFilesRow> assetFilesRows = await GetAllAssetRows();

            if (assetFilesRows.Count > 0)
            {
                return assetFilesRows.Find(x => x.AssetsPath == assetsFilePath) ?? new AssetFilesRow() { AssetFileIds = assetFilesRows.Count.ToString(), AssetsPath = assetsFilePath };
            }
            else
            {
                return new AssetFilesRow() { AssetFileIds = assetFilesRows.Count.ToString(), AssetsPath = assetsFilePath };
            }
        }
        public async Task<AssetFilesRow?> GetAssetFilesRowUsingAssetId(string id)
        {
            List<AssetFilesRow> assetFilesRows = await GetAllAssetRows();

            if (assetFilesRows.Count > 0)
            {
                return assetFilesRows.Find(x => x.AssetFileIds == id);
            }
            else
            {
                return null;
            }
        }
        private async Task UpdateAssetFile(AssetFilesRow assetRow)
        {
            string filePath = Path.Combine(baseAppDataPath, assetFilesFileName);

            bool found = File.Exists(filePath);
            if (!found)
            {
                File.Create(filePath).Close();
            }

            List<AssetFilesRow> assetFilesRows = await GetAllAssetRows();

            bool foundInFile = (assetFilesRows.Find(x => x.AssetsPath == assetRow.AssetsPath) != null);

            if (foundInFile)
            {
                int index = assetFilesRows.FindIndex(x => x.AssetsPath == assetRow.AssetsPath);
                assetFilesRows[index] = assetRow;
            }
            else
            {
                assetFilesRows.Add(assetRow);
            }

            using (StreamWriter sw = new StreamWriter(filePath))
            {
                await sw.WriteLineAsync(assetRow.Headers);
                foreach (var asset in assetFilesRows)
                {
                    await sw.WriteLineAsync(asset.CustomToString());
                }
            }
        }
        private async Task UpdatePackagesFile(List<FoundNuGetPackageRow> packages)
        {
            string filePath = Path.Combine(baseAppDataPath, foundNuGetPackagesFileName);

            bool found = File.Exists(filePath);
            if (!found)
            {
                File.Create(filePath).Close();
            }

            using (StreamWriter sw = new StreamWriter(filePath))
            {
                await sw.WriteLineAsync(new FoundNuGetPackageRow().Headers);
                foreach (var asset in packages)
                {
                    await sw.WriteLineAsync(asset.CustomToString());
                }
            }
        }

        #region Getters and Setters

        public string GetBaseAppDataPath()
        {
            return baseAppDataPath;
        }

        // IDK if these are necassary
        #region App Code Files
        public string GetFoundNuGetPackagesFileName()
        {
            return foundNuGetPackagesFileName;
        }
        public string GetAssetFilesFileName()
        {
            return assetFilesFileName;
        }
        #endregion

        #endregion
    }
}
