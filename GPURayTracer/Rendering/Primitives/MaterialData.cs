using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct MaterialData
    {
        public Vec3 emmissiveColor;
        public Vec3 diffuseColor;
        public float ref_idx;
        public float reflectivity;
        public float reflectionConeAngleRadians;

        public MaterialData(Vec3 emmissiveColor, Vec3 diffuseColor, float ref_idx, float reflectivity, float reflectionConeAngleRadians)
        {
            this.emmissiveColor = emmissiveColor;
            this.diffuseColor = diffuseColor;
            this.ref_idx = ref_idx;
            this.reflectivity = reflectivity;
            this.reflectionConeAngleRadians = reflectionConeAngleRadians;
        }

        public static MaterialData makeDiffuse(Vec3 diffuseColor)
        {
            return new MaterialData(new Vec3(), diffuseColor, 0, 0, 0);
        }

        public static MaterialData makeLight(Vec3 emmissiveColor)
        {
            return new MaterialData(emmissiveColor, new Vec3(), 0, 0, 0);
        }
    }
}
