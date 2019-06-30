# hedera
Unity Editor env art tool for procedurally generating 3D ivy meshes

### current state
this isn't usable, I was in the middle of fighting with Unity's shitty OnSceneGUI support... I can't get custom handles to draw for me at all, which makes painting and visualization very difficult. Someday I'll come back to this and fix it / seek revenge on Unity. But right now, it's very unusable / only 50% done.

### license
GPL2 (due to the original author's use of GPL2). I'm not a lawyer, but this is my interpretation:
- you can use the ivy assets / results of this tool without triggering GPL2
- if you don't distribute the code, then you don't have to license your project under GPL2 either
- for this reason, all the Hedera code is in an \Editor\ folder, so that Unity automatically strips it out when you make a build

### acknowledgments
- based on code by Thomas Luft, copyright 2007 http://graphics.uni-konstanz.de/~luft/ivy_generator/
- also uses code by Weng Xiao Yi https://github.com/phoenixzz/IvyGenerator
