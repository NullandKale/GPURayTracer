using System;
using System.Collections.Generic;
using System.Text;

namespace GPURayTracer.Rendering.Primitives
{
    public struct MaterialData
    {
        public enum MaterialPrefab
        {
            ivory,
            glass,
            rubber,
            mirror,
        }

        public static MaterialData ivory = new MaterialData(new Vec3(0.4f, 0.4f, 0.3f), 0.6f, 0.3f, 0.1f, 0.0f, 50, 1.0f);
        public static MaterialData glass = new MaterialData(new Vec3(0.6f, 0.7f, 0.8f), 0.0f, 0.5f, 0.1f, 0.8f, 125, 1.5f);
        public static MaterialData blackRubber = new MaterialData(new Vec3(0, 0, 0), 0.9f, 0.1f, 0.0f, 0.0f, 10, 1.0f);
        public static MaterialData redRubber = new MaterialData(new Vec3(1, 0, 0), 0.9f, 0.1f, 0.0f, 0.0f, 10, 1.0f);
        public static MaterialData greenRubber = new MaterialData(new Vec3(0, 1, 0), 0.9f, 0.1f, 0.0f, 0.0f, 10, 1.0f);
        public static MaterialData blueRubber = new MaterialData(new Vec3(0, 0, 1), 0.9f, 0.1f, 0.0f, 0.0f, 10, 1.0f);
        public static MaterialData whiteRubber = new MaterialData(new Vec3(1, 1, 1), 0.9f, 0.1f, 0.0f, 0.0f, 10, 1.0f);
        public static MaterialData mirror = new MaterialData(new Vec3(1.0f, 1.0f, 1.0f), 0.0f, 0.5f, 0.8f, 0.0f, 1450, 1.0f);

        public Vec3 diffuseColor;
        public float ref_idx;
        public float specularExponent;
        public float a0, a1, a2, a3;

        public MaterialData(Vec3 diffuseColor, float a0, float a1, float a2, float a3, float ref_idx, float specularExponent)
        {
            this.diffuseColor = diffuseColor;
            this.ref_idx = ref_idx;
            this.specularExponent = specularExponent;
            this.a0 = a0;
            this.a1 = a1;
            this.a2 = a2;
            this.a3 = a3;
        }

        public MaterialData(MaterialPrefab prefab, Vec3 diffuseColor)
        {
            this.diffuseColor = diffuseColor;

            switch (prefab)
            {
                case MaterialPrefab.ivory:
                    this.ref_idx = 10;
                    this.specularExponent = 1.0f;
                    this.a0 = 0.6f;
                    this.a1 = 0.3f;
                    this.a2 = 0.1f;
                    this.a3 = 0.0f;
                    break;
                case MaterialPrefab.glass:
                    this.ref_idx = 125;
                    this.specularExponent = 1.5f;
                    this.a0 = 0.0f;
                    this.a1 = 0.5f;
                    this.a2 = 0.1f;
                    this.a3 = 0.8f;
                    break;
                case MaterialPrefab.rubber:
                    this.ref_idx = 10;
                    this.specularExponent = 1.0f;
                    this.a0 = 0.9f;
                    this.a1 = 0.1f;
                    this.a2 = 0.0f;
                    this.a3 = 0.0f;
                    break;
                case MaterialPrefab.mirror:
                    this.ref_idx = 1450;
                    this.specularExponent = 1.0f;
                    this.a0 = 0.0f;
                    this.a1 = 10.0f;
                    this.a2 = 0.8f;
                    this.a3 = 0.0f;
                    break;
                default:
                    this.ref_idx = 1450;
                    this.specularExponent = 1.0f;
                    this.a0 = 0.0f;
                    this.a1 = 10.0f;
                    this.a2 = 0.8f;
                    this.a3 = 0.0f;
                    break;
            }
        }
    }
}
