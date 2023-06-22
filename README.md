# RevitFileUpgrader

Bulk Revit File Upgrader. Support Revit 2017, 2018, 2019, 2020, 2021, 2022, 2023.

## Description

This repository creates a Revit add-in that can upgrade Revit files to a newer version. There is a project file for Revit 2017, 2018, 2019, 2020, 2021, 2022, 2023, each which will open files of the last version and then upgrade it to the stated version (ie RevitFileUpgrade2023 will open Revit 2022 files and upgrade them to 2023). 

## Installation

To install the Revit File Upgrader, first clone the repository to your machine. This can be done with the GitHub Desktop app. You will need to have Visual Studio downloaded with the .NET 4.5.2 framework development tools installed (if you want to use the 2017 and 2018 version). 

Before accessing the add-in you either need to write an Add In file for Revit, or use the Revit Add-In Manager provided by their Software Development Kit (recommended). To do the latter, first download the Revit SDK for whichever version of Revit you will be using to upgrade. Once downloaded, navigate to *C:\Revit Version SDK\Add-In Manager* where you will find the *Autodesk.AddInManager.addin* file. Open this file in a simple text editor, and replace *TARGETDIR* with the diretory that contains the file (most likely *C:\Revit Version SDK\Add-In Manager*). Then, copy this file into *C:\ProgramData\Autodesk\Revit\AddIns\Version* for the corresponding Revit version (the ProgramData folder may be hidden). This process needs to be repeated for each version of Revit.

## Usage

The Revit File Upgrader can be accessed through the Revit interface under the *Add-Ins* tab. With the version of Revit that you'd like to upgrade families to open, navigate to External Tools in the Add-Ins tab and then select *Add-In Manager (Manual Mode)*. From here, Load the corresponding .dll file for the file upgrader, most likely under *C:\Users\USER\OneDrive - HLW International LLP\Documents\GitHub\RevitFileUpgrader\RevitFileUpgradeYEAR\bin\Debug\RevitFileUpgradeYEAR.dll* where USER is your username, and YEAR is the Revit version you are using. Then in the pop-up, click the subfile *RevitFileUpgrade.CmdParameterManager* and run it.

In the following pop-up, simply select a Source parent folder and a Destination parent folder and click Update. The status of the file updater will show in the pop-up. The final output of the upgrader is located in the Destination folder, with the same subdirectories as the original library. Only the families and files that were updated are placed in the new folder, along with a .txt log listing all the changes.