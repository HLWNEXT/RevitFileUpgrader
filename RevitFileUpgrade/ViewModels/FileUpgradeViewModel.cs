using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using RevitFileUpgrade.Interfaces;
using System.Windows.Input;
using RevitFileUpgrade.Commands;
using Autodesk.Revit.UI;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using RevitFileUpgrade.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RevitFileUpgrade.ViewModels
{
    public delegate void OperateFamily(FileInfo file, String destPath, ref bool addInfo, ref List<string> fileTypes, ref IList<FileInfo> files, ref StreamWriter writer);


    class ParameterManagerViewModel : BaseViewModel, IViewModel
    {
        List<string> fileTypes = new List<string>() { ".rfa" , ".rvt"};
        StreamWriter writer = null;
        IList<FileInfo> files = new List<FileInfo>();

        // Variable to store if user cancels the process
        bool cancelled = false;
        bool addInfo = false;

        public int fileCount;

        private ObservableCollection<string> _textBoxContents = new ObservableCollection<string>();
        public ObservableCollection<string> TextBoxContents
        {
            get { return _textBoxContents;}
            set
            {
                _textBoxContents = value;
                OnPropertyChanged("TextBoxContents");
            }
        }

        private double _currentProgress;
        public double CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                _currentProgress = value;
                OnPropertyChanged("CurrentProgress");
            }
        }

        private string _sourcePath = "";
        public string SourcePath
        {
            get { return _sourcePath; }
            set
            {
                if (value == _sourcePath)
                {
                    return;
                }
                else
                {
                    _sourcePath = value;
                    OnPropertyChanged("SourcePath");
                }
            }
        }

        private string _destinationPath = "";
        public string DestinationPath
        {
            get { return _destinationPath; }
            set
            {
                if (value == _destinationPath)
                {
                    return;
                }
                else
                {
                    _destinationPath = value;
                    OnPropertyChanged("DestinationPath");
                }
            }
        }


        // Handler for Source folder browse button
        private void GetSourcePath()
        {
            // Open the folder browser dialog
            FolderBrowserDialog dlg = new FolderBrowserDialog();

            // Disable New Folder button since it is source location
            dlg.ShowNewFolderButton = false;

            // Provide description 
            dlg.Description = "Select the Source folder :";

            // Show the folder browse dialog
            dlg.ShowDialog();

            // Populate the source path text box
            SourcePath = dlg.SelectedPath;
        }

        // Handler for the Destination folder browse button
        private void GetDestinationPath()
        {
            // Open the folder browser dialog
            FolderBrowserDialog dlgDest = new FolderBrowserDialog();

            // Enable the New folder button since users should have ability to create destination folder incase it did not pre-exist
            dlgDest.ShowNewFolderButton = true;

            // Provide description
            dlgDest.Description = "Select the Destination folder : ";

            // Show the folder browse dialog
            dlgDest.ShowDialog();

            // Populate the destination path text box
            DestinationPath = dlgDest.SelectedPath;
        }

        public static void DoEvents()
        {
            ParameterManager.AppWindow.Dispatcher.Invoke(DispatcherPriority.Background,new Action(delegate { }));
        }

        public void TraverseAll(DirectoryInfo source, DirectoryInfo target)
        {
            try
            {
                // Check for user input events
                DoEvents();

                // If destination directory does not exist, create new directory
                if (!Directory.Exists(target.FullName))
                {
                    Directory.CreateDirectory(target.FullName);
                }

                foreach (FileInfo fi in source.GetFiles())
                {
                    //// Check for user input events
                    DoEvents();
                    if (!cancelled)
                    {
                        System.Security.AccessControl.FileSecurity sec = fi.GetAccessControl();
                        if (!sec.AreAccessRulesProtected)
                        {
                            // Proceed only if it is not a back up file
                            if (IsNotBackupFile(fi))
                            {
                                // Check if the file already exists, if not proceed
                                if (!AlreadyExists(target, fi))
                                {
                                    // The method contains the code to upgrade the file
                                    //Upgrade(fi, target.FullName);
                                    addInfo = false;
                                    UpgradeFamily(fi, target.FullName, ref addInfo, ref fileTypes, ref files, ref writer);
                                    
                                    if (addInfo)
                                    {
                                        String msg = " has been upgraded";

                                        // Log file and user interface updates
                                        _textBoxContents.Add("\n" + fi.Name + msg);
                                        //lstBxUpdates.TopIndex = lstBxUpdates.Items.Count - 1;
                                        writer.WriteLine(fi.FullName + msg);
                                        writer.Flush();
                                        CurrentProgress = CurrentProgress + 1.0 / 6.3;
                                    }
                                }
                                else
                                {
                                    // Print that the file already exists
                                    String msg = " already exists!";
                                    writer.WriteLine("------------------------------");
                                    writer.WriteLine("Error: "
                                      + target.FullName + "\\" + fi.Name + " " + msg);
                                    writer.WriteLine("------------------------------");
                                    writer.Flush();

                                    TextBoxContents.Add(
                                      "-------------------------------");
                                    TextBoxContents.Add("Error: "
                                      + target.FullName + "\\" + fi.Name + " " + msg);
                                    TextBoxContents.Add(
                                      "-------------------------------");
                                    //lstBxUpdates.TopIndex = lstBxUpdates.Items.Count - 1;
                                }
                            }
                        }
                        else
                        {
                            String msg = " is not accessible or read-only!";
                            writer.WriteLine("-------------------------------");
                            writer.WriteLine("Error: " + fi.FullName + msg);
                            writer.WriteLine("-------------------------------");
                            writer.Flush();

                            TextBoxContents.Add(
                              "------------------------------");
                            TextBoxContents.Add("Error: " + fi.FullName + msg);
                            TextBoxContents.Add(
                              "------------------------------");
                            //lstBxUpdates.TopIndex = lstBxUpdates.Items.Count - 1;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                // Check for user input events
                DoEvents();

                // RFT resave creates backup files 
                // Delete these backup files created
                foreach (FileInfo backupFile in target.GetFiles())
                {
                    if (!IsNotBackupFile(backupFile))
                        File.Delete(backupFile.FullName);
                }

                // Using recursion to work with sub-directories
                foreach (DirectoryInfo sourceSubDir in source.GetDirectories())
                {
                    DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(sourceSubDir.Name);
                    TraverseAll(sourceSubDir, nextTargetSubDir);

                    // Delete the empty folders - this is created when none of the files in them meet our upgrade criteria
                    if (nextTargetSubDir.GetFiles().Count() == 0 && nextTargetSubDir.GetDirectories().Count() == 0)
                        Directory.Delete(nextTargetSubDir.FullName);
                }
            }
            catch
            {

            }
        }

        // Helper method to check if file already exists in target folder
        private bool AlreadyExists(DirectoryInfo target, FileInfo file)
        {
            foreach (FileInfo infoTarget in target.GetFiles())
            {
                if (infoTarget.Name.Equals(file.Name))
                    return true;
            }
            return false;
        }

        // Helps determine if the source file is back up file or not Backup files are determined by the format : <project_name>.<nnnn>.rvt
        // This utility ignores backup files
        private bool IsNotBackupFile(FileInfo rootFile)
        {
            // Check if the file is a backup file

            if (rootFile.Name.Length < 9)
            {
                return true;
            }
            else
            {
                if (rootFile.Name.Substring(rootFile.Name.Length - 9)
                  .Length > 0)
                {
                    String backUpFileName = rootFile.Name.Substring(
                      rootFile.Name.Length - 9);
                    long result = 0;

                    // Check each char in the file name if it follows 
                    // the back up file naming convention

                    if (
                      backUpFileName[0].ToString().Equals(".")
                        && Int64.TryParse(backUpFileName[1].ToString(), out result)
                        && Int64.TryParse(backUpFileName[2].ToString(), out result)
                        && Int64.TryParse(backUpFileName[3].ToString(), out result)
                        && Int64.TryParse(backUpFileName[4].ToString(), out result)
                        )
                        return false;
                }
                return true;
            }
        }


        // Searches the directory and creates an internal list of files to be upgraded
        void SearchDir(DirectoryInfo sDir, bool first)
        {
            try
            {
                // If at root level, true for first call to this method

                if (first)
                {
                    foreach (FileInfo rootFile in sDir.GetFiles())
                    {
                        // Create internal list of files to be upgraded
                        // This will help with Progress bar

                        // Proceed only if it is not a back up file
                        if (IsNotBackupFile(rootFile))
                        {
                            // Keep adding files to the internal list of files
                            if (fileTypes.Contains(rootFile.Extension) || rootFile.Extension.Equals(".txt"))
                            {
                                if (rootFile.Extension.Equals(".txt"))
                                {
                                    if (fileTypes.Contains(".rfa"))
                                    {
                                        foreach (FileInfo rft in sDir.GetFiles())
                                        {
                                            if (rft.Name.Remove(rft.Name.Length - 4, 4).Equals(rootFile.Name.Remove(rootFile.Name.Length - 4, 4)) && !rft.Extension.Equals(rootFile.Extension))
                                            { 
                                                files.Add(rootFile); 
                                                break; 
                                            }
                                        }
                                    }
                                }
                                else
                                    files.Add(rootFile);
                            }
                        }
                    }
                }

                // Get access to each sub-directory in the root directory
                foreach (DirectoryInfo direct in sDir.GetDirectories())
                {
                    System.Security.AccessControl.DirectorySecurity sec =
                      direct.GetAccessControl();
                    if (!sec.AreAccessRulesProtected)
                    {
                        foreach (FileInfo fInfo in direct.GetFiles())
                        {
                            // Proceed only if it is not a back up file
                            if (IsNotBackupFile(fInfo))
                            {
                                // Keep adding files to the internal list of files
                                if (fileTypes.Contains(fInfo.Extension) || fInfo.Extension.Equals(".txt"))
                                {
                                    if (fInfo.Extension.Equals(".txt"))
                                    {
                                        if (fileTypes.Contains(".rfa"))
                                        {
                                            foreach (FileInfo rft in direct.GetFiles())
                                            {
                                                if (
                                                  rft.Name.Remove(
                                                  rft.Name.Length - 4, 4).Equals(
                                                  fInfo.Name.Remove(fInfo.Name.Length - 4, 4)
                                                  )
                                                  && !(rft.Extension.Equals(fInfo.Extension))
                                                  )
                                                { files.Add(fInfo); break; }
                                            }
                                        }
                                    }
                                    else
                                        files.Add(fInfo);
                                }
                            }
                        }

                        // Use recursion to drill down further into 
                        // directory structure
                        SearchDir(direct, false);
                    }
                    else
                    {
                        String msg = " is not accessible or read-only!";
                        writer.WriteLine("------------------------------------");
                        writer.WriteLine("Error: " + direct.FullName + msg);
                        writer.WriteLine("------------------------------------");
                        writer.Flush();

                        TextBoxContents.Add("------------------------------");
                        TextBoxContents.Add("Error: " + direct.FullName + msg);
                        TextBoxContents.Add("------------------------------");
                        //lstBxUpdates.TopIndex = lstBxUpdates.Items.Count - 1;
                    }
                }
            }
            catch (Exception excpt)
            {
                writer.WriteLine("-------------------------------------");
                writer.WriteLine("Error :" + excpt.Message);
                writer.WriteLine("-------------------------------------");
                writer.Flush();
            }
        }

        //public delegate void OperateFamily(string familyName);

        public OperateFamily UpgradeFamily;


        // Handler code for the Upgrade button click event
        private void Start()
        {
            // Initialize the count for success and failed files
            CmdParameterManager.success = 0;
            CmdParameterManager.failed = 0;


            // Ensure all path information is filled in 
            if (SourcePath.Length > 0 && DestinationPath.Length > 0)
            {
                // Perform checks to see if all the paths are valid
                DirectoryInfo dir = new DirectoryInfo(SourcePath);
                DirectoryInfo dirDest = new DirectoryInfo(DestinationPath);

                if (!dir.Exists)
                {
                    SourcePath = String.Empty;
                    return;
                }

                if (!dirDest.Exists)
                {
                    DestinationPath = String.Empty;
                    return;
                }

                // Ensure destination folder is not inside the source folder
                var dirs = from nestedDirs in dir.EnumerateDirectories("*") where dirDest.FullName.Contains(nestedDirs.FullName) select nestedDirs;
                if (dirs.Count() > 0)
                {
                    TaskDialog.Show(
                      "Abort Upgrade",
                      "Selected Destination folder, " + dirDest.Name +
                      ", is contained in the Source folder. Please select a" +
                      " Destination folder outside the Source folder.");
                    DestinationPath = String.Empty;
                    return;
                }

                // If paths are valid, create log and initialize it
                writer = File.CreateText(DestinationPath + "\\" + "UpgraderLog.txt");

                // Clear list box 
                TextBoxContents.Clear();
                files.Clear();

                // Progress bar initialization
                CurrentProgress = 0;

                // Search the directory and create thelist of files to be upgraded
                SearchDir(dir, true);

                // Set Progress bar base values for progression
                fileCount = files.Count;

                // Traverse through source directory and upgrade files which match the type criteria
                TraverseAll(
                  new DirectoryInfo(SourcePath),
                  new DirectoryInfo(DestinationPath));

                // In case no files were found to match the required criteria
                if (CmdParameterManager.failed.Equals(0) && CmdParameterManager.success.Equals(0))
                {
                    String msg = "No relevant files found for upgrade!";
                    TaskDialog.Show("Incomplete", msg);
                    writer.WriteLine(msg);
                    writer.Flush();
                }
                else
                {
                    if (CmdParameterManager.failures.Count > 0)
                    {
                        String msg = "-------------"
                          + "List of files that "
                          + "failed to be upgraded"
                          + "--------------------";

                        // Log failed files information
                        writer.WriteLine("\n");
                        writer.WriteLine(msg);
                        writer.WriteLine("\n");
                        writer.Flush();

                        // Display the failed files information
                        TextBoxContents.Add("\n");
                        TextBoxContents.Add(msg);
                        TextBoxContents.Add("\n");
                        //lstBxUpdates.TopIndex = lstBxUpdates.Items.Count - 1;
                        foreach (String str in CmdParameterManager.failures)
                        {
                            writer.WriteLine(str);
                            TextBoxContents.Add("\n" + str);
                            //lstBxUpdates.TopIndex = lstBxUpdates.Items.Count - 1;
                        }
                        CmdParameterManager.failures.Clear();
                        writer.Flush();
                    }

                    // Display final completion dialogwith success rate
                    TaskDialog.Show("Completed",
                      CmdParameterManager.success + "/" + (CmdParameterManager.success + CmdParameterManager.failed)
                      + " files have been successfully upgraded! "
                      + "\n\nA log file has been created at :\n"
                      + DestinationPath);
                }
                // Reset the Progress bar
                CurrentProgress = 0;

                // Close the Writer object
                writer.Close();
            }
        }

        // Handler for the Cancel button
        private void Close()
        {
            // Set the cancelled variable to true
            cancelled = true;
        }

        public ICommand GetSource
        {
            get
            {
                return new DelegateCommand((x) => GetSourcePath());
            }
        }

        public ICommand GetDestination 
        {
            get
            {
                return new DelegateCommand((x) => GetDestinationPath());
            }
        }

        public ICommand Add
        {
            get
            {
                return new DelegateCommand((x) => Start());
            }
        }

        public ICommand Exit
        {
            get
            {
                return new DelegateCommand((x) => Close());
            }
        }


    }
}
