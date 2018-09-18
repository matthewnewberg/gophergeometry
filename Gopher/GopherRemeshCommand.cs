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

namespace Gopher
{
    [System.Runtime.InteropServices.Guid("b42c3bc0-4010-4183-af60-e62ce0cbb1a4")]
    public class GopherRemeshCommand : Command
    {
        static GopherRemeshCommand _instance;
        public GopherRemeshCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GopherRemeshCommand command.</summary>
        public static GopherRemeshCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "GopherRemesh"; }
        }


        double constriantAngle = 30.0f;
        double minEdgeLength = 1.0f;
        double maxEdgeLength = 2.0f;
        int smoothSteps = 20;
        double smoothSpeed = 0.5f;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            const ObjectType geometryFilter = ObjectType.Mesh;

            OptionDouble minEdgeLengthOption = new OptionDouble(minEdgeLength, 0.001, 200);
            OptionDouble maxEdgeLengthOption = new OptionDouble(maxEdgeLength, 0.001, 200);
            OptionDouble constriantAngleOption = new OptionDouble(constriantAngle, 0.001, 360);
            OptionInteger smoothStepsOptions = new OptionInteger(smoothSteps, 0, 10000);
            OptionDouble smoothSpeedOption = new OptionDouble(smoothSpeed, 0.01, 1.0);
            
            GetObject go = new GetObject();
            go.SetCommandPrompt("Select meshes to remesh");
            go.GeometryFilter = geometryFilter;
            go.AddOptionDouble("ConstraintAngle", ref constriantAngleOption);
            go.AddOptionDouble("MinEdge", ref minEdgeLengthOption);
            go.AddOptionDouble("MaxEdge", ref maxEdgeLengthOption);
            go.AddOptionInteger("SmoothSteps", ref smoothStepsOptions);
            go.AddOptionDouble("SmoothSpeed", ref smoothSpeedOption);

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

            minEdgeLength = minEdgeLengthOption.CurrentValue;
            maxEdgeLength = maxEdgeLengthOption.CurrentValue;
            constriantAngle = constriantAngleOption.CurrentValue;
            smoothSteps = smoothStepsOptions.CurrentValue;
            smoothSpeed = smoothSpeedOption.CurrentValue;
            
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

                var res = GopherUtil.RemeshMesh(mesh, (float)minEdgeLength, (float)maxEdgeLength, (float)constriantAngle, (float)smoothSpeed, smoothSteps);

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
