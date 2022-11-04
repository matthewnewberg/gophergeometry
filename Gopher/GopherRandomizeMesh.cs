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
