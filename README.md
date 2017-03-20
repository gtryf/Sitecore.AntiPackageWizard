# Sitecore Create Anti-Package Wizard

## Purpose
Installing a Sitecore package means destructively altering your 
content tree and/or file system. If you need to have a checkpoint to
return to after installation, you'll have to take a database and
file system backup so if things go wrong you can do a restore.

Furthermore, on a production authoring environment, content editing
will have to pause until the package has been installed and tested,
since otherwise restoring the database will mean lost work.

The answer to these problems was given by [Sitecore Rocks](https://github.com/JakobChristensen/Sitecore.Rocks)'s anti-package
feature, but this is a developer's power tool. Sitecore admins were
distinctly lacking in this feature.

This is where this plugin comes in. Borrowing heavily from the Rocks
source code, and peeking into Sitecore's stock install Wizard, this
solution adds a "Create Anti-Package" wizard to the developer menu.
All you need to do is upload or choose your package, and the tool
will generate an anti-package for you, which becomes available for download.

## Building/Installing
Build the solution using Visual Studio 2015+. From the source tree,
copy the "sitecore" folder into your Sitecore solution root,
and the Sitecore.AntiPackageWizard.dll file into your bin folder.
Finally, from the Content Editor, switch to the core database and do
an "Update Tree" operation (Developer tab) on the sitecore root item.