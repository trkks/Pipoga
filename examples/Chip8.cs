using System;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

using Pipoga;

namespace Pipoga.Examples
{
    /// <summary>
    /// Chip-8 emulation following the tutorial at:
    /// https://austinmorlan.com/posts/chip8_emulator/
    /// </summary>
    class Chip8 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        PixelDisplay screen;
        Input input;
        Emulator emu;
        double lastCycle;
        double cycleDelay;

        public Chip8(string[] args)
        {
            if (args.Length <= 1)
            {
                throw new Exception("Need ROM path");
            }

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            screen = new PixelDisplay(
                new Point(5),
                new Point(Emulator.VIDEO_WIDTH, Emulator.VIDEO_HEIGHT)
            );

            input = new Input();

            emu = new Emulator();
            emu.LoadROM(args[1]);

            lastCycle = 0.0;
            cycleDelay = 10.0; // NOTE This might need user configuration.
        }

        protected override void Initialize()
        {
            // Set the app-window according to the screen and pixel size.
            graphics.PreferredBackBufferWidth = screen.ScreenSize.X;
            graphics.PreferredBackBufferHeight = screen.ScreenSize.Y;
            graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            screen.PixelTexture = Content.Load<Texture2D>("Pixel");
        }

        protected override void Update(GameTime gameTime)
        {
            input.Update();

            if (ProcessInput())
            {
                Exit();
            }

            double dtEmu = gameTime.TotalGameTime.TotalMilliseconds - lastCycle;
            
            if (dtEmu > cycleDelay)
            {
                lastCycle = gameTime.TotalGameTime.TotalMilliseconds;
                emu.Cycle();
                screen.Plot(
                    emu.video.Select((pixelOn, i) => {
                        int x = i % Emulator.VIDEO_WIDTH; 
                        int y = i / Emulator.VIDEO_WIDTH;
                        var color = pixelOn > 0 ? Color.White : Color.Black;
                        return new Vertex(x, y, color);
                    })
                );
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            screen.Draw(spriteBatch);

            spriteBatch.End();
            base.Draw(gameTime);
        }

        bool ProcessInput()
        {
            if (input.IsKeyDown(Keys.Escape))
            {
                return true; 
            }

            /*
            * The recommended input mapping is 
            * Keypad       Keyboard
            * +-+-+-+-+    +-+-+-+-+
            * |1|2|3|C|    |1|2|3|4|
            * +-+-+-+-+    +-+-+-+-+
            * |4|5|6|D|    |Q|W|E|R|
            * +-+-+-+-+ => +-+-+-+-+
            * |7|8|9|E|    |A|S|D|F|
            * +-+-+-+-+    +-+-+-+-+
            * |A|0|B|F|    |Z|X|C|V|
            * +-+-+-+-+    +-+-+-+-+
            */

            emu.keypad[0x1] = (byte)(input.IsKeyDown(Keys.NumPad1) ? 1 : 0);
            emu.keypad[0x2] = (byte)(input.IsKeyDown(Keys.NumPad2) ? 1 : 0);
            emu.keypad[0x3] = (byte)(input.IsKeyDown(Keys.NumPad3) ? 1 : 0);
            emu.keypad[0xC] = (byte)(input.IsKeyDown(Keys.NumPad4) ? 1 : 0);
            emu.keypad[0x4] = (byte)(input.IsKeyDown(Keys.Q)       ? 1 : 0);
            emu.keypad[0x5] = (byte)(input.IsKeyDown(Keys.W)       ? 1 : 0);
            emu.keypad[0x6] = (byte)(input.IsKeyDown(Keys.E)       ? 1 : 0);
            emu.keypad[0xD] = (byte)(input.IsKeyDown(Keys.R)       ? 1 : 0);
            emu.keypad[0x7] = (byte)(input.IsKeyDown(Keys.A)       ? 1 : 0);
            emu.keypad[0x8] = (byte)(input.IsKeyDown(Keys.S)       ? 1 : 0);
            emu.keypad[0x9] = (byte)(input.IsKeyDown(Keys.D)       ? 1 : 0);
            emu.keypad[0xE] = (byte)(input.IsKeyDown(Keys.F)       ? 1 : 0);
            emu.keypad[0xA] = (byte)(input.IsKeyDown(Keys.Z)       ? 1 : 0);
            emu.keypad[0x0] = (byte)(input.IsKeyDown(Keys.X)       ? 1 : 0);
            emu.keypad[0xB] = (byte)(input.IsKeyDown(Keys.C)       ? 1 : 0);
            emu.keypad[0xF] = (byte)(input.IsKeyDown(Keys.V)       ? 1 : 0);

            return false;
        }
    }

