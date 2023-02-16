namespace Iris.Core
{
    internal sealed partial class GBA
    {
        private void HandleSWI(UInt32 value)
        {
            Byte function = (Byte)((value >> 16) & 0xff);

            switch (function)
            {
                case 0x06:
                    Div();
                    break;

                default:
                    throw new Exception(string.Format("BIOS: Unknown function 0x{0:x2}", function));
            }
        }

        private void HandleIRQ()
        {
            throw new NotImplementedException("BIOS: HandleIRQ unimplemented");
        }

        private void Div()
        {
            Int32 number = (Int32)_cpu.Reg[0];
            Int32 denom = (Int32)_cpu.Reg[1];
            _cpu.Reg[0] = (UInt32)(number / denom);
            _cpu.Reg[1] = (UInt32)(number % denom);
            _cpu.Reg[3] = (UInt32)Math.Abs((Int32)_cpu.Reg[0]);
        }
    }
}
