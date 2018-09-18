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


namespace Gopher
{
    [System.Runtime.InteropServices.Guid("a2bbd311-b2fb-4aad-9dba-fa79304e7d22")]
    public class GopherRandomizeMesh : Command
    {
        static GopherRandomizeMesh _instance;
        public GopherRandomizeMesh()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GopherBruteForce command.</summary>
        public static GopherRandomizeMesh Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "GopherRandomizeMesh"; }
        }

        double amount = 0.01;
        int tries = 16;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            OptionDouble amountOption = new OptionDouble(amount, 0.001, 300);
            OptionInteger triesOption = new OptionInteger(tries, 1, 1000);

            GetObject go = new GetObject();
            go.SetCommandPrompt("Select meshes to Randomize");
            go.AddOptionDouble("Amount", ref amountOption);
            go.AddOptionInteger("Tries", ref triesOption);
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Mesh;

            for (;;)
            {
                GetResult res = go.GetMultiple(1, 0);

                if (res == GetResult.Option)
                {
                    tries = triesOption.CurrentValue;
                    amount = amountOption.CurrentValue;
                    continue;
                }

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

                g3.DMesh3 outputMeshRandom;
                GopherUtil.RandomizeMesh(mesh, out outputMeshRandom, amount, tries);

                var rhinoOutputMeshRandom = GopherUtil.ConvertToRhinoMesh(outputMeshRandom);
                doc.Objects.Replace(obj, rhinoOutputMeshRandom);
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
