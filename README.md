# RevitAKSOpen ![GenieOpener](RevitAKSOpen/AKSOpen3.png)

Revit add-in in C# &ndash; creates a custom ribbon tab with a single control for opening workshared Revit files in one click. The same control is added to the Addins tab. The controls are active at all times.

## Why?
Opening a workshared Revit file to be a Local work file is a multistep process. Revit furthermore is sloppy about it. It trashes the same local folder with all the local files it saves along with their overhead files. Revit also leaves a trap in the Revit desktop for you when it deposits an icon for the central file. Of course you can reopen your local file instead of the central, but some are not so daring. Their managers might actually insist they never use a local to avoid "issues". Autodesk's publications actually suggest to operate that way. Situations where the Revit coordinator replaces the central with a copy orphans your local, so you have to start from the central.

Why not present the user with choices from what they have been working on or point them to choices of the appropriet Revit version to be opened ready to go as a local stored in an organized way, saving previous work also in an organized way. This add-in does that.      

This repository is provided for sharing and learning purposes. Perhaps someone might provide improvements or education. Perhaps it will help to boost someone further up the steep learning curve needed to create Revit task add-ins. Hopefully it does not show too much of the wrong way.  

This add-in demonstrates many of the typical tasks and implementation required for providing a tab menu interface and dealing with files.

Much of the code is by others. Its mangling and ignorant misuse is my ongoing doing. Much thanks to the professionals like Jeremy Tammik who provided the means directly or by mention one way or another for probably all the code needed.
