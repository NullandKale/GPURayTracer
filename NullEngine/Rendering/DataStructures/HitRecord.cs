using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NullEngine.Rendering.DataStructures
{
    public struct HitRecord
    {
        public float t;
        public bool inside;
        public Vec3 p;
        public Vec3 normal;
        public int materialID;
        public int drawableID;

        public HitRecord(float t, Vec3 p, Vec3 normal, bool inside, int materialID, int drawableID)
        {
            this.t = t;
            this.inside = inside;
            this.p = p;
            this.normal = normal;
            this.materialID = materialID;
            this.drawableID = drawableID;
        }

        public HitRecord(float t, Vec3 p, Vec3 normal, Vec3 rayDirection, int materialID, int drawableID)
        {
            this.t = t;
            inside = Vec3.dot(normal, rayDirection) > 0;
            this.p = p;
            this.normal = normal;
            this.materialID = materialID;
            this.drawableID = drawableID;
        }
    }
}
