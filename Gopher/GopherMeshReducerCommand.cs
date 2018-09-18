///////////////////////////////////////////////////////////////////////////////
// Gopher Geometry
// Copyright(C) 2018  Matthew Newberg

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License v3 as 
// published by the Free Software Foundation, either version 3 of the 
// License, or (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with this program.If not, see<http://www.gnu.org/licenses/>.
///////////////////////////////////////////////////////////////////////////////

using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;
using g3;

namespace Gopher
{
    [System.Runtime.InteropServices.Guid("9311b973-5bbd-4408-9abf-cb4a0944d960")]
    public class GopherMeshReducerCommand : Command
    {
        static GopherMeshReducerCommand _instance;
        public GopherMeshReducerCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GopherMeshReducer command.</summary>
        public static GopherMeshReducerCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "GopherMeshReducer"; }
        }

        int maxTriangleCount = 0;
        double minEdgeLength = 5;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const ObjectType geometryFilter = ObjectType.Mesh;
            
            OptionDouble minEdgeLengthOption = new OptionDouble(minEdgeLength, 0.001, 200);
            OptionInteger maxTriangleCountOption = new OptionInteger(maxTriangleCount, 0, 100000000);
            
            GetObject go = new GetObject();
            go.SetCommandPrompt("Select meshes to reduce");
            go.GeometryFilter = geometryFilter;
            go.AddOptionDouble("MinEdge", ref minEdgeLengthOption);
            go.AddOptionInteger("MaxTriangle", ref maxTriangleCountOption);

            go.GroupSelect = true;
            go.SubObjectSelect = false;
            go.EnableClearObjectsOnEntry(false);
            go.EnableUnselectObjectsOnExit(false);
            go.DeselectAllBeforePostSelect = false;

            bool bHavePreselectedObjects = false;

            for (;;)
            {
                GetResult res = go.GetMultiple(1, 0);

                if (res == GetResult.Option)
                {
                    go.EnablePreSelect(false, true);
                    continue;
                }

                else if (go.CommandResult() != Result.Success)
                    return go.CommandResult();

                if (go.ObjectsWerePreselected)
                {
                    bHavePreselectedObjects = true;
                    go.EnablePreSelect(false, true);
                    continue;
                }

                break;
            }

            maxTriangleCount = maxTriangleCountOption.CurrentValue;
            minEdgeLength = minEdgeLengthOption.CurrentValue;

            if (bHavePreselectedObjects)
            {
                // Normally, pre-selected objects will remain selected, when a
                // command finishes, and post-selected objects will be unselected.
                // This this way of picking, it is possible to have a combination
                // of pre-selected and post-selected. So, to make sure everything
                // "looks the same", lets unselect everything before finishing
                // the command.
                for (int i = 0; i < go.ObjectCount; i++)
                {
                    RhinoObject rhinoObject = go.Object(i).Object();
                    if (null != rhinoObject)
                        rhinoObject.Select(false);
                }
                doc.Views.Redraw();
            }

            bool result = false;

            foreach (var obj in go.Objects())
            {
                var rhinoMesh = obj.Mesh();

                if (rhinoMesh == null || !rhinoMesh.IsValid)
                    continue;

                var mesh = GopherUtil.ConvertToD3Mesh(obj.Mesh());
                
                GopherUtil.ReduceMesh(mesh, (float)minEdgeLength, maxTriangleCount);

                double mine, maxe, avge;
                MeshQueries.EdgeLengthStats(mesh, out mine, out maxe, out avge);
                   RhinoApp.WriteLine("Edge length stats: {0} {1} {2}", mine, maxe, avge);

                var newRhinoMesh = GopherUtil.ConvertToRhinoMesh(mesh);

                if (newRhinoMesh != null && newRhinoMesh.IsValid)
                {
                    doc.Objects.Replace(obj, newRhinoMesh);
                }

                if (newRhinoMesh != null && newRhinoMesh.IsValid)
                {
                    result |= doc.Objects.Replace(obj, newRhinoMesh);
                }
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
