using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ginWare
{
    internal class Scanner
    {
        private byte[] m_vDumpedRegion;

        public Scanner()
        {
            Process = null;
            Address = IntPtr.Zero;
            Size = 0;
            m_vDumpedRegion = null;
        }

        public Scanner(Process proc, IntPtr address, int size)
        {
            Process = proc;
            Address = address;
            Size = size;
        }

        public Process Process { get; set; }

        public IntPtr Address { get; set; }

        public int Size { get; set; }

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer,
            int dwSize, out int lpNumberOfBytesRead);

        private bool DumpMemory()
        {
            try
            {
                if (Process == null)
                    return false;
                if (Process.HasExited)
                    return false;
                if (Address == IntPtr.Zero)
                    return false;
                if (Size == 0)
                    return false;

                m_vDumpedRegion = new byte[Size];

                var bReturn = false;
                var nBytesRead = 0;

                bReturn = ReadProcessMemory(Process.Handle, Address, m_vDumpedRegion, Size, out nBytesRead);

                if (bReturn == false || nBytesRead != Size)
                    return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool MaskCheck(int nOffset, byte[] btPattern, string strMask)
        {
            for (var x = 0; x < btPattern.Length; x++)
            {
                if (strMask[x] == '?')
                    continue;

                if (strMask[x] == 'x' && btPattern[x] != m_vDumpedRegion[nOffset + x])
                    return false;
            }

            return true;
        }

        public IntPtr FindPattern(byte[] btPattern, string strMask, int nOffset, bool wNonZero = false)
        {
            try
            {
                if (m_vDumpedRegion == null || m_vDumpedRegion.Length == 0)
                    if (!DumpMemory())
                        return IntPtr.Zero;

                if (strMask.Length != btPattern.Length)
                    return IntPtr.Zero;

                for (var x = 0; x < m_vDumpedRegion.Length; x++)
                    if (MaskCheck(x, btPattern, strMask))
                        return new IntPtr((int) Address + x + nOffset);

                return IntPtr.Zero;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        public void ResetRegion()
        {
            m_vDumpedRegion = null;
        }
    }
}