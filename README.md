# Iris

Iris is a GameBoy Advance emulator (and maybe DS in the future).

## Roadmap

- Implement all missing instructions
- Simplify instruction decoding and refact their implementation along the way
- Pass all ARMWrestler's tests
- Pass arm.gba and thumb.gba tests
- Pass all FuzzArm's tests
- Boot to menu all games from compatibility list
- Optimize if <60fps in release build
- TBD

## Resources used

- The Official Gameboy Advance Programming Manual
- The ARM Architecture Reference Manual
- [GBATEK](https://problemkaputt.de/gbatek.htm)
- ARMWrestler test ROM
- [gba-tests test ROMs](https://github.com/jsmolka/gba-tests) (arm.gba and thumb.gba)
- [FuzzARM test ROMs](https://github.com/DenSinH/FuzzARM)
