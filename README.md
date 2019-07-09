# hedera
<img width=320px align=right src=https://user-images.githubusercontent.com/2285943/60858537-c1559800-a1dc-11e9-81c6-495bf4ea13f6.gif> 

#### paint 3D ivy in the Unity Editor, simulate growth and clinging in real-time*
<code>* real-time mesh generation might be very slow on older computers / integrated GPUs</code>

- procedurally generate foliage meshes for your games and 3D worlds
- includes textures, shaders, presets for painting realistic ivy, cartoon ivy, or even ropes and cables
- vert counts and polycounts usually < 5k unless you paint a lot of ivy; [download sample ivy .OBJ here (right-click and Save As)](https://raw.githubusercontent.com/radiatoryang/hedera/master/Example/ExampleIvyMeshExport.obj)
    - merge multiple ivy meshes to save draw calls, or just let static batching work
    - randomize vertex colors for subtle color variation, auto-unwrap ivy UV2s for lightmapping
    - store ivy meshes directly in your project, or export to .OBJ
- guide and user documentation is on the [Wiki](https://github.com/radiatoryang/hedera/wiki)
- tested on Unity 2019.1.8 and MacBook Pro 2017 (but probably works ok on older Unity versions)

### usage
- download the latest .unitypackage from [Releases](https://github.com/radiatoryang/hedera/releases)
- or, clone this Git repository as a [submodule](https://git-scm.com/book/en/v2/Git-Tools-Submodules), or clone as a package via [UpmGitExtension](https://github.com/mob-sakai/UpmGitExtension)

### contributors
- please post bug reports or (small) feature requests as an [Issue](https://github.com/radiatoryang/hedera/issues)
- [Pull Requests](https://github.com/radiatoryang/hedera/pulls) are welcome and encouraged

### license
**GPL2** (due to original author's use of GPL2)
- **You can use the ivy assets / generated meshes and results, with any or no license, in commercial or closed source projects.** GPL2 focuses on the code, and does not apply to program output.
- All code is editor-only and stripped upon build, which (I think) avoids GPL2's wrath. This code basically won't be in your build, which means you aren't distributing it.
- If you use this code / tool in your own tool AND distribute that tool, then your tool must use GPL2. Note that Unity Asset Store bans licenses like GPL, so no part of this tool can ever be put on the Asset Store.
- _(but I am not a lawyer, this is not legal advice, etc.)_

### acknowledgments
- based on [C++ code by Thomas Luft](http://graphics.uni-konstanz.de/~luft/ivy_generator/) from 2006 (!)
- based on [Unity C# port by Weng Xiao Yi](https://github.com/phoenixzz/IvyGenerator)
- uses [painting code](https://github.com/marmitoTH/Unity-Prefab-Placement-Editor) and [foliage shader code](https://github.com/marmitoTH/unity-enhanced-foliage) by Lucas Rodrigues
- uses [.OBJ exporter code](https://wiki.unity3d.com/index.php/ExportOBJ) by DaveA, KeliHlodversson, tgraupmann, drobe
- uses [Douglas-Peucker line simplifier code](https://github.com/rohaanhamid/simplify-csharp) by Rohaan Hamid