    class Emulator
    {
        const ushort ROM_START     = 0x200;
        const ushort FONTSET_START = 0x50;
        public const ushort VIDEO_WIDTH   = 64;
        public const ushort VIDEO_HEIGHT  = 32;

        // 16 registers with labels V0 to VF; All CPU operations are based on
        // registers; VF is (specially) used as a flag.
        byte[] registers = new byte[16];

        // 3 Segments: 
        //   0x000-0x1FF: Chip-8 interpreter, now unused except for --
        //   0x050-0x0A0: -- 16 5-byte characters built into the interpreter.
        //   0x200-0xFFF: ROM instructions (+ possible free space left after)
        byte[] memory = new byte[4096];
        
        // Special register for storing memory addresses used in operations.
        ushort index;

        // Address of the next instruction to execute; an opcode is 2 bytes
        // but only 1 byte is fetched at a time so actually getting the
        // instruction from memory means fetching from PC and PC+1
        ushort pc = ROM_START;

        // Stores up to 16 addresses (from pc), that are used in function calls
        // "pushing" is the act of storing the pc's address for returning later
        // "popping" is the act of taking a stored address and setting pc to it
        ushort[] stack = new ushort[16];

        // Since popping the stack does not actually remove the addresses
        // (prob. performance/unnecessary complexity reasons), a stack pointer
        // is needed for navigation. Implemented as an index to the stack array
        byte sp;

        // If not set to 0, decrement at the specified clock rate (60Hz default)
        byte delayTimer;

        // Same behaviour as delayTimer, but a constant tone plays when non-zero
        byte soundTimer;

        // 16 input keys; states are either pressed or not pressed
        public byte[] keypad = new byte[16];

        // 64 by 32 pixels monochrome (on/off) display. A display pixel can be
        // set or unset with a sprite (XOR operation)
        public byte[] video =
            new byte[Emulator.VIDEO_WIDTH * Emulator.VIDEO_HEIGHT];

        // The current operation
        ushort opcode;

        // Fonts are available with interpreter
        byte[] fontset = new byte[] {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80, // F
        };

        // Table for handling the functions of 34 different opcodes:
		// 34 opcodes = 12 opcodes + 4 subtables = 12 + 2 + 9 + 2 + 9 opcodes
		// (Actually 35 but one is unused, see
		// http://devernay.free.fr/hacks/chip8/C8TECH10.HTM#0nnn)
        Action[] table = new Action[12 + 4];
        // Subtable for opcodes starting with '0'. 
		// NOTE This and the others contain NULL functions in indices for
		// which there is no equivalent opcode (which will cause problems with
		// bad ROMs?).
        Action[] table0 = new Action[2 + 13]; // IDs: 0 E
        // Subtable for opcodes starting with '8'.
        Action[] table8 = new Action[9 + 6];  // IDs: 0 1 2 3 4 5 6 7 E
        // Subtable for opcodes starting with 'E'.
        Action[] tableE = new Action[2 + 13]; // IDs: 1 E
        // Subtable for opcodes starting with 'F'.
        Action[] tableF = new Action[9 + 93]; // IDs: 7 A 15 8 E 9 3 55 65

