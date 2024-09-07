# LatiosRagdoll

Is not a package that implements Ragdoll Wizard for Latios. You will have to create joint/shape and other things yourself.
The purpose of the package is to provide a possibility to spawn a set of physical bodies in runtime, define their position based on current bones and then synchronize the position of bones from physical bodies.


You need to put this package and https://github.com/xacce/LatiosKinematicAnnotation to make it work.

First, annotate the bones from https://github.com/xacce/LatiosKinematicAnnotation. Then when you have a list of paths as well as annotations for the skeleton (left hard, right hand, right upper hand etc).

Add LatiosRagdolledAuthoring to the animated object, specify the LatiosPathsAnnotations and click Create ragdoll dummy for annotated.
This will create a set of annotated objects on the scene and their locations will match the skeleton. All you have to do is make the ragdoll itself.
When you are done, save the prefab of the ragdoll and specify it in the LatiosRagdolledAuthoring field of the animated object.