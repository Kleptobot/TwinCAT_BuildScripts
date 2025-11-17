place the .pkgdef and .dll files in the following directory
C:\Program Files\Beckhoff\TcXaeShell\Common7\IDE\Extensions

place two plain text files in the same directory as the TwinCAT solution:
 - pre_build_scripts.txt
 - post_build_scripts.txt

The plugin reads the files and executes each line as a powershell argument.
Scritps are executed sequentially from top to bottom.
The execution context of the scripts is the solution directory.

below is an example of what a script file might contain:

python C:\Git\scripts\example.py
"& ""C:\Git\scripts\example.ps1"""
"& ""C:\Git\scripts\example.bat"""

In the Tools menu there will be an additional menu items:
 - Run Scripts and BUild
 - Run Pre Scripts
 - Run Post Scripts
