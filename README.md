# Iris

Iris is a WIP GameBoy Advance emulator (and maybe DS in the future). I wanted to emulate the GameBoy Advance and needed a project as a playground to learn the C# language so I started this.

## TODOLIST

### v1

- GBA:
  - BIOS LLE
  - Missing PPU features used in Pokemon Mystery Dungeon (sprites, etc..)
  - Pokemon Mystery Dungeon in playable state

- NDS:
  - TinyFB test ROM
  - Missing ARMv5TE instructions
  - Pass ARMWrestler test ROM
  - BIOS + Firmware LLE
  - All PPU features used in Pokemon Mystery Dungeon
  - Pokemon Mystery Dungeon in playable state
  - Rudimentary audio

- Common:
  - Limit framerate to 60 FPS
  - Load/save states
  - Error reporting (ROM infos, emulator version, emulator state, etc..)

### v2

- GBA: Rudimentary audio
- Common: Use OpenGL to make rendering faster with Silk.NET

### v3

- GBA: Kirby Nightmare in Dreamland in playable state

### Later

- GBA:
  - Be able to boot from BIOS LLE
  - Waitstates
  - Instruction pipeline flush timing
  - Prefetch buffer
  - BIOS HLE timings

- NDS:
  - Be able to boot from BIOS/Firmware LLE
  - Timings (same as GBA: ARM946E-S instruction timings, waitstates, instruction pipeline flush, BIOS HLE, etc..)

- Common:
  - Add settings dialog
  - Add option to choose between BIOS HLE and BIOS LLE
  - Add option to enable/disable framerate limiter
  - Add about dialog

## Tested games

### GBA

None atm

### NDS

None atm

## Screenshots

<p align="center">
  <img src="Screenshots/Capture.PNG"/>
  <img src="Screenshots/Capture-2.PNG"/>
</p>

## Resources

- The Official Gameboy Advance Programming Manual
- ARM Architecture Reference Manual
- ARM7TDMI Technical Reference Manual
- [GBATEK](https://problemkaputt.de/gbatek.htm)
- [ARMWrestler test ROM](https://github.com/destoer/armwrestler-gba-fixed)
- [gba-tests test ROMs](https://github.com/jsmolka/gba-tests) (arm.gba and thumb.gba)
- [FuzzARM test ROMs](https://github.com/DenSinH/FuzzARM)
- [TONC demos](https://www.coranac.com/tonc/text/toc.htm) (key_demo.gba)
