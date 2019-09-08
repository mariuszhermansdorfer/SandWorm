/*
 *     __                      .__              
 *   _/  |______ _______  _____|__| ___________ 
 *   \   __\__  \\_  __ \/  ___/  |/ __ \_  __ \
 *    |  |  / __ \|  | \/\___ \|  \  ___/|  | \/
 *    |__| (____  /__|  /____  >__|\___  >__|   
 *              \/           \/        \/       
 * 
 *    Copyright Cameron Newnham 2015-2016
 *
 *    This program is free software: you can redistribute it and/or modify
 *    it under the terms of the GNU General Public License as published by
 *    the Free Software Foundation, either version 3 of the License, or
 *    (at your option) any later version.
 *
 *    This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *    GNU General Public License for more details.
 *
 *    You should have received a copy of the GNU General Public License
 *    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Rhino;
using Rhino.DocObjects;
using Grasshopper.Kernel.Data;

namespace SandWorm
{
    public class GH_PointCloud : GH_GeometricGoo<PointCloud>, IGH_PreviewData, IGH_BakeAwareObject
    {
        public GH_PointCloud()
        {
            this.m_value = new PointCloud();
        }

        public GH_PointCloud(PointCloud cloud)
        {
            this.m_value = cloud;
        }

        public GH_PointCloud(GH_PointCloud other)
        {
            m_value = (PointCloud)other.m_value.Duplicate();
        }

        public override BoundingBox Boundingbox
        {
            get
            {
                return this.m_value.GetBoundingBox(true);
            }
        }

        public override string TypeDescription
        {
            get
            {
                return "A point cloud with optional colors and normals.";
            }
        }

        public override string TypeName
        {
            get
            {
                return "PointCloud";
            }
        }

        public BoundingBox ClippingBox
        {
            get
            {
                return this.m_value.GetBoundingBox(true);
            }
        }

        public bool IsBakeCapable
        {
            get
            {
                return this.m_value.Count > 0;
            }
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new GH_PointCloud((PointCloud)this.m_value.Duplicate());
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            return this.m_value.GetBoundingBox(xform);
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            var dup = this.m_value.Duplicate();
            xmorph.Morph(dup);
            return new GH_PointCloud((PointCloud)dup);
        }

        public override string ToString()
        {
            return String.Format("PointCloud: {0}", m_value.Count);
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            var dup = this.m_value.Duplicate();
            dup.Transform(xform);
            return new GH_PointCloud((PointCloud)dup);
        }

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, new ObjectAttributes(), obj_ids);
        }

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            obj_ids.Add(doc.Objects.AddPointCloud(m_value, att));
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            // No meshes to draw
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawPointCloud(this.m_value, args.Thickness);
        }

        public override bool CastFrom(object source)
        {
            if (source.GetType() == typeof(PointCloud))
            {
                this.m_value = (PointCloud)source;
                return true;
            }

            if (source.GetType() == typeof(GH_PointCloud))
            {
                this.m_value = ((GH_PointCloud)source).Value;
                return true;
            }

            var asOtherPC = (GH_GeometricGoo<PointCloud>)source;
            if (asOtherPC != null)
            {
                this.m_value = asOtherPC.Value;
                return true;
            }

            if (source.GetType() == typeof(IEnumerable<Point3d>))
            {
                this.m_value = new PointCloud((IEnumerable<Point3d>)source);
                return true;
            }

            return base.CastFrom(source);
        }

        public override bool CastTo<Q>(out Q target)
        {

            if (typeof(Q) == typeof(PointCloud))
            {
                target = (Q)(object)this.m_value;
                return true;
            }

            if (typeof(Q) == typeof(GH_PointCloud))
            {
                target = (Q)(object)this;
                return true;
            }

            if (typeof(Q) == typeof(IEnumerable<Point3d>))
            {
                target = (Q)(object)this.m_value.GetPoints();
                return true;
            }

            return base.CastTo<Q>(out target);
        }
    }
}
