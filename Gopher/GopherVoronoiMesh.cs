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
using System.Collections.Generic;

namespace Gopher
{
    [System.Runtime.InteropServices.Guid("b2a6d633-1688-4bd6-b821-f946f2a4a9dc")]
    public class GopherVoronoiMesh : Command
    {
        static GopherVoronoiMesh _instance;
        public GopherVoronoiMesh()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GopherVoronoiMesh command.</summary>
        public static GopherVoronoiMesh Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "GopherVoronoiMesh"; }
        }

        static OptionToggle outputMeshToggle = new OptionToggle(true, "Off", "On");
        static OptionToggle outputLinesToggle = new OptionToggle(true, "Off", "On");
        static OptionToggle outputPolylinesToggle = new OptionToggle(true, "Off", "On");
        static OptionToggle outputNGon = new OptionToggle(true, "Off", "On");

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            GetObject go = new GetObject();
            go.SetCommandPrompt("Select meshes to Voronoi");
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Mesh;


            go.AddOptionToggle("OutputMesh", ref outputMeshToggle);
            go.AddOptionToggle("OutputLines", ref outputLinesToggle);
            go.AddOptionToggle("OutputPolylines", ref outputPolylinesToggle);
            go.AddOptionToggle("OutputNGon", ref outputNGon);

            for (;;)
            {
                GetResult res = go.GetMultiple(1, 0);

                if (res == GetResult.Option)
                    continue;

                if (go.CommandResult() != Result.Success)
                    return go.CommandResult();

                break;
            }

            if (go.ObjectCount < 1)
                return Result.Failure;

            foreach (var obj in go.Objects())
            {
                var rhinoMesh = obj.Mesh();

                if (rhinoMesh == null || !rhinoMesh.IsValid)
                    continue;

                var mesh = GopherUtil.ConvertToD3Mesh(obj.Mesh());

                g3.DMesh3 outputMesh;
                List<g3.Line3d> listLines;
                List<g3.PolyLine3d> listPolylines;
                GopherUtil.VoronoiMesh(mesh, out outputMesh, out listLines, out listPolylines);

                var rhinoOutputMesh = GopherUtil.ConvertToRhinoMesh(outputMesh);

                if (outputPolylinesToggle.CurrentValue)
                {
                    foreach (var p in listPolylines)
                    {
                        var rp = Gopher.GopherUtil.ConvertToRhinoPolyline(p);
                        doc.Objects.AddPolyline(rp);
                    }
                }

                if (outputLinesToggle.CurrentValue)
                {
                    foreach (var l in listLines)
                    {
                        var rl = GopherUtil.ConvertToRhinoLine(l);
                        doc.Objects.AddLine(rl);
                    }
                }

                if (outputMeshToggle.CurrentValue)
                {
                    if (outputNGon.CurrentValue)
                    {
                        //   rhinoOutputMesh.Ngons.AddPlanarNgons(doc.ModelAbsoluteTolerance);
                        doc.Objects.AddMesh(Gopher.GopherUtil.ConvertToRhinoMesh(listPolylines));
                    }
                    else
                    {
                        doc.Objects.AddMesh(rhinoOutputMesh);
                    }
                }
            }

            return Result.Success;
        }


    }
}
