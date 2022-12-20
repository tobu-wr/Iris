using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iris
{
    public class GBA
    {
        private readonly Memory memory = new();
        private readonly CPU cpu;

        public GBA()
        {
            this.cpu = new CPU(memory.Read16, memory.Read32, memory.Write, 0x0800_0000);
        }

        public void LoadROM(string filename)
        {
            memory.LoadROM(filename);
        }

        public void Run()
        {
            while (true)
            {
                cpu.Step();
            }
        }
    }
}