        // A method for getting a random byte
        Random random;
        Func<byte> randomByte;

        public Emulator()
        {
            // Load the fonts into memory.
            for (int i = 0; i < this.fontset.Length; i++)
            {
                this.memory[FONTSET_START + i] = this.fontset[i];
            }

            // Seed the random byte.
            this.random = new Random();
            this.randomByte = () => (byte)this.random.Next(Byte.MaxValue);

            // Set up the function table.
			// First the indices of subtables:
            this.table[0x0] = () => this.table0[this.opcode & 0xF]();
            this.table[0x8] = () => this.table8[this.opcode & 0xF]();
            this.table[0xE] = () => this.tableE[this.opcode & 0xF]();
            // Two-byte mask needed to identify between 15 55 and 65.
            this.table[0xF] = () => this.tableF[this.opcode & 0xFF]();

            // Next the actual opcodes:
            this.table0[0x0] = this.OP_00E0;
            this.table0[0xE] = this.OP_00EE;

			this.table[0x1] = this.OP_1nnn;
			this.table[0x2] = this.OP_2nnn;
			this.table[0x3] = this.OP_3xkk;
            this.table[0x4] = this.OP_4xkk;
			this.table[0x5] = this.OP_5xy0;
			this.table[0x6] = this.OP_6xkk;
			this.table[0x7] = this.OP_7xkk;

			this.table8[0x0] = this.OP_8xy0;
			this.table8[0x1] = this.OP_8xy1;
			this.table8[0x2] = this.OP_8xy2;
			this.table8[0x3] = this.OP_8xy3;
			this.table8[0x4] = this.OP_8xy4;
			this.table8[0x5] = this.OP_8xy5;
			this.table8[0x6] = this.OP_8xy6;
			this.table8[0x7] = this.OP_8xy7;
			this.table8[0xE] = this.OP_8xyE;

			this.table[0x9] = this.OP_9xy0;
			this.table[0xA] = this.OP_Annn;
			this.table[0xB] = this.OP_Bnnn;
			this.table[0xC] = this.OP_Cxkk;
			this.table[0xD] = this.OP_Dxyn;

			this.tableE[0xE] = this.OP_Ex9E;
			this.tableE[0x1] = this.OP_ExA1;

			this.tableF[0x07] = this.OP_Fx07;
			this.tableF[0x0A] = this.OP_Fx0A;
			this.tableF[0x15] = this.OP_Fx15;
			this.tableF[0x18] = this.OP_Fx18;
			this.tableF[0x1E] = this.OP_Fx1E;
			this.tableF[0x29] = this.OP_Fx29;
			this.tableF[0x33] = this.OP_Fx33;
			this.tableF[0x55] = this.OP_Fx55;
			this.tableF[0x65] = this.OP_Fx65;
        }

        public void LoadROM(string filename)
        {
            byte[] rom = File.ReadAllBytes(filename);
            if (rom.Length > this.memory.Length)
            {
                throw new Exception(
                    $"ROM too big {rom.Length}. Maximum {this.memory.Length}."
                );
            }

            for (int i = 0; i < rom.Length; i++)
            {
                this.memory[ROM_START + i] = rom[i];
            }
        }

		public void Cycle()
		{
			// Fetch.
			this.opcode = (ushort)(
                (this.memory[this.pc] << 8) | this.memory[this.pc + 1]
            );

            // Move to next instruction.
            this.pc += 2;

            // Identify and call the function represented by the opcode.
            this.table[(this.opcode & 0xF000) >> 12]();

            // Each cycle counts as "time" passing:
            if (delayTimer > 0) { delayTimer--; }
            if (soundTimer > 0) { soundTimer--; }
		}

        // ============================ OPCODES ============================

