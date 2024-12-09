using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using NuGet.Packaging.Core;
using System.Drawing;

namespace NuGetPackageManagerWebApp.AppCode.Models.CustomNuGetModels
{
    class NuGetPackageInAssetFile
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
    }

    public class NuGetNodeInformation
    {
        public string packageId { get; set; }
        public string packageVersion { get; set; }
        public List<PackageDependency> dependencies { get; set; }
    }

    public class CustomNodeModel : NodeModel
    {
        public CustomNodeModel(NuGetNodeInformation data, Blazor.Diagrams.Core.Geometry.Point? position = null) : base(position) 
        {
            if (data != null)
            {
                Title = data.packageId;
                packageVersion = data.packageVersion;
                dependencies = data.dependencies;
            }
            else
            {
                Title = "";
                packageVersion = "";
                dependencies = new List<PackageDependency>();
            }
            
            if(dependencies.Count > 0)
            {
                //ToDependeciesPort = AddPort(new PortModel(this, PortAlignment.Right));
                //ToDependeciesPortAnchor = new SinglePortAnchor(ToDependeciesPort);
            }
        }

        public LinkModel addAsDependencyNode(CustomNodeModel fromNode)
        {
            //if(InPort == null)
            //{
            //    InPort = AddPort(new PortModel(this, PortAlignment.Left));
            //}
            //if(InPortAnchor == null)
            //{
            //    InPortAnchor = new SinglePortAnchor(InPort);
            //}
            return new LinkModel(fromNode, this);
        }
        public string packageVersion { get; set; } = "";
        public List<PackageDependency> dependencies { get; set; } = new List<PackageDependency>();
        // PortModel? ToDependeciesPort { get; set; } = null;
        //public PortModel? InPort { get; set; } = null;
        //public SinglePortAnchor? InPortAnchor { get; set; } = null;
        //public SinglePortAnchor? ToDependeciesPortAnchor { get; set; } = null;
    }
}
