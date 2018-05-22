# This project has been merged into Mass Effect 3 Mod Manager Command Line. This repository is only kept for commit history.
You can access this tool in at [the Mod Manager Command Line repository](http://github.com/mgamerz/modmanagercommandline) under Transplanter-CLI.

## Mass Effect 3 GUI Transplanter / PCC Data Dumper
Transplants GFX files from one PCC to another in ME3, as well as dumping useful information from PCC files into text files for easy searching. Integrated into [Mass Effect 3 Mod Manager](http://github.com/mgamerz/me3modmanager) to quickly fix override conflicts between DLC mods with the Mass Effect 3 controller mods, and allow easy dumping of files for searchability.


---
### Command Line Interface
Can transplant single file or batch by doing a folder. Contains multiple switches for extended operations such as exporting various types of PCC data into a useful text format, GUI file extraction, and Exec function dumping.

#### NOTE: THESE NEED UPDATED FOR 1.0.0.15, THEY ARE OUTDATED.

Required (mutually exclusive):
 * **-i/--inputfile pccfile** Path to a pcc file. Acts as the source in all operations.
 * **-f/--inputfolder folderpath** Path to a folder contain pcc files. This will recursively be searched, so all pcc's will be found.

Operations (mutually exclusive):
 * **-t/--transplantfile pccfile** Path to a file that will have GUI files injected into. This operation only works with -i/--inputfile.
 * **-g/--gui-extract** Extracts GUI files from the specified input file or all files in the input folder. They are placed into folders with the same name as the PCC file.
 * **-x/--extract** Extracts information from PCC files and puts them into text files of the same name. Requires at least one of the following switches:
  * _-n/--names_ Dumps the name table and name indexes
  * _-m/--imports_ Dumps the import list and import indexes. Includes Package + Object name, offset, and what package it is being sourced from
  * _-e/--exports_ Dumps the export list and export indexes. Includes Package + Object name, superclass, class, and offset.
  * _-s/--scripts_ Dumps function text for function exports. 
  * _-c/--coalesced_ Dumps Coalesced exports with a [C] prepended to the export text.
  * _-d/--data_ Dumps hex data from the export into the text file. This makes files very large. It is useful for file comparisons between PCC files. Automatically turns on the --exports flag.
 * **-u/--exec-dump** Dumps exec function exports and their function text into a file named ExecFunctions.txt.

Optional:
* **-v/--verbose** Turns on verbose logging so you can see some extra stuff going on
* **-o/--outputfolder** Redirects output files into the specified folder. If it does not exist, it will be created.


#### Issues
You can raise issues for this project on this github page or on [my modding forum](http://me3tweaks.com/forums).

#### License
Uses some PCC related code from the open source [ME3Explorer Project](http://github.com/me3explorer/me3explorer).

This program is licensed under GPL. All derivatives of this work must include their source code when distributed.
