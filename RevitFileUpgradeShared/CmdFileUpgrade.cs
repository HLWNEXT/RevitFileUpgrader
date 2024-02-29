using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using RevitFileUpgrade.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace RevitFileUpgrade
{
    [Serializable()]
    [Transaction(TransactionMode.Manual)]
    public class CmdParameterManager : IExternalCommand
    {

        // Container for previous opened document
        private Document previousDocument = null;
        private ExternalCommandData cmdData;
        public static int success;
        public static int failed;
        public static IList<String> failures = new List<String>();


        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication UIApp = commandData.Application;
            UIApp.DialogBoxShowing += new EventHandler<DialogBoxShowingEventArgs>(OnDialogShowing);
            UIApp.Application.FailuresProcessing += new EventHandler<FailuresProcessingEventArgs>(OnFailuresProcessing);
            cmdData = commandData;

            var instance = new ParameterManager
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };
            instance.UpdateFamily += Upgrade;
            var _ = new WindowInteropHelper(instance)
            {
                Owner = Process.GetCurrentProcess().MainWindowHandle
            };
            instance.ShowDialog();
            //UpgraderForm form = new UpgraderForm(commandData);
            //form.ShowDialog();

            UIApp.Application.FailuresProcessing -= OnFailuresProcessing;
            UIApp.DialogBoxShowing -= OnDialogShowing;
            return Result.Succeeded;
        }


        private static void OnFailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            FailuresAccessor failuresAccessor = e.GetFailuresAccessor();
            IEnumerable<FailureMessageAccessor> failureMessages = failuresAccessor.GetFailureMessages();
            foreach (FailureMessageAccessor failureMessage in failureMessages)
            {
                if (failureMessage.GetSeverity() == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(failureMessage);
                }
            }
            e.SetProcessingResult(FailureProcessingResult.Continue);
        }

        private static void OnDialogShowing(object o, DialogBoxShowingEventArgs e)
        {
            if (e.Cancellable)
            {
                e.Cancel();
            }
            //worry about this later - 1002 = cancel
            if (e.DialogId == "TaskDialog_Unresolved_References")
            {
                e.OverrideResult(1002);
            }
            //Don't sync newly created files. 1003 = close
            if (e.DialogId == "TaskDialog_Local_Changes_Not_Synchronized_With_Central")
            {
                e.OverrideResult(1003);
            }
            if (e.DialogId == "TaskDialog_Save_Changes_To_Local_File")
            {
                //Relinquish unmodified elements and worksets
                e.OverrideResult(1001);
            }
        }

        // Method which upgrades each file
        private void Upgrade(FileInfo file, String destPath, ref bool addInfo, ref List<string> fileTypes, ref IList<FileInfo> files, ref StreamWriter writer)
        {
            //addInfo = false;
            // Check if file type is what is expected to be upgraded or is a text file which is for files which contain type information for certain family files
#if RevitFileUpgrade2023
            if (file.Name.Contains("2022") && (fileTypes.Contains(file.Extension) || file.Extension.Equals(".txt")))
#elif RevitFileUpgrade2024
            if (file.Name.Contains("2023") && (fileTypes.Contains(file.Extension) || file.Extension.Equals(".txt")))
#endif
            {
                try
                {
                    // If it is a text file
                    if (file.Extension.Equals(".txt"))
                    {
                        if (fileTypes.Contains(".rfa"))
                        {
                            bool copy = false;

                            // Check each file from the list to see if the text file has the same name as any of the family files or if it is just a standalone text file. In case of standalone text file, ignore.
                            foreach (FileInfo rft in files)
                            {
                                if (
                                  rft.Name.Remove(rft.Name.Length - 4, 4).Equals(file.Name.Remove(file.Name.Length - 4, 4)) && ! rft.Extension.Equals(file.Extension)
                                  )
                                { copy = true; break; }
                            }
                            if (copy)
                            {
                                // Copy the text file into target destination
                                File.Copy(file.DirectoryName +
                                  "\\" + file.Name, destPath +
                                  "\\" + file.Name, true);
                                addInfo = true;
                            }
                        }
                    }

                    // For other file types other than text file
                    else
                    {
                        // This is the main function that opens and save a given file. 
                        // Check if the file is RFT file since we have to use OpenDocumentFile for RFT files
                        if (file.Extension.Equals(".rft"))
                        {
                            Document doc = cmdData.Application.Application.OpenDocumentFile(file.FullName);

                            String destinationFile = destPath + "\\" + file.Name;

                            Transaction trans = new Transaction(doc, "T1");
                            trans.Start();
                            doc.Regenerate();
                            trans.Commit();

                            doc.SaveAs(destinationFile);
                            doc.Close();

                            addInfo = true;
                        }
                        else
                        {
                            // Open a Revit file as an active document. 
                            UIApplication UIApp = cmdData.Application;
                            var App = UIApp.Application;
                            Document doc = App.OpenDocumentFile(file.FullName);

                            // Try closing the previously opened document after another one is opened. We are doing this because we cannot explicitely close an active document at a moment.  
                            if (previousDocument != null)
                            {
                                bool saveModified = true;
                                previousDocument.Close(saveModified);
                            }

                            //// Initial a transaction to add parameters.
                            //try
                            //{
                            //    Transaction t = new Transaction(doc, "Add Parameter");
                            //    t.Start();
                            //    FamilyParameter version = doc.FamilyManager.AddParameter("Version", BuiltInParameterGroup.PG_TEXT, ParameterType.Text, false);
                            //    FamilyParameter lastPublishedDate = doc.FamilyManager.AddParameter("Last Published Date", BuiltInParameterGroup.PG_TEXT, ParameterType.Text, false);
                            //    FamilyParameter publishedBy = doc.FamilyManager.AddParameter("Published by", BuiltInParameterGroup.PG_TEXT, ParameterType.Text, false);

                            //    doc.Regenerate();
                            //    doc.FamilyManager.Set(version, "0");
                            //    doc.FamilyManager.Set(lastPublishedDate, System.DateTime.Today.ToString());
                            //    doc.FamilyManager.Set(publishedBy, "Chenzhang Wang");
                            //    t.Commit();
                            //}
                            //catch
                            //{

                            //}
                            


                            // Save the Revit file to the target destination.
                            // Since we are opening a file as an active document, it takes care of preview. 
                            String destinationFile = destPath + "\\" + file.Name;
                            var versionIndex = destinationFile.Length - 8;

#if RevitFileUpgrade2023
                            var destionationFilePath = destinationFile.Remove(versionIndex, 4).Insert(versionIndex, "2023");
#elif RevitFileUpgrade2024
                            var destionationFilePath = destinationFile.Remove(versionIndex, 4).Insert(versionIndex, "2024");
#endif


                            doc.SaveAs(destionationFilePath);
                            //doc.SaveAs(destinationFile);

                            // Saving the current document to close it later.   
                            // If we had a method to close an active document, we want to close it here. However, since we opened it as an active document, we cannot do so.
                            // We'll close it after the next file is opened.
                            previousDocument = doc;

                            // Set variable to know if upgrade 
                            // was successful - for status updates
                            addInfo = true;
                        }
                        //// Try closing the previously opened document after another one is opened. We are doing this because we cannot explicitely close an active document at a moment.  
                        //if (previousDocument != null)
                        //{
                        //    bool saveModified = true;
                        //    previousDocument.Close(saveModified);
                        //}
                    }

                    

                    if (addInfo) ++success;
                    
                }
                catch (Exception ex)
                {
                    failures.Add(file.FullName
                      + " could not be upgraded: "
                      + ex.Message);

                    //bar.PerformStep();

                    ++failed;
                }
            }
        }
    }
}