        void OP_00E0() // CLS
        {
            Array.Fill<byte>(this.video, 0);
        }
        void OP_00EE() // RET
        {
            this.sp--;
            this.pc = this.stack[this.sp];
        }
        void OP_1nnn() // JP addr
        {
            ushort address = (ushort)(this.opcode & 0x0FFF);
            this.pc = address;
        }
        void OP_2nnn() // CALL addr
        {
            ushort address = (ushort)(this.opcode & 0x0FFF);
            // Save current program position
            this.stack[this.sp] = pc;
            // Create new stack frame
            this.sp++;
            // Jump to the specified subroutine
            this.pc = address;
        }
        void OP_3xkk() // SE Vx, byte
        {
            byte registerIndex = (byte)((this.opcode & 0x0F00) >> 8);
            byte byteConstant = (byte)(this.opcode & 0x00FF);

            if (this.registers[registerIndex] == byteConstant)
            {
                // Skip the next instruction
                this.pc += 2;
            }
        }
        void OP_4xkk() // SNE Vx, byte
        {
            byte registerIndex = (byte)((this.opcode & 0x0F00) >> 8);
            byte byteConstant = (byte)(this.opcode & 0x00FF);

            if (this.registers[registerIndex] != byteConstant)
            {
                // Skip the next instruction
                this.pc += 2;
            }
        }
        void OP_5xy0() // SE Vx, Vy
        {
            byte reg1 = (byte)((this.opcode & 0x0F00) >> 8);
            byte reg2 = (byte)((this.opcode & 0x00F0) >> 4);

            if (this.registers[reg1] == this.registers[reg2])
            {
                // Skip the next instruction
                this.pc += 2;
            }
        }
        void OP_6xkk() // LD Vx, byte
        {
            byte registerIndex = (byte)((this.opcode & 0x0F00) >> 8);
            byte byteConstant = (byte)(this.opcode & 0x00FF);
            // Load constant to register
            this.registers[registerIndex] = byteConstant;
        }
        void OP_7xkk() // ADD Vx, byte
        {
            byte registerIndex = (byte)((this.opcode & 0x0F00) >> 8);
            byte byteConstant = (byte)(this.opcode & 0x00FF);
            this.registers[registerIndex] += byteConstant;
        }
        void OP_8xy0() // LD Vx, Vy
        {
            byte target = (byte)((opcode & 0x0F00) >> 8);
            byte source = (byte)((opcode & 0x00F0) >> 4);
            // Load register value to another
            this.registers[target] = this.registers[source];
        }
        void OP_8xy1() // OR Vx, Vy
        {
            byte reg1 = (byte)((this.opcode & 0x0F00) >> 8);
            byte reg2 = (byte)((this.opcode & 0x00F0) >> 4);
            // OR between registers and store result into first
            this.registers[reg1] |= this.registers[reg2];
        }
        void OP_8xy2() // AND Vx, Vy
        {
            byte reg1 = (byte)((this.opcode & 0x0F00) >> 8);
            byte reg2 = (byte)((this.opcode & 0x00F0) >> 4);
            // AND between registers and store result into first
            this.registers[reg1] &= this.registers[reg2];
        }
        void OP_8xy3() // XOR Vx, Vy
        {
            byte reg1 = (byte)((this.opcode & 0x0F00) >> 8);
            byte reg2 = (byte)((this.opcode & 0x00F0) >> 4);
            // XOR between registers and store result into first
            this.registers[reg1] ^= this.registers[reg2];
        }
        void OP_8xy4() // ADD Vx, Vy (overflow)
        {
            ushort reg1 = (ushort)((this.opcode & 0x0F00) >> 8);
            ushort reg2 = (ushort)((this.opcode & 0x00F0) >> 4);
            ushort sum = (ushort)(this.registers[reg1] + this.registers[reg2]);
            if (sum > Byte.MaxValue)
            {
                // Set the overflow flag
                this.registers[0xF] = 1;
            }
            else 
            {
                this.registers[0xF] = 0;
            }
            this.registers[reg1] = (byte)(sum & 0xFF);
        }
        void OP_8xy5() // SUB Vx, Vy
        {
            ushort reg1 = (ushort)((this.opcode & 0x0F00) >> 8);
            ushort reg2 = (ushort)((this.opcode & 0x00F0) >> 4);
            
            if (this.registers[reg1] > this.registers[reg2])
            {
                // Set flag to implicate reg1 > reg2
                this.registers[0xF] = 1;
            }
            else
            {
                this.registers[0xF] = 0;
            }
            this.registers[reg1] -= this.registers[reg2];
        }
        void OP_8xy6() // SHR Vx
        {
            byte registerIndex = (byte)((this.opcode & 0x0F00) >> 8);
            // Save LSB
            this.registers[0xF] = (byte)(this.registers[registerIndex] & 1);
            // Divide by 2
            registers[registerIndex] >>= 1;
        }
        void OP_8xy7() // SUBN Vx, Vy
        {
            byte reg1 = (byte)((this.opcode & 0x0F00) >> 8);
            byte reg2 = (byte)((this.opcode & 0x00F0) >> 4);

            if (this.registers[reg2] > this.registers[reg1])
            {
                // Set flag to implicate reg2 > reg1
                this.registers[0xF] = 1;
            }
            else
            {
                this.registers[0xF] = 0;
            }
            // Subtract former from latter and save to former
            this.registers[reg1] = (byte)(this.registers[reg2] - this.registers[reg1]);
        }
        void OP_8xyE() // SHL Vx {, Vy}
        {
            byte registerIndex = (byte)((this.opcode & 0x0F00) >> 8);
            //Save MSB
            this.registers[0xF] =
                (byte)((this.registers[registerIndex] & 0x80) >> 7);
            // Multiply by 2
            this.registers[registerIndex] <<= 1;
        }
        void OP_9xy0() // SNE Vx, Vy
        {
            byte reg1 = (byte)((this.opcode & 0x0F00) >> 8);
            byte reg2 = (byte)((this.opcode & 0x00F0) >> 4);
            
            if (this.registers[reg1] != this.registers[reg2])
            {
                // Skip the next instruction
                this.pc += 2;
            }
        }
        void OP_Annn() // LD I, addr
        {
            ushort address = (ushort)(this.opcode & 0x0FFF);
            this.index = address;
        }
        void OP_Bnnn() // JP V0, addr
        {
            ushort address = (ushort)(this.opcode & 0x0FFF);
            this.pc = (ushort)(registers[0] + address);
        }
        void OP_Cxkk() // RND Vx, byte
        {
            byte registerIndex = (byte)((this.opcode & 0x0F00) >> 8);
            byte byteConstant = (byte)(this.opcode & 0x00FF);
            // Save random byte AND constant byte
            this.registers[registerIndex] = (byte)(randomByte() & byteConstant);
        }
        void OP_Dxyn() // DRW Vx, Vy, nibble
        {
            byte reg1 = (byte)((this.opcode & 0x0F00) >> 8);
            byte reg2 = (byte)((this.opcode & 0x00F0) >> 4);
            byte height = (byte)(this.opcode & 0x000F);

            // Wrap coordinates around screen boundaries
            int x = this.registers[reg1] % Emulator.VIDEO_WIDTH;
            int y = this.registers[reg2] % Emulator.VIDEO_HEIGHT;

            // Unset the collision flag
            this.registers[0xF] = 0;

            for (int row = 0; row < height; row++)
            {
                byte spriteByte = this.memory[this.index + row];
                // Sprites are 8 pixels wide
                for (int col = 0; col < 8; col++)
                {
                    byte spritePixel = (byte)(spriteByte & (0x80 >> col));
                    int videoIndex = (y + row) * Emulator.VIDEO_WIDTH + (x + col);

                    // Sprite pixel is on
                    if (spritePixel != 0)
                    {
                        // Video pixel is on
                        if (this.video[videoIndex] != 0)
                        {
                            // Set collision flag to true
                            this.registers[0xF] = 1;
                        }
                        // Draw the sprite
                        this.video[videoIndex] ^= spritePixel;
                    }
                }
            }

        }
        void OP_Ex9E() // SKP Vx
        {
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);
            byte key = this.registers[reg];

