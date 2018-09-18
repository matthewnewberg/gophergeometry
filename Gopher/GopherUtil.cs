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

using System;
using System.Collections.Generic;
using g3;

namespace Gopher
{
    public class GopherUtil
    {
        public class GopherMeshProjectionTarget : IProjectionTarget
        {
            public DMesh3 Mesh { get; set; }
            public ISpatial Spatial { get; set; }
            public double amount = 1.0;
            public double maxDistance = float.MaxValue;

            public GopherMeshProjectionTarget() { }
            public GopherMeshProjectionTarget(DMesh3 mesh, ISpatial spatial)
            {
                Mesh = mesh;
                Spatial = spatial;
            }

            public Vector3d Project(Vector3d vPoint, int identifier = -1)
            {
                int tNearestID = Spatial.FindNearestTriangle(vPoint);
                DistPoint3Triangle3 q = MeshQueries.TriangleDistance(Mesh, tNearestID, vPoint);

                double curAmount = 1 - amount;

                if (maxDistance < float.MaxValue && maxDistance > 0)
                {
                    var distance = vPoint.Distance(q.TriangleClosest);

                    if (distance < maxDistance)
                    {
                        double distanceAmount = distance / maxDistance;
                        double projectDistanceAmount = 1 - distanceAmount;

                        return ((vPoint * curAmount) + (q.TriangleClosest * amount)) * projectDistanceAmount + (vPoint * distanceAmount);
                    }
                }

                return q.TriangleClosest;
            }
        }


        public static g3.DMesh3 ConvertToD3Mesh(Rhino.Geometry.Mesh mesh)
        {
            g3.DMesh3 ret = new g3.DMesh3(true, false, false, false);

            if (mesh.Normals.Count < mesh.Vertices.Count)
                mesh.Normals.ComputeNormals();

            if (mesh.Normals.Count != mesh.Vertices.Count)
                return ret;

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var vertex = new g3.NewVertexInfo();
                vertex.n = new g3.Vector3f(mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z);
                vertex.v = new g3.Vector3d(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z);
                ret.AppendVertex(vertex);
            }


            foreach (var mf in mesh.Faces)
            {

                if (mf.IsQuad)
                {
                    double dist1 = mesh.Vertices[mf.A].DistanceTo(mesh.Vertices[mf.C]);
                    double dist2 = mesh.Vertices[mf.B].DistanceTo(mesh.Vertices[mf.D]);
                    if (dist1 > dist2)
                    {

                        ret.AppendTriangle(mf.A, mf.B, mf.D);
                        ret.AppendTriangle(mf.B, mf.C, mf.D);

                    }
                    else
                    {
                        ret.AppendTriangle(mf.A, mf.B, mf.C);
                        ret.AppendTriangle(mf.A, mf.C, mf.D);
                    }
                }
                else
                {
                    ret.AppendTriangle(mf.A, mf.B, mf.C);
                }

            }

            return ret;
        }

        public static Rhino.Geometry.Line ConvertToRhinoLine(g3.Line3d line)
        {

            var end = line.PointAt(1.0);
            return new Rhino.Geometry.Line(line.Origin.x, line.Origin.y, line.Origin.z, end.x, end.y, end.z);
        }

        public static Rhino.Geometry.Polyline ConvertToRhinoPolyline(g3.PolyLine3d inputLine)
        {

            var pLine = new Rhino.Geometry.Polyline();

            foreach (var p in inputLine)
                pLine.Add(p.x, p.y, p.z);

            return pLine;
        }

        public static Rhino.Geometry.Mesh ConvertToRhinoMesh(List<g3.PolyLine3d> inputLines)
        {
            Rhino.Geometry.Mesh result = new Rhino.Geometry.Mesh();
            foreach (var l in inputLines)
            {
                var poly = ConvertToRhinoPolyline(l);
                var temp = Rhino.Geometry.Mesh.CreateFromClosedPolyline(poly);
                temp.Ngons.AddPlanarNgons(double.MaxValue / 10);
                result.Append(temp);
            }

            result.UnifyNormals();

            return result;
        }

