using System;
using System.Management;
using System.Diagnostics;

namespace USB
{
    class Program
    {
        static void Main(string[] args)
        {
            string key = "374088-663454-118019-167673-484022-649935-066671-461186";
            string driveLetter="";

            //Korisi se za detekciju kada je dodan novi disk (drive letter)
            ManagementEventWatcher watcherDisk = new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_DiskDrive'");

            watcherDisk.EventArrived += (sender, e) =>
            {
                driveLetter = ReadDriveLetter();
                BitLockerUnlock(driveLetter, key);
                watcherDisk.Stop();
            };

            //USB
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");

            ManagementEventWatcher watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += (sender, eventArgs) =>
            {
                Console.WriteLine("USB uređaj spojen.");           
                watcher.Stop();
                watcherDisk.Start();
            };

            watcher.Start();
            Console.WriteLine("Pritisnite Enter za otvaranje USB-a.");
            Console.ReadLine();
        }

        static void BitLockerUnlock(string drive, string key)
        {
            string command = $"Unlock-BitLocker -MountPoint \"{drive}\" -RecoveryPassword \"{key}\"";
            Console.WriteLine(command);

            //PowerShell
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "powershell.exe"; // Specify PowerShell executable
            psi.Arguments = $"-NoProfile -ExecutionPolicy unrestricted -Command \"{command}\""; // Specify PowerShell command
            psi.CreateNoWindow = false; // Do not create a window
            psi.RedirectStandardOutput = true; // Redirect standard output
            psi.RedirectStandardError = true; // Redirect standard error
            psi.UseShellExecute = false; // Do not use shell execution
                                         // Start the process
            Process process = new Process();
            process.StartInfo = psi;
            process.Start();

            // Read the output and error streams
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            // Wait for the process to exit
            process.WaitForExit();

            // Display output and error
            Console.WriteLine("Output:");
            Console.WriteLine(output);
            Console.WriteLine("Error:");
            Console.WriteLine(error);
        }

        static string ReadDriveLetter() 
        {
            SelectQuery query1 = new SelectQuery("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query1);

            foreach (ManagementObject disk in searcher.Get())
            {
                string diskSerial = disk["SerialNumber"].ToString();

                string assocQuery = $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{disk["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition";
                ManagementObjectSearcher assocSearcher = new ManagementObjectSearcher(assocQuery);

                foreach (ManagementObject assoc in assocSearcher.Get())
                {
                    string partQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{assoc["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition";
                    ManagementObjectSearcher partSearcher = new ManagementObjectSearcher(partQuery);

                    foreach (ManagementObject part in partSearcher.Get())
                    {
                        Console.WriteLine("Drive letter USB uređaja: " + part["DeviceID"]);
                        return part["DeviceID"].ToString();
                    }
                }
            }
            return null;
        }
    }
}
