using System;
using System.Linq;
using System.IO;
using System.IO.Compression;

namespace RenameNEAFiles
{
    class Program
    {
        static void Main(string[] args)
        {

            //bool debug = true;
            bool debug = false;
            if (debug) Console.WriteLine("Starting program");

            // Validate command line args
            // RenameNEAFiles filename
            if (args.Count() < 1)
            {
                if (debug) Console.WriteLine("This program requires a filename and optionally boolean for debug on command line");
                Environment.Exit(1);
            }
            string inFile = args[0];
            string dbg = "";
            if (args.Count() == 2) dbg = args[1];
            if (dbg == "true") debug = true;

            string workFolder = "C:\\ProgramData\\IFH\\work\\";
            string outputFolder = "C:\\ProgramData\\IFH\\work\\done\\";
            string dateStamp = DateTime.Now.ToString("yyyyMMdd");
            string logFileName = "C:\\ProgramData\\IFH\\logs\\RenameNEAFiles_" + dateStamp + ".log";

            // Verify folders
            if (!Directory.Exists("C:\\ProgramData\\IFH\\"))
            {
                try
                {
                    Directory.CreateDirectory("C:\\ProgramData\\IFH");
                }
                catch (Exception e)
                {
                    if (debug) Console.WriteLine("Unable to create C:\\ProgramData\\IFH folder. Error : " + e.Message.ToString());
                    Environment.Exit(1);
                }
            }
            if (!Directory.Exists("C:\\ProgramData\\IFH\\work\\"))
            {
                try
                {
                    Directory.CreateDirectory("C:\\ProgramData\\IFH\\work");
                }
                catch (Exception e)
                {
                    if (debug) Console.WriteLine("Unable to create C:\\ProgramData\\IFH\\work folder. Error : " + e.Message.ToString());
                    Environment.Exit(1);
                }
            }
            if (!Directory.Exists("C:\\ProgramData\\IFH\\work\\done\\"))
            {
                try
                {
                    Directory.CreateDirectory("C:\\ProgramData\\IFH\\work\\done");
                }
                catch (Exception e)
                {
                    if (debug) Console.WriteLine("Unable to create C:\\ProgramData\\IFH\\work\\done folder. Error : " + e.Message.ToString());
                    Environment.Exit(1);
                }
            }
            if (!Directory.Exists("C:\\ProgramData\\IFH\\logs\\"))
            {
                try
                {
                    Directory.CreateDirectory("C:\\ProgramData\\IFH\\logs");
                }
                catch (Exception e)
                {
                    if (debug) Console.WriteLine("Unable to create C:\\ProgramData\\IFH\\logs folder. Error : " + e.Message.ToString());
                    Environment.Exit(1);
                }
            }
            
            // Verify Can write to log
            StreamWriter logFile = null;
            try
            {
                logFile = File.AppendText(logFileName);
                LogIt("RenameNEAFiles Executed at " + DateTime.Now.ToString("yyyyMMdd hh:mm:ss"));
            }
            catch (Exception ex)
            {
                if (debug) Console.WriteLine("Unable to write to log file");
                if (debug) Console.ReadKey();
                logFile.Close();
                Environment.Exit(1);
            }

            // Verify workFolder - If Mirth called this pgm then it exists but just to make sure ...
            if (!Directory.Exists(workFolder))
            {
                LogIt("Work Folder " + workFolder + " does not exist");
                try
                {
                    Directory.CreateDirectory(workFolder);
                }
                catch (Exception e)
                {
                    if (debug) Console.WriteLine("Unable to create " + workFolder + " Error : " + e.Message.ToString());
                    LogIt("Unable to create " + workFolder);
                    if (debug) Console.ReadKey();
                    logFile.Close();
                    Environment.Exit(1);
                }
            }

            // Verify inFile
            if (!File.Exists(workFolder + inFile))
            {
                LogIt("inFile does not exist : " + workFolder + inFile);
                if (debug) Console.ReadKey();
                logFile.Close();
                Environment.Exit(1);
            }

            // Verify Output folder exists
            if (!Directory.Exists(outputFolder))
            {
                LogIt("Ouput folder " + outputFolder + " does not exist or is inaccessible");
                if (debug) Console.ReadKey();
                logFile.Close();
                Environment.Exit(1);
            }

            if (debug)
            {
                LogIt("Original Filenames");
                using (ZipArchive inZip = ZipFile.OpenRead(workFolder + inFile))
                {
                    foreach (ZipArchiveEntry entry in inZip.Entries)
                    {
                        LogIt(entry.FullName.ToString());
                        LogIt(entry.Name);
                    }
                }
            }


            // Everything validated, now can start real work

            string filePrefix = "";

            if (debug)
            {
                // Open Zip Archive
                using (ZipArchive inZip = ZipFile.OpenRead(workFolder + inFile))
                {
                    foreach (ZipArchiveEntry entry in inZip.Entries)
                    {
                        if (entry.Name.Substring(0, 1).ToString() == "_")
                        {
                            int breakpos = nthIndexOf(entry.Name, '-', 3);
                            filePrefix = entry.Name.Substring(0, breakpos);
                        }
                        LogIt(entry.Name);
                    }
                }
            }


            using (var archive = new ZipArchive(File.Open(workFolder + inFile, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update))
            {
                string newName = "";
                string ftype = "";
                int numXrays = 1;
                var entries = archive.Entries.ToArray();
                foreach (var entry in entries)
                {

                    ftype = fileType(entry.Name.ToString());
                    if (entry.Name.Substring(0, 1).ToString() == "_") newName = filePrefix + "Periodontal Charting" + ftype;
                    else
                    {
                        newName = filePrefix + "XRAY-" + numXrays.ToString() + ftype;
                        numXrays++;
                    }
                    var newEntry = archive.CreateEntry(newName);
                    using (var a = entry.Open())
                    using (var b = newEntry.Open())
                        a.CopyTo(b);
                    entry.Delete();
                }
            }

            File.Copy(workFolder + inFile, outputFolder + inFile,true);
            File.Delete(workFolder + inFile);
            if (debug)
            {
                LogIt("Aftermath");
                using (ZipArchive inZip = ZipFile.OpenRead(outputFolder + inFile))
                {
                    foreach (ZipArchiveEntry entry in inZip.Entries)
                    {
                        LogIt(entry.Name);
                    }
                }
            }


            logFile.Close();
            if (debug) Console.ReadKey();
            Environment.Exit(1);


            bool LogIt(string msg)
            {
                string TimeStamp = DateTime.Now.ToString("yyyyMMdd hh:mm:ss");
                if (logFile.BaseStream.CanWrite)
                {
                    logFile.WriteLine(TimeStamp + " - " + msg);
                    if (debug) Console.WriteLine(TimeStamp + " - " + msg);
                    return true;
                }
                else
                {
                    if (debug) Console.WriteLine(TimeStamp + "Unable to write to Log File");
                    return false;
                }
            }

            int nthIndexOf(string s, char c, int x)
            {
                int n = 0;
                int pos;
                for (pos = 0; (pos < s.Length) && (n < x); pos++)
                {
                    if (s[pos] == c) n++;
                }

                return pos;
            }

            string fileType(string s)
            {
                bool found = false;
                int pos;
                for (pos = s.Length - 1; found == false; pos--)
                {
                    if (s[pos] == '.') found = true;
                }

                return s.Substring(pos + 1);
            }
        }

    }
}