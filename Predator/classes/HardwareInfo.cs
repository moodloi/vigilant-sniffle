using System;
using System.Management;

namespace Predator
{
    public static class HardwareInfo
    {
        public static string GetHDDSerialNo()
        {
            var mangnmt = new ManagementClass("Win32_LogicalDisk");
            var mcol = mangnmt.GetInstances();
            var result = "";
            foreach (ManagementObject strt in mcol) result += Convert.ToString(strt["VolumeSerialNumber"]);
            return result;
        }

        public static string CPU()
        {
            var mangnmt = new ManagementClass("Win32_Processor");
            var mcol = mangnmt.GetInstances();
            var result = "";
            foreach (ManagementObject strt in mcol) result += Convert.ToString(strt["Name"]);
            return result;
        }

        public static string DiskSize()
        {
            var mangnmt = new ManagementClass("Win32_DiskDrive");
            var mcol = mangnmt.GetInstances();
            var result = "";
            foreach (ManagementObject strt in mcol) result += Convert.ToString(strt["Size"]);
            return result;
        }

        public static string VideoAdapter()
        {
            var mangnmt = new ManagementClass("Win32_VideoController");
            var mcol = mangnmt.GetInstances();
            var result = "";
            foreach (ManagementObject strt in mcol) result += Convert.ToString(strt["Name"]);
            return result;
        }

        public static string ResolutionH()
        {
            var mangnmt = new ManagementClass("Win32_VideoController");
            var mcol = mangnmt.GetInstances();
            var result = "";
            foreach (ManagementObject strt in mcol) result += Convert.ToString(strt["VideoModeDescription"]);
            return result;
        }

        public static string RefreshRate()
        {
            var mangnmt = new ManagementClass("Win32_DisplayControllerConfiguration");
            var mcol = mangnmt.GetInstances();
            var result = "";
            foreach (ManagementObject strt in mcol) result += Convert.ToString(strt["RefreshRate"]);
            return result;
        }

        public static string GetMACAddress()
        {
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var moc = mc.GetInstances();
            var MACAddress = string.Empty;
            foreach (ManagementObject mo in moc)
            {
                if (MACAddress == string.Empty)
                    if ((bool)mo["IPEnabled"])
                        MACAddress = mo["MacAddress"].ToString();
                mo.Dispose();
            }

            MACAddress = MACAddress.Replace(":", "");
            return MACAddress;
        }

        public static string GetPhysicalMemory()
        {
            var oMs = new ManagementScope();
            var oQuery = new ObjectQuery("SELECT Capacity FROM Win32_PhysicalMemory");
            var oSearcher = new ManagementObjectSearcher(oMs, oQuery);
            var oCollection = oSearcher.Get();
            long MemSize = 0;
            long mCap = 0;
            foreach (ManagementObject obj in oCollection)
            {
                mCap = Convert.ToInt64(obj["Capacity"]);
                MemSize += mCap;
            }

            MemSize = MemSize / 1024 / 1024;
            return MemSize + "MB";
        }

        public static string GetComputerName()
        {
            var mc = new ManagementClass("Win32_ComputerSystem");
            var moc = mc.GetInstances();
            var info = string.Empty;
            foreach (ManagementObject mo in moc) info = (string)mo["Name"];
            return info;
        }

        public static string OSname()
        {
            var mc = new ManagementClass("Win32_OperatingSystem");
            var moc = mc.GetInstances();
            var info = string.Empty;
            foreach (ManagementObject mo in moc) info = (string)mo["Caption"];
            return info;
        }
    }
}