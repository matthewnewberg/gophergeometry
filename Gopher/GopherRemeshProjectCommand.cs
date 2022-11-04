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
    [System.Runtime.InteropServices.Guid("21a67105-60b9-48a4-b61d-fa23c489223c")]
    public class GopherRemeshProjectCommand : Command
    {
        static GopherRemeshProjectCommand _instance;
        public GopherRemeshProjectCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GopherRemeshProjectCommand command.</summary>
        public static GopherRemeshProjectCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "GopherRemeshProject"; }
        }

        double constriantAngle = 30.0f;
        double minEdgeLength = 1.0f;
        double maxEdgeLength = 2.0f;
        int smoothSteps = 20;
        double smoothSpeed = 0.5f;
        double projectedAmount = .9f;
        double projectedDistance = 20.0f;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            const ObjectType geometryFilter = ObjectType.Mesh;

            OptionDouble minEdgeLengthOption = new OptionDouble(minEdgeLength, 0.001, 200);
            OptionDouble maxEdgeLengthOption = new OptionDouble(maxEdgeLength, 0.001, 200);
            OptionDouble constriantAngleOption = new OptionDouble(constriantAngle, 0.001, 360);
            OptionInteger smoothStepsOptions = new OptionInteger(smoothSteps, 0, 1000);
            OptionDouble smoothSpeedOption = new OptionDouble(smoothSpeed, 0.01, 1.0);
            OptionDouble projectAmountOption = new OptionDouble(projectedAmount, 0.01, 1.0);
            OptionDouble projectedDistanceOption = new OptionDouble(projectedDistance, 0.01, 100000.0);

            GetObject go = new GetObject();
            go.SetCommandPrompt("Select mesh to remesh");
            go.GeometryFilter = geometryFilter;
            go.AddOptionDouble("ConstraintAngle", ref constriantAngleOption);
            go.AddOptionDouble("MinEdge", ref minEdgeLengthOption);
            go.AddOptionDouble("MaxEdge", ref maxEdgeLengthOption);
            go.AddOptionInteger("SmoothSteps", ref smoothStepsOptions);
            go.AddOptionDouble("SmoothSpeed", ref smoothSpeedOption);
            go.AddOptionDouble("ProjectAmount", ref projectAmountOption);
            go.AddOptionDouble("ProjectDistance", ref projectedDistanceOption);

            go.GroupSelect = true;
            go.SubObjectSelect = false;
            go.EnableClearObjectsOnEntry(false);
            go.EnableUnselectObjectsOnExit(false);
            go.DeselectAllBeforePostSelect = false;

            bool bHavePreselectedObjects = false;

            for (;;)
            {
                GetResult res = go.Get();

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

            GetObject goProjected = new GetObject();
            goProjected.EnablePreSelect(false, true);
            goProjected.SetCommandPrompt("Select mesh to project to");
            goProjected.GeometryFilter = geometryFilter;
            goProjected.AddOptionDouble("ConstraintAngle", ref constriantAngleOption);
            goProjected.AddOptionDouble("MinEdge", ref minEdgeLengthOption);
            goProjected.AddOptionDouble("MaxEdge", ref maxEdgeLengthOption);
            goProjected.AddOptionInteger("SmoothSteps", ref smoothStepsOptions);
            goProjected.AddOptionDouble("SmoothSpeed", ref smoothSpeedOption);
            goProjected.AddOptionDouble("ProjectAmount", ref projectAmountOption);
            goProjected.AddOptionDouble("ProjectDistance", ref projectedDistanceOption);

            goProjected.GroupSelect = true;
            goProjected.SubObjectSelect = false;
            goProjected.EnableClearObjectsOnEntry(false);
            goProjected.EnableUnselectObjectsOnExit(false);


            for (;;)
            {
                GetResult res = goProjected.Get();

                if (res == GetResult.Option)
                    continue;

                else if (goProjected.CommandResult() != Result.Success)
                    return goProjected.CommandResult();

                break;
            }


            minEdgeLength = minEdgeLengthOption.CurrentValue;
            maxEdgeLength = maxEdgeLengthOption.CurrentValue;
            constriantAngle = constriantAngleOption.CurrentValue;
            smoothSteps = smoothStepsOptions.CurrentValue;
            smoothSpeed = smoothSpeedOption.CurrentValue;
            projectedAmount = projectAmountOption.CurrentValue;
            projectedDistance = projectedDistanceOption.CurrentValue;

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

            if (goProjected.ObjectCount < 1)
                return Result.Failure;

            foreach (var obj in go.Objects())
            {
                var rhinoMesh = obj.Mesh();

                if (rhinoMesh == null || !rhinoMesh.IsValid)
                    continue;

                var mesh = GopherUtil.ConvertToD3Mesh(obj.Mesh());


                var rhinoMeshProject = goProjected.Object(0).Mesh();

                if (rhinoMeshProject == null || !rhinoMeshProject.IsValid)
                    continue;

                var meshProjected = GopherUtil.ConvertToD3Mesh(rhinoMeshProject);

                var mesh2 = new DMesh3(mesh);
                var mesh3 = new DMesh3(mesh);

                var res3 = GopherUtil.RemeshMesh(mesh3, (float)minEdgeLength, (float)maxEdgeLength, (float)constriantAngle, (float)smoothSpeed, smoothSteps, meshProjected, (float)projectedAmount, (float)projectedDistance);

                var watch = System.Diagnostics.Stopwatch.StartNew();
                // the code that you want to measure comes here

                var res = GopherUtil.RemeshMesh(mesh, (float)minEdgeLength, (float)maxEdgeLength, (float)constriantAngle, (float)smoothSpeed, smoothSteps, meshProjected, (float)projectedAmount, (float)projectedDistance);

                watch.Stop();

                var watch2 = System.Diagnostics.Stopwatch.StartNew();

                var res2 = GopherUtil.RemeshMeshNew(mesh2, (float)minEdgeLength, (float)maxEdgeLength, (float)constriantAngle, (float)smoothSpeed, smoothSteps, meshProjected, (float)projectedAmount, (float)projectedDistance);

                watch2.Stop();


                Rhino.RhinoApp.WriteLine("Time 1: {0} Time 2: {1}", watch.ElapsedMilliseconds, watch2.ElapsedMilliseconds);

                var newRhinoMesh = GopherUtil.ConvertToRhinoMesh(res);

                var newRhinoMesh2 = GopherUtil.ConvertToRhinoMesh(mesh2);

                newRhinoMesh2.Translate(new Rhino.Geometry.Vector3d(newRhinoMesh2.GetBoundingBox(true).Diagonal.X, 0, 0));

                doc.Objects.Add(newRhinoMesh2);

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
