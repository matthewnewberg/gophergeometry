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
    [System.Runtime.InteropServices.Guid("86146c92-f6ca-411e-b7c5-8363aeae7608")]
    public class GopherRemeshConstrainedCommand : Command
    {
        static GopherRemeshConstrainedCommand _instance;
        public GopherRemeshConstrainedCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GopherRemeshConstrainedCommand command.</summary>
        public static GopherRemeshConstrainedCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "GopherRemeshConstrained"; }
        }

        double constriantAngle = 30.0f;
        double minEdgeLength = 0.1f;
        double maxEdgeLength = 0.2f;
        int smoothSteps = 20;
        double smoothSpeed = 0.5f;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            const ObjectType geometryFilter = ObjectType.MeshEdge;

            OptionDouble minEdgeLengthOption = new OptionDouble(minEdgeLength, 0.001, 200);
            OptionDouble maxEdgeLengthOption = new OptionDouble(maxEdgeLength, 0.001, 200);
            OptionDouble constriantAngleOption = new OptionDouble(constriantAngle, 0.001, 360);
            OptionInteger smoothStepsOptions = new OptionInteger(smoothSteps, 0, 1000);
            OptionDouble smoothSpeedOption = new OptionDouble(smoothSpeed, 0.01, 1.0);

            GetObject go = new GetObject();
            go.SetCommandPrompt("Select mesh edges to constrain during remesh");
            go.GeometryFilter = geometryFilter;

            go.AddOptionDouble("ConstraintAngle", ref constriantAngleOption);
            go.AddOptionDouble("MinEdge", ref minEdgeLengthOption);
            go.AddOptionDouble("MaxEdge", ref maxEdgeLengthOption);
            go.AddOptionInteger("SmoothSteps", ref smoothStepsOptions);
            go.AddOptionDouble("SmoothSpeed", ref smoothSpeedOption);

            go.GroupSelect = true;
            go.SubObjectSelect = true;

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

                break;
            }

            minEdgeLength = minEdgeLengthOption.CurrentValue;
            maxEdgeLength = maxEdgeLengthOption.CurrentValue;
            constriantAngle = constriantAngleOption.CurrentValue;
            smoothSteps = smoothStepsOptions.CurrentValue;
            smoothSpeed = smoothSpeedOption.CurrentValue;

            System.Collections.Generic.List<g3.Line3d> constrain = new System.Collections.Generic.List<g3.Line3d>();
            System.Collections.Generic.List<System.Guid> meshes = new System.Collections.Generic.List<System.Guid>();

            foreach (var obj in go.Objects())
            {
                if (!meshes.Contains(obj.ObjectId))
                    meshes.Add(obj.ObjectId);

                ObjRef objref = new ObjRef(obj.ObjectId);

                var mesh = objref.Mesh();

                var line = mesh.TopologyEdges.EdgeLine(obj.GeometryComponentIndex.Index);

                var dir = line.Direction;

                constrain.Add(new g3.Line3d(new Vector3d(line.FromX, line.FromY, line.FromZ), new Vector3d(dir.X, dir.Y, dir.Z)));
                
            }

            foreach (var guid in meshes)
            {
                var objref = new ObjRef(guid);

                var mesh = GopherUtil.ConvertToD3Mesh(objref.Mesh());
                var res = GopherUtil.RemeshMesh(mesh, (float)minEdgeLength, (float)maxEdgeLength, (float)constriantAngle, (float)smoothSpeed, smoothSteps, constrain);
                var newMesh = GopherUtil.ConvertToRhinoMesh(res);

                doc.Objects.Replace(objref, newMesh);
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
