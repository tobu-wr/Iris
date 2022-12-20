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
        public delegate UInt16 ReadMemory16(UInt32 address);
        public delegate UInt32 ReadMemory32(UInt32 address);
        public delegate void WriteMemory(UInt32 address, UInt32 value);

        private const UInt32 LR = 14;
        private const UInt32 PC = 15;

        private readonly ReadMemory16 readMemory16;
        private readonly ReadMemory32 readMemory32;
        private readonly WriteMemory writeMemory;
        private readonly UInt32[] reg = new UInt32[16];
        private UInt32 cpsr;
        private UInt32 nextInstructionAddress;

        public CPU(ReadMemory16 readMemory16, ReadMemory32 readMemory32, WriteMemory writeMemory, UInt32 startAddress)
        {
            this.readMemory16 = readMemory16;
            this.readMemory32 = readMemory32;
            this.writeMemory = writeMemory;

            nextInstructionAddress = startAddress;
            reg[PC] = nextInstructionAddress + 4;

            cpsr = 0b11111; // System mode
        }

        public void Step()
        {
            if (((cpsr >> 5) & 1) == 0) // ARM mode
            {
                if (reg[PC] != nextInstructionAddress + 4)
                {
                    nextInstructionAddress = reg[PC];
                }

                UInt32 instruction = readMemory32(nextInstructionAddress);
                nextInstructionAddress += 4;
                reg[PC] = nextInstructionAddress + 4;

                switch ((instruction >> 25) & 0b111)
                {
                    case 0b000:
                        // Multiplies & Extra load/stores
                        if ((instruction & 0x0000_0090) == 0x0000_0090)
                        {
                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                            Environment.Exit(1);
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

                        // Data processing immediate shift
                        else if ((instruction & 0x0000_0010) == 0x0000_0000)
                        {
                            Console.WriteLine("Unknown ARM instruction 0x{0:x8} at address 0x{1:x8}", instruction, nextInstructionAddress);
                            Environment.Exit(1);
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
                                case 0b0100:
                                    ARM_Add_Immediate(instruction);
                                    break;

                                case 0b1001:
                                    ARM_TestEquivalence_Immediate(instruction);
                                    break;

                                case 0b1010:
                                    ARM_Compare_Immediate(instruction);
                                    break;

                                case 0b1100:
                                    ARM_LogicalInclusiveOR_Immediate(instruction);
                                    break;

                                case 0b1101:
                                    ARM_Move_Immediate(instruction);
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
                            UInt32 l = (instruction >> 20) & 1;
                            if (l == 1)
                            {
                                ARM_LoadRegister_ImmediateOffset(instruction);
                            }
                            else
                            {
                                ARM_StoreRegister_ImmediateOffset(instruction);
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

                UInt16 instruction = readMemory16(nextInstructionAddress);
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
                                        Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                        Environment.Exit(1);
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
                                    Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
                                    Environment.Exit(1);
                                }
                            }
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
                        else
                        {
                            Console.WriteLine("Unknown THUMB instruction 0x{0:x4} at address 0x{1:x8}", instruction, nextInstructionAddress);
                            Environment.Exit(1);
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

        private bool GetFlag_Z()
        {
            return ((cpsr >> 30) & 1) == 1;
        }

        private void SetFlag_Z(bool flag)
        {
            if (flag)
            {
                cpsr |= 1 << 30;
            }
            else
            {
                cpsr &= ~(1u << 30);
            }
        }

        private bool GetFlag_C()
        {
            return ((cpsr >> 29) & 1) == 1;
        }

        private void SetFlag_C(bool flag)
        {
            if (flag)
            {
                cpsr |= 1 << 29;
            }
            else
            {
                cpsr &= ~(1u << 29);
            }
        }

        private bool ConditionPassed(UInt32 cond)
        {
            switch (cond)
            {
                // EQ / Equal
                case 0b0000:
                    return GetFlag_Z();

                // CS/HS / Carry set/unsigned higher or same
                case 0b0010:
                    return GetFlag_C();

                // AL / Always (unconditional)
                case 0b1110:
                    return true;

                // UNPREDICTABLE
                case 0b1111:
                    Console.WriteLine("Condition {0} UNPREDICTABLE", 0b1111);
                    Environment.Exit(1);
                    return false;

                // Unimplemented
                default:
                    Console.WriteLine("Condition {0} unimplemented", cond);
                    Environment.Exit(1);
                    return false;
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

        private bool InPrivilegedMode()
        {
            return (cpsr & 0b1_1111) != 0b1_0000; // mode != user
        }

        // ********************************************************************
        //                                  ARM
        // ********************************************************************

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
                cpsr |= (reg[rm] & 1) << 5;
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

        // ADD (immediate)
        private void ARM_Add_Immediate(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                int rotateAmount = 2 * (int)rotateImm;
                UInt32 shifterOperand = (imm >> rotateAmount) | (imm << (32 - rotateAmount));
                reg[rd] = reg[rn] + shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    Console.WriteLine("ADD (immediate): S field unimplemented");
                    Environment.Exit(1);
                }
            }
        }

        // TEQ (immediate)
        private void ARM_TestEquivalence_Immediate(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                int rotateAmount = 2 * (int)rotateImm;
                UInt32 shifterOperand = (imm >> rotateAmount) | (imm << (32 - rotateAmount));
                UInt32 aluOut = reg[rn] ^ shifterOperand;
                // TODO: N flag
                SetFlag_Z(aluOut == 0);
                // TODO: C flag
            }
        }

        // CMP (immediate)
        private void ARM_Compare_Immediate(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                int rotateAmount = 2 * (int)rotateImm;
                UInt32 shifterOperand = (imm >> rotateAmount) | (imm << (32 - rotateAmount));
                UInt32 aluOut = reg[rn] - shifterOperand;
                // TODO: N flag
                SetFlag_Z(aluOut == 0);
                // TODO: C flag
                // TODO: V flag
            }
        }

        // ORR (immediate)
        private void ARM_LogicalInclusiveOR_Immediate(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rn = (instruction >> 16) & 0b1111;
                UInt32 rd = (instruction >> 12) & 0b1111;
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                int rotateAmount = 2 * (int)rotateImm;
                UInt32 shifterOperand = (imm >> rotateAmount) | (imm << (32 - rotateAmount));
                reg[rd] = reg[rn] | shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    Console.WriteLine("ORR (immediate): S field unimplemented");
                    Environment.Exit(1);
                }
            }
        }

        // MOV (immediate)
        private void ARM_Move_Immediate(UInt32 instruction)
        {
            UInt32 cond = (instruction >> 28) & 0b1111;
            if (ConditionPassed(cond))
            {
                UInt32 rd = (instruction >> 12) & 0b1111;
                UInt32 rotateImm = (instruction >> 8) & 0b1111;
                UInt32 imm = instruction & 0xff;

                int rotateAmount = 2 * (int)rotateImm;
                UInt32 shifterOperand = (imm >> rotateAmount) | (imm << (32 - rotateAmount));
                reg[rd] = shifterOperand;

                UInt32 s = (instruction >> 20) & 1;
                if (s == 1)
                {
                    Console.WriteLine("MOV (immediate): S field unimplemented");
                    Environment.Exit(1);
                }
            }
        }

        // ==============================
        // Load/store immediate offset
        // ==============================

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

                UInt32 data = readMemory32(address);

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
                writeMemory(address, reg[rd]);
            }
        }

        // ==============================
        // Branch and branch with link
        // ==============================

        // BL
        private void ARM_BranchLink(UInt32 instruction)
        {
            Console.WriteLine("BL unimplemented");
            Environment.Exit(1);
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
        //                                  THUMB
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
                SetFlag_C(((reg[rm] >> (32 - imm)) & 1) == 1);
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

        // ==============================
        // Load from literal pool
        // ==============================

        // LDR (PC relative)
        private void THUMB_LoadRegister_PCRelative(UInt16 instruction)
        {
            UInt16 rd = (UInt16)((instruction >> 8) & 0b111);
            UInt16 imm = (UInt16)(instruction & 0xff);
            UInt32 address = (reg[PC] & 0xffff_fffc) + (UInt32)(imm * 4);
            reg[rd] = readMemory32(address);
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
            SetFlag_Z(reg[rd] == 0);
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