            if (this.keypad[key] != 0)
            {
                this.pc += 2;
            }
        }
        void OP_ExA1() // SKNP Vx
        {
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);
            byte key = this.registers[reg];

            if (this.keypad[key] == 0)
            {
                this.pc += 2;
            }
        }
        void OP_Fx07() // LD Vx, DT
        {
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);

            this.registers[reg] = this.delayTimer;
        }
        void OP_Fx0A() // LD Vx, K
        {
            // Wait for next keypress by running same instruction indefinitely

            byte reg = (byte)((this.opcode & 0x0F00) >> 8);

            if (this.keypad[0] != 0)
            {
                this.registers[reg] = 0;
            }
            else if (this.keypad[1] != 0)
            {
                this.registers[reg] = 1;
            }
            else if (this.keypad[2] != 0)
            {
                this.registers[reg] = 2;
            }
            else if (this.keypad[3] != 0)
            {
                this.registers[reg] = 3;
            }
            else if (this.keypad[4] != 0)
            {
                this.registers[reg] = 4;
            }
            else if (this.keypad[5] != 0)
            {
                this.registers[reg] = 5;
            }
            else if (this.keypad[6] != 0)
            {
                this.registers[reg] = 6;
            }
            else if (this.keypad[7] != 0)
            {
                this.registers[reg] = 7;
            }
            else if (this.keypad[8] != 0)
            {
                this.registers[reg] = 8;
            }
            else if (this.keypad[9] != 0)
            {
                this.registers[reg] = 9;
            }
            else if (this.keypad[10] != 0)
            {
                this.registers[reg] = 10;
            }
            else if (this.keypad[11] != 0)
            {
                this.registers[reg] = 11;
            }
            else if (this.keypad[12] != 0)
            {
                this.registers[reg] = 12;
            }
            else if (this.keypad[13] != 0)
            {
                this.registers[reg] = 13;
            }
            else if (this.keypad[14] != 0)
            {
                this.registers[reg] = 14;
            }
            else if (this.keypad[15] != 0)
            {
                this.registers[reg] = 15;
            }
            else
            {
                this.pc -= 2;
            }
        }
        void OP_Fx15() // LD DT, Vx
        {
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);
            this.delayTimer = this.registers[reg];
        }
        void OP_Fx18() // LD ST, Vx
        {
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);
            this.soundTimer = this.registers[reg];
        }
        void OP_Fx1E() // ADD I, Vx
        {
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);
            this.index += this.registers[reg];
        }
        void OP_Fx29() // LD F, Vx
        {
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);
            byte digit = this.registers[reg];

            // Get first index of one of the 5 byte font characters
            this.index = (ushort)(FONTSET_START + (5 * digit));
        }
        void OP_Fx33() // LD B, Vx
        {
            // Store a 3 byte decimal number spread to 3 memory locations
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);
            byte val = this.registers[reg];

            // Ones-place
            this.memory[this.index + 2] = (byte)(val % 10);
            val /= 10;

            // Tens-place
            this.memory[this.index + 1] = (byte)(val % 10);
            val /= 10;

            // Hundreds-place
            this.memory[this.index] = (byte)(val % 10); 
        }
        void OP_Fx55() // LD [I], Vx
        {
            // Store multiple registers into memory 
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);

            for (byte i = 0; i <= reg; i++)
            {
                this.memory[this.index + i] = this.registers[i];
            }
        }
        void OP_Fx65() // LD Vx, [I]
        {
            // Read multiple registers from memory 
            byte reg = (byte)((this.opcode & 0x0F00) >> 8);

            for (byte i = 0; i <= reg; i++)
            {
                this.registers[i] = this.memory[this.index + i];
            }
        }
    }
}
