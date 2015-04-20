<img src="https://raw.github.com/et1337/opentk-deferred-rendering/master/screenshot2.jpg" />

# OpenTK Deferred Rendering Sample
by [Evan Todd](http://et1337.wordpress.com)

## Hardware Requirements

Support for Multiple Render Targets and Shader Model 2

## Mac Requirements
Mono, MonoDevelop, and OpenTK.

## Windows Requirements
Visual Studio and OpenTK.

Download OpenTK from [here](https://sourceforge.net/projects/opentk/files/latest/download).

## Controls
Spacebar - Toggle display mode

This sample demonstrates a very basic deferred renderer. While a forward
renderer normally calculates lighting as it renders the scene, a deferred
renderer saves information about the materials in the scene to a number of
buffers. These are later sampled to calculate the lighting, and then composited
into a final image. There are many advantages to the technique, but probably
the most useful feature is its ability to render large amounts of lights at
relatively low cost, irrespective of the triangular complexity of the scene.

This sample renders a simple textured cube with 100 deferred point lights. Press
spacebar to view the buffers used to generate the scene. The buffers are
(clockwise from the top left): normals, light accumulation, depth, and diffuse.

The renderer is greatly simplified in the interest of time. Real high dynamic
range is essential for a real deferred renderer. This sample simulates it by
storing scaled values in the lighting buffer.

## The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
