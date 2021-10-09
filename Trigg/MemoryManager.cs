using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ginWare
{
    internal class MemoryManager
    {
        public static int handle;

        public static Process csgo;


        public MemoryManager(int procHandle, Process process)
        {
            handle = procHandle;
            csgo = process;
        }

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size,
            int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, uint nSize,
            out IntPtr lpNumberOfBytesWritten);

        public int ReadInt(int address)
        {
            var buffer = new byte[4];
            ReadProcessMemory(handle, address, buffer, 4, 0);
            return BitConverter.ToInt32(buffer, 0);
        }

        public int ReadFloat(int address)
        {
            var buffer = new byte[4];
            ReadProcessMemory(handle, address, buffer, sizeof(float), 0);
            return BitConverter.ToInt32(buffer, 0);
        }

        public byte ReadByte(int address)
        {
            var buffer = new byte[1];
            ReadProcessMemory(handle, address, buffer, 1, 0);
            return buffer[0];
        }

        public bool ReadBool(int address)
        {
            var buffer = new byte[sizeof(bool)];
            var Zero = IntPtr.Zero;
            ReadProcessMemory(handle, address, buffer, sizeof(bool), 0);
            return Convert.ToBoolean(buffer[0]);
        }

        public void WriteInt(int address, int val)
        {
            var buffer = BitConverter.GetBytes(val);
            var Zero = IntPtr.Zero;
            WriteProcessMemory(handle, address, buffer, (uint) buffer.Length, out Zero);
        }

        public void WriteFloat(int address, float val)
        {
            var buffer = BitConverter.GetBytes(val);
            var Zero = IntPtr.Zero;
            WriteProcessMemory(handle, address, buffer, (uint) buffer.Length, out Zero);
        }

        public void WriteBool(int address, bool val)
        {
            var buffer = BitConverter.GetBytes(val);
            var Zero = IntPtr.Zero;
            WriteProcessMemory(handle, address, buffer, (uint) buffer.Length, out Zero);
        }
    }
}