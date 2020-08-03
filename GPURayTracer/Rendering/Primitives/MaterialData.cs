using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct MaterialData
    {
        public int type;
        public Vec3 emmissiveColor;
        public Vec3 diffuseColor;
        public float ref_idx;
        public float reflectivity;
        public float reflectionConeAngleRadians;

        public MaterialData(Vec3 emmissiveColor, Vec3 diffuseColor, float ref_idx, float reflectivity, float reflectionConeAngleRadians, int type)
        {
            this.type = type;
            this.emmissiveColor = emmissiveColor;
            this.diffuseColor = diffuseColor;
            this.ref_idx = ref_idx;
            this.reflectivity = reflectivity;
            this.reflectionConeAngleRadians = reflectionConeAngleRadians;
        }

        public static MaterialData makeDiffuse(Vec3 diffuseColor)
        {
            return new MaterialData(new Vec3(), diffuseColor, 0, 0, 0, 0);
        }

        public static MaterialData makeGlass(Vec3 diffuseColor, float ref_idx)
        {
            return new MaterialData(new Vec3(), diffuseColor, ref_idx, 0, 0, 1);
        }

        public static MaterialData makeMirror(Vec3 diffuseColor, float fuzz)
        {
            return new MaterialData(new Vec3(), diffuseColor, 0, 0, (fuzz < 1 ? fuzz : 1), 2);
        }

        public static MaterialData makeMirror(Vec3 diffuseColor)
        {
            return new MaterialData(new Vec3(), diffuseColor, 0, 0, 0, 2);
        }

        public static MaterialData makeLight(Vec3 emmissiveColor)
        {
            return new MaterialData(emmissiveColor, new Vec3(), 0, 0, 0, 3);
        }
    }
}
