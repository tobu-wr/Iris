using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Iris.UserInterface
{
    internal partial class MainWindow : Form
    {
        private Common.System _system;
        private Thread _systemThread;

        private readonly Keyboard _keyboard;
        private readonly XboxController _xboxController;

        private Dictionary<Keyboard.Key, Common.System.Key> _keyboardMapping = new()
        {
            { Keyboard.Key.A, Common.System.Key.A },
            { Keyboard.Key.Z, Common.System.Key.B },
            { Keyboard.Key.Space, Common.System.Key.Select },
            { Keyboard.Key.Return, Common.System.Key.Start },
            { Keyboard.Key.Right, Common.System.Key.Right },
            { Keyboard.Key.Left, Common.System.Key.Left },
            { Keyboard.Key.Up, Common.System.Key.Up },
            { Keyboard.Key.Down, Common.System.Key.Down },
            { Keyboard.Key.S, Common.System.Key.R },
            { Keyboard.Key.Q, Common.System.Key.L },
            { Keyboard.Key.E, Common.System.Key.X },
            { Keyboard.Key.R, Common.System.Key.Y },
        };

        private Dictionary<XboxController.Button, Common.System.Key> _xboxControllerMapping = new()
        {
            { XboxController.Button.A, Common.System.Key.A },
            { XboxController.Button.B, Common.System.Key.B },
            { XboxController.Button.Back, Common.System.Key.Select },
            { XboxController.Button.Start, Common.System.Key.Start },
            { XboxController.Button.DPadRight, Common.System.Key.Right },
            { XboxController.Button.DPadLeft, Common.System.Key.Left },
            { XboxController.Button.DPadUp, Common.System.Key.Up },
            { XboxController.Button.DPadDown, Common.System.Key.Down },
            { XboxController.Button.RightShoulder, Common.System.Key.R },
            { XboxController.Button.LeftShoulder, Common.System.Key.L },
            { XboxController.Button.X, Common.System.Key.X },
            { XboxController.Button.Y, Common.System.Key.Y },
        };

        private FormWindowState _previousWindowState;

        private readonly Stopwatch _frameStopwatch = new();
        private int _frameCount;
        private long _frameDuration;
        private long _squareFrameDuration;
        private long _minFrameDuration;
        private long _maxFrameDuration;
        private readonly System.Windows.Forms.Timer _performanceCounterTimer = new();

        private bool _framerateLimiterEnabled = Properties.Settings.Default.FramerateLimiterEnabled;
        private readonly Stopwatch _framerateLimiterStopwatch = Stopwatch.StartNew();
        private long _framerateLimiterLastFrameTime;

        private bool _automaticPauseEnabled = Properties.Settings.Default.AutomaticPauseEnabled;
        private bool _resume;

        private bool _skipIntroEnabled = Properties.Settings.Default.SkipIntroEnabled;

        private const int TextureWidth = 240;
        private const int TextureHeight = 160;

        // Cache the delegate to avoid allocating a new one each frame
        private readonly Delegate _presentFrameDelegate;

        private UInt16[] _frameBuffer;

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct TimeCaps
        {
            internal readonly uint _periodMin;
            private readonly uint _periodMax; // not used
        }

        private readonly TimeCaps _timeCaps = new();

        [LibraryImport("winmm.dll", EntryPoint = "timeGetDevCaps")]
        private static partial uint TimeGetDevCaps(ref TimeCaps tc, uint size);

        [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static partial uint TimeBeginPeriod(uint period);

        [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static partial uint TimeEndPeriod(uint period);

        internal MainWindow(string[] args)
        {
            TimeGetDevCaps(ref _timeCaps, (uint)Unsafe.SizeOf<TimeCaps>());
            _ = TimeBeginPeriod(_timeCaps._periodMin);

            System.Console.WriteLine($"[Iris.UserInterface.MainWindow] System timer resolution: {_timeCaps._periodMin}ms");
            System.Console.WriteLine($"[Iris.UserInterface.MainWindow] Stopwatch is high-resolution: {Stopwatch.IsHighResolution}");
            System.Console.WriteLine($"[Iris.UserInterface.MainWindow] Stopwatch resolution: {1_000_000_000 / Stopwatch.Frequency}ns");

            InitializeComponent();

            limitFramerateToolStripMenuItem.Checked = _framerateLimiterEnabled;
            automaticPauseToolStripMenuItem.Checked = _automaticPauseEnabled;
            skipIntroToolStripMenuItem.Checked = _skipIntroEnabled;

#if DEBUG
            Text += " (DEBUG)";
#elif RELEASE_DEV
            Text += " (DEV)";
#endif

            Activated += MainWindow_Activated;
            Deactivate += MainWindow_Deactivate;

            glControl.Load += GLControl_OnLoad;
            glControl.Resize += GLControl_OnResize;

            _keyboard = new(Keyboard_KeyDown, Keyboard_KeyUp);
            _xboxController = new(XboxController_ButtonDown, XboxController_ButtonUp);

            _performanceCounterTimer.Interval = 1000;
            _performanceCounterTimer.Tick += PerformanceCounterTimer_Tick;

            _presentFrameDelegate = PresentFrameDelegate;

            if (args.Length > 0)
                LoadROM(args[0]);
        }

        protected override void Dispose(bool disposing)
        {
            if ((_system != null) && _system.IsRunning())
                Pause();

            _ = TimeEndPeriod(_timeCaps._periodMin);

            Properties.Settings.Default.FramerateLimiterEnabled = _framerateLimiterEnabled;
            Properties.Settings.Default.AutomaticPauseEnabled = _automaticPauseEnabled;
            Properties.Settings.Default.SkipIntroEnabled = _skipIntroEnabled;
            Properties.Settings.Default.Save();

            if (disposing)
                components?.Dispose();

            base.Dispose(disposing);
        }

        private void PollInput()
        {
            if (ActiveForm != this)
                return;

            _keyboard.PollInput();
            _xboxController.PollInput();
        }

        private void PresentFrame()
        {
            // could add an option to switch between synchronous (by default) and asynchronous frame presentation to choose between framerate stability and performance (potentially with VSYNC to avoid tearing)

            Invoke(_presentFrameDelegate);

            if (_framerateLimiterEnabled)
            {
                // Force garbage collection of generations 0 and 1 to avoid slowdowns
                // Collecting generation 2 would have more performance impact with no additional benefit
                // Doing it here, at the beginning of the new frame, reduces the input latency
                // Could add an option to disable it if more performance is needed
                GC.Collect(1);

                // could add frame delay here for input latency mitigation (frame delay is better for reducing latency than syncing on input polling)
            }
        }

        private void PresentFrameDelegate()
        {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, TextureWidth, TextureHeight, PixelFormat.Rgba, PixelType.UnsignedShort1555Reversed, _frameBuffer);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

            if (_framerateLimiterEnabled)
            {
                const double TargetFrameRate = 59.737411711095921;

                long targetFrameDuration = (long)Math.Round(Stopwatch.Frequency / TargetFrameRate, MidpointRounding.AwayFromZero);
                long targetFrameTime = _framerateLimiterLastFrameTime + targetFrameDuration;
                long currentFrameTime = _framerateLimiterStopwatch.ElapsedTicks;

                if (currentFrameTime < targetFrameTime)
                {
                    long sleepTime = targetFrameTime - currentFrameTime;
                    int ms = (int)(1000 * sleepTime / Stopwatch.Frequency - _timeCaps._periodMin);

                    if (ms > 0)
                        Thread.Sleep(ms);

                    while (_framerateLimiterStopwatch.ElapsedTicks < targetFrameTime)
                    { }

                    _framerateLimiterLastFrameTime = targetFrameTime;
                }
                else
                {
                    _framerateLimiterLastFrameTime = currentFrameTime;
                }
            }

            glControl.SwapBuffers();

            long frameDuration = _frameStopwatch.ElapsedTicks;
            _frameStopwatch.Restart();

            ++_frameCount;

            _frameDuration += frameDuration;
            _squareFrameDuration += frameDuration * frameDuration;

            if (_frameCount == 1) // first frame
            {
                _minFrameDuration = frameDuration;
                _maxFrameDuration = frameDuration;
            }
            else
            {
                _minFrameDuration = Math.Min(frameDuration, _minFrameDuration);
                _maxFrameDuration = Math.Max(frameDuration, _maxFrameDuration);
            }
        }

        private void LoadROM(string fileName)
        {
            Common.System system;

            try
            {
                system = fileName.EndsWith(".gba") ? new GBA.GBA_System(PollInput, PresentFrame) : new NDS.NDS_System(PollInput, PresentFrame);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                system.LoadROM(fileName);
            }
            catch (Exception ex)
            {
                system.Dispose();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _system?.Dispose();
            _system = system;
            _system.ResetState(_skipIntroEnabled);

            _frameBuffer = _system.GetFrameBuffer();

            loadStateToolStripMenuItem.Enabled = true;
            saveStateToolStripMenuItem.Enabled = true;

            runToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            resetToolStripMenuItem.Enabled = true;

            statusToolStripStatusLabel.Text = "Paused";
        }

        private void Run()
        {
            _frameStopwatch.Restart();
            _frameCount = 0;
            _frameDuration = 0;
            _squareFrameDuration = 0;
            _minFrameDuration = 0;
            _maxFrameDuration = 0;
            _performanceCounterTimer.Start();

            runToolStripMenuItem.Enabled = false;
            pauseToolStripMenuItem.Enabled = true;

            statusToolStripStatusLabel.Text = "Running";

            _systemThread = new(RunSystemThread)
            {
                IsBackground = true,
                Name = "Iris System"
            };

            _systemThread.Start();
        }

        private void RunSystemThread()
        {
            try
            {
                _system.Run();
            }
            catch (Exception ex)
            {
                _system.Pause();
                _system.ResetState(_skipIntroEnabled);

                Invoke(() =>
                {
                    _performanceCounterTimer.Stop();

                    runToolStripMenuItem.Enabled = true;
                    pauseToolStripMenuItem.Enabled = false;

                    statusToolStripStatusLabel.Text = "Paused";
                    UpdateFpsToolStripStatusLabelText(0, 0, 0, 0);

                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        private void Pause()
        {
            _system.Pause();

            SpinWait spinWait = new();

            while (_systemThread.IsAlive)
            {
                spinWait.SpinOnce();
                Application.DoEvents();
            }

            _performanceCounterTimer.Stop();

            runToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;

            statusToolStripStatusLabel.Text = "Paused";
            UpdateFpsToolStripStatusLabelText(0, 0, 0, 0);
        }

        private void LoadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = (_system != null) && _system.IsRunning();

            if (running)
                Pause();

            OpenFileDialog dialog = new()
            {
#if RELEASE_FINAL
                Filter = "GBA files (*.gba)|*.gba|All ROM files (*.gba)|*.gba"
#else
                Filter = "GBA files (*.gba)|*.gba|NDS files (*.nds)|*.nds|All ROM files (*.gba;*.nds)|*.gba;*.nds"
#endif
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                LoadROM(dialog.FileName);

            if (running)
                Run();
        }

        private void LoadStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _system.IsRunning();

            if (running)
                Pause();

            OpenFileDialog dialog = new()
            {
                Filter = (_system is GBA.GBA_System) ? "GBA State Save files (*.gss)|*.gss" : "NDS State Save files (*.nss)|*.nss"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using FileStream fileStream = File.Open(dialog.FileName, FileMode.Open, FileAccess.Read);
                    using GZipStream gzipStream = new(fileStream, CompressionMode.Decompress);
                    using BinaryReader reader = new(gzipStream, System.Text.Encoding.UTF8, false);

                    _system.LoadState(reader);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (running)
                Run();
        }

        private void SaveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _system.IsRunning();

            if (running)
                Pause();

            SaveFileDialog dialog = new()
            {
                Filter = (_system is GBA.GBA_System) ? "GBA State Save files (*.gss)|*.gss" : "NDS State Save files (*.nss)|*.nss"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using FileStream fileStream = File.Open(dialog.FileName, FileMode.Create, FileAccess.Write);
                    using GZipStream gzipStream = new(fileStream, CompressionMode.Compress);
                    using BinaryWriter writer = new(gzipStream, System.Text.Encoding.UTF8, false);

                    _system.SaveState(writer);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (running)
                Run();
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void FullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fullScreenToolStripMenuItem.Checked)
            {
                FormBorderStyle = FormBorderStyle.None;
                _previousWindowState = WindowState;
                WindowState = FormWindowState.Normal; // mandatory
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = _previousWindowState;
            }
        }

        private void RunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Run();
        }

        private void PauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Pause();
        }

        private void ResetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = _system.IsRunning();

            if (running)
                Pause();

            _system.ResetState(_skipIntroEnabled);

            if (running)
                Run();
        }

        private void LimitFramerateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _framerateLimiterEnabled = limitFramerateToolStripMenuItem.Checked;
        }

        private void AutomaticPauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _automaticPauseEnabled = automaticPauseToolStripMenuItem.Checked;
        }

        private void SkipIntroToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _skipIntroEnabled = skipIntroToolStripMenuItem.Checked;
        }

        private void InputSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool running = (_system != null) && _system.IsRunning();

            if (running)
                Pause();

            using InputSettingsDialog dialog = new(ref _keyboardMapping, ref _xboxControllerMapping);
            dialog.ShowDialog();

            if (running)
                Run();
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            if (!_automaticPauseEnabled || (_system == null))
                return;

            if (_resume)
            {
                Run();
                _resume = false;
            }
        }

        private void MainWindow_Deactivate(object sender, EventArgs e)
        {
            if (!_automaticPauseEnabled || (_system == null))
                return;

            if (_system.IsRunning())
            {
                Pause();
                _resume = true;
            }
        }

        private void GLControl_OnLoad(object sender, EventArgs e)
        {
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            float[] vertexData = [
                1, 1, 1, 1,
                1, -1, 1, 0,
                -1, -1, 0, 0,
                -1, 1, 0, 1
            ];

            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);

            uint[] indices = [
                0, 1, 3,
                1, 2, 3
            ];

            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(float), indices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgb5, TextureWidth, TextureHeight);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            string vertexShaderSource = File.ReadAllText("VertexShader.glsl");
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            string fragmentShaderSource = File.ReadAllText("FragmentShader.glsl");
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            int shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            GL.UseProgram(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Disable VSYNC
            glControl.Context.SwapInterval = 0;
        }

        private void GLControl_OnResize(object sender, EventArgs e)
        {
            GL.Viewport(0, 0, glControl.ClientSize.Width, glControl.ClientSize.Height);
        }

        private void Keyboard_KeyDown(Keyboard.Key key)
        {
            if (_keyboardMapping.TryGetValue(key, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.Input);
        }

        private void Keyboard_KeyUp(Keyboard.Key key)
        {
            if (_keyboardMapping.TryGetValue(key, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.NoInput);
        }

        private void XboxController_ButtonDown(XboxController.Button button)
        {
            if (_xboxControllerMapping.TryGetValue(button, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.Input);
        }

        private void XboxController_ButtonUp(XboxController.Button button)
        {
            if (_xboxControllerMapping.TryGetValue(button, out Common.System.Key value))
                _system.SetKeyStatus(value, Common.System.KeyStatus.NoInput);
        }

        private void PerformanceCounterTimer_Tick(object sender, EventArgs e)
        {
            if (_frameCount == 0)
            {
                UpdateFpsToolStripStatusLabelText(0, 0, 0, 0);
                return;
            }

            double fps = Math.Round((double)_frameCount * Stopwatch.Frequency / _frameDuration, 2, MidpointRounding.AwayFromZero);

            double sdFrameDuration = Math.Sqrt((double)_squareFrameDuration / _frameCount - Math.Pow((double)_frameDuration / _frameCount, 2));
            double sdFps = Math.Round(Stopwatch.Frequency * sdFrameDuration * Math.Pow((double)_frameCount / _frameDuration, 2), 2, MidpointRounding.AwayFromZero);

            double minFps = Math.Round((double)Stopwatch.Frequency / _maxFrameDuration, 2, MidpointRounding.AwayFromZero);
            double maxFps = Math.Round((double)Stopwatch.Frequency / _minFrameDuration, 2, MidpointRounding.AwayFromZero);

            UpdateFpsToolStripStatusLabelText(fps, sdFps, minFps, maxFps);

            _frameCount = 0;
            _frameDuration = 0;
            _squareFrameDuration = 0;
            _minFrameDuration = 0;
            _maxFrameDuration = 0;
        }

        private void UpdateFpsToolStripStatusLabelText(double fps, double sd, double min, double max)
        {
            fpsToolStripStatusLabel.Text = $"FPS: {fps:F2} (sd: {sd:F2} | min: {min:F2} | max: {max:F2})";
        }
    }
}
