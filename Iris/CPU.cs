using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
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

        private const UInt32 SP = 13;
        private const UInt32 LR = 14;
        private const UInt32 PC = 15;

        private delegate void ARM_InstructionHandler(CPU cpu, UInt32 instruction);

        private readonly struct ARM_InstructionTableEntry
        {
            public readonly UInt32 mask;
            public readonly UInt32 expected;
            public readonly ARM_InstructionHandler handler;

            public ARM_InstructionTableEntry(UInt32 mask, UInt32 expected, ARM_InstructionHandler handler)
            {
                this.mask = mask;
                this.expected = expected;
                this.handler = handler;
            }
        }

        private static readonly ARM_InstructionTableEntry[] ARM_InstructionTable = new ARM_InstructionTableEntry[]
        {
            // ADC
            new(0x0fa0_0000, 0x02a0_0000, ARM_ADC), // I bit is 1
            new(0x0fa0_0090, 0x00a0_0000, ARM_ADC), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fa0_0090, 0x00a0_0080, ARM_ADC), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fa0_0090, 0x00a0_0010, ARM_ADC), // I bit is 0, bit[7] is 0 and bit[4] is 1

            // ADD
            new(0x0fa0_0000, 0x0280_0000, ARM_ADD), // I bit is 1
            new(0x0fa0_0090, 0x0080_0000, ARM_ADD), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fa0_0090, 0x0080_0080, ARM_ADD), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fa0_0090, 0x0080_0010, ARM_ADD), // I bit is 0, bit[7] is 0 and bit[4] is 1

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
            new(0x0ff0_0090, 0x0170_0010, ARM_CMN), //I bit is 0, bit[7] is 0 and bit[4] is 1

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

            // MVN
            new(0x0fe0_0000, 0x03e0_0000, ARM_MVN), // I bit is 1
            new(0x0fe0_0090, 0x01e0_0000, ARM_MVN), // I bit is 0, bit[7] is 0 and bit[4] is 0
            new(0x0fe0_0090, 0x01e0_0080, ARM_MVN), // I bit is 0, bit[7] is 1 and bit[4] is 0
            new(0x0fe0_0090, 0x01e0_0010, ARM_MVN), // I bit is 0, bit[7] is 0 and bit[4] is 1s

            // MUL
            new(0x0fe0_00f0, 0x0000_0090, ARM_MUL),

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

            // SMULL
            new(0x0fe0_00f0, 0x00c0_0090, ARM_SMULL),

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

        private readonly ICallbacks callbacks;
        private readonly UInt32[] reg = new UInt32[16];
        private UInt32 cpsr;
        private UInt32 nextInstructionAddress;

        public CPU(ICallbacks callbacks)
        {
            this.callbacks = callbacks;
        }

        public void Init(UInt32 pc, UInt32 cpsr)
        {
            nextInstructionAddress = pc;
            reg[PC] = nextInstructionAddress + 4;

            this.cpsr = cpsr;
        }

        public void Step()
        {
            if (((cpsr >> 5) & 1) == 0) // ARM mode
            {
                if (reg[PC] != nextInstructionAddress + 4)
                {
                    nextInstructionAddress = reg[PC];
                }

                UInt32 instruction = callbacks.ReadMemory32(nextInstructionAddress);
                nextInstructionAddress += 4;
                reg[PC] = nextInstructionAddress + 4;

                foreach (ARM_InstructionTableEntry entry in ARM_InstructionTable)
                {
                    if ((instruction & entry.mask) == entry.expected)
                    {
                        entry.handler(this, instruction);
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
                                Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                Environment.Exit(1);
                            }

                            // Extra load/stores
                            else
                            {
                                if (((instruction >> 6) & 1) == 0)
                                {
                                    if (((instruction >> 5) & 1) == 0)
                                    {
                                        Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                        Environment.Exit(1);
                                    }
                                    else
                                    {
                                        if (((instruction >> 22) & 1) == 0)
                                        {
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                                        Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                                        Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                                        Environment.Exit(1);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                Environment.Exit(1);
                            }
                        }

                        // Miscellaneous instructions
                        else if ((instruction & 0x0190_0010) == 0x0100_0000)
                        {
                            if ((instruction & 0x0fb0_00f0) == 0x0120_0000)
                            {
                                ARM_MoveToStatusRegister_Register(instruction);
                            }
                            else
                            {
                                Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                Environment.Exit(1);
                            }
                        }

                        // Data processing register shift
                        else if ((instruction & 0x0000_0090) == 0x0000_0010)
                        {
                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                    Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        case 0b10:
                                            ARM_StoreRegisterByte_ImmediateOffset(instruction);
                                            break;

                                        case 0b11:
                                            //ARM_LoadRegisterByte_ImmediatePreIndexed(instruction);
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;

                                        default:
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                            Environment.Exit(1);
                                            break;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                        Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                        Environment.Exit(1);
                        break;
                }
            }
            else // THUMB mode
            {
                if (reg[PC] != nextInstructionAddress + 2)
                {
                    nextInstructionAddress = reg[PC];
                }

                UInt16 instruction = callbacks.ReadMemory16(nextInstructionAddress);
                nextInstructionAddress += 2;
                reg[PC] = nextInstructionAddress + 2;

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
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                    Environment.Exit(1);
                                    break;
                            }
                            break;
                        }

                    case 0b010:
                        // Load/store register offset
                        if (((instruction >> 12) & 1) == 1)
                        {
                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                                Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                                else
                                {
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                            }
                            else
                            {
                                if (l == 1)
                                {
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                    Environment.Exit(1);
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                                Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
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
                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
                            Environment.Exit(1);
                        }
                        break;

                    // Unknown
                    default:
                        Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
                        Environment.Exit(1);
                        break;
                }
            }
        }

        private UInt32 GetFlag(Flags flag)
        {
            return (cpsr >> (int)flag) & 1;
        }

        private void SetFlag(Flags flag, UInt32 value)
        {
            if (value == 1)
            {
                cpsr |= 1u << (int)flag;
            }
            else
            {
                cpsr &= ~(1u << (int)flag);
            }
        }

        private bool ConditionPassed(UInt32 cond)
        {
            switch (cond)
            {
                // EQ / Equal
                case 0b0000:
                    return GetFlag(Flags.Z) == 1;

                // NE / Not equal
                case 0b0001:
                    return GetFlag(Flags.Z) == 0;

                // CS/HS / Carry set/unsigned higher or same
                case 0b0010:
                    return GetFlag(Flags.C) == 1;

                // CC/LO / Carry clear/unsigned lower
                case 0b0011:
                    return GetFlag(Flags.C) == 0;

                // MI / Minus/negative
                case 0b0100:
                    return GetFlag(Flags.N) == 1;

                // PL / Plus/positive or zero
                case 0b0101:
                    return GetFlag(Flags.N) == 0;

                // VS / Overflow
                case 0b0110:
                    return GetFlag(Flags.V) == 1;

                // VC / No overflow
                case 0b0111:
                    return GetFlag(Flags.V) == 0;

                // GT / Signed greater than
                case 0b1100:
                    return (GetFlag(Flags.Z) == 0) && (GetFlag(Flags.N) == GetFlag(Flags.V));

                // LE / Signed less than or equal
                case 0b1101:
                    return (GetFlag(Flags.Z) == 1) || (GetFlag(Flags.N) != GetFlag(Flags.V));

                // AL / Always (unconditional)
                case 0b1110:
                    return true;

                // UNPREDICTABLE
                case 0b1111:
                    return false;

                // Unimplemented
                default:
                    Console.WriteLine("Condition {0} unimplemented", cond);
                    Environment.Exit(1);
                    return false;
            }
        }

        private bool InPrivilegedMode()
        {
            return (cpsr & 0b1_1111) != 0b1_0000; // mode != user
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
        //                          ARM instructions
        // ********************************************************************

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

        private (UInt32 shifterOperand, UInt32 shifterCarryOut) GetShifterOperandAndCarryOut(UInt32 instruction)
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
                                shifterOperand = reg[rm];
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else
                            {
                                shifterOperand = LogicalShiftLeft(reg[rm], (int)shiftImm);
                                shifterCarryOut = (reg[rm] >> (32 - (int)shiftImm)) & 1;
                            }
                            break;

                        case 0b01: // Logical shift right
                            if (shiftImm == 0)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = reg[rm] >> 31;
                            }
                            else
                            {
                                shifterOperand = LogicalShiftRight(reg[rm], (int)shiftImm);
                                shifterCarryOut = (reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;

                        case 0b10: // Arithmetic shift right
                            if (shiftImm == 0)
                            {
                                if ((reg[rm] & 0x8000_0000) == 0)
                                    shifterOperand = 0;
                                else
                                    shifterOperand = 0xffff_ffff;

                                shifterCarryOut = reg[rm] >> 31;
                            }
                            else
                            {
                                shifterOperand = ArithmeticShiftRight(reg[rm], (int)shiftImm);
                                shifterCarryOut = (reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;

                        case 0b11: // Rotate right
                            if (shiftImm == 0)
                            {
                                shifterOperand = LogicalShiftLeft(GetFlag(Flags.C), 31) | LogicalShiftRight(reg[rm], 1);
                                shifterCarryOut = reg[rm] & 1;
                            }
                            else
                            {
                                shifterOperand = RotateRight(reg[rm], (int)shiftImm);
                                shifterCarryOut = (reg[rm] >> ((int)shiftImm - 1)) & 1;
                            }
                            break;
                    }
                }
                else if (((instruction >> 7) & 1) == 0) // Register shifts
                {
                    UInt32 rs = (instruction >> 8) & 0b1111;
                    switch (shift)
                    {
                        case 0b00: // Logical shift left
                            if ((reg[rs] & 0xff) == 0)
                            {
                                shifterOperand = reg[rm];
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else if ((reg[rs] & 0xff) < 32)
                            {
                                shifterOperand = LogicalShiftLeft(reg[rm], (int)(reg[rs] & 0xff));
                                shifterCarryOut = (reg[rm] >> (32 - (int)(reg[rs] & 0xff))) & 1;
                            }
                            else if ((reg[rs] & 0xff) == 32)
                            {
                                shifterOperand = 0;
                                shifterCarryOut = reg[rm] & 1;
                            }
                            else
                            {
                                shifterOperand = 0;
                                shifterCarryOut = 0;
                            }
                            break;

                        case 0b10: // Arithmetic shift right
                            if ((reg[rs] & 0xff) == 0)
                            {
                                shifterOperand = reg[rm];
                                shifterCarryOut = GetFlag(Flags.C);
                            }
                            else if ((reg[rs] & 0xff) < 32)
                            {
                                shifterOperand = ArithmeticShiftRight(reg[rm], (int)(reg[rs] & 0xff));
                                shifterCarryOut = (reg[rm] >> ((int)(reg[rs] & 0xff) - 1)) & 1;
                            }
                            else
                            {
                                if ((reg[rm] & 0x8000_0000) == 0)
                                    shifterOperand = 0;
                                else
                                    shifterOperand = 0xffff_ffff;

                                shifterCarryOut = reg[rm] >> 31;
                            }
                            break;

                        default:
                            Console.WriteLine("CPU: encoding unimplemented");
                            Environment.Exit(1);
                            break;
                    }
                }
                else
                {
                    throw new Exception("CPU: Wrong encoding");
                }
            }

            return (shifterOperand, shifterCarryOut);
        }

        private static void ARM_ADC(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                // Addressing mode 1
                var (shifterOperand, _) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = cpu.reg[rn] + shifterOperand + cpu.GetFlag(Flags.C);

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
                        // TODO: C flag
                        // TODO: V flag
                    }
                }
            }
        }

        private static void ARM_ADD(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                // Addressing mode 1
                var (shifterOperand, _) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = cpu.reg[rn] + shifterOperand;

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
                        // TODO: C flag
                        // TODO: V flag
                    }
                }
            }
        }

        private static void ARM_AND(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                // Addressing mode 1
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = cpu.reg[rn] & shifterOperand;

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
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
                // Addressing mode 1
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = cpu.reg[rn] & ~shifterOperand;

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
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
                // Addressing mode 1
                var (shifterOperand, _) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 aluOut = cpu.reg[rn] + shifterOperand;

                cpu.SetFlag(Flags.N, aluOut >> 31);
                cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                // TODO: C flag
                // TODO: V flag
            }
        }

        private static void ARM_CMP(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                // Addressing mode 1
                var (shifterOperand, _) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 aluOut = cpu.reg[rn] - shifterOperand;

                cpu.SetFlag(Flags.N, aluOut >> 31);
                cpu.SetFlag(Flags.Z, (aluOut == 0) ? 1u : 0u);
                // TODO: C flag
                // TODO: V flag
            }
        }

        private static void ARM_EOR(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                // Addressing mode 1
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = cpu.reg[rn] ^ shifterOperand;

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
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
                cpu.reg[rd] = cpu.reg[rm] * cpu.reg[rs] + cpu.reg[rn];

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
                        // TODO: C flag
                    }
                }
            }
        }

        private static void ARM_MOV(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                // Addressing mode 1
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = shifterOperand;

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
                        cpu.SetFlag(Flags.C, shifterCarryOut);
                    }
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
                cpu.reg[rd] = cpu.reg[rm] * cpu.reg[rs];

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
                        // TODO: C flag
                    }
                }
            }
        }

        private static void ARM_MVN(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                // Addressing mode 1
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = ~shifterOperand;

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
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
                // Addressing mode 1
                var (shifterOperand, shifterCarryOut) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = cpu.reg[rn] | shifterOperand;

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
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
                // Addressing mode 1
                var (shifterOperand, _) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = shifterOperand - cpu.reg[rn] - ~cpu.GetFlag(Flags.C);

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
                        // TODO: C flag
                        // TODO: V flag
                    }
                }
            }
        }

        private static void ARM_SBC(CPU cpu, UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (cpu.ConditionPassed(cond))
            {
                // Addressing mode 1
                var (shifterOperand, _) = cpu.GetShifterOperandAndCarryOut(instruction);

                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                cpu.reg[rd] = cpu.reg[rn] - shifterOperand - ~cpu.GetFlag(Flags.C);

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
                        cpu.SetFlag(Flags.N, cpu.reg[rd] >> 31);
                        cpu.SetFlag(Flags.Z, (cpu.reg[rd] == 0) ? 1u : 0u);
                        // TODO: C flag
                        // TODO: V flag
                    }
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
                cpu.reg[rdHi] = (cpu.reg[rm] * cpu.reg[rs]) >> 32;
                cpu.reg[rdLo] = cpu.reg[rm] * cpu.reg[rs];

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    cpu.SetFlag(Flags.N, cpu.reg[rdHi] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.reg[rdHi] == 0 && cpu.reg[rdLo] == 0) ? 1u : 0u);
                    // TODO: C flag
                    // TODO: V flag
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
                cpu.reg[rdHi] = (cpu.reg[rm] * cpu.reg[rs]) >> 32;
                cpu.reg[rdLo] = cpu.reg[rm] * cpu.reg[rs];

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    cpu.SetFlag(Flags.N, cpu.reg[rdHi] >> 31);
                    cpu.SetFlag(Flags.Z, (cpu.reg[rdHi] == 0 && cpu.reg[rdLo] == 0) ? 1u : 0u);
                    // TODO: C flag
                    // TODO: V flag
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

                UInt32 address = reg[rn];
                if (u == 1)
                {
                    reg[rn] += offset;
                }
                else
                {
                    reg[rn] -= offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                callbacks.WriteMemory16(address, (UInt16)reg[rd]);
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
                    address = reg[rn] + offset;
                }
                else
                {
                    address = reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                callbacks.WriteMemory16(address, (UInt16)reg[rd]);
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

                UInt32 address = reg[rn];
                if (u == 1)
                {
                    reg[rn] += offset;
                }
                else
                {
                    reg[rn] -= offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                reg[rd] = callbacks.ReadMemory16(address);
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
                    address = reg[rn] + offset;
                }
                else
                {
                    address = reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                reg[rd] = callbacks.ReadMemory16(address);
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
                cpsr = (cpsr & ~(1u << 5)) | (reg[rm] & 1) << 5;
                reg[PC] = reg[rm] & 0xffff_fffe;
            }
        }

        // MSR (register)
        private void ARM_MoveToStatusRegister_Register(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rm = instruction & 0b1111;
                UInt32 operand = reg[rm];

                const UInt32 unallocMask = 0x0fff_ff00;
                if ((operand & unallocMask) != 0)
                {
                    Console.WriteLine("MSR (register): UNPREDICTABLE (attempt to set reserved bits)");
                    Environment.Exit(1);
                }

                UInt32 fieldMask = (instruction >> 16) & 0b1111;
                UInt32 byteMask = (UInt32)(((fieldMask & 0b0001) != 0) ? 0x0000_00ff : 0)
                                | (UInt32)(((fieldMask & 0b0010) != 0) ? 0x0000_ff00 : 0)
                                | (UInt32)(((fieldMask & 0b0100) != 0) ? 0x00ff_0000 : 0)
                                | (UInt32)(((fieldMask & 0b1000) != 0) ? 0xff00_0000 : 0);

                UInt32 r = (instruction >> 22) & 1;
                if (r == 0)
                {
                    const UInt32 userMask = 0xf0000000;

                    UInt32 mask = 0;
                    if (InPrivilegedMode())
                    {
                        const UInt32 stateMask = 0x00000020;
                        if ((operand & stateMask) != 0)
                        {
                            Console.WriteLine("MSR (register): UNPREDICTABLE (attempt to set non-ARM execution state)");
                            Environment.Exit(1);
                        }
                        else
                        {
                            const UInt32 privMask = 0x0000000f;
                            mask = byteMask & (userMask | privMask);
                        }
                    }
                    else
                    {
                        mask = byteMask & userMask;
                    }

                    cpsr = (cpsr & ~mask) | (operand & mask);
                }
                else
                {
                    Console.WriteLine("MSR (register): R field unimplemented");
                    Environment.Exit(1);
                }
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
                reg[rd] = reg[rn] - shifterOperand;

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
                        SetFlag(Flags.N, reg[rd] >> 31);
                        SetFlag(Flags.Z, (reg[rd] == 0) ? 1u : 0u);
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
                UInt32 aluOut = reg[rn] & shifterOperand;
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
                UInt32 aluOut = reg[rn] ^ shifterOperand;
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

                UInt32 address = reg[rn];
                if (u == 1)
                {
                    reg[rn] += offset;
                }
                else
                {
                    reg[rn] -= offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                reg[rd] = callbacks.ReadMemory8(address);
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
                    address = reg[rn] + offset;
                }
                else
                {
                    address = reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                reg[rd] = callbacks.ReadMemory8(address);
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
                    address = reg[rn] + offset;
                }
                else
                {
                    address = reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                callbacks.WriteMemory8(address, (Byte)reg[rd]);
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

                UInt32 address = reg[rn];
                if (u == 1)
                {
                    reg[rn] += offset;
                }
                else
                {
                    reg[rn] -= offset;
                }

                UInt32 data = callbacks.ReadMemory32(address);
                UInt32 rd = (instruction >> 12) & 0b1111;

                if (rd == PC)
                {
                    reg[PC] = data & 0xffff_fffc;
                }
                else
                {
                    reg[rd] = data;
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
                    address = reg[rn] + offset;
                }
                else
                {
                    address = reg[rn] - offset;
                }

                UInt32 data = callbacks.ReadMemory32(address);
                UInt32 rd = (instruction >> 12) & 0b1111;

                if (rd == PC)
                {
                    reg[PC] = data & 0xffff_fffc;
                }
                else
                {
                    reg[rd] = data;
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

                UInt32 address = reg[rn];
                if (u == 1)
                {
                    reg[rn] += offset;
                }
                else
                {
                    reg[rn] -= offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                callbacks.WriteMemory32(address, reg[rd]);
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
                    address = reg[rn] + offset;
                }
                else
                {
                    address = reg[rn] - offset;
                }

                UInt32 rd = (instruction >> 12) & 0b1111;
                callbacks.WriteMemory32(address, reg[rd]);
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

                UInt32 startAddress = reg[rn] - (Number_Of_Set_Bits_In(registerList, 16) * 4);
                UInt32 endAddress = reg[rn] - 4;

                UInt32 w = (instruction >> 21) & 1;
                if (w == 1)
                {
                    reg[rn] -= (Number_Of_Set_Bits_In(registerList, 16) * 4);
                }

                UInt32 address = startAddress;
                for (int i = 0; i <= 15; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        callbacks.WriteMemory32(address, reg[i]);
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

                UInt32 startAddress = reg[rn];
                UInt32 endAddress = reg[rn] + (Number_Of_Set_Bits_In(registerList, 16) * 4) - 4;

                UInt32 w = (instruction >> 21) & 1;
                if (w == 1)
                {
                    reg[rn] += (Number_Of_Set_Bits_In(registerList, 16) * 4);
                }

                UInt32 address = startAddress;
                for (int i = 0; i <= 14; ++i)
                {
                    if (((registerList >> i) & 1) == 1)
                    {
                        reg[i] = callbacks.ReadMemory32(address);
                        address += 4;
                    }
                }

                if (((registerList >> 15) & 1) == 1)
                {
                    UInt32 value = callbacks.ReadMemory32(address);
                    reg[PC] = value & 0xffff_fffc;
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
                reg[LR] = nextInstructionAddress;

                UInt32 imm = instruction & 0xff_ffff;
                reg[PC] += (SignExtend_30(imm, 24) << 2);
            }
        }

        // B
        private void ARM_Branch(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 imm = instruction & 0xff_ffff;
                reg[PC] += (SignExtend_30(imm, 24) << 2);
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
                reg[rd] = reg[rm];
            }
            else
            {
                SetFlag(Flags.C, (reg[rm] >> (32 - imm)) & 1);
                reg[rd] = reg[rm] << imm;
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
            reg[rd] = reg[rn] + reg[rm];
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
            reg[rd] = reg[rn] - reg[rm];
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
            reg[rd] = imm;
            // TODO: N flag
            // TODO: Z flag
        }

        // SUB (immediate)
        private void THUMB_Subtract_Immediate(UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);
            reg[rd] -= imm;
            // TODO: N flag
            SetFlag(Flags.Z, (reg[rd] == 0) ? 1u : 0u);
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
            UInt32 address = (reg[PC] & 0xffff_fffc) + (UInt32)(imm * 4);
            reg[rd] = callbacks.ReadMemory32(address);
        }

        // ==============================
        // Data-processing register
        // ==============================

        // BIC
        private void THUMB_BitClear(UInt16 instruction)
        {
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            UInt16 rd = (UInt16)(instruction & 0b111);
            reg[rd] &= ~reg[rm];
            // TODO: N flag
            SetFlag(Flags.Z, (reg[rd] == 0) ? 1u : 0u);
        }

        // ==============================
        // Branch/exchange instruction set
        // ==============================

        // BX
        private void THUMB_BranchExchange(UInt16 instruction)
        {
            UInt16 h2 = (UInt16)((instruction >> 6) & 1);
            UInt16 rm = (UInt16)((instruction >> 3) & 0b111);
            cpsr = (cpsr & ~(1u << 5)) | (reg[(h2 << 3) | rm] & 1) << 5;
            reg[PC] = reg[(h2 << 3) | rm] & 0xffff_fffe;
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
            reg[(h1 << 3) | rd] = reg[(h2 << 3) | rm];
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
            UInt32 address = reg[rn] + ((UInt32)imm * 4);
            callbacks.WriteMemory32(address, reg[rd]);
        }

        // ==============================
        // Miscellaneous
        // ==============================

        // POP
        private void THUMB_PopMultipleRegisters(UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);
            UInt32 startAddress = reg[SP];
            UInt32 endAddress = reg[SP] + 4 * (r + Number_Of_Set_Bits_In(registerList, 8));
            UInt32 address = startAddress;

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    reg[i] = callbacks.ReadMemory32(address);
                    address += 4;
                }
            }

            if (r == 1)
            {
                UInt32 value = callbacks.ReadMemory32(address);
                reg[PC] = value & 0xffff_fffe;
            }

            reg[SP] = endAddress;
        }

        // PUSH
        private void THUMB_PushMultipleRegisters(UInt16 instruction)
        {
            UInt16 r = (UInt16)((instruction >> 8) & 1);
            UInt16 registerList = (UInt16)(instruction & 0xff);
            UInt32 startAddress = reg[SP] - 4 * (r + Number_Of_Set_Bits_In(registerList, 8));
            UInt32 endAddress = reg[SP] - 4;
            UInt32 address = startAddress;

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    callbacks.WriteMemory32(address, reg[i]);
                    address += 4;
                }
            }

            if (r == 1)
            {
                callbacks.WriteMemory32(address, reg[LR]);
            }

            reg[SP] -= 4 * (r + Number_Of_Set_Bits_In(registerList, 8));
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
                reg[PC] += SignExtend(imm, 8) << 1;
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
            UInt32 startAddress = reg[rn];
            UInt32 endAddress = reg[rn] + (Number_Of_Set_Bits_In(registerList, 8) * 4) - 4;
            UInt32 address = startAddress;

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    reg[i] = callbacks.ReadMemory32(address);
                    address += 4;
                }
            }

            reg[rn] += Number_Of_Set_Bits_In(registerList, 8) * 4;
        }

        // STMIA
        private void THUMB_StoreMultipleIncrementAfter(UInt16 instruction)
        {
            UInt16 rn = (UInt16)((instruction >> 8) & 0b111);
            UInt16 registerList = (UInt16)(instruction & 0xff);
            UInt32 startAddress = reg[rn];
            UInt32 endAddress = reg[rn] + (Number_Of_Set_Bits_In(registerList, 8) * 4) - 4;
            UInt32 address = startAddress;

            for (int i = 0; i <= 7; ++i)
            {
                if (((registerList >> i) & 1) == 1)
                {
                    callbacks.WriteMemory32(address, reg[i]);
                    address += 4;
                }
            }

            reg[rn] += Number_Of_Set_Bits_In(registerList, 8) * 4;
        }

        // ==============================
        // BL prefix
        // ==============================

        private void THUMB_BranchLink_Prefix(UInt16 instruction)
        {
            UInt16 offset = (UInt16)(instruction & 0x7ff);
            reg[LR] = reg[PC] + (SignExtend(offset, 11) << 12);
        }

        // ==============================
        // BL suffix
        // ==============================
        private void THUMB_BranchLink_Suffix(UInt16 instruction)
        {
            UInt16 offset = (UInt16)(instruction & 0x7ff);
            reg[PC] = reg[LR] + ((UInt32)offset << 1);
            reg[LR] = nextInstructionAddress | 1;
        }
    }
}
