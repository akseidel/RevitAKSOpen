# RevitAKSOpen ![GenieOpener](RevitAKSOpen/AKSOpen3.png)

Revit add-in in C# &ndash; creates a custom ribbon tab with a single control for opening workshared Revit files in one click. The same control is added to the Addins tab. The controls are active at all times.

## Why?
Opening a workshared Revit file to be a Local work file is a multistep process. Revit, furthermore, is sloppy about it. It trashes the same local folder with all the local files and their overhead files it saves. Revit also leaves a trap in the Revit desktop when it deposits an icon for the central file one chose. Of course one can reopen the saved local file instead of the central file, but some are not so daring and often forget to do the critical initial sync. Their Revit managers might insist they never use a local to avoid "issues". Autodesk's publications actually suggest to operate that way. Situations where the Revit coordinator replaces the central file with a copy orphans the local file, so one has to start from the central file again.

Why not present the user with choices from what they have already been working on or point them to choices of the appropriate Revit version to be opened ready to go as a local file stored in an organized, structured pattern while also saving previous work in a similar manner. This add-in does that. It tries, in one click, to automatically pass the various Revit roadblocks and hazards, with added smarts, to properly open a workshared file.      

This add-in demonstrates many of the typical tasks and implementation required for providing a tab menu interface and dealing with files. A back burner improvement is to make the user selection landing areas larger, perhaps changing to or adding preview icons to the interface.

## Specific Interest
**"Tell Me About It" Mode**
* A function reporting a Revit file's metadata to determine what Revit version it may be. It reports other useful information. This is one way to determine a Revit file's provenance without actually opening the file in Revit.

**Zero document state operation**
* Having the add-in available to run when there is not already a Revit file open is called zero document state availability. It makes sense for this add-in to have that availability.

**Writing to the Revit status bar**
* A thing to know. Revit funnels most of its feedback to the status bar. This add-in uses the status bar to be a whimsical check on who is paying attention.

**Useful mundane file and directory operations for reuse**
* Making local folders identified with the user's initials.
* Moving prior local files to a local stash folder.
* Indexing the prior local file names so that one can see their generation. This involves the "make a unique name" task one runs into needing in many situations.
* Pruning the number of stashed files.

This repository is provided for sharing and learning purposes. Perhaps someone might provide improvements or education. Perhaps it will help to boost someone further up the steep learning curve needed to create Revit task add-ins. Hopefully it does not show too much of the wrong way.  

Much of the code is by others. Its mangling and ignorant misuse is my ongoing doing. Much thanks to the professionals like Jeremy Tammik who provided the means directly or by mention one way or another for probably all the code needed.
