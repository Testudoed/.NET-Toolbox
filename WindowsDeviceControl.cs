using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace QRC.ICS.Service
{
    class WindowsDeviceControl
    {
        #region Member Variables
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        public bool SetDeviceState(string deviceName, bool bEnable)
        {
            IntPtr hDevInfo = (IntPtr)INVALID_HANDLE_VALUE;
            string lcDeviceName = deviceName.ToLower();
            try
            {
                Guid myGUID = System.Guid.Empty;
                bool retVal = false;
                hDevInfo = SetupDiGetClassDevs(ref myGUID, 0, IntPtr.Zero, DIGCF_ALLCLASSES | DIGCF_PRESENT);
                if (hDevInfo.ToInt32() == INVALID_HANDLE_VALUE)
                {
                    return retVal;
                }
                SP_DEVINFO_DATA DeviceInfoData;
                DeviceInfoData = new SP_DEVINFO_DATA();
                DeviceInfoData.cbSize = (uint)Marshal.SizeOf(DeviceInfoData);
                UInt32 i;
                StringBuilder DeviceName = new StringBuilder("");
                DeviceName.Capacity = MAX_DEV_LEN;
                for (i = 0; SetupDiEnumDeviceInfo(hDevInfo, i, DeviceInfoData); i++)
                {
                    // Try to get Device Name Property
                    if (SetupDiGetDeviceRegistryProperty(hDevInfo, DeviceInfoData, SPDRP_DEVICEDESC, 0, DeviceName, MAX_DEV_LEN, IntPtr.Zero))
                    {
                        // If Device Matches requested device
                        if (DeviceName.ToString().ToLower().Contains(lcDeviceName))
                        {
                            // Try to disable device
                            if (EnableDisable(hDevInfo, DeviceInfoData, bEnable))
                            {
                                retVal = true;
                            }
                            break;
                        }
                    }
                }
                return retVal;
            }
            catch (Exception)
            {
                // Make sure Device List is cleaned up and re-throw
                throw;
            }
            finally
            {
                if (hDevInfo.ToInt32() != INVALID_HANDLE_VALUE)
                    SetupDiDestroyDeviceInfoList(hDevInfo);
            }
        }

        private bool EnableDisable(IntPtr hDevInfo, SP_DEVINFO_DATA devInfoData, bool bEnable)
        {
            IntPtr p_propertyChangeParams = (IntPtr)0;
            IntPtr p_devInfoData = (IntPtr)0;
            SP_PROPCHANGE_PARAMS propertyChangeParams;

            try
            {
                // Allocate unmanaged device info structure
                p_devInfoData = Marshal.AllocHGlobal(Marshal.SizeOf(devInfoData));
                // Copy managed structure to unmanaged
                Marshal.StructureToPtr(devInfoData, p_devInfoData, true);

                // Setup propertyChange Params
                propertyChangeParams = new SP_PROPCHANGE_PARAMS();
                propertyChangeParams.ClassInstallHeader.cbSize = Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER));
                propertyChangeParams.ClassInstallHeader.InstallFunction = DIF_PROPERTYCHANGE;
                propertyChangeParams.Scope = DICS_FLAG_CONFIGSPECIFIC;
                propertyChangeParams.HwProfile = 0;

                // Set Enable or Disable 
                propertyChangeParams.StateChange = bEnable ? DICS_START : DICS_STOP;

                // Allocate unmanaged property change param struture
                p_propertyChangeParams = Marshal.AllocHGlobal(Marshal.SizeOf(propertyChangeParams));
                // Copy managed structure to unmanaged
                Marshal.StructureToPtr(propertyChangeParams, p_propertyChangeParams, true);

                if (SetupDiSetClassInstallParams(hDevInfo, p_devInfoData, p_propertyChangeParams, Marshal.SizeOf(typeof(SP_PROPCHANGE_PARAMS))))
                    if (SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, hDevInfo, p_devInfoData))
                        return true;
                return false;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                // Clean up allocated memory
                if ((int)p_propertyChangeParams != 0)
                    Marshal.FreeHGlobal(p_propertyChangeParams);
                if ((int)p_devInfoData != 0)
                    Marshal.FreeHGlobal(p_devInfoData);
            }
        }
        #endregion

        #region Enums
        #endregion

        #region SetupApi.dll Import
        private const int DIGCF_ALLCLASSES = (0x00000004);
        private const int DIGCF_PRESENT = (0x00000002);
        private const int SPDRP_DEVICEDESC = (0x00000000);
        private const int INVALID_HANDLE_VALUE = -1;
        private const int MAX_DEV_LEN = 1000;
        public const int DIF_PROPERTYCHANGE = (0x00000012);
        public const int DICS_FLAG_GLOBAL = (0x00000001);
        public const int DICS_FLAG_CONFIGSPECIFIC = (0x00000002);
        public const int DICS_ENABLE = (0x00000001);
        public const int DICS_DISABLE = (0x00000002);
        public const int DICS_START = (0x00000004);
        public const int DICS_STOP = (0x00000005);

        //SP_DEVINFO_DATA
        [StructLayout(LayoutKind.Sequential)]
        public class SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid classGuid;
            public uint devInst;
            public IntPtr reserved;
        };

        [StructLayout(LayoutKind.Sequential)]
        public class SP_CLASSINSTALL_HEADER
        {
            public int cbSize;
            public int InstallFunction;
        }; 

        [StructLayout(LayoutKind.Sequential)]
        public class SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader = new SP_CLASSINSTALL_HEADER();
            public int StateChange;
            public int Scope;
            public int HwProfile;
        };

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(ref Guid gClass, UInt32 iEnumerator, IntPtr hParent, UInt32 nFlags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(IntPtr lpInfoSet, UInt32 dwIndex, SP_DEVINFO_DATA devInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(IntPtr lpInfoSet, SP_DEVINFO_DATA DeviceInfoData, UInt32 Property, UInt32 PropertyRegDataType, StringBuilder PropertyBuffer, UInt32 PropertyBufferSize, IntPtr RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int SetupDiDestroyDeviceInfoList(IntPtr lpInfoSet);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, IntPtr ClassInstallParams, int ClassInstallParamsSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern Boolean SetupDiCallClassInstaller(UInt32 InstallFunction, IntPtr DeviceInfoSet, IntPtr DeviceInfoData);
        #endregion
    }
}
