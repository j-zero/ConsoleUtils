using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace free
{
    internal class Program
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);


        static void Main(string[] args)
        {
            long memKb;
            GetPhysicallyInstalledSystemMemory(out memKb);
            long installedRam = (memKb / 1024);

            //PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            //Console.WriteLine($"CPU Usage: {cpuCounter.NextValue()}%");
            Console.WriteLine($"RAM Usage: {ramCounter.NextValue()}/{installedRam}MB");
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady) {

                    //ObjectDumper.Dump(drive);

                    double percentFree = 100 * (double)drive.TotalFreeSpace / drive.TotalSize;

                    Console.WriteLine($"{drive.Name} - {drive.DriveType}: {String.Format("{0:0.00}", percentFree)}% free");

                    //Console.Write(drive.AvailableFreeSpace);
                    //Console.WriteLine(drive.TotalSize);
                }
            }
        }

        // based on https://stackoverflow.com/a/14488941
        private void _CalculateHumanReadableSize(Int64 value, out string _humanReadbleSize,out string _humanReadbleSizeSuffix, int factor = 1024, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            //if (value < 0) { return "-" + CalculateHumanReadableSize(-value, decimalPlaces); }
            if (value == 0)
            {
                _humanReadbleSize = "0";
                _humanReadbleSizeSuffix = "";
                return;
            }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, factor);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= factor;
            }

            _humanReadbleSize = string.Format("{0:n" + decimalPlaces + "}", adjustedSize);
            _humanReadbleSizeSuffix = SizeSuffixes[mag];
            /*
            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
            */
        }



        private static readonly string[] SizeSuffixes =
            { "", "k", "M", "G", "T", "P", "E", "Z", "Y" };


    }
}
