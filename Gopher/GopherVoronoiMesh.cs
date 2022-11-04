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
