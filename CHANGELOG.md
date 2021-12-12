# changelog

This is an ongoing changelog for Hedera, a free open-source 3D ivy painting plug-in package for Unity. https://github.com/radiatoryang/hedera

## v1.2.3 - 12 December 2021
- started a formal changelog
- in IvyEditor, added no-alt keyboard modifier, so orbiting 3D camera in scene view doesn't cause painting (https://github.com/radiatoryang/hedera/issues/12)
- move example ivy profiles and textures to "Samples" folder, so that Package Manager users can import the data directly into their Assets (and thus modify them)
- added better adhesion -- fires raycasts to calculate nearest surface, good for mesh colliders? enable / disable with the new checkbox right below the "Start Painting" button... thanks for the PR, @alexjhetherington ! (see https://github.com/radiatoryang/hedera/pull/14 + https://github.com/radiatoryang/hedera/issues/13)


## v1.2.0 - 2 November 2020

- Merge pull request #10 from radiatoryang/develop
- corrected package.json
- fix SceneView.drawGizmos bug (see https://github.com/radiatoryang/hedera/issues/9 ), change LightmapStatic to ContributeGI for 2019.2+, moved Examples to Samples folder (for UPM), update package.json to reflect new package
- if leaf mesher doesn't find cling vector for ivy node, then re-use the last one it had
- speed up simulation tick FPS from 10 FPS to 24 FPS
- add specular highlights / glossy reflections toggles to HederaIvyFoliage example shader


## v1.1.0 - 14 August 2020

- Merge pull request #8 from radiatoryang/develop
- fix leaf positioning and rotation; leaves now face outwards based on ivy cling normal (see https://github.com/radiatoryang/hedera/issues/5)
- clamp leaf density and leaf sunlight back to 100%, instead of weird hacks
- rotated all leaf textures 90 degrees counterclockwise, for leaf alignment fix
- Merge pull request #7 from radiatoryang/develop
- trying to fix bug where tool gizmos (move, rotate, scale, etc) can get stuck hidden, if the IvyEditor doesn't have time to re-enable them
- ivy painting now ignores colliders marked as triggers
- added warning if gizmos are disabled in Unity 2019+ which breaks OnSceneGUI ... see https://github.com/radiatoryang/hedera/issues/6 (thanks id-0-ru and Roland09)
- added .asmdef


## v1.0.0 - 9 July 2019

- initial release