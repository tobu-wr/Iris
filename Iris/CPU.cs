using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Iris
{
    public class CPU
    {
        public interface ICallbacks
        {
            Byte ReadMemory8(UInt32 address);
            UInt16 ReadMemory16(UInt32 address);
            UInt32 ReadMemory32(UInt32 address);
            void WriteMemory8(UInt32 address, Byte value);
            void WriteMemory16(UInt32 address, UInt16 value);
            void WriteMemory32(UInt32 address, UInt32 value);
        }

        private readonly struct ARM_InstructionTableEntry
        {
            public delegate void InstructionHandler(CPU cpu, UInt32 instruction);

            public readonly UInt32 Mask;
            public readonly UInt32 Expected;
            public readonly InstructionHandler Handler;

            public ARM_InstructionTableEntry(UInt32 mask, UInt32 expected, InstructionHandler handler)
            {
                Mask = mask;
                Expected = expected;
                Handler = handler;
            }
        }

        private static readonly ARM_InstructionTableEntry[] ARM_InstructionTable = new ARM_InstructionTableEntry[]
        {
            // ADC
            new(0x0fe0_0000, 0x02a0_0000, ARM_ADC), // I bit is 1
            new(0x0fe0_0090, 0x00a0_0000, ARM_ADC), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x00a0_0080, ARM_ADC), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x00a0_0010, ARM_ADC), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // ADD
            new(0x0fe0_0000, 0x0280_0000, ARM_ADD), // I bit is 1
            new(0x0fe0_0090, 0x0080_0000, ARM_ADD), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0080_0080, ARM_ADD), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0080_0010, ARM_ADD), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // AND
            new(0x0fe0_0000, 0x0200_0000, ARM_AND), // I bit is 1
            new(0x0fe0_0090, 0x0000_0000, ARM_AND), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0000_0080, ARM_AND), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0000_0010, ARM_AND), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // BIC
            new(0x0fe0_0000, 0x03c0_0000, ARM_BIC), // I bit is 1
            new(0x0fe0_0090, 0x01c0_0000, ARM_BIC), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x01c0_0080, ARM_BIC), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x01c0_0010, ARM_BIC), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // CMN
            new(0x0ff0_0000, 0x0370_0000, ARM_CMN), // I bit is 1
            new(0x0ff0_0090, 0x0170_0000, ARM_CMN), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_0090, 0x0170_0080, ARM_CMN), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_0090, 0x0170_0010, ARM_CMN), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // CMP
            new(0x0ff0_0000, 0x0350_0000, ARM_CMP), // I bit is 1
            new(0x0ff0_0090, 0x0150_0000, ARM_CMP), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0ff0_0090, 0x0150_0080, ARM_CMP), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0ff0_0090, 0x0150_0010, ARM_CMP), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // EOR
            new(0x0fe0_0000, 0x0220_0000, ARM_EOR), // I bit is 1
            new(0x0fe0_0090, 0x0020_0000, ARM_EOR), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0020_0080, ARM_EOR), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0020_0010, ARM_EOR), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // MLA
            new(0x0fe0_00f0, 0x0020_0090, ARM_MLA),

            // MOV
            new(0x0fe0_0000, 0x03a0_0000, ARM_MOV), // I bit is 1
            new(0x0fe0_0090, 0x01a0_0000, ARM_MOV), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x01a0_0080, ARM_MOV), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x01a0_0010, ARM_MOV), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // MRS
            new(0x0fb0_0000, 0x0100_0000, ARM_MRS),

            // MSR
            new(0x0fb0_0000, 0x0320_0000, ARM_MSR), // Immediate operand
            new(0x0fb0_00f0, 0x0120_0000, ARM_MSR), // Register operand

            // MUL
            new(0x0fe0_00f0, 0x0000_0090, ARM_MUL),

            // MVN
            new(0x0fe0_0000, 0x03e0_0000, ARM_MVN), // I bit is 1
            new(0x0fe0_0090, 0x01e0_0000, ARM_MVN), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x01e0_0080, ARM_MVN), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x01e0_0010, ARM_MVN), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // ORR
            new(0x0fe0_0000, 0x0380_0000, ARM_ORR), // I bit is 1
            new(0x0fe0_0090, 0x0180_0000, ARM_ORR), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x0180_0080, ARM_ORR), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x0180_0010, ARM_ORR), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // RSC
            new(0x0fe0_0000, 0x02e0_0000, ARM_RSC), // I bit is 1
            new(0x0fe0_0090, 0x00e0_0000, ARM_RSC), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x00e0_0080, ARM_RSC), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x00e0_0010, ARM_RSC), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // SBC
            new(0x0fe0_0000, 0x02c0_0000, ARM_SBC), // I bit is 1
            new(0x0fe0_0090, 0x00c0_0000, ARM_SBC), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x00c0_0080, ARM_SBC), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x00c0_0010, ARM_SBC), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // SMLAL
            new(0x0fe0_00f0, 0x00e0_0090, ARM_SMLAL),

            // SMULL
            new(0x0fe0_00f0, 0x00c0_0090, ARM_SMULL),

            // SWP
            new(0x0ff0_00f0, 0x0100_0090, ARM_SWP),

            // SWPB
            new(0x0ff0_00f0, 0x0140_0090, ARM_SWPB),

            // UMLAL
            new(0x0fe0_00f0, 0x00a0_0090, ARM_UMLAL),

            // UMULL
            new(0x0fe0_00f0, 0x0080_0090, ARM_UMULL),
        };

        private enum Flags
        {
            V = 28,
            C = 29,
            Z = 30,
            N = 31
        };

        private const UInt32 UserMode = 0b1_0000;
        private const UInt32 SystemMode = 0b1_1111;
        private const UInt32 SupervisorMode = 0b1_0011;
        private const UInt32 AbortMode = 0b1_0111;
        private const UInt32 UndefinedMode = 0b1_1011;
        private const UInt32 InterruptMode = 0b1_0010;
        private const UInt32 FastInterruptMode = 0b1_0001;

        private const UInt32 SP = 13;
        private const UInt32 LR = 14;
        private const UInt32 PC = 15;

        // exposed registers
        private readonly UInt32[] _reg = new UInt32[16];
        private UInt32 _cpsr;
        private UInt32 _spsr;

        // banked registers
        private UInt32 _reg8, _reg9, _reg10, _reg11, _reg12, _reg13, _reg14;
        private UInt32 _reg13_svc, _reg14_svc;
        private UInt32 _reg13_abt, _reg14_abt;
        private UInt32 _reg13_und, _reg14_und;
        private UInt32 _reg13_irq, _reg14_irq;
        private UInt32 _reg8_fiq, _reg9_fiq, _reg10_fiq, _reg11_fiq, _reg12_fiq, _reg13_fiq, _reg14_fiq;
        private UInt32 _spsr_svc, _spsr_abt, _spsr_und, _spsr_irq, _spsr_fiq;

        private readonly ICallbacks _callbacks;
        private UInt32 _nextInstructionAddress;

        public CPU(ICallbacks callbacks)
        {
            _callbacks = callbacks;
        }

        public void Init(UInt32 pc, UInt32 cpsr)
        {
            _nextInstructionAddress = pc;
            _reg[PC] = _nextInstructionAddress + 4;

            _cpsr = cpsr;
        }

        public void Step()
        {
            if (((_cpsr >> 5) & 1) == 0) // ARM mode
            {
                if (_reg[PC] != _nextInstructionAddress + 4)
                {
                    _nextInstructionAddress = _reg[PC];
                }

                UInt32 instruction = _callbacks.ReadMemory32(_nextInstructionAddress);
                _nextInstructionAddress += 4;
                _reg[PC] = _nextInstructionAddress + 4;

                foreach (ARM_InstructionTableEntry entry in ARM_InstructionTable)
                {
                    if ((instruction & entry.Mask) == entry.Expected)
                    {
                        entry.Handler(this, instruction);
                        return;
                    }
                }

                switch ((instruction >> 25) & 0b111)
                {
                    case 0b000:
                        // Multiplies & Extra load/stores
                        if ((instruction & 0x0000_0090) == 0x0000_0090)
                        {
                            // Multiplies
                            if (((instruction >> 24) & 1) == 0 && ((instruction >> 5) & 0b11) == 0)
                            {
                                Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                Environment.Exit(1);
                            }

                            // Extra load/stores
                            else
                            {
                                if (((instruction >> 6) & 1) == 0)
                                {
                                    if (((instruction >> 5) & 1) == 0)
                                    {
                                        Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                        Environment.Exit(1);
                                    }
                                    else
                                    {
                                        if (((instruction >> 22) & 1) == 0)
                                        {
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                        }
                                        else
                                        {
                                            UInt32 p = (instruction >> 24) & 1;
                                            UInt32 w = (instruction >> 21) & 1;
                                            UInt32 l = (instruction >> 20) & 1;
                                            if (l == 0)
                                            {
                                                switch ((p << 1) | w)
                                                {
                                                    case 0b00:
                                                        ARM_StoreRegisterHalfWord_ImmediatePostIndexed(instruction);
                                                        break;

                                                    case 0b10:
                                                        ARM_StoreRegisterHalfWord_ImmediateOffset(instruction);
                                                        break;

                                                    default:
                                                        Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                                        Environment.Exit(1);
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                switch ((p << 1) | w)
                                                {
                                                    case 0b00:
                                                        ARM_LoadRegisterHalfWord_ImmediatePostIndexed(instruction);
                                                        break;

                                                    case 0b10:
                                                        ARM_LoadRegisterHalfWord_ImmediateOffset(instruction);
                                                        break;

                                                    default:
                                                        Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                                        Environment.Exit(1);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                            }
                        }

                        // Miscellaneous instructions
                        else if ((instruction & 0x0190_0090) == 0x0100_0010)
                        {
                            if ((instruction & 0x0ff0_00f0) == 0x0120_0010)
                            {
                                ARM_BranchExchange(instruction);
                            }
                            else
                            {
                                Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                Environment.Exit(1);
                            }
                        }

                        // Miscellaneous instructions
                        else if ((instruction & 0x0190_0010) == 0x0100_0000)
                        {
                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                            Environment.Exit(1);
                        }

                        // Data processing register shift
                        else if ((instruction & 0x0000_0090) == 0x0000_0010)
                        {
                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                            Environment.Exit(1);
                        }
                        break;

                    // Data processing immediate
                    case 0b001:
                        {
                            UInt32 opcode = (instruction >> 21) & 0b1111;
                            switch (opcode)
                            {
                                case 0b0010:
                                    ARM_Subtract_Immediate(instruction);
                                    break;

                                case 0b1000:
                                    ARM_Test_Immediate(instruction);
                                    break;

                                case 0b1001:
                                    ARM_TestEquivalence_Immediate(instruction);
                                    break;

                                default:
                                    Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                    break;
                            }
                            break;
                        }

                    // Load/store immediate offset
                    case 0b010:
                        {
                            UInt32 p = (instruction >> 24) & 1;
                            UInt32 b = (instruction >> 22) & 1;
                            UInt32 w = (instruction >> 21) & 1;
                            UInt32 l = (instruction >> 20) & 1;
                            if (b == 1)
                            {
                                if (l == 1)
                                {
                                    switch ((p << 1) | w)
                                    {
                                        case 0b00:
                                            ARM_LoadRegisterByte_ImmediatePostIndexed(instruction);
                                            break;

                                        case 0b10:
                                            ARM_LoadRegisterByte_ImmediateOffset(instruction);
                                            break;

                                        case 0b11:
                                            //ARM_LoadRegisterByte_ImmediatePreIndexed(instruction);
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;
                                    }
                                }
                                else
                                {
                                    switch ((p << 1) | w)
                                    {
                                        case 0b00:
                                            // ARM_StoreRegisterByte_ImmediatePostIndexed(instruction);
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        case 0b10:
                                            ARM_StoreRegisterByte_ImmediateOffset(instruction);
                                            break;

                                        case 0b11:
                                            //ARM_LoadRegisterByte_ImmediatePreIndexed(instruction);
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                if (l == 1)
                                {
                                    switch ((p << 1) | w)
                                    {
                                        case 0b00:
                                            ARM_LoadRegister_ImmediatePostIndexed(instruction);
                                            break;

                                        case 0b10:
                                            ARM_LoadRegister_ImmediateOffset(instruction);
                                            break;

                                        case 0b11:
                                            //ARM_LoadRegister_ImmediatePreIndexed(instruction);
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;
                                    }
                                }
                                else
                                {
                                    switch ((p << 1) | w)
                                    {
                                        case 0b00:
                                            ARM_StoreRegister_ImmediatePostIndexed(instruction);
                                            break;

                                        case 0b10:
                                            ARM_StoreRegister_ImmediateOffset(instruction);
                                            break;

                                        case 0b11:
                                            //ARM_StoreRegister_ImmediatePreIndexed(instruction);
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;
                                    }
                                }
                            }
                            break;
                        }

                    // Load/store multiple
                    case 0b100:
                        {
                            UInt32 l = (instruction >> 20) & 1;
                            UInt32 s = (instruction >> 22) & 1;
                            UInt32 pu = (instruction >> 23) & 0b11;
                            if (l == 0)
                            {
                                if (s == 0)
                                {
                                    switch (pu)
                                    {
                                        case 0b10:
                                            ARM_StoreMultiple_FullDescending(instruction);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                            }
                            else
                            {
                                if (s == 0)
                                {
                                    switch (pu)
                                    {
                                        case 0b01:
                                            ARM_LoadMultiple_FullDescending(instruction);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                            }
                            break;
                        }

                    // Branch and branch with link
                    case 0b101:
                        {
                            UInt32 l = (instruction >> 24) & 1;
                            if (l == 1)
                            {
                                ARM_BranchLink(instruction);
                            }
                            else
                            {
                                ARM_Branch(instruction);
                            }
                            break;
                        }

                    // Unknown
                    default:
                        Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                        Environment.Exit(1);
                        break;
                }
            }
            else // THUMB mode
            {
                if (_reg[PC] != _nextInstructionAddress + 2)
                {
                    _nextInstructionAddress = _reg[PC];
                }

                UInt16 instruction = _callbacks.ReadMemory16(_nextInstructionAddress);
                _nextInstructionAddress += 2;
                _reg[PC] = _nextInstructionAddress + 2;

                switch ((instruction >> 13) & 0b111)
                {
                    case 0b000:
                        {
                            UInt16 opcode = (UInt16)((instruction >> 11) & 0b11);

                            // Shift by immediate
                            if (opcode != 0b11)
                            {
                                THUMB_LogicalShiftLeft_Immediate(instruction);
                            }
                            else
                            {
                                // Add/subtract register
                                if (((instruction >> 10) & 1) == 0)
                                {
                                    UInt16 opc = (UInt16)((instruction >> 9) & 1);
                                    if (opc == 0)
                                    {
                                        THUMB_Add_Register(instruction);
                                    }
                                    else
                                    {
                                        THUMB_Sub_Register(instruction);
                                    }
                                }

                                // Add/subtract immediate
                                else
                                {
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                            }
                            break;
                        }

                    // Add/subtract/compare/move immediate
                    case 0b001:
                        {
                            UInt16 opcode = (UInt16)((instruction >> 11) & 0b11);
                            switch (opcode)
                            {
                                case 0b00:
                                    THUMB_Move_Immediate(instruction);
                                    break;

                                case 0b11:
                                    THUMB_Subtract_Immediate(instruction);
                                    break;

                                default:
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                    break;
                            }
                            break;
                        }

                    case 0b010:
                        // Load/store register offset
                        if (((instruction >> 12) & 1) == 1)
                        {
                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                            Environment.Exit(1);
                        }
                        else
                        {
                            // Load from literal pool
                            if (((instruction >> 11) & 1) == 1)
                            {
                                THUMB_LoadRegister_PCRelative(instruction);
                            }
                            else
                            {
                                // Data-processing register
                                if (((instruction >> 10) & 1) == 0)
                                {
                                    UInt16 opcode = (UInt16)((instruction >> 6) & 0b1111);
                                    switch (opcode)
                                    {
                                        case 0b1110:
                                            THUMB_BitClear(instruction);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;
                                    }
                                }
                                else
                                {
                                    // Branch/exchange instruction set
                                    if (((instruction >> 8) & 0b11) == 0b11)
                                    {
                                        if (((instruction >> 7) & 1) == 1)
                                        {
                                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                            Environment.Exit(1);
                                        }
                                        else
                                        {
                                            THUMB_BranchExchange(instruction);
                                        }
                                    }

                                    // Special data processing
                                    else
                                    {

                                        UInt16 opcode = (UInt16)((instruction >> 8) & 0b11);
                                        switch (opcode)
                                        {
                                            case 0b10:
                                                THUMB_Move(instruction);
                                                break;

                                            default:
                                                Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                                Environment.Exit(1);
                                                break;
                                        }

                                    }
                                }
                            }
                        }
                        break;

                    // Load/store word/byte immediate offset
                    case 0b011:
                        {
                            UInt16 b = (UInt16)((instruction >> 12) & 1);
                            UInt16 l = (UInt16)((instruction >> 11) & 1);
                            if (b == 1)
                            {
                                if (l == 1)
                                {
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                                else
                                {
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                            }
                            else
                            {
                                if (l == 1)
                                {
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                                else
                                {
                                    THUMB_StoreRegister_ImmediateOffset(instruction);
                                }
                            }
                            break;
                        }

                    case 0b101:
                        // Miscellaneous
                        if (((instruction >> 12) & 1) == 1)
                        {
                            switch ((instruction >> 9) & 0b11)
                            {
                                // Push/pop register list
                                case 0b10:
                                    {
                                        UInt16 l = (UInt16)((instruction >> 11) & 1);
                                        if (l == 1)
                                        {
                                            THUMB_PopMultipleRegisters(instruction);
                                        }
                                        else
                                        {
                                            THUMB_PushMultipleRegisters(instruction);
                                        }
                                        break;
                                    }
                                default:
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                    Environment.Exit(1);
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                            Environment.Exit(1);
                        }
                        break;

                    case 0b110:
                        if (((instruction >> 12) & 1) == 1)
                        {
                            // Conditional branch
                            if (((instruction >> 9) & 0b111) != 0b111)
                            {
                                THUMB_Branch_Conditional(instruction);
                            }
                            else
                            {
                                Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                                Environment.Exit(1);
                            }
                        }

                        // Load/store multiple
                        else
                        {
                            UInt16 l = (UInt16)((instruction >> 11) & 1);
                            if (l == 1)
                            {
                                THUMB_LoadMultipleIncrementAfter(instruction);
                            }
                            else
                            {
                                THUMB_StoreMultipleIncrementAfter(instruction);
                            }
                        }
                        break;

                    case 0b111:
                        if (((instruction >> 12) & 1) == 1)
                        {
                            // BL prefix
                            if (((instruction >> 11) & 1) == 0)
                            {
                                THUMB_BranchLink_Prefix(instruction);
                            }

                            // BL suffix
                            else
                            {
                                THUMB_BranchLink_Suffix(instruction);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                            Environment.Exit(1);
                        }
                        break;

                    // Unknown
                    default:
                        Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, _nextInstructionAddress);
                        Environment.Exit(1);
                        break;
                }
            }
        }

        private static UInt32 SignExtend(UInt32 value, int size)
        {
            if ((value >> (size - 1)) == 1)
            {
                return value | (0xffff_ffff << size);
            }
            else
            {
                return value;
            }
        }

        private static UInt32 SignExtend_30(UInt32 value, int size)
        {
            return SignExtend(value, size) & 0x3fff_ffff;
        }

        private static UInt32 Number_Of_Set_Bits_In(UInt32 value, int size)
        {
            UInt32 count = 0;
            for (int i = 0; i < size; ++i)
            {
                count += ((value >> i) & 1);
            }
            return count;
        }

        // ********************************************************************
        //                               ARM
        // ********************************************************************

        private UInt32 GetMode()
        {
            const UInt32 mask = 0b1_1111;
            return _cpsr & mask;
        }

        private bool InAPrivilegedMode()
        {
            return GetMode() != UserMode;
        }

        private bool CurrentModeHasSPSR()
        {
            return GetMode() switch
            {
                UserMode or SystemMode => false,
                _ => true,
            };
        }

        private void SetCPSR(UInt32 value)
        {
            const UInt32 mask = 0b1_1111;
            UInt32 previousMode = _cpsr & mask;
            UInt32 newMode = value & mask;

            _cpsr = value;

            if (previousMode != newMode)
            {
                // save previous mode registers
                switch (previousMode)
                {
                    case UserMode:
                    case SystemMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13 = _reg[13];
                        _reg14 = _reg[14];
                        break;
                    case SupervisorMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13_svc = _reg[13];
                        _reg14_svc = _reg[14];
                        _spsr_svc = _spsr;
                        break;
                    case AbortMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13_abt = _reg[13];
                        _reg14_abt = _reg[14];
                        _spsr_abt = _spsr;
                        break;
                    case UndefinedMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13_und = _reg[13];
                        _reg14_und = _reg[14];
                        _spsr_und = _spsr;
                        break;
                    case InterruptMode:
                        _reg8 = _reg[8];
                        _reg9 = _reg[9];
                        _reg10 = _reg[10];
                        _reg11 = _reg[11];
                        _reg12 = _reg[12];
                        _reg13_irq = _reg[13];
                        _reg14_irq = _reg[14];
                        _spsr_irq = _spsr;
                        break;
                    case FastInterruptMode:
                        _reg8_fiq = _reg[8];
                        _reg9_fiq = _reg[9];
                        _reg10_fiq = _reg[10];
                        _reg11_fiq = _reg[11];
                        _reg12_fiq = _reg[12];
                        _reg13_fiq = _reg[13];
                        _reg14_fiq = _reg[14];
                        _spsr_fiq = _spsr;
                        break;
                }

                // load new mode registers
                switch (newMode)
                {
                    case UserMode:
                    case SystemMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13;
                        _reg[14] = _reg14;
                        break;
                    case SupervisorMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13_svc;
                        _reg[14] = _reg14_svc;
                        _spsr = _spsr_svc;
                        break;
                    case AbortMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13_abt;
                        _reg[14] = _reg14_abt;
                        _spsr = _spsr_abt;
                        break;
                    case UndefinedMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13_und;
                        _reg[14] = _reg14_und;
                        _spsr = _spsr_und;
                        break;
                    case InterruptMode:
                        _reg[8] = _reg8;
                        _reg[9] = _reg9;
                        _reg[10] = _reg10;
                        _reg[11] = _reg11;
                        _reg[12] = _reg12;
                        _reg[13] = _reg13_irq;
                        _reg[14] = _reg14_irq;
                        _spsr = _spsr_irq;
                        break;
                    case FastInterruptMode:
                        _reg[8] = _reg8_fiq;
                        _reg[9] = _reg9_fiq;
                        _reg[10] = _reg10_fiq;
                        _reg[11] = _reg11_fiq;
                        _reg[12] = _reg12_fiq;
                        _reg[13] = _reg13_fiq;
                        _reg[14] = _reg14_fiq;
                        _spsr = _spsr_fiq;
                        break;
                }
            }
        }

        private UInt32 GetFlag(Flags flag)
        {
            return (_cpsr >> (int)flag) & 1;
        }

        private void SetFlag(Flags flag, UInt32 value)
        {
            UInt32 mask = (UInt32)1 << (int)flag;
            if (value == 1)
                _cpsr |= mask;
            else
                _cpsr &= ~mask;
        }

        private bool ConditionPassed(UInt32 cond)
        {
            switch (cond)
            {
                case 0b0000: // EQ
                    return GetFlag(Flags.Z) == 1;
                case 0b0001: // NE
                    return GetFlag(Flags.Z) == 0;
                case 0b0010: // CS/HS
                    return GetFlag(Flags.C) == 1;
                case 0b0011: // CC/LO
                    return GetFlag(Flags.C) == 0;
                case 0b0100: // MI
                    return GetFlag(Flags.N) == 1;
                case 0b0101: // PL
                    return GetFlag(Flags.N) == 0;
                case 0b0110: // VS
                    return GetFlag(Flags.V) == 1;
                case 0b0111: // VC
                    return GetFlag(Flags.V) == 0;
                case 0b1010: // GE
                    return GetFlag(Flags.N) == GetFlag(Flags.V);
                case 0b1100: // GT
                    return (GetFlag(Flags.Z) == 0) && (GetFlag(Flags.N) == GetFlag(Flags.V));
                case 0b1101: // LE
                    return (GetFlag(Flags.Z) == 1) || (GetFlag(Flags.N) != GetFlag(Flags.V));
                case 0b1110: // AL
                    return true;
                default: // Unimplemented
                    Console.WriteLine("Condition {0} unimplemented", cond);
                    Environment.Exit(1);
                    return false;
            }
        }

        private static UInt32 RotateRight(UInt32 value, int rotateAmount)
        {
            return (value >> rotateAmount) | (value << (32 - rotateAmount));
        }

        private static UInt32 LogicalShiftLeft(UInt32 value, int shiftAmount)
        {
            return value << shiftAmount;
        }

        private static UInt32 LogicalShiftRight(UInt32 value, int shiftAmount)
        {
            return value >> shiftAmount;
        }

        private static UInt32 ArithmeticShiftRight(UInt32 value, int shiftAmount)
        {
            return (UInt32)((Int32)value >> shiftAmount);
        }

        private (UInt32 shifterOperand, UInt32 shifterCarryOut) GetShifterOperand(UInt32 instruction)
        {
            UInt32 shifterOperand = 0;
            UInt32 shifterCarryOut = 0;

            if (((instruction >> 25) & 1) == 1) // 32-bit immediate
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                UInt32 rotateAmount = rotateImm * 2;
                shifterOperand = RotateRight(imm, (int)rotateAmount);

                if (rotateImm == 0)
                    shifterCarryOut = GetFlag(Flags.C);
                else
                    shifterCarryOut = shifterOperand >> 31;
            }
            else
            {
                UInt32 shift = (instruction >> 5) & 0b11;
                UInt32 rm = instruction & 0b1111;
                if (((instruction >> 4) & 1) == 0) // Immediate shifts
                {
                    UInt32 shiftImm = (instruction >> 7) & 0b1_1111;
                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if (shiftImm == 0)
                            {
                                shifterOperand = _reg[rm];
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else
                            {
                                shifterOperand = LogicalShiftLeft(_reg[rm], (int)shiftImm);
                                shifterCarryOut = (_reg[rm] >> (32 - (int)shiftImm)) & 1;
                            }
                            break;
                        case 0b01: // Logical shift right
                            if (shiftImm == 0)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = _reg[rm] >> 31;
                            }
                            else
                            {
                                shifterOperand = LogicalShiftRight(_reg[rm], (int)shiftImm);
                                shifterCarryOut = (_reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                        case 0b10: // Arithmetic shift right
                            if (shiftImm == 0)
                            {
                                if ((_reg[rm] >> 31) == 0)
                                    shifterOperand = 0;
                                else
                                    shifterOperand = 0xffff_ffff;

                                shifterCarryOut = _reg[rm] >> 31;
                            }
                            else
                            {
                                shifterOperand = ArithmeticShiftRight(_reg[rm], (int)shiftImm);
                                shifterCarryOut = (_reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                        case 0b11: // Rotate right
                            if (shiftImm == 0)
                            {
                                shifterOperand = LogicalShiftLeft(GetFlag(Flags.C), 31) | LogicalShiftRight(_reg[rm], 1);
                                shifterCarryOut = _reg[rm] & 1;
                            }
                            else
                            {
                                shifterOperand = RotateRight(_reg[rm], (int)shiftImm);
                                shifterCarryOut = (_reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                    }
                }
                else if (((instruction >> 7) & 1) == 0) // Register shifts
                {
                    UInt32 rs = (instruction >> 8) & 0b1111;
                    UInt32 regRm = (rm == PC) ? _reg[PC] + 4 : _reg[rm];
                    UInt32 regRs = _reg[rs] & 0xff;
                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if (regRs == 0)
                            {
                                shifterOperand = regRm;
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else if (regRs < 32)
                            {
                                shifterOperand = LogicalShiftLeft(regRm, (int)regRs);
                                shifterCarryOut = (regRm >> (32 - (int)regRs)) & 1;
                            }
                            else if (regRs == 32)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = regRm & 1;
                            }
                            else
                            {
                                shifterOperand = 0;
                                shifterCarryOut = 0;
                            }
                            break;
                        case 0b10: // Arithmetic shift right
                            if (regRs == 0)
                            {
                                shifterOperand = regRm;
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else if (regRs < 32)
                            {
                                shifterOperand = ArithmeticShiftRight(regRm, (int)regRs);
                                shifterCarryOut = (regRm >> ((int)regRs - 1)) & 1;
                            }
                            else
                            {
                                if ((regRm >> 31) == 0)
                                    shifterOperand = 0;
                                else
                                    shifterOperand = 0xffff_ffff;

                                shifterCarryOut = regRm >> 31;
                            }
                            break;
                        default:
                            Console.WriteLine("CPU: encoding unimplemented");
                            Environment.Exit(1);
                            break;
                    }
                }
                else
                    throw new Exception("CPU: Wrong encoding");
            }

            return (shifterOperand, shifterCarryOut);
        }

        private static UInt32 CarryFrom(UInt64 result)
        {
            return (result > 0xffff_ffff) ? 1u : 0u;
        }

        private static UInt32 BorrowFrom(UInt32 leftOperand, UInt32 rightOperand)
        {
            return (leftOperand < rightOperand) ? 1u : 0u;
        }

        private static UInt32 OverflowFrom_Addition(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) == (rightOperand >> 31)) && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        private static UInt32 OverflowFrom_Subtraction(UInt32 leftOperand, UInt32 rightOperand, UInt32 result)
        {
            return (((leftOperand >> 31) != (rightOperand >> 31)) && ((leftOperand >> 31) != (result >> 31))) ? 1u : 0u;
        }

        private static void ARM_ADC(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;

                UInt32 regRn = cpu._reg[rn];
                UInt64 rightOperand = (UInt64)shifterOperand + (UInt64)cpu.GetFlag(Flags.C);
                UInt64 result = (UInt64)regRn + rightOperand;
                cpu._reg[rd] = (UInt32)result;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("ADC: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, CarryFrom(result));
                        cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRn, (UInt32)rightOperand, cpu._reg[rd]));
                    }
                }
            }
        }

        private static void ARM_ADD(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;

                UInt32 regRn = cpu._reg[rn];
                UInt64 result = (UInt64)regRn + (UInt64)shifterOperand;
                cpu._reg[rd] = (UInt32)result;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("ADD: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, CarryFrom(result));
                        cpu.SetFlag(Flags.V, OverflowFrom_Addition(regRn, shifterOperand, cpu._reg[rd]));
                    }
                }
            }
        }

        private static void ARM_AND(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu._reg[rd] = cpu._reg[rn] & shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("AND: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, shifterCarryOut);
                    }
                }
            }
        }

        private static void ARM_BIC(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu._reg[rd] = cpu._reg[rn] & ~shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("BIC: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, shifterCarryOut);
                    }
                }
            }
        }

        private static void ARM_CMN(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;

                UInt64 result = (UInt64)cpu._reg[rn] + (UInt64)shifterOperand;
                UInt32 aluOut = (UInt32)result;

                cpu.SetFlag(Flags.N, aluOut >> 31);
                cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flags.C, CarryFrom(result));
                cpu.SetFlag(Flags.V, OverflowFrom_Addition(cpu._reg[rn], shifterOperand, aluOut));
            }
        }

        private static void ARM_CMP(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 aluOut = cpu._reg[rn] - shifterOperand;

                cpu.SetFlag(Flags.N, aluOut >> 31);
                cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                cpu.SetFlag(Flags.C, ~BorrowFrom(cpu._reg[rn], shifterOperand) & 1);
                cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(cpu._reg[rn], shifterOperand, aluOut));
            }
        }

        private static void ARM_EOR(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu._reg[rd] = cpu._reg[rn] ^ shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("EOR: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, shifterCarryOut);
                    }
                }
            }
        }

        private static void ARM_MLA(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 rd = (instruction >> 16) & 0b1111;
                UInt32 rn = (instruction >> 12) & 0b1111;
                UInt32 rs = (instruction >> 8) & 0b1111;
                UInt32 rm = instruction & 0b1111;
                cpu._reg[rd] = cpu._reg[rm] * cpu._reg[rs] + cpu._reg[rn];

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("MLA: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    }
                }
            }
        }

        private static void ARM_MOV(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu._reg[rd] = shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("MOV: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, shifterCarryOut);
                    }
                }
            }
        }

        private static void ARM_MRS(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 r = (instruction >> 22) & 1;
                UInt32 rd = (instruction >> 12) & 0b1111;

                if (r == 1)
                    cpu._reg[rd] = cpu._spsr;
                else
                    cpu._reg[rd] = cpu._cpsr;
            }
        }

        private static void ARM_MSR(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 operand;
                if (((instruction >> 25) & 1) == 1)
                {
                    UInt32 rotateImm = (instruction >> 8) & 0b1111;
                    UInt32 imm = instruction & 0xff;

                    int rotateAmount = 2 * (int)rotateImm;
                    operand = RotateRight(imm, rotateAmount);
                }
                else
                {
                    UInt32 rm = instruction & 0b1111;
                    operand = cpu._reg[rm];
                }

                UInt32 fieldMask = (instruction >> 16) & 0b1111;
                UInt32 byteMask = (UInt32)((((fieldMask >> 0) & 1) == 1) ? 0x0000_00ff : 0)
                                | (UInt32)((((fieldMask >> 1) & 1) == 1) ? 0x0000_ff00 : 0)
                                | (UInt32)((((fieldMask >> 2) & 1) == 1) ? 0x00ff_0000 : 0)
                                | (UInt32)((((fieldMask >> 3) & 1) == 1) ? 0xff00_0000 : 0);

                const UInt32 UserMask = 0xf000_0000;
                const UInt32 PrivMask = 0x0000_000f;
                const UInt32 StateMask = 0x0000_0020;

                UInt32 r = (instruction >> 22) & 1;
                if (r == 0)
                {
                    UInt32 mask;
                    if (cpu.InAPrivilegedMode())
                        mask = byteMask & (UserMask | PrivMask);
                    else
                        mask = byteMask & UserMask;

                    cpu.SetCPSR((cpu._cpsr & ~mask) | (operand & mask));
                }
                else if (cpu.CurrentModeHasSPSR())
                {
                    UInt32 mask = byteMask & (UserMask | PrivMask | StateMask);
                    cpu._spsr = (cpu._spsr & ~mask) | (operand & mask);
                }
            }
        }

        private static void ARM_MUL(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 rd = (instruction >> 16) & 0b1111;
                UInt32 rs = (instruction >> 8) & 0b1111;
                UInt32 rm = instruction & 0b1111;
                cpu._reg[rd] = cpu._reg[rm] * cpu._reg[rs];

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("MUL: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                    }
                }
            }
        }

        private static void ARM_MVN(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu._reg[rd] = ~shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("MVN: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, shifterCarryOut);
                    }
                }
            }
        }

        private static void ARM_ORR(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu._reg[rd] = cpu._reg[rn] | shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("ORR: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, shifterCarryOut);
                    }
                }
            }
        }

        private static void ARM_RSC(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;

                UInt32 rightOperand = cpu._reg[rn] + (~cpu.GetFlag(Flags.C) & 1);
                cpu._reg[rd] = shifterOperand - rightOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("RSC: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, ~BorrowFrom(shifterOperand, rightOperand) & 1);
                        cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(shifterOperand, rightOperand, cpu._reg[rd]));
                    }
                }
            }
        }

        private static void ARM_SBC(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                var (shifterOperand, _) = cpu.GetShifterOperand(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;

                UInt32 regRn = cpu._reg[rn];
                UInt32 rightOperand = shifterOperand + (~cpu.GetFlag(Flags.C) & 1);
                cpu._reg[rd] = regRn - rightOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("SBC: S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        cpu.SetFlag(Flags.N, cpu._reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu._reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, ~BorrowFrom(regRn, rightOperand) & 1);
                        cpu.SetFlag(Flags.V, OverflowFrom_Subtraction(regRn, rightOperand, cpu._reg[rd]));
                    }
                }
            }
        }

        private static void ARM_SMLAL(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 rdHi = (instruction >> 16) & 0b1111;
                UInt32 rdLo = (instruction >> 12) & 0b1111;
                UInt32 rs = (instruction >> 8) & 0b1111;
                UInt32 rm = instruction & 0b1111;

                Int64 result = (Int64)(Int32)cpu._reg[rm] * (Int64)(Int32)cpu._reg[rs];
                UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu._reg[rdLo];
                cpu._reg[rdLo] = (UInt32)resultLo;
                cpu._reg[rdHi] += (UInt32)(result >> 32) + CarryFrom(resultLo);

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    cpu.SetFlag(Flags.N, cpu._reg[rdHi] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rdHi] == 0 && cpu._reg[rdLo] == 0) ? 1u : 0u);
                }
            }
        }

        private static void ARM_SMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 rdHi = (instruction >> 16) & 0b1111;
                UInt32 rdLo = (instruction >> 12) & 0b1111;
                UInt32 rs = (instruction >> 8) & 0b1111;
                UInt32 rm = instruction & 0b1111;

                Int64 result = (Int64)(Int32)cpu._reg[rm] * (Int64)(Int32)cpu._reg[rs];
                cpu._reg[rdHi] = (UInt32)(result >> 32);
                cpu._reg[rdLo] = (UInt32)result;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    cpu.SetFlag(Flags.N, cpu._reg[rdHi] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rdHi] == 0 && cpu._reg[rdLo] == 0) ? 1u : 0u);
                }
            }
        }

        private static void ARM_SWP(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                UInt32 rm = instruction & 0b1111;

                UInt32 address = cpu._reg[rn];
                UInt32 temp = cpu._callbacks.ReadMemory32(address);
                cpu._callbacks.WriteMemory32(address, cpu._reg[rm]);
                cpu._reg[rd] = temp;
            }
        }

        private static void ARM_SWPB(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                UInt32 rm = instruction & 0b1111;

                UInt32 address = cpu._reg[rn];
                Byte temp = cpu._callbacks.ReadMemory8(address);
                cpu._callbacks.WriteMemory8(address, (Byte)cpu._reg[rm]);
                cpu._reg[rd] = temp;
            }
        }

        private static void ARM_UMLAL(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 rdHi = (instruction >> 16) & 0b1111;
                UInt32 rdLo = (instruction >> 12) & 0b1111;
                UInt32 rs = (instruction >> 8) & 0b1111;
                UInt32 rm = instruction & 0b1111;

                UInt64 result = (UInt64)cpu._reg[rm] * (UInt64)cpu._reg[rs];
                UInt64 resultLo = (UInt64)(UInt32)result + (UInt64)cpu._reg[rdLo];
                cpu._reg[rdLo] = (UInt32)resultLo;
                cpu._reg[rdHi] += (UInt32)(result >> 32) + CarryFrom(resultLo);

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    cpu.SetFlag(Flags.N, cpu._reg[rdHi] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rdHi] == 0 && cpu._reg[rdLo] == 0) ? 1u : 0u);
                }
            }
        }

        private static void ARM_UMULL(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                UInt32 rdHi = (instruction >> 16) & 0b1111;
                UInt32 rdLo = (instruction >> 12) & 0b1111;
                UInt32 rs = (instruction >> 8) & 0b1111;
                UInt32 rm = instruction & 0b1111;

                UInt64 result = (UInt64)cpu._reg[rm] * (UInt64)cpu._reg[rs];
                cpu._reg[rdHi] = (UInt32)(result >> 32);
                cpu._reg[rdLo] = (UInt32)result;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    cpu.SetFlag(Flags.N, cpu._reg[rdHi] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu._reg[rdHi] == 0 && cpu._reg[rdLo] == 0) ? 1u : 0u);
                }
            }
        }

        // ==============================
        // Multiplies & Extra load/stores
        // ==============================

        // STRH (immediate post-indexed)
        private void ARM_StoreRegisterHalfWord_ImmediatePostIndexed(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 immH = (instruction >> 8) & 0b1111;
                UInt32 immL = instruction & 0b1111;
                UInt32 offset = (immH << 4) | immL;

                UInt32 address = _reg[rn];
                if (u == 1)
                {
                    _reg[rn] += offset;
                }
                else
                {
                    _reg[rn] -= offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                _callbacks.WriteMemory16(address, (UInt16)_reg[rd]);
            }
        }

        // STRH (immediate offset)
        private void ARM_StoreRegisterHalfWord_ImmediateOffset(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 immH = (instruction >> 8) & 0b1111;
                UInt32 immL = instruction & 0b1111;
                UInt32 offset = (immH << 4) | immL;

                UInt32 address;
                if (u == 1)
                {
                    address = _reg[rn] + offset;
                }
                else
                {
                    address = _reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                _callbacks.WriteMemory16(address, (UInt16)_reg[rd]);
            }
        }

        // LDRH (immediate post-indexed)
        private void ARM_LoadRegisterHalfWord_ImmediatePostIndexed(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 immH = (instruction >> 8) & 0b1111;
                UInt32 immL = instruction & 0b1111;
                UInt32 offset = (immH << 4) | immL;

                UInt32 address = _reg[rn];
                if (u == 1)
                {
                    _reg[rn] += offset;
                }
                else
                {
                    _reg[rn] -= offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                _reg[rd] = _callbacks.ReadMemory16(address);
            }
        }

        // LDRH (immediate offset)
        private void ARM_LoadRegisterHalfWord_ImmediateOffset(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 immH = (instruction >> 8) & 0b1111;
                UInt32 immL = instruction & 0b1111;
                UInt32 offset = (immH << 4) | immL;

                UInt32 address;
                if (u == 1)
                {
                    address = _reg[rn] + offset;
                }
                else
                {
                    address = _reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                _reg[rd] = _callbacks.ReadMemory16(address);
            }
        }

        // ==============================
        // Miscellaneous instructions
        // ==============================

        // BX
        private void ARM_BranchExchange(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rm = instruction & 0b1111;
                SetCPSR((_cpsr & ~(1u << 5)) | (_reg[rm] & 1) << 5);
                _reg[PC] = _reg[rm] & 0xffff_fffe;
            }
        }

        // ==============================
        // Data processing immediate
        // ==============================

        // SUB (immediate)
        private void ARM_Subtract_Immediate(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                int rotateAmount = 2 * (int)rotateImm;
                UInt32 shifterOperand = (imm >> rotateAmount) | (imm << (32 - rotateAmount));

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                _reg[rd] = _reg[rn] - shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    if (rd == PC)
                    {
                        Console.WriteLine("SUB (immediate): S field partially implemented");
                        Environment.Exit(1);
                    }
                    else
                    {
                        SetFlag(Flags.N, _reg[rd] >> 31);
                        SetFlag(Flags.Z, (_reg[rd] == 0) ? 1u : 0u);
                        // TODO: C flag
                        // TODO: V flag
                    }
                }
            }
        }

        // TST (immediate)
        private void ARM_Test_Immediate(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                int rotateAmount = 2 * (int)rotateImm;
                UInt32 shifterOperand = (imm >> rotateAmount) | (imm << (32 - rotateAmount));

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 aluOut = _reg[rn] & shifterOperand;
                // TODO: N flag
                SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                // TODO: C flag
            }
        }

        // TEQ (immediate)
        private void ARM_TestEquivalence_Immediate(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                int rotateAmount = 2 * (int)rotateImm;
                UInt32 shifterOperand = (imm >> rotateAmount) | (imm << (32 - rotateAmount));

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 aluOut = _reg[rn] ^ shifterOperand;
                // TODO: N flag
                SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                // TODO: C flag
            }
        }

        // ==============================
        // Load/store immediate offset
        // ==============================

        // LDRB (immediate post-indexed)
        private void ARM_LoadRegisterByte_ImmediatePostIndexed(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 offset = instruction & 0xfff;

                UInt32 address = _reg[rn];
                if (u == 1)
                {
                    _reg[rn] += offset;
                }
                else
                {
                    _reg[rn] -= offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                _reg[rd] = _callbacks.ReadMemory8(address);
            }
        }

        // LDRB (immediate offset)
        private void ARM_LoadRegisterByte_ImmediateOffset(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 offset = instruction & 0xfff;

                UInt32 address;
                if (u == 1)
                {
                    address = _reg[rn] + offset;
                }
                else
                {
                    address = _reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                _reg[rd] = _callbacks.ReadMemory8(address);
            }
        }

        // STRB (immediate offset)
        private void ARM_StoreRegisterByte_ImmediateOffset(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 offset = instruction & 0xfff;

                UInt32 address;
                if (u == 1)
                {
                    address = _reg[rn] + offset;
                }
                else
                {
                    address = _reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                _callbacks.WriteMemory8(address, (Byte)_reg[rd]);
            }
        }

        // LDR (immediate post-indexed)
        private void ARM_LoadRegister_ImmediatePostIndexed(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 offset = instruction & 0xfff;

                UInt32 address = _reg[rn];
                if (u == 1)
                {
                    _reg[rn] += offset;
                }
                else
                {
                    _reg[rn] -= offset;
                }

                UInt32 data = _callbacks.ReadMemory32(address);
                UInt32 rd = (instruction >> 12) & 0b1111;

                if (rd == PC)
                {
                    _reg[PC] = data & 0xffff_fffc;
                }
                else
                {
                    _reg[rd] = data;
                }
            }
        }

        // LDR (immediate offset)
        private void ARM_LoadRegister_ImmediateOffset(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 offset = instruction & 0xfff;

                UInt32 address;
                if (u == 1)
                {
                    address = _reg[rn] + offset;
                }
                else
                {
                    address = _reg[rn] - offset;
                }

                UInt32 data = _callbacks.ReadMemory32(address);
                UInt32 rd = (instruction >> 12) & 0b1111;

                if (rd == PC)
                {
                    _reg[PC] = data & 0xffff_fffc;
                }
                else
                {
                    _reg[rd] = data;
                }
            }
        }

        // STR (immediate post-indexed)
        private void ARM_StoreRegister_ImmediatePostIndexed(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 offset = instruction & 0xfff;

                UInt32 address = _reg[rn];
                if (u == 1)
                {
                    _reg[rn] += offset;
                }
                else
                {
                    _reg[rn] -= offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                _callbacks.WriteMemory32(address, _reg[rd]);
            }
        }

        // STR (immediate offset)
        private void ARM_StoreRegister_ImmediateOffset(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 u = (instruction >> 23) & 1;
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 offset = instruction & 0xfff;

                UInt32 address;
                if (u == 1)
                {
                    address = _reg[rn] + offset;
                }
                else
                {
                    address = _reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                _callbacks.WriteMemory32(address, _reg[rd]);
            }
        }

        // ==============================
        // Load/store multiple
        // ==============================

        // STMFD
        private void ARM_StoreMultiple_FullDescending(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 registerList = instruction & 0xffff;

                UInt32 startAddress = _reg[rn] - (Number_Of_Set_Bits_In(registerList, 16) * 4);
                UInt32 endAddress = _reg[rn] - 4;

                UInt32 w = (instruction >> 21) & 1;
                if (w == 1)
                {
                    _reg[rn] -= (Number_Of_Set_Bits_In(registerList, 16) * 4);
                }

                UInt32 address = startAddress;
                for (int i = 0; i <= 15; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        _callbacks.WriteMemory32(address, _reg[i]);
                        address += 4;
                    }
                }
            }
        }

        // LDMFD
        private void ARM_LoadMultiple_FullDescending(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 registerList = instruction & 0xffff;

                UInt32 startAddress = _reg[rn];
                UInt32 endAddress = _reg[rn] + (Number_Of_Set_Bits_In(registerList, 16) * 4) - 4;

                UInt32 w = (instruction >> 21) & 1;
                if (w == 1)
                {
                    _reg[rn] += (Number_Of_Set_Bits_In(registerList, 16) * 4);
                }

                UInt32 address = startAddress;
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        _reg[i] = _callbacks.ReadMemory32(address);
                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                {
                    UInt32 value = _callbacks.ReadMemory32(address);
                    _reg[PC] = value & 0xffff_fffc;
                }
            }
        }

        // ==============================
        // Branch and branch with link
        // ==============================

        // BL
        private void ARM_BranchLink(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                _reg[LR] = _nextInstructionAddress;

                UInt32 imm = instruction & 0xff_ffff;
                _reg[PC] += (SignExtend_30(imm, 24) << 2);
            }
        }

        // B
        private void ARM_Branch(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 imm = instruction & 0xff_ffff;
                _reg[PC] += (SignExtend_30(imm, 24) << 2);
            }
        }

        // ********************************************************************
        //                         THUMB instructions
        // ********************************************************************

        // ==============================
        // Shift by immediate
        // ==============================

        private void THUMB_LogicalShiftLeft_Immediate(UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);

            if (imm == 0)
            {
                _reg[rd] = _reg[rm];
            }
            else
            {
                SetFlag(Flags.C, (_reg[rm] >> (32 - imm)) & 1);
                _reg[rd] = _reg[rm] << imm;
            }

            // TODO: N flag
            // TODO: Z flag
        }

        // ==============================
        // Add/subtract register
        // ==============================

        // ADD (register)
        private void THUMB_Add_Register(UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);
            _reg[rd] = _reg[rn] + _reg[rm];
            // TODO: N flag
            // TODO: Z flag
            // TODO: C flag
            // TODO: V flag
        }

        // SUB (register)
        private void THUMB_Sub_Register(UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 6) & 0b111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);
            _reg[rd] = _reg[rn] - _reg[rm];
            // TODO: N flag
            // TODO: Z flag
            // TODO: C flag
            // TODO: V flag
        }

        // ==============================
        // Add/subtract/compare/move immediate
        // ==============================

        // MOV (immediate)
        private void THUMB_Move_Immediate(UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);
            _reg[rd] = imm;
            // TODO: N flag
            // TODO: Z flag
        }

        // SUB (immediate)
        private void THUMB_Subtract_Immediate(UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);
            _reg[rd] -= imm;
            // TODO: N flag
            SetFlag(Flags.Z, (_reg[rd] == 0) ? 1u : 0u);
            // TODO: C flag
            // TODO: V flag
        }

        // ==============================
        // Load from literal pool
        // ==============================

        // LDR (PC relative)
        private void THUMB_LoadRegister_PCRelative(UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);
            UInt32 address = (_reg[PC] & 0xffff_fffc) + (UInt32)(imm * 4);
            _reg[rd] = _callbacks.ReadMemory32(address);
        }

        // ==============================
        // Data-processing register
        // ==============================

        // BIC
        private void THUMB_BitClear(UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);
            _reg[rd] &= ~_reg[rm];
            // TODO: N flag
            SetFlag(Flags.Z, (_reg[rd] == 0) ? 1u : 0u);
        }

        // ==============================
        // Branch/exchange instruction set
        // ==============================

        // BX
        private void THUMB_BranchExchange(UInt16 instruction)
        {
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            SetCPSR((_cpsr & ~(1u << 5)) | (_reg[(h2 << 3) | rm] & 1) << 5);
            _reg[PC] = _reg[(h2 << 3) | rm] & 0xffff_fffe;
        }

        // ==============================
        // Special data processing
        // ==============================

        // MOV
        private void THUMB_Move(UInt16 instruction)
        {
            UInt16 h1 = (UInt16)((instruction >> 7) & 1);
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);
            _reg[(h1 << 3) | rd] = _reg[(h2 << 3) | rm];
        }

        // ==============================
        // Load/store word/byte immediate offset
        // ==============================

        // STR (immediate offset)
        private void THUMB_StoreRegister_ImmediateOffset(UInt16 instruction)
        {
            UInt16 imm = (UInt16)((instruction >> 6) & 0b1_1111);
            UInt16 rn = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);
            UInt32 address = _reg[rn] + ((UInt32)imm * 4);
            _callbacks.WriteMemory32(address, _reg[rd]);
        }

        // ==============================
        // Miscellaneous
        // ==============================

        // POP
        private void THUMB_PopMultipleRegisters(UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);
            UInt32 startAddress = _reg[SP];
            UInt32 endAddress = _reg[SP] + 4 * (r + Number_Of_Set_Bits_In(registerList, 8));
            UInt32 address = startAddress;

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    _reg[i] = _callbacks.ReadMemory32(address);
                    address += 4;
                }
            }

            if (r == 1)
            {
                UInt32 value = _callbacks.ReadMemory32(address);
                _reg[PC] = value & 0xffff_fffe;
            }

            _reg[SP] = endAddress;
        }

        // PUSH
        private void THUMB_PushMultipleRegisters(UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);
            UInt32 startAddress = _reg[SP] - 4 * (r + Number_Of_Set_Bits_In(registerList, 8));
            UInt32 endAddress = _reg[SP] - 4;
            UInt32 address = startAddress;

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    _callbacks.WriteMemory32(address, _reg[i]);
                    address += 4;
                }
            }

            if (r == 1)
            {
                _callbacks.WriteMemory32(address, _reg[LR]);
            }

            _reg[SP] -= 4 * (r + Number_Of_Set_Bits_In(registerList, 8));
        }

        // ==============================
        // Conditional branch
        // ==============================

        // B (conditional)
        private void THUMB_Branch_Conditional(UInt16 instruction)
        {
            UInt16 cond = (UInt16)((instruction >> 8) & 0b1111);
            if (ConditionPassed(cond))
            {
                UInt16 imm = (UInt16)(instruction & 0xff);
                _reg[PC] += SignExtend(imm, 8) << 1;
            }
        }

        // ==============================
        // Load/store multiple
        // ==============================

        // LDMI
        private void THUMB_LoadMultipleIncrementAfter(UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);
            UInt32 startAddress = _reg[rn];
            UInt32 endAddress = _reg[rn] + (Number_Of_Set_Bits_In(registerList, 8) * 4) - 4;
            UInt32 address = startAddress;

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    _reg[i] = _callbacks.ReadMemory32(address);
                    address += 4;
                }
            }

            _reg[rn] += Number_Of_Set_Bits_In(registerList, 8) * 4;
        }

        // STMIA
        private void THUMB_StoreMultipleIncrementAfter(UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);
            UInt32 startAddress = _reg[rn];
            UInt32 endAddress = _reg[rn] + (Number_Of_Set_Bits_In(registerList, 8) * 4) - 4;
            UInt32 address = startAddress;

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    _callbacks.WriteMemory32(address, _reg[i]);
                    address += 4;
                }
            }

            _reg[rn] += Number_Of_Set_Bits_In(registerList, 8) * 4;
        }

        // ==============================
        // BL prefix
        // ==============================

        private void THUMB_BranchLink_Prefix(UInt16 instruction)
        {
            UInt16 offset = (UInt16)(instruction & 0x7ff);
            _reg[LR] = _reg[PC] + (SignExtend(offset, 11) << 12);
        }

        // ==============================
        // BL suffix
        // ==============================
        private void THUMB_BranchLink_Suffix(UInt16 instruction)
        {
            UInt16 offset = (UInt16)(instruction & 0x7ff);
            _reg[PC] = _reg[LR] + ((UInt32)offset << 1);
            _reg[LR] = _nextInstructionAddress | 1;
        }
    }
}
