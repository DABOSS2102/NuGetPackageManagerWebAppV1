namespace NuGetPackageManagerWebApp.AppCode.Models.FileModels
{
    public class FoundNuGetPackageRow
    {
        public string CustomPackageId { get; set; } = "";
        public string PackageName { get; set; } = "";
        public string PackageVersion { get; set; } = "";
        public FoundNuGetPackageRow()
        {
            CustomPackageId = "";
            PackageName = "";
            PackageVersion = "";
        }
        public FoundNuGetPackageRow(string csvRow)
        {
            var parts = csvRow.Split(',');
            if (parts.Length == 3)
            {
                CustomPackageId = parts[0];
                PackageName = parts[1];
                PackageVersion = parts[2];
            }
        }
        public readonly string Headers = "CustomPackageId,PackageName,PackageVersion";
        public string CustomToString()
        {
            return $"{CustomPackageId},{PackageName},{PackageVersion}";
        }
    }
    public class AssetFilesRow
    {
        public string AssetFileIds { get; set; } = "";
        public string Name { get; set; } = "";
        public string ProjectOrSolution { get; set; } = "";
        public string AssetsPath { get; set; } = "";
        public string TargetFramework { get; set; } = "";

        // This is a list of the AssetFileIds
        // Each one is separated by an underscore "_"
        public List<string> CustomProjects { get; set; } = new List<string>();

        // This is a list of the CustomPackageIds
        // Each one is separated by an underscore "_"
        public List<string> PackagesBeingUsed { get; set; } = new List<string>();
        public readonly string Headers = "AssetFileIds,Name,ProjectOrSolution,AssetsPath,CustomProjects,PackagesBeingUsed";
        public AssetFilesRow()
        {
            AssetFileIds = "";
            Name = "";
            ProjectOrSolution = "";
            AssetsPath = "";
            CustomProjects = new List<string>();
        }
        public AssetFilesRow(string csvRow)
        {
            var parts = csvRow.Split(',');
            if (parts.Length == 6)
            {
                AssetFileIds = parts[0];
                Name = parts[1];
                ProjectOrSolution = parts[2];
                AssetsPath = parts[3];
                var parts2 = parts[4].Split('_');
                foreach (var part in parts2)
                {
                    CustomProjects.Add(part);
                }
                var parts3 = parts[5].Split('_');
                foreach (var part in parts3)
                {
                    PackagesBeingUsed.Add(part);
                }
            }
        }
        public string CustomToString()
        {
            return $"{AssetFileIds},{Name},{ProjectOrSolution},{AssetsPath},{string.Join("_", CustomProjects)},{string.Join("_", PackagesBeingUsed)}";
        }
    }
    public class CombinedSavedData
    {
        public AssetFilesRow AssetFileInfo { get; set; } = new AssetFilesRow();
        public List<FoundNuGetPackageRow> FoundPackages { get; set; } = new List<FoundNuGetPackageRow>();
    }
}
