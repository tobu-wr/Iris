﻿using Iris.CPU;
using System.Runtime.InteropServices;

namespace Iris.GBA
{
    internal sealed class BIOS_LLE : BIOS
    {
        private const int KB = 1024;
        private const int BIOS_Size = 16 * KB;
        private readonly IntPtr _bios = Marshal.AllocHGlobal(BIOS_Size); // TODO: free memory

        private CPU_Core _cpu;

        internal BIOS_LLE(string filename)
        {
            Byte[] data;

            try
            {
                data = File.ReadAllBytes(filename);
            }
            catch
            {
                throw new Exception("Iris.GBA.BIOS_LLE: Could not load BIOS");
            }

            if (data.Length != BIOS_Size)
                throw new Exception("Iris.GBA.BIOS_LLE: Wrong BIOS size");

            Marshal.Copy(data, 0, _bios, BIOS_Size);
        }

        internal override void Init(CPU_Core cpu, Memory memory)
        {
            _cpu = cpu;

            // TODO: map BIOS to memory
        }

        internal override void Reset()
        {
            _cpu.NextInstructionAddress = 0;
        }

        internal override byte Read8(uint address)
        {
            throw new NotImplementedException();
        }

        internal override ushort Read16(uint address)
        {
            throw new NotImplementedException();
        }

        internal override uint Read32(uint address)
        {
            throw new NotImplementedException();
        }

        internal override void HandleSWI(uint value)
        {
            throw new NotImplementedException();
        }

        internal override void HandleIRQ()
        {
            throw new NotImplementedException();
        }
    }
}
