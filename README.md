# NULLrender

![rendered spheres](https://i.imgur.com/EjMDWJ3.png)

Nullrender is a GPU accelerated ray tracing renderer written in c#. As with most modern ray tracing performance is maintained by Temporal, Zbuffer, and Metadata driven denoising, currently at full resolution the scene above renders at 200+ fps on a rtx 2060. As of the time of writing the renderer only supports spheres with albedo and reflections, but emissive materials with shadow are in progress, as well as triangle ray tracing, and textures.

My ultimate goal is to make a simple open-source component based ray traced game engine for low poly / low res games with modern graphics tech. I am up for collaboration in this, so if you are interested in this at all I would be happy to have the help.

A test build to play with is available [here](https://github.com/NullandKale/GPURayTracer/releases). If no cuda device is found it will render in CPU mode.

## Ways to contribute:

Let me know you are interested in working on a feature by Email, PM, Github, or the [discord server](https://discord.gg/f3zwf2D). Even downloading the test builds and testing performance on different hardware would be useful.

You can also just hop on the [discord server](https://discord.gg/f3zwf2D) to talk about ray tracing and or how all of this works.

## Performance

Nullrender does not use any RTX features, so it should have decent performance on a wide range of Nvidia GPU hardware. 

My minimum goal for performance is 60 fps @ 1/2 res on a 2060 with around 10k triangles and a few dozen lights. Think Quake2 RTX level graphics. I think this is achievable with some optimization. I would not be against adding RTX features or changing the rendering to c++ for better cuda support as a last resort.

There are no acceleration structures built into the ray tracing, but they are planned for after triangles are added.

## WPF? Why WPF? Why C#?

I was initially just testing out ILGPU and dotnet WPF, but when I realized the performance I decided it would be fun to work on a project I have been wanting to do for a while. I love making game engines and ray tracing is so powerful and is so achievable now, why not?

Dotnet WPF also has planned linux support so linux is a possibility as well.

## Features currently being worked on:

- Shadows and Reflections from emissive materials
- Triangle hit detection
- Triangle meshes and mesh memory management

## Planned features that need help:

- Spatio-Temporal Denoising  
- Sparse Voxel Ray tracing
- Blue noise for RNG
- Texture mapping and memory management
- Normal maps
- BVH object hit 
- Water Material with caustics?

## Resources for Ray Tracing / Cuda

- [Cuda C Programming Guide (PDF)](https://docs.nvidia.com/cuda/archive/9.1/pdf/CUDA_C_Programming_Guide.pdf)
- [Ray Tracing Gems](http://www.realtimerendering.com/raytracinggems/)
- [Tiny Ray Tracer](https://github.com/ssloy/tinyraytracer)
- [Ray Tracing In One Weekend](https://raytracing.github.io/books/RayTracingInOneWeekend.html)
- [Matt Godbolts Path Tracer 3 Ways Talk](https://www.youtube.com/watch?v=HG6c4Kwbv4I)
- [Matt Godbolts Path Tracer 3 Ways Code](https://github.com/mattgodbolt/pt-three-ways)
- [Ray Tracing in One Weekend in CUDA](https://github.com/rogerallen/raytracinginoneweekendincuda/tree/master)
- [My own other ray tracer](https://github.com/NullandKale/CRT)

## Fun Graphical Glitches
[Two Triangles](https://gfycat.com/plasticspicykoalabear)

[Bad Shadows](https://gfycat.com/portlyfarkakapo)

[Smear](https://gfycat.com/viciousmatureindianjackal)
