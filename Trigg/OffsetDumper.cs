using System;
using System.Diagnostics;
using System.Text;

namespace ginWare
{
    internal class OffsetDumper
    {
        public static MemoryManager Mem;
        public static int handle;
        private static long ClientSize, EngineSize;
        private static int ClientBase, EngineBase;
        private static Scanner _sigScan;
        private static IntPtr pAddr;
        private static Process csgo;

        public OffsetDumper(int procHandle, int ClientBaseAddress, long ClientBaseSize, int EngineBaseAddress,
            long EngineBaseSize, Process process)
        {
            handle = procHandle;
            ClientBase = ClientBaseAddress;
            ClientSize = ClientBaseSize;
            EngineBase = EngineBaseAddress;
            EngineSize = EngineBaseSize;
            Mem = new MemoryManager(handle, Program.CSGO);
            csgo = process;
            _sigScan = new Scanner(csgo, IntPtr.Zero, 0xFFFF);
            pAddr = _sigScan.FindPattern(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0x51, 0x55, 0xFC, 0x11}, "xxxx?xx?", 12);
        }

        public int FindEntityList()
        {
            byte[] pattern =
            {
                0x05, 0x00, 0x00, 0x00, 0x00,
                0xc1, 0xe9, 0x00,
                0x39, 0x48, 0x04
            };
            var mask = MaskFromPattern(pattern);
            int address, val1, val2;

            address = FindAddress(pattern, 1, mask, ClientBase, ClientSize);
            val1 = Mem.ReadInt(address);
            address = FindAddress(pattern, 7, mask, ClientBase, ClientSize);
            val2 = Mem.ReadByte(address);
            val1 = val1 + val2 - ClientBase;
            return val1;
        }

        public int FindLocalPlayer()
        {
            byte[] pattern =
            {
                0x8D, 0x34, 0x85, 0x00, 0x00, 0x00, 0x00, //lea esi, [eax*4+client.dll+xxxx]
                0x89, 0x15, 0x00, 0x00, 0x00, 0x00, //mov [client.dll+xxxx],edx
                0x8B, 0x41, 0x08, //mov eax,[ecx+08]
                0x8B, 0x48, 0x00 //mov ecx,[eax+04]
            };
            var mask = MaskFromPattern(pattern);
            int address, val1, val2;

            address = FindAddress(pattern, 3, mask, ClientBase, ClientSize);
            val1 = Mem.ReadInt(address);
            address = FindAddress(pattern, 18, mask, ClientBase, ClientSize);
            val2 = Mem.ReadByte(address);
            val1 += val2;
            val1 -= ClientBase;

            return val1;
        }

        public int FindEnginePointer()
        {
            byte[] pattern =
            {
                0xC2, 0x00, 0x00,
                0xCC,
                0xCC,
                0x8B, 0x0D, 0x00, 0x00, 0x00, 0x00, //<<<<
                0x33, 0xC0,
                0x83, 0xB9
            };
            var mask = MaskFromPattern(pattern);
            int address, val1;

            address = FindAddress(pattern, 7, mask, EngineBase, EngineSize); //Find x1
            if (address != 0)
            {
                val1 = Mem.ReadInt(address); //Read x1
                address = val1 - EngineBase;
            }

            return address;
        }

        //public int FindCrosshairIndex()
        //{
        //	byte[] int1 = BitConverter.GetBytes(0x842A981E);
        //	byte[] int2 = BitConverter.GetBytes(0x682A981E);

        //	byte[] pattern = new byte[]{
        //		0x56,                           //push esi
        //		0x57,                           //push edi
        //		0x8B, 0xF9,                     //mov edi,ecx
        //		0xC7, 0x87, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  //mov [edi+xxxx], ????
        //		0x8B, 0x0D, 0x00, 0x00, 0x00, 0x0, //mov ecx,[client.dll+????]
        //		0x81, 0xF9, 0x00, 0x00, 0x00, 0x0, //cmp ecx, client.dll+????
        //		0x75, 0x07,                     //jne client.dll+????
        //		0xA1, 0x00, 0x00, 0x00, 0x00,   //mov eax,[client.dll+????]
        //		0xEB, 0x07                      //jmp client.dll+????
        //		};
        //	string mask = MaskFromPattern(pattern);
        //	int address, val1;

        //	address = FindAddress(pattern, 6, mask, ClientBase, ClientSize);
        //	val1 = Mem.ReadInt(address);
        //	//val1 -= localPlayer;

        //	return val1;
        //}

        private static ProcessModule GetModuleByName(Process process, string name)
        {
            try
            {
                foreach (ProcessModule module in process.Modules)
                    if (module.FileName.EndsWith(name))
                        return module;
            }
            catch
            {
            }

            return null;
        }

        private static long GetModuleSize(Process process, string name)
        {
            var module = GetModuleByName(process, name);
            if (module != null)
                return module.ModuleMemorySize;
            return 0L;
        }

        private static IntPtr GetModuleBaseAddressByName(Process process, string name)
        {
            var module = GetModuleByName(process, name);
            if (module != null)
                return module.BaseAddress;
            return IntPtr.Zero;
        }

        private static int FindAddress(byte[] pattern, int offset, string mask, int dllAddress, long dllSize,
            bool wNonZero = false)
        {
            var address = 0;
            for (var i = 0; i < dllSize && address == 0; i += 0xFFFF)
            {
                _sigScan.Address = new IntPtr(dllAddress + i);
                address = _sigScan.FindPattern(pattern, mask, offset, wNonZero).ToInt32();
                _sigScan.ResetRegion();
            }

            return address;
        }

        private static string MaskFromPattern(byte[] pattern)
        {
            var builder = new StringBuilder();
            foreach (var data in pattern)
                if (data == 0x00)
                    builder.Append('?');
                else
                    builder.Append('x');
            return builder.ToString();
        }
    }
}