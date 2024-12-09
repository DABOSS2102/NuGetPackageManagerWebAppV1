using NuGetPackageManagerWebApp.AppCode.CustomNuGetV2;
using NuGetPackageManagerWebApp.AppCode.Models.CustomNuGetModels;
using NuGetPackageManagerWebApp.AppCode.Models.FileModels;

namespace NuGetPackageManagerWebApp.AppCode
{
    public class VisualizeChartBackEnd
    {
        private readonly NuGetFileServiceV2 _NFS; // NFS = NugetFileService
        private readonly OutsideCalls _OC; // OC = OutsideCalls
        public VisualizeChartBackEnd(NuGetFileServiceV2 nugetFileService, OutsideCalls outsideCalls)
        {
            _NFS = nugetFileService;
            _OC = outsideCalls;
        }

        public async Task<List<NuGetNodeInformation>> GetData(string assetId)
        {
            List<NuGetNodeInformation> returnData = new List<NuGetNodeInformation>();
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return returnData;
            }
            AssetFilesRow? assetFileRow = await GetNodeData(assetId);
            if (assetFileRow == null)
            {
                return returnData;
            }
            List<FoundNuGetPackageRow> foundNuGetPackages = await GetPackagesBasedOnIds(assetFileRow.PackagesBeingUsed);
            if (foundNuGetPackages.Count == 0)
            {
                return returnData;
            }
            List<NuGetNodeInformation> nodeInformation = await _OC.PullMetadataForPackagesFromApi(foundNuGetPackages);
            if (nodeInformation.Count == 0)
            {
                return returnData;
            }
            else
            {
                returnData = nodeInformation;
            }

            return returnData;
        }

        public async Task<AssetFilesRow?> GetNodeData(string assetId)
        {
            AssetFilesRow? returnData = null;
            if(string.IsNullOrWhiteSpace(assetId))
            {
                return returnData;
            }

            return await _NFS.GetAssetFilesRowUsingAssetId(assetId);
        }

        public async Task<List<FoundNuGetPackageRow>> GetPackagesBasedOnIds(List<string> packageIds)
        {
            List<FoundNuGetPackageRow> returnData = new List<FoundNuGetPackageRow>();
            if (packageIds == null || packageIds.Count == 0)
            {
                return returnData;
            }

            returnData = await _NFS.GetSavedPackageRowsUsingPackageIds(packageIds);

            return returnData;
        }



    }
}
