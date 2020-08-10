using GPURayTracer.Rendering;
using GPURayTracer.Rendering.Primitives;
using ILGPU.IR.Values;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GPURayTracer.Loaders
{
    public static class MeshLoader
    {
        //ported from: https://github.com/mattgodbolt/pt-three-ways/blob/master/src/util/ObjLoader.cpp
        public static Dictionary<string, MaterialData> LoadMaterialsFromFile(string filename)
        {
            Dictionary<string, MaterialData> materials = new Dictionary<string, MaterialData>();
            string[] lines;

            try
            {
                lines = File.ReadAllLines(filename);
            }
            catch
            {
                materials.Add("", new MaterialData());
                return materials;
            }

            string materialName = "";

            MaterialData material = new MaterialData();
            int illum = 2;
            Vec3 ambientColor = new Vec3();
            Vec3 emmissiveColor = new Vec3();
            bool materialComplete = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] split = line.Split(" ");

                if (line.Length > 0 && line[0] != '#' && split.Length >= 2)
                {
                    switch (split[0])
                    {
                        case "newmtl":
                            {
                                if(materialComplete)
                                {
                                    if(illum == 3)
                                    {
                                        material.reflectivity = ambientColor.length();
                                    }

                                    if (emmissiveColor.x > 0 || emmissiveColor.y > 0 || emmissiveColor.z > 0)
                                    {
                                        material.type = 3;
                                        material.color = emmissiveColor;
                                    }
                                    else
                                    {
                                        material.type = 0;
                                    }

                                    materials.Add(materialName, material);

                                    materialComplete = false;
                                    material = new MaterialData();
                                    illum = 2;
                                    ambientColor = new Vec3();
                                    emmissiveColor = new Vec3();
                                }

                                materialName = split[1];
                                break;
                            }
                        case "Ns":
                            {
                                if(double.TryParse(split[1], out double ns))
                                {
                                    material.reflectionConeAngleRadians = (float)(Math.PI * Math.Clamp(1.0 - (ns / 100.0), 0.0, 1.0));
                                }
                                break;
                            }
                        case "Ka":
                            {
                                if(double.TryParse(split[1], out double ar) && double.TryParse(split[2], out double ag) && double.TryParse(split[3], out double ab))
                                {
                                    ambientColor = new Vec3(ar, ag, ab);
                                }
                                break;
                            }
                        case "Kd":
                            {
                                if (double.TryParse(split[1], out double dr) && double.TryParse(split[2], out double dg) && double.TryParse(split[3], out double db))
                                {
                                    material.color = new Vec3(dr, dg, db);
                                }
                                break;
                            }
                        case "Ks":
                            {
                                //ignored
                                break;
                            }
                        case "Ke":
                            {
                                if (double.TryParse(split[1], out double er) && double.TryParse(split[2], out double eg) && double.TryParse(split[3], out double eb))
                                {
                                    emmissiveColor = new Vec3(er, eg, eb);
                                }
                                break;
                            }
                        case "Ni":
                            {
                                if (double.TryParse(split[1], out double ns))
                                {
                                    material.ref_idx = (float)(Math.PI * Math.Clamp(1.0 - (ns / 100.0), 0.0, 1.0));
                                }
                                break;
                            }
                        case "d":
                            {
                                //ignored
                                break;
                            }
                        case "illum":
                            {
                                if(int.TryParse(split[1], out int il))
                                {
                                    illum = il;
                                    materialComplete = true;
                                }
                                break;
                            }
                    }

                }

            }

            if (materialComplete)
            {
                if (illum == 3)
                {
                    material.reflectivity = ambientColor.length();
                }

                if (emmissiveColor.x > 0 || emmissiveColor.y > 0 || emmissiveColor.z > 0)
                {
                    material.type = 3;
                    material.color = emmissiveColor;
                }
                else
                {
                    material.type = 0;
                }

                materials.Add(materialName, material);
            }

            return materials;
        }

        public static hGPUMesh LoadMeshFromFile(Vec3 pos, WorldData worldData, string filename)
        {
            Dictionary<string, MaterialData> materials = LoadMaterialsFromFile(filename + ".mtl");

            string[] lines = File.ReadAllLines(filename + ".obj");

            List<float> verticies = new List<float>();
            List<int> triangles = new List<int>();
            List<int> mats = new List<int>();

            int mat = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] split = line.Split(" ");

                if (line.Length > 0 && line[0] != '#' && split.Length >= 2)
                {
                    switch (split[0])
                    {
                        case "v":
                            {
                                if (double.TryParse(split[1], out double v0) && double.TryParse(split[2], out double v1) && double.TryParse(split[3], out double v2))
                                {
                                    verticies.Add((float)v0);
                                    verticies.Add((float)-v1);
                                    verticies.Add((float)v2);
                                }
                                break;
                            }
                        case "f":
                            {
                                List<int> indexes = new List<int>();
                                for(int j = 1; j < split.Length; j++)
                                {
                                    string[] indicies = split[j].Split("/");

                                    if(indicies.Length >= 1)
                                    {
                                        if (int.TryParse(indicies[0], out int i0))
                                        {
                                                indexes.Add(i0 < 0 ? i0 + verticies.Count : i0 - 1);
                                        }
                                    }
                                }

                                for(int j = 1; j < indexes.Count - 1; ++j)
                                {
                                    triangles.Add(indexes[0]);
                                    triangles.Add(indexes[j]);
                                    triangles.Add(indexes[j+1]);
                                    mats.Add(mat);
                                }

                                break;
                            }
                        case "usemtl":
                            {
                                if(materials.ContainsKey(split[1]))
                                {
                                    MaterialData material = materials[split[1]];
                                    mat = worldData.worldBuffer.addMaterial(material);
                                }
                                else
                                {
                                    mat = worldData.worldBuffer.addMaterial(MaterialData.makeDiffuse(new Vec3(1, 0, 1)));
                                }

                                break;
                            }
                    }

                }
            }

            return new hGPUMesh(pos, verticies, triangles, mats);
        }
    }
}