        public static List<g3.PolyLine3d> ConvertMeshToPolylines(Rhino.Geometry.Mesh mesh)
        {
            var result = new List<g3.PolyLine3d>();

            if (mesh.Ngons.Count == 0)
                mesh.Ngons.AddPlanarNgons(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

            foreach (var n in mesh.Ngons)
            {
                var b = n.BoundaryVertexIndexList();

                var pL = new g3.PolyLine3d();

                foreach (var v in b)
                {
                    var temp = mesh.Vertices[(int)v];
                    pL.AppendVertex(new Vector3d(temp.X, temp.Y, temp.Z));
                }
            }

            return result;
        }

        public static Rhino.Geometry.Mesh ConvertToRhinoMesh(g3.DMesh3 largeMesh)
        {
            var mesh = new g3.DMesh3();

            mesh.CompactCopy(largeMesh);

            Rhino.Geometry.Mesh ret = new Rhino.Geometry.Mesh();

            foreach (var p in mesh.Vertices())
            {
                ret.Vertices.Add(new Rhino.Geometry.Point3d(p.x, p.y, p.z));
            }

            ret.Normals.Count = ret.Vertices.Count;

            for (int i = 0; i < ret.Vertices.Count; i++)
            {
                var n = mesh.GetVertexNormal(i);
                ret.Normals[i] = new Rhino.Geometry.Vector3f(n.x, n.y, n.z);
            }


            foreach (var f in mesh.Triangles())
            {
                if (f.a >= 0 && f.a < ret.Vertices.Count &&
                   f.b >= 0 && f.b < ret.Vertices.Count &&
                   f.c >= 0 && f.c < ret.Vertices.Count)
                {
                    ret.Faces.AddFace(new Rhino.Geometry.MeshFace(f.a, f.b, f.c));
                }
                else
                {
                    Rhino.RhinoApp.WriteLine("Error Triangle:" + f.a + "," + f.b + ",", +f.c);
                }

            }

            ret.Normals.ComputeNormals();

            return ret;
        }

        public static DMesh3 RemeshMesh(g3.DMesh3 mesh, float minEdgeLength, float maxEdgeLength, float contraintAngle, float smoothSpeed, int smoothPasses, g3.DMesh3 projectMeshInput = null, float projectAmount = 1.0f, float projectedDistance = float.MaxValue, List<Line3d> constrainedLines = null)
        {
            g3.DMesh3 projectMesh = projectMeshInput;

            if (projectMesh == null)
                projectMesh = mesh;

            DMesh3 projectMeshCopy = new DMesh3(projectMesh);
            DMeshAABBTree3 treeProject = new DMeshAABBTree3(projectMeshCopy);
            treeProject.Build();
            MeshProjectionTarget targetProject = new MeshProjectionTarget()
            {
                Mesh = projectMeshCopy,
                Spatial = treeProject
            };

            MeshConstraints cons = new MeshConstraints();
            EdgeRefineFlags useFlags = EdgeRefineFlags.NoFlip;
            foreach (int eid in mesh.EdgeIndices())
            {
                double fAngle = MeshUtil.OpeningAngleD(mesh, eid);
                if (fAngle > contraintAngle)
                {
                    cons.SetOrUpdateEdgeConstraint(eid, new EdgeConstraint(useFlags));
                    Index2i ev = mesh.GetEdgeV(eid);
                    //int nSetID0 = (mesh.GetVertex(ev[0]).y > 1) ? 1 : 2;
                    //int nSetID1 = (mesh.GetVertex(ev[1]).y > 1) ? 1 : 2;
                    cons.SetOrUpdateVertexConstraint(ev[0], new VertexConstraint(true));
                    cons.SetOrUpdateVertexConstraint(ev[1], new VertexConstraint(true));
                }

                if (constrainedLines != null)
                {
                    Index2i ev = mesh.GetEdgeV(eid);

                    Vector3d p1 = mesh.GetVertex(ev.a);
                    Vector3d p2 = mesh.GetVertex(ev.b);

                    foreach (var v in constrainedLines)
                    {
                        if (p1.CompareTo(v.Origin) == 0)
                        {
                            Vector3d p = v.PointAt(1.0);

                            if (p2.CompareTo(p) == 0)
                            {
                                cons.SetOrUpdateEdgeConstraint(eid, EdgeConstraint.FullyConstrained);
                                break;
                            }
                        }
                    }

                    foreach (var v in constrainedLines)
                    {
                        if (p2.CompareTo(v.Origin) == 0)
                        {
                            Vector3d p = v.PointAt(1.0);

                            if (p1.CompareTo(p) == 0)
                            {
                                cons.SetOrUpdateEdgeConstraint(eid, EdgeConstraint.FullyConstrained);
                                break;
                            }
                        }
                    }
                }
            }

            // TODO Constrain Vertices too far away
            foreach (int vid in mesh.VertexIndices())
            {
                var v = mesh.GetVertex(vid);

                //v.Distance()
                //targetProject.Project()
            }

            Remesher rProjected = new Remesher(mesh);
            rProjected.SetExternalConstraints(cons);
            rProjected.SetProjectionTarget(targetProject);
            rProjected.Precompute();
            rProjected.EnableFlips = rProjected.EnableSplits = rProjected.EnableCollapses = true;
            rProjected.MinEdgeLength = minEdgeLength; //0.1f;
            rProjected.MaxEdgeLength = maxEdgeLength; // 0.2f;
            rProjected.EnableSmoothing = true;
            rProjected.SmoothSpeedT = smoothSpeed; // .5;

            if (projectMeshInput != null)
            {
                float bestSmoothPassProjectAmount = projectAmount / smoothPasses;
                float testbestSmoothPassProjectAmount = float.MaxValue;
                for (float smoothPassProjectAmount = -.1f; smoothPassProjectAmount < 1.1f; smoothPassProjectAmount += 0.005f)
                {
                    double test = 0;

                    for (int i = 0; i < smoothPasses; i++)
                        test = 1.0 * smoothPassProjectAmount + test * (1 - smoothPassProjectAmount);

                    if (Math.Abs(test - projectAmount) < Math.Abs(testbestSmoothPassProjectAmount - projectAmount))
                    {
                        bestSmoothPassProjectAmount = (float)smoothPassProjectAmount;
                        testbestSmoothPassProjectAmount = (float)test;
                    }
                }

                for (int k = 0; k < smoothPasses; ++k) // smoothPasses = 20
                    rProjected.FullProjectionPass(bestSmoothPassProjectAmount, projectedDistance);
            }
            else
            {
                for (int k = 0; k < smoothPasses; ++k) // smoothPasses = 20
                    rProjected.BasicRemeshPass();
            }


            return mesh;
        }

        //

        public static DMesh3 RemeshMeshNew(g3.DMesh3 mesh, float minEdgeLength, float maxEdgeLength, float contraintAngle, float smoothSpeed, int smoothPasses, g3.DMesh3 projectMeshInput = null, float projectAmount = 1.0f, float projectedDistance = float.MaxValue)
        {
            g3.DMesh3 projectMesh = projectMeshInput;

            if (projectMesh == null)
                projectMesh = mesh;

            DMesh3 projectMeshCopy = new DMesh3(projectMesh);
            DMeshAABBTree3 treeProject = new DMeshAABBTree3(projectMeshCopy);
            treeProject.Build();
            GopherMeshProjectionTarget targetProject = new GopherMeshProjectionTarget()
            {
                Mesh = projectMeshCopy,
                Spatial = treeProject,
                amount = projectAmount,
                maxDistance = projectedDistance
            };

            MeshConstraints cons = new MeshConstraints();
            EdgeRefineFlags useFlags = EdgeRefineFlags.NoFlip;
            foreach (int eid in mesh.EdgeIndices())
            {
                double fAngle = MeshUtil.OpeningAngleD(mesh, eid);
                if (fAngle > contraintAngle)
                {
                    cons.SetOrUpdateEdgeConstraint(eid, new EdgeConstraint(useFlags));
                    Index2i ev = mesh.GetEdgeV(eid);
                    //int nSetID0 = (mesh.GetVertex(ev[0]).y > 1) ? 1 : 2;
                    //int nSetID1 = (mesh.GetVertex(ev[1]).y > 1) ? 1 : 2;
                    cons.SetOrUpdateVertexConstraint(ev[0], new VertexConstraint(true));
                    cons.SetOrUpdateVertexConstraint(ev[1], new VertexConstraint(true));
                }
            }

            // TODO Constrain Vertices too far away
            foreach (int vid in mesh.VertexIndices())
            {
                var v = mesh.GetVertex(vid);

                //v.Distance()
                //targetProject.Project()
            }

            Remesher rProjected = new Remesher(mesh);
            rProjected.SetExternalConstraints(cons);
            rProjected.SetProjectionTarget(targetProject);
            rProjected.Precompute();
            rProjected.EnableFlips = rProjected.EnableSplits = rProjected.EnableCollapses = true;
            rProjected.MinEdgeLength = minEdgeLength; //0.1f;
            rProjected.MaxEdgeLength = maxEdgeLength; // 0.2f;
            rProjected.EnableSmoothing = true;
            rProjected.SmoothSpeedT = smoothSpeed; // .5;

            if (projectMeshInput != null)
            {
                float bestSmoothPassProjectAmount = projectAmount / smoothPasses;
                float testbestSmoothPassProjectAmount = float.MaxValue;
                for (float smoothPassProjectAmount = -.1f; smoothPassProjectAmount < 1.1f; smoothPassProjectAmount += 0.005f)
                {
                    double test = 0;

                    for (int i = 0; i < smoothPasses; i++)
                        test = 1.0 * smoothPassProjectAmount + test * (1 - smoothPassProjectAmount);

                    if (Math.Abs(test - projectAmount) < Math.Abs(testbestSmoothPassProjectAmount - projectAmount))
                    {
                        bestSmoothPassProjectAmount = (float)smoothPassProjectAmount;
                        testbestSmoothPassProjectAmount = (float)test;
                    }
                }

                targetProject.amount = bestSmoothPassProjectAmount;
                targetProject.maxDistance = projectedDistance;

                for (int k = 0; k < smoothPasses; ++k) // smoothPasses = 20
                    rProjected.BasicRemeshPass();
            }
            else
            {
                for (int k = 0; k < smoothPasses; ++k) // smoothPasses = 20
                    rProjected.BasicRemeshPass();
            }


            return mesh;
        }

        public static DMesh3 RemeshMesh(g3.DMesh3 mesh, float minEdgeLength, float maxEdgeLength, float contraintAngle, float smoothSpeed, int smoothPasses, List<Line3d> constrainedLines)
        {
            // construct mesh projection target
            DMesh3 meshCopy = new DMesh3(mesh);
            DMeshAABBTree3 tree = new DMeshAABBTree3(meshCopy);
            tree.Build();
            MeshProjectionTarget target = new MeshProjectionTarget()
            {
                Mesh = meshCopy,
                Spatial = tree
            };

            MeshConstraints cons = new MeshConstraints();
            EdgeRefineFlags useFlags = EdgeRefineFlags.NoFlip;
            foreach (int eid in mesh.EdgeIndices())
            {
                double fAngle = MeshUtil.OpeningAngleD(mesh, eid);

                Index2i ev = mesh.GetEdgeV(eid);

                if (fAngle > contraintAngle)
                {
                    cons.SetOrUpdateEdgeConstraint(eid, new EdgeConstraint(useFlags));


                    // TODO Ids based off of ?? What?
                    int nSetID0 = (mesh.GetVertex(ev[0]).y > 1) ? 1 : 2;
                    int nSetID1 = (mesh.GetVertex(ev[1]).y > 1) ? 1 : 2;
                    cons.SetOrUpdateVertexConstraint(ev[0], new VertexConstraint(true, nSetID0));
                    cons.SetOrUpdateVertexConstraint(ev[1], new VertexConstraint(true, nSetID1));
                }

                Vector3d p1 = mesh.GetVertex(ev.a);
                Vector3d p2 = mesh.GetVertex(ev.b);


                foreach (var v in constrainedLines)
                {
                    if (p1.CompareTo(v.Origin) == 0)
                    {
                        Vector3d p = v.PointAt(1.0);

                        if (p2.CompareTo(p) == 0)
                        {
                            cons.SetOrUpdateEdgeConstraint(eid, EdgeConstraint.FullyConstrained);
                            break;
                        }
                    }
                }

                foreach (var v in constrainedLines)
                {
                    if (p2.CompareTo(v.Origin) == 0)
                    {
                        Vector3d p = v.PointAt(1.0);

                        if (p1.CompareTo(p) == 0)
                        {
                            cons.SetOrUpdateEdgeConstraint(eid, EdgeConstraint.FullyConstrained);
                            break;
                        }
                    }
                }
            }

            Remesher r = new Remesher(mesh);
            r.SetExternalConstraints(cons);
            r.SetProjectionTarget(target);
            r.Precompute();
            r.EnableFlips = r.EnableSplits = r.EnableCollapses = true;
            r.MinEdgeLength = minEdgeLength; //0.1f;
            r.MaxEdgeLength = maxEdgeLength; // 0.2f;
            r.EnableSmoothing = true;
            r.SmoothSpeedT = smoothSpeed; // .5;
            for (int k = 0; k < smoothPasses; ++k) // smoothPasses = 20
                r.BasicRemeshPass();
            return mesh;
        }

        public static bool ReduceMesh(DMesh3 mesh, float edgeLength, int triangleCount)
        {
            Reducer r = new Reducer(mesh);

            DMeshAABBTree3 tree = new DMeshAABBTree3(new DMesh3(mesh));
            tree.Build();


            /*
            MeshConstraints cons = new MeshConstraints();
            EdgeRefineFlags useFlags = EdgeRefineFlags.NoFlip;
            foreach (int eid in mesh.EdgeIndices())
            {
                double fAngle = MeshUtil.OpeningAngleD(mesh, eid);
                if (fAngle > contraintAngle)
                {
                    cons.SetOrUpdateEdgeConstraint(eid, new EdgeConstraint(useFlags));
                    Index2i ev = mesh.GetEdgeV(eid);
                    int nSetID0 = (mesh.GetVertex(ev[0]).y > 1) ? 1 : 2;
                    int nSetID1 = (mesh.GetVertex(ev[1]).y > 1) ? 1 : 2;
                    cons.SetOrUpdateVertexConstraint(ev[0], new VertexConstraint(true, nSetID0));
                    cons.SetOrUpdateVertexConstraint(ev[1], new VertexConstraint(true, nSetID1));
                }
            }
            */

            if (triangleCount > 0)
            {
                r.ReduceToTriangleCount(3000);
            }
            else
            {
                r.ReduceToEdgeLength(edgeLength);
            }


            return true;
        }

        private class MeshNode
        {

            public int meshIndex;
            public int index;
            public g3.Frame3f frame;
            public g3.Index3i neighbors_index;
            public g3.Index3i vertex_index;
            public List<MeshNode> neighbors = new List<MeshNode>();
            public bool locked = false;

            internal MeshNode(int i, int fi, g3.Frame3f f, g3.Index3i neighbors_index, g3.Index3i vertex_index)
            {
                frame = f;
                this.neighbors_index = neighbors_index;
                this.vertex_index = vertex_index;
                meshIndex = fi;
                index = i;
            }

            internal bool UsesVertex(int vi)
            {
                return vertex_index[0] == vi || vertex_index[1] == vi || vertex_index[2] == vi;

            }

            internal bool Randomize(g3.DMesh3 mesh, DMeshAABBTree3 tree, Random r, double max, double moveTries, double average)
            {
                bool result = false;

                for (int i = 0; i < moveTries; i++)
                {
                    result |= this.RandomAdjust(mesh, tree, r, max, moveTries, average);

                    //foreach (var n in neighbors)
                    //    result |= n.RandomAdjust(mesh, tree, r, max, moveTries, average);
                }

                return result;
            }

            internal double CompuateAverageArea(g3.DMesh3 mesh)
            {
                double area = this.TriangleArea(mesh);

                foreach (var n in neighbors)
                    area += n.TriangleArea(mesh);

                area /= (neighbors.Count + 1);

                return area;
            }

            internal double HowCloseToTargetArea(g3.DMesh3 mesh, double targetArea, int depth)
            {
                double area = Math.Pow(Math.Abs(this.TriangleArea(mesh) - targetArea) / targetArea, 3) * 3;

                if (depth == 0)
                    return area;

                foreach (var n in neighbors)
                    area += n.HowCloseToTargetArea(mesh, targetArea, depth - 1);

                return area / (neighbors.Count + 1);
            }

            internal double TriangleArea(g3.DMesh3 mesh)
            {
                return mesh.GetTriArea(meshIndex);
            }

            internal double GetTriangleAnglesQuality(g3.DMesh3 mesh)
            {
                Vector3d v0 = Vector3d.Zero, v1 = Vector3d.Zero, v2 = Vector3d.Zero;
                mesh.GetTriVertices(meshIndex, ref v0, ref v1, ref v2);

                Vector3d anglesD = Vector3d.Zero;

                Vector3d e00 = (v1 - v0);
                e00.Normalize();
                Vector3d e01 = (v2 - v0);
                e01.Normalize();
                anglesD.x = Vector3d.AngleD(e00, e01);

                Vector3d e10 = (v0 - v1);
                e10.Normalize();
                Vector3d e11 = (v2 - v1);
                e11.Normalize();
                anglesD.y = Vector3d.AngleD(e10, e11);

                anglesD.z = 180 - anglesD.x - anglesD.y;

                double resultA = Math.Min(Math.Min(Math.Abs(anglesD.x - 90) / 10.0, Math.Abs(anglesD.y - 90) / 10.0), Math.Abs(anglesD.z - 90) / 10.0);

                double resultB = Math.Abs(anglesD.x - 60) / 30.0 + Math.Abs(anglesD.y - 60) / 30.0 + Math.Abs(anglesD.z - 60) / 30.0;

                double result = Math.Min(resultA, resultB);

                return Math.Pow(result, 3);
            }

            internal double GetTriangleTotalAnglesQualityHelper(g3.DMesh3 mesh, int depth)
            {
                double total = GetTriangleAnglesQuality(mesh);

                if (depth == 0)
                    return total;

                foreach (var n in neighbors)
                    total += GetTriangleTotalAnglesQualityHelper(mesh, depth - 1);

                return total / (neighbors.Count + 1);
            }

            double GetNormalQuality(g3.DMesh3 mesh, g3.Vector3d target, int depth)
            {
                if (depth == 0)
                    return 1 - (mesh.GetTriNormal(meshIndex).Dot(target));

                double amount = GetNormalQuality(mesh, target, 0);

                foreach (var n in neighbors)
                    amount += GetNormalQuality(mesh, target, depth - 1);

                return amount;
            }

            internal bool RandomAdjust(g3.DMesh3 mesh, DMeshAABBTree3 tree, Random r, double max, double moveTries, double targetArea)
            {
                bool moved = false;

                if (this.locked)
                    return false;

                for (int i = 0; i < moveTries; i++)
                {
                    var v0 = mesh.GetVertex(vertex_index.a);
                    var v1 = mesh.GetVertex(vertex_index.b);
                    var v2 = mesh.GetVertex(vertex_index.c);

                    var v0_old = mesh.GetVertex(vertex_index.a);
                    var v1_old = mesh.GetVertex(vertex_index.b);
                    var v2_old = mesh.GetVertex(vertex_index.c);

                    v0.x += (r.NextDouble() * max * 2 - max);
                    v0.y += (r.NextDouble() * max * 2 - max);
                    v0.z += (r.NextDouble() * max * 2 - max);

                    v1.x += (r.NextDouble() * max * 2 - max);
                    v1.y += (r.NextDouble() * max * 2 - max);
                    v1.z += (r.NextDouble() * max * 2 - max);

                    v2.x += (r.NextDouble() * max * 2 - max);
                    v2.y += (r.NextDouble() * max * 2 - max);
                    v2.z += (r.NextDouble() * max * 2 - max);

                    int tNearestID = tree.FindNearestTriangle(v0);
                    DistPoint3Triangle3 q = MeshQueries.TriangleDistance(tree.Mesh, tNearestID, v0);
                    v0 = q.TriangleClosest;

                    tNearestID = tree.FindNearestTriangle(v1);
                    q = MeshQueries.TriangleDistance(tree.Mesh, tNearestID, v1);
                    v1 = q.TriangleClosest;

                    tNearestID = tree.FindNearestTriangle(v2);
                    q = MeshQueries.TriangleDistance(tree.Mesh, tNearestID, v2);
                    v2 = q.TriangleClosest;

                    double oldArea = (HowCloseToTargetArea(mesh, targetArea, 2) / targetArea) * 3;

                    double oldAngleQuality = GetTriangleTotalAnglesQualityHelper(mesh, 2);

                    var n = mesh.GetTriNormal(meshIndex);

                    double oldNormalQuality = GetNormalQuality(mesh, n, 2) * 6;

                    mesh.SetVertex(vertex_index.a, v0);
                    mesh.SetVertex(vertex_index.b, v1);
                    mesh.SetVertex(vertex_index.c, v2);

                    double newArea = (HowCloseToTargetArea(mesh, targetArea, 2) / targetArea) * 3;
                    double newAngleQuality = GetTriangleTotalAnglesQualityHelper(mesh, 2);
                    double newNormalQuality = GetNormalQuality(mesh, n, 2) * 6;

                    if ((oldArea + oldAngleQuality + oldNormalQuality) < (newArea + newAngleQuality + newNormalQuality))
                    {
                        mesh.SetVertex(vertex_index.a, v0_old);
                        mesh.SetVertex(vertex_index.b, v1_old);
                        mesh.SetVertex(vertex_index.c, v2_old);
                    }
                    else
                    {
                        moved = true;

                    }
                }

                return moved;
            }
        }

        public static bool RandomizeMesh(g3.DMesh3 mesh, out g3.DMesh3 outputMesh, double amount, double moveTries)
        {
            System.Collections.Generic.SortedDictionary<int, MeshNode> faces = new System.Collections.Generic.SortedDictionary<int, MeshNode>();

            int index = 0;
            foreach (var meshFaceIndex in mesh.TriangleIndices())
            {
                var frame = mesh.GetTriFrame(meshFaceIndex);

                g3.Index3i neighbors = mesh.GetTriNeighbourTris(meshFaceIndex);
                g3.Index3i vertex_index = mesh.GetTriangle(meshFaceIndex);

                faces.Add(meshFaceIndex, new MeshNode(index++, meshFaceIndex, frame, neighbors, vertex_index));
            }

            foreach (var f in faces)
            {
                f.Value.neighbors.Clear();
                f.Value.neighbors.Capacity = 3;
                for (int i = 0; i < 3; ++i)
                {
                    int fn = f.Value.neighbors_index[i];
                    if (fn >= 0)
                        f.Value.neighbors.Add(faces[fn]);
                }

                if (f.Value.neighbors.Count < 3)
                {
                    f.Value.locked = true;

                    foreach (var n in f.Value.neighbors)
                        n.locked = true;
                }
            }

            DMesh3 projectMeshCopy = new DMesh3(mesh);
            outputMesh = new DMesh3(mesh);

            if (faces.Count == 0)
                return false;


            DMeshAABBTree3 treeProject = new DMeshAABBTree3(projectMeshCopy);
            treeProject.Build();

            Random r = new Random();

            bool result = false;


            //for (int i = 0; i < moveTries; i++)
            //{
            double faceArea = 0;

            foreach (var f in faces)
            {
                faceArea += f.Value.TriangleArea(outputMesh);
            }

            faceArea /= faces.Count;

            foreach (var f in faces)
            {
                result |= f.Value.Randomize(outputMesh, treeProject, r, amount, moveTries, faceArea);
            }

            double newFaceArea = 0;

            foreach (var f in faces)
            {
                newFaceArea += f.Value.TriangleArea(outputMesh);
            }

            newFaceArea /= faces.Count;

            return result;
        }

        //public static void VoronoiMesh(List<g3.PolyLine3d> mesh, out List<g3.Line3d> listLines, out List<g3.PolyLine3d> listPolylines)
        //{
        //    System.Collections.Generic.SortedDictionary<int, MeshNode> faces = new System.Collections.Generic.SortedDictionary<int, MeshNode>();

        //    int index = 0;
        //    foreach (var meshFaceIndex in mesh.TriangleIndices())
        //    {
        //        var frame = mesh.GetTriFrame(meshFaceIndex);

        //        g3.Index3i neighbors = mesh.GetTriNeighbourTris(meshFaceIndex);
        //        g3.Index3i vertex_index = mesh.GetTriangle(meshFaceIndex);

        //        faces.Add(meshFaceIndex, new MeshNode(index++, meshFaceIndex, frame, neighbors, vertex_index));
        //    }


        //    foreach (var f in faces)
        //    {
        //        f.Value.neighbors.Clear();
        //        f.Value.neighbors.Capacity = 3;
        //        for (int i = 0; i < 3; ++i)
        //        {
        //            int fn = f.Value.neighbors_index[i];
        //            if (fn >= 0)
        //                f.Value.neighbors.Add(faces[fn]);
        //        }

        //        if (f.Value.neighbors.Count < 3)
        //        {
        //            f.Value.locked = true;

        //            foreach (var n in f.Value.neighbors)
        //                n.locked = true;
        //        }
        //    }

        //    outputMesh = new g3.DMesh3(g3.MeshComponents.None);
        //    listLines = new List<g3.Line3d>();
        //    listPolylines = new List<g3.PolyLine3d>();
        //    foreach (var f in faces)
        //    {
        //        outputMesh.AppendVertex(f.Value.frame.Origin);
        //    }

        //    HashSet<int> processedPoints = new HashSet<int>();

        //    foreach (var f in faces)
        //    {
        //        for (int i = 0; i < 3; i++)
        //        {
        //            List<int> outputLine = new List<int>();

        //            if (processedPoints.Contains(f.Value.vertex_index[i]))
        //                continue;

        //            int checkVertex = f.Value.vertex_index[i];

        //            MeshNode currentFaces = f.Value;
        //            MeshNode prevFace = null;

        //            bool fullLoop = false;

        //            while (true)
        //            {
        //                for (int j = 0; j < currentFaces.neighbors.Count; j++)
        //                {

        //                    var neighbor = currentFaces.neighbors[j];
        //                    if (neighbor.UsesVertex(checkVertex))
        //                    {

        //                        if (neighbor == prevFace)
        //                            continue;

        //                        if (neighbor == f.Value)
        //                        {
        //                            fullLoop = true;
        //                            break; // Found full loop
        //                        }

        //                        outputLine.Add(neighbor.index);

        //                        prevFace = currentFaces;
        //                        currentFaces = neighbor;
        //                        j = -1;
        //                    }
        //                }

        //                break;
        //            }

        //            if (fullLoop)
        //            {
        //                processedPoints.Add(checkVertex);

        //                var polyline = new g3.PolyLine3d();

        //                if (outputLine.Count > 2)
        //                {
        //                    g3.Vector3d centerPoint = f.Value.frame.Origin;

        //                    foreach (var p in outputLine)
        //                        centerPoint += outputMesh.GetVertex(p);

        //                    centerPoint /= (outputLine.Count + 1);

        //                    int center = outputMesh.AppendVertex(centerPoint);

        //                    var pS = outputMesh.GetVertex(f.Value.index);
        //                    var p0 = outputMesh.GetVertex(outputLine[0]);
        //                    var pE = outputMesh.GetVertex(outputLine[outputLine.Count - 1]);

        //                    var normal = mesh.GetTriNormal(f.Value.meshIndex);

        //                    polyline.AppendVertex(pS);
        //                    polyline.AppendVertex(p0);

        //                    listLines.Add(new g3.Line3d(pS, p0 - pS));

        //                    var n = MathUtil.Normal(centerPoint, pS, p0);

        //                    bool reverseTri = n.Dot(normal) < 0;

        //                    if (!reverseTri)
        //                        outputMesh.AppendTriangle(center, f.Value.index, outputLine[0]);
        //                    else
        //                        outputMesh.AppendTriangle(center, outputLine[0], f.Value.index);

        //                    for (int j = 0; j < outputLine.Count - 1; j++)
        //                    {
        //                        var p1 = outputMesh.GetVertex(outputLine[j]);
        //                        var p2 = outputMesh.GetVertex(outputLine[j + 1]);

        //                        listLines.Add(new g3.Line3d(p1, p2 - p1));
        //                        polyline.AppendVertex(p2);

        //                        if (!reverseTri)
        //                            outputMesh.AppendTriangle(center, outputLine[j], outputLine[j + 1]);
        //                        else
        //                            outputMesh.AppendTriangle(center, outputLine[j + 1], outputLine[j]);
        //                    }

        //                    polyline.AppendVertex(pS);
        //                    listLines.Add(new g3.Line3d(pE, pS - pE));

        //                    listPolylines.Add(polyline);

        //                    if (!reverseTri)
        //                        outputMesh.AppendTriangle(center, outputLine[outputLine.Count - 1], f.Value.index);
        //                    else
        //                        outputMesh.AppendTriangle(center, f.Value.index, outputLine[outputLine.Count - 1]);
        //                }
        //            }
        //        }

        //    }
        //}

        public static void VoronoiMesh(g3.DMesh3 mesh, out g3.DMesh3 outputMesh, out List<g3.Line3d> listLines, out List<g3.PolyLine3d> listPolylines)
        {
            System.Collections.Generic.SortedDictionary<int, MeshNode> faces = new System.Collections.Generic.SortedDictionary<int, MeshNode>();

            int index = 0;
            foreach (var meshFaceIndex in mesh.TriangleIndices())
            {
                var frame = mesh.GetTriFrame(meshFaceIndex);

                g3.Index3i neighbors = mesh.GetTriNeighbourTris(meshFaceIndex);
                g3.Index3i vertex_index = mesh.GetTriangle(meshFaceIndex);

                faces.Add(meshFaceIndex, new MeshNode(index++, meshFaceIndex, frame, neighbors, vertex_index));
            }


            foreach (var f in faces)
            {
                f.Value.neighbors.Clear();
                f.Value.neighbors.Capacity = 3;
                for (int i = 0; i < 3; ++i)
                {
                    int fn = f.Value.neighbors_index[i];
                    if (fn >= 0)
                        f.Value.neighbors.Add(faces[fn]);
                }

                if (f.Value.neighbors.Count < 3)
                {
                    f.Value.locked = true;

                    foreach (var n in f.Value.neighbors)
                        n.locked = true;
                }
            }

            outputMesh = new g3.DMesh3(g3.MeshComponents.None);
            listLines = new List<g3.Line3d>();
            listPolylines = new List<g3.PolyLine3d>();
            foreach (var f in faces)
            {
                outputMesh.AppendVertex(f.Value.frame.Origin);
            }

            HashSet<int> processedPoints = new HashSet<int>();

            foreach (var f in faces)
            {
                for (int i = 0; i < 3; i++)
                {
                    List<int> outputLine = new List<int>();

                    if (processedPoints.Contains(f.Value.vertex_index[i]))
                        continue;

                    int checkVertex = f.Value.vertex_index[i];

                    MeshNode currentFaces = f.Value;
                    MeshNode prevFace = null;

                    bool fullLoop = false;

                    while (true)
                    {
                        for (int j = 0; j < currentFaces.neighbors.Count; j++)
                        {

                            var neighbor = currentFaces.neighbors[j];
                            if (neighbor.UsesVertex(checkVertex))
                            {

                                if (neighbor == prevFace)
                                    continue;

                                if (neighbor == f.Value)
                                {
                                    fullLoop = true;
                                    break; // Found full loop
                                }

                                outputLine.Add(neighbor.index);

                                prevFace = currentFaces;
                                currentFaces = neighbor;
                                j = -1;
                            }
                        }

                        break;
                    }

                    if (fullLoop)
                    {
                        processedPoints.Add(checkVertex);

                        var polyline = new g3.PolyLine3d();

                        if (outputLine.Count > 2)
                        {
                            g3.Vector3d centerPoint = f.Value.frame.Origin;

                            foreach (var p in outputLine)
                                centerPoint += outputMesh.GetVertex(p);

                            centerPoint /= (outputLine.Count + 1);

                            int center = outputMesh.AppendVertex(centerPoint);

                            var pS = outputMesh.GetVertex(f.Value.index);
                            var p0 = outputMesh.GetVertex(outputLine[0]);
                            var pE = outputMesh.GetVertex(outputLine[outputLine.Count - 1]);

                            var normal = mesh.GetTriNormal(f.Value.meshIndex);

                            polyline.AppendVertex(pS);
                            polyline.AppendVertex(p0);

                            listLines.Add(new g3.Line3d(pS, p0 - pS));

                            var n = MathUtil.Normal(centerPoint, pS, p0);

                            bool reverseTri = n.Dot(normal) < 0;

                            if (!reverseTri)
                                outputMesh.AppendTriangle(center, f.Value.index, outputLine[0]);
                            else
                                outputMesh.AppendTriangle(center, outputLine[0], f.Value.index);

                            for (int j = 0; j < outputLine.Count - 1; j++)
                            {
                                var p1 = outputMesh.GetVertex(outputLine[j]);
                                var p2 = outputMesh.GetVertex(outputLine[j + 1]);

                                listLines.Add(new g3.Line3d(p1, p2 - p1));
                                polyline.AppendVertex(p2);

                                if (!reverseTri)
                                    outputMesh.AppendTriangle(center, outputLine[j], outputLine[j + 1]);
                                else
                                    outputMesh.AppendTriangle(center, outputLine[j + 1], outputLine[j]);
                            }

                            polyline.AppendVertex(pS);
                            listLines.Add(new g3.Line3d(pE, pS - pE));

                            listPolylines.Add(polyline);

                            if (!reverseTri)
                                outputMesh.AppendTriangle(center, outputLine[outputLine.Count - 1], f.Value.index);
                            else
                                outputMesh.AppendTriangle(center, f.Value.index, outputLine[outputLine.Count - 1]);
                        }
                    }
                }

            }
        }
    }
}
