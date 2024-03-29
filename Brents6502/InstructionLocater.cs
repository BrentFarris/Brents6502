﻿using Brents6502.Assembling;
using Brents6502.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Brents6502
{
    public class InstructionLocater
    {
        private readonly List<IInstruction> _instructions = new List<IInstruction>();

        public InstructionLocater()
        {
            _instructions = Assembly.GetCallingAssembly()
                .GetTypes().Where(t => typeof(IInstruction).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => Activator.CreateInstance(t)).Cast<IInstruction>().ToList();
        }

        public IInstruction GetInstructionForLine(Line line)
        {
            var filtered = _instructions.Where(i => i.Mnemonic.ToUpper() == line.Instruction.Source.ToUpper());
            if (!filtered.Any())
                throw new Exception($"The instruction {line.Instruction.Source} on line {line.LineNumber} is not a valid instruction ({line.Source})");
            InstructionType type = InstructionType.None;
            if (line.Arguments.Length > 0)
                type = line.Arguments[0].GetArgType();
            var selected = filtered.FirstOrDefault(i => i.ArgType == type);
            if (selected == null)
                throw new Exception($"The instruction {line.Instruction.Source} on line {line.LineNumber} has invalid arguments ({line.Source})");
            return selected;
        }

        public override string ToString()
        {
            StringBuilder table = new StringBuilder();
            var sorted = _instructions.OrderBy(o => o.Mnemonic);
            int counter = 0;
            foreach (var i in sorted.GroupBy(g => g.Mnemonic))
            {
                if (counter++ != 0)
                    table.Append($" / ");

                table.Append($"[{i.Key}](#{i.Key})");
            }

            table.AppendLine("\n");

            table.AppendLine("| Mnemonic | Argument | OpCode | Flags | Clock | SkipClock | BoundsClock |");
            table.AppendLine("| :------: | :------: | :----: | :---: | :---: | :-------: | :---------: |");
            foreach (var i in sorted)
            {
                List<char> flags = new List<char>();
                if ((i.AffectedFlags & (int)ProcessorFlags.Negative) != 0)
                    flags.Add('N');
                if ((i.AffectedFlags & (int)ProcessorFlags.Overflow) != 0)
                    flags.Add('O');
                if ((i.AffectedFlags & (int)ProcessorFlags.B0) != 0)
                    flags.Add('-');
                if ((i.AffectedFlags & (int)ProcessorFlags.B1) != 0)
                    flags.Add('B');
                if ((i.AffectedFlags & (int)ProcessorFlags.Decimal) != 0)
                    flags.Add('D');
                if ((i.AffectedFlags & (int)ProcessorFlags.InterruptDisable) != 0)
                    flags.Add('I');
                if ((i.AffectedFlags & (int)ProcessorFlags.Zero) != 0)
                    flags.Add('Z');
                if ((i.AffectedFlags & (int)ProcessorFlags.Carry) != 0)
                    flags.Add('C');

                string argument;
                switch (i.ArgType)
                {
                    case InstructionType.Accumulator:
                        argument = "A";
                        break;
                    case InstructionType.Address:
                        argument = "$0200";
                        break;
                    case InstructionType.AddressX:
                        argument = "$0200,X";
                        break;
                    case InstructionType.AddressY:
                        argument = "$0200,Y";
                        break;
                    case InstructionType.Indirect:
                        argument = "($0200)";
                        break;
                    case InstructionType.IndirectX:
                        argument = "($09),X";
                        break;
                    case InstructionType.IndirectY:
                        argument = "($09),Y";
                        break;
                    case InstructionType.Literal:
                        argument = "#09 or #$F9";
                        break;
                    case InstructionType.ZeroPage:
                        argument = "$F9";
                        break;
                    case InstructionType.ZeroPageX:
                        argument = "$F9,X";
                        break;
                    case InstructionType.ZeroPageY:
                        argument = "$F9,Y";
                        break;
                    default:
                        argument = " ";
                        break;
                }

                table.AppendLine($"| <a name=\"{i.Mnemonic}\">{i.Mnemonic}</a> | {argument} | {$"0x{i.OperationCode.ToString("X2")}"} | {string.Join(' ', flags.ToArray())} | {i.Clocks} | {i.SkippedClocks} | {i.PageBoundaryClocks} |");
            }
            return table.ToString();
        }
    }
}
