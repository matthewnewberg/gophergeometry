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
using System.Collections.Generic;

namespace Gopher
{
    [System.Runtime.InteropServices.Guid("ea378a9e-577a-487a-941c-b77a80504819")]
    public class GopherProjectSelectedFaces : Command
    {
        static GopherProjectSelectedFaces _instance;
        public GopherProjectSelectedFaces()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GopherProjectSelectedFaces command.</summary>
        public static GopherProjectSelectedFaces Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "GopherProjectSelectedFaces"; }
        }

        double constriantAngle = 170.0f;
        double minEdgeLength = 0.5f;
        double maxEdgeLength = 2.0f;
        int smoothSteps = 20;
        double smoothSpeed = 0.5f;
        double projectedAmount = .99f;
        double projectedDistance = 20.0f;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            bool bHavePreselectedObjects = false;

            const ObjectType geometryFilter = ObjectType.MeshFace;

            OptionDouble minEdgeLengthOption = new OptionDouble(minEdgeLength, 0.001, 200);
            OptionDouble maxEdgeLengthOption = new OptionDouble(maxEdgeLength, 0.001, 200);
            OptionDouble constriantAngleOption = new OptionDouble(constriantAngle, 0.001, 360);
            OptionInteger smoothStepsOptions = new OptionInteger(smoothSteps, 0, 100);
            OptionDouble smoothSpeedOption = new OptionDouble(smoothSpeed, 0.01, 1.0);
            OptionDouble projectAmountOption = new OptionDouble(projectedAmount, 0.01, 1.0);
            OptionDouble projectedDistanceOption = new OptionDouble(projectedDistance, 0.01, 100000.0);

            GetObject go = new GetObject();
            go.SetCommandPrompt("Select mesh faces to project onto another mesh");
            go.GeometryFilter = geometryFilter;

            go.AddOptionDouble("ConstraintAngle", ref constriantAngleOption);
            go.AddOptionDouble("MinEdge", ref minEdgeLengthOption);
            go.AddOptionDouble("MaxEdge", ref maxEdgeLengthOption);
            go.AddOptionInteger("SmoothSteps", ref smoothStepsOptions);
            go.AddOptionDouble("SmoothSpeed", ref smoothSpeedOption);

            go.GroupSelect = true;
            go.SubObjectSelect = true;

            for (; ; )
            {
                GetResult faceres = go.GetMultiple(1, 0);

                if (faceres == GetResult.Option)
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

            minEdgeLength = minEdgeLengthOption.CurrentValue;
            maxEdgeLength = maxEdgeLengthOption.CurrentValue;
            constriantAngle = constriantAngleOption.CurrentValue;
            smoothSteps = smoothStepsOptions.CurrentValue;
            smoothSpeed = smoothSpeedOption.CurrentValue;

            //System.Collections.Generic.List<System.Guid> meshes = new System.Collections.Generic.List<System.Guid>();

            System.Guid rhinoMesh = System.Guid.Empty;

            System.Collections.Generic.List<int> removeFaces = new System.Collections.Generic.List<int>();

            g3.DMesh3 projectFaces = new g3.DMesh3(true, false, false, false);

            Rhino.Geometry.Mesh rhinoInputMesh = new Rhino.Geometry.Mesh();

            for (int i=0; i <  go.ObjectCount; i++)
            {
                ObjRef obj = go.Object(i);

                if (rhinoMesh == System.Guid.Empty)
                {
                    rhinoMesh = obj.ObjectId;
                    rhinoInputMesh = obj.Mesh();

                    for (int j = 0; j < rhinoInputMesh.Vertices.Count; j++)
                    {
                        var vertex = new g3.NewVertexInfo();
                        vertex.n = new g3.Vector3f(rhinoInputMesh.Normals[j].X, rhinoInputMesh.Normals[j].Y, rhinoInputMesh.Normals[j].Z);
                        vertex.v = new g3.Vector3d(rhinoInputMesh.Vertices[j].X, rhinoInputMesh.Vertices[j].Y, rhinoInputMesh.Vertices[j].Z);
                        projectFaces.AppendVertex(vertex);
                    }
                }

                var m = rhinoInputMesh;

                if (rhinoMesh != obj.ObjectId)
                    continue;

                removeFaces.Add(obj.GeometryComponentIndex.Index);

                var mf = rhinoInputMesh.Faces[obj.GeometryComponentIndex.Index];


                if (mf.IsQuad)
                {
                    double dist1 = m.Vertices[mf.A].DistanceTo(m.Vertices[mf.C]);
                    double dist2 = m.Vertices[mf.B].DistanceTo(m.Vertices[mf.D]);
                    if (dist1 > dist2)
                    {

                        projectFaces.AppendTriangle(mf.A, mf.B, mf.D);
                        projectFaces.AppendTriangle(mf.B, mf.C, mf.D);

                    }
                    else
                    {
                        projectFaces.AppendTriangle(mf.A, mf.B, mf.C);
                        projectFaces.AppendTriangle(mf.A, mf.C, mf.D);
                    }
                }
                else
                {

                    projectFaces.AppendTriangle(mf.A, mf.B, mf.C);
                }
            }

            if (rhinoInputMesh == null)
                return Result.Failure;

            removeFaces.Sort();
            removeFaces.Reverse();
            
            foreach (var removeFace in removeFaces)
                rhinoInputMesh.Faces.RemoveAt(removeFace);

            rhinoInputMesh.Compact();

            GetObject goProjected = new GetObject();
            goProjected.EnablePreSelect(false, true);
            goProjected.SetCommandPrompt("Select mesh to project to");
            goProjected.GeometryFilter = ObjectType.Mesh;
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


            for (; ; )
            {
                GetResult resProject = goProjected.Get();

                if (resProject == GetResult.Option)
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


            var rhinoMeshProject = goProjected.Object(0).Mesh();

            if (rhinoMeshProject == null || !rhinoMeshProject.IsValid)
                return Result.Failure;

            var meshProjected = GopherUtil.ConvertToD3Mesh(rhinoMeshProject);

            var res = GopherUtil.RemeshMesh(projectFaces, (float)minEdgeLength, (float)maxEdgeLength, (float)constriantAngle, (float)smoothSpeed, smoothSteps, meshProjected, (float)projectedAmount, (float)projectedDistance);

            var newRhinoMesh = GopherUtil.ConvertToRhinoMesh(res);

            if (newRhinoMesh != null && newRhinoMesh.IsValid)
            {
                newRhinoMesh.Append(rhinoInputMesh);

                result |= doc.Objects.Replace(rhinoMesh, newRhinoMesh);
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
