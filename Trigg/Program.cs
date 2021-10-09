using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ginWare
{
    internal class Program
    {
        // These almost never change
        // Getting them dynamically with OffsetDumper and Scanner would slow down the Initialization for almost no reason
        private const int TeamNum = 0xF4;
        private const int Spotted = 0x93D;

        private const int ToggleExit = 0x2E; // Delete
        private const int ToggleTrig = 0x2D; // Insert

        private static int Client;
        private static int pHandle;
        public static int ClientBase = 0;
        public static int EngineBase;
        public static long ClientSize, EngineSize;
        public static Process CSGO;
        public static OffsetDumper Dumper;
        public static MemoryManager Mem;

        // Addresses
        public static int oLP;
        public static int EntList;
        public static int localPlayer;
        public static int localTeam;

        public static int MaxPlayerCount = 65;

        private static bool TrigState;

        [DllImport("kernel32.dll")]
        public static extern int OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size,
            int lpNumberOfBytesRead);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, uint nSize,
            out IntPtr lpNumberOfBytesWritten);

        private static void Main(string[] args)
        {
            var gameFound = false;
            while (true)
            {
                if (gameFound == false)
                {
                    foreach (var p in Process.GetProcesses())
                        if (p.ProcessName.Equals("csgo"))
                        {
                            CSGO = p;
                            pHandle = OpenProcess(0x001F0FF, false, p.Id);
                            foreach (ProcessModule mod in p.Modules)
                            {
                                if (mod.ModuleName.Equals("client_panorama.dll"))
                                {
                                    Client = (int) mod.BaseAddress;
                                    ClientSize = mod.ModuleMemorySize;
                                    Dumper = new OffsetDumper(pHandle, Client, ClientSize, EngineBase, EngineSize,
                                        CSGO);
                                    Mem = new MemoryManager(pHandle, CSGO);

                                    InitializeOffsets();

                                    gameFound = true;
                                }

                                if (mod.ModuleName.Equals("engine.dll"))
                                {
                                    EngineBase = (int) mod.BaseAddress;
                                    EngineSize = mod.ModuleMemorySize;
                                }
                            }
                        }
                }
                else
                {
                    // Set addresses
                    localPlayer = Mem.ReadInt(Client + oLP);
                    localTeam = Mem.ReadInt(localPlayer + TeamNum);
                    if (GetAsyncKeyState(ToggleTrig) != 0)
                    {
                        TrigState = !TrigState;
                        Thread.Sleep(500);
                    }

                    if (GetAsyncKeyState(ToggleExit) != 0) Environment.Exit(0);

                    if (TrigState)
                        Radar();
                }

                Thread.Sleep(1);
            }
        }

        public static void InitializeOffsets()
        {
            oLP = Dumper.FindLocalPlayer();
            EntList = Dumper.FindEntityList();
        }

        public static void Radar()
        {
            while (true)
            {
                for (var i = 1; i < 64; i++)
                {
                    var Player = Mem.ReadInt(Client + EntList + (i - 1) * 0x10);
                    var mTeam = Mem.ReadInt(localPlayer + TeamNum);
                    var eTeam = Mem.ReadInt(Player + TeamNum);
                    if (mTeam != eTeam)
                    {
                        Mem.WriteInt(Player + Spotted, 1);
                        Thread.Sleep(1);
                    }
                }

                if (GetAsyncKeyState(ToggleTrig) != 0)
                {
                    TrigState = !TrigState;
                    Thread.Sleep(500);
                    break;
                }

                Thread.Sleep(30);
            }
        }
    }
}