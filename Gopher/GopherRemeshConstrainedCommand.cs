///////////////////////////////////////////////////////////////////////////////
// Gopher Geometry
// Copyright(C) 2022  Matthew Newberg

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
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
