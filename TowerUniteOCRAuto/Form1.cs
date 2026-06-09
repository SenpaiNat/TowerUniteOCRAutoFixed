using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Patagames.Ocr;

namespace TowerUniteOCRAuto
{
    public partial class Form1 : Form
    {
        // ------------------------------------------------------------------
        //  Fields
        // ------------------------------------------------------------------
        private int timesBypassed = 0;      // Number of successful AFK bypasses (letter pressed)
        private int timesClicked = 0;       // Number of times spacebar was pressed as fallback
        public static bool cont = false;    // True = automation running; toggled by hotkey
        public static bool pressImageFound = false; // Prevents spacebar from firing while a letter is being processed

        private OcrApi api;                 // Tesseract OCR engine instance
        private Keys lastKeyPressed;        // Last key pressed by user (for hotkey binding)
        private bool changed = false;       // Flag to prevent recursive textbox updates
        private PreviewForm previewForm;    // Reference to the binary preview window

        // Win32 API: send a keyboard message directly to a window (used for key simulation)
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private IntPtr towerUniteHandle = IntPtr.Zero; // Handle of the Tower Unite game window
        private KeyboardHook hook = new KeyboardHook(); // Global hotkey listener (from KeyboardHook.cs)

        // ------------------------------------------------------------------
        //  Constructor
        // ------------------------------------------------------------------
        public Form1()
        {
            InitializeComponent();   // Creates all UI controls from the designer file
            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);

            MaximizeBox = false;     // Disable window maximize button
            Console.Title = "TowerUniteOCRAuto";
            Console.WriteLine("Looking for Tower Unite handle...");

            // Find the Tower Unite process by its executable name (typical for Unreal Engine games)
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Equals("Tower-Win64-Shipping"))
                {
                    towerUniteHandle = p.MainWindowHandle;
                    Console.WriteLine("FOUND HANDLE: " + towerUniteHandle);
                }
            }

            // If game is not running, show error and quit
            if (towerUniteHandle == IntPtr.Zero)
            {
                MessageBox.Show("Tower Unite is not running.\nPlease run Tower Unite before running this application.\nClosing application.");
                Application.Exit();
                Environment.Exit(0);
            }

            Console.WriteLine("Working with your Primary Screen with dimensions: " + Screen.PrimaryScreen.Bounds.Width + "x" +
                              Screen.PrimaryScreen.Bounds.Height);
            Console.WriteLine("I recommend you TURN OFF your ChatBox as to not mess with the OCR reading.\n\tGo into Tower Unite ingame, then: Settings->Content->Disable Chat");
            Console.WriteLine("Note that as of now, the default button that will be pressed is SPACE.\n\tIf you are on Video Blackjack, go into your Tower Unite ingame, then: Settings->Controls->Mouse 1 (Click, Putt, Etc) | Rebind to SPACE");
        }

        // ------------------------------------------------------------------
        //  Helper: Get the screen rectangle to capture for OCR
        //  Uses the monitor index selected from the ComboBox.
        // ------------------------------------------------------------------
        /// <summary>
        /// Returns the rectangle (in screen coordinates) that will be captured for OCR.
        /// The monitor index is read from the GUI's monitorComboBox.
        /// If no item is selected, defaults to 0.
        /// </summary>
        private Rectangle GetCaptureRect()
        {
            // Read the monitor index from the ComboBox
            int monitorIndex = monitorComboBox.SelectedIndex;
            if (monitorIndex < 0) monitorIndex = 0; // fallback

            // Ensure it's within the valid range of connected monitors
            if (monitorIndex >= Screen.AllScreens.Length)
                monitorIndex = Screen.AllScreens.Length - 1;
            if (monitorIndex < 0) monitorIndex = 0;

            Rectangle monitorBounds = Screen.AllScreens[monitorIndex].Bounds;

            // These relative coordinates worked for the primary monitor (index 0).
            // If the AFK letter appears at a different position on other monitors,
            // you may need to adjust these values or make them configurable.
            int relativeX = 857;
            int relativeY = 425;
            int relativeX2 = 1081;
            int relativeY2 = 562;

            int x1 = monitorBounds.X + relativeX;
            int y1 = monitorBounds.Y + relativeY;
            int x2 = monitorBounds.X + relativeX2;
            int y2 = monitorBounds.Y + relativeY2;
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        // ------------------------------------------------------------------
        //  Event: Preview button click (opens the binary preview window)
        // ------------------------------------------------------------------
        private void previewButton_Click(object sender, EventArgs e)
        {
            if (previewForm == null || previewForm.IsDisposed)
                previewForm = new PreviewForm(() => GetCaptureRect());
            previewForm.Show();
        }

        // ------------------------------------------------------------------
        //  Global hotkey event: toggles automation on/off
        // ------------------------------------------------------------------
        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (StartedStoppedLabel.Text.Equals("Stopped"))
            {
                cont = true;
                bool success = false;
                StartedStoppedLabel.Text = "Started";
                int delTime = 0, ranTime = 0;
                if (int.TryParse(delayTimeTextBox.Text, out delTime) && int.TryParse(randomTimeTextBox.Text, out ranTime))
                {
                    if (delTime > 0 && ranTime >= 0)
                    {
                        // Start the OCR scanning thread and the spacebar fallback thread
                        CallRunOCRAsync(3000);
                        CallRunAutoKeyPressAsync(delTime, ranTime);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Starting auto...");
                        Console.ResetColor();
                        success = true;
                    }
                }
                if (!success)
                    MessageBox.Show("Only integer values are allowed in the time textboxes\nMake sure they are non-negative, and your delay must be a non-zero time.");
            }
            else
            {
                // Stop automation
                cont = false;
                StartedStoppedLabel.Text = "Stopped";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Stopping auto...");
                Console.ResetColor();
            }
        }

        // ------------------------------------------------------------------
        //  Form load: initialise Tesseract OCR engine and populate monitor combo
        // ------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            api = OcrApi.Create();
            api.Init(Patagames.Ocr.Enums.Languages.English);
            // Restrict recognised characters to lowercase letters only (reduces false positives)
            api.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyz");
            // Page segmentation mode 7 = treat image as a single text line (best for a single letter)
            api.SetVariable("tessedit_pageseg_mode", "7");

            // --- Populate the monitor selection ComboBox ---
            monitorComboBox.Items.Clear();
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                monitorComboBox.Items.Add(i);
            }
            if (monitorComboBox.Items.Count > 0)
                monitorComboBox.SelectedIndex = 0; // default to primary monitor
            // The info label already shows "0 = Primary, 1 = Second, etc."
        }

        // ------------------------------------------------------------------
        //  Spacebar fallback thread
        // ------------------------------------------------------------------
        private async void CallRunAutoKeyPressAsync(int msDelay, int msRand) =>
            await Task.Factory.StartNew(() => RunAutoKeyPress(msDelay, msRand));

        /// <summary>
        /// Runs in a background thread. Presses SPACE at random intervals
        /// (msDelay + random up to msRand) but only when pressImageFound == false.
        /// This ensures SPACE is pressed only when no letter is detected.
        /// </summary>
        private bool RunAutoKeyPress(int msDelay, int msRand)
        {
            Random rand = new Random();
            while (cont)
            {
                Thread.Sleep(msDelay + rand.Next(msRand) + 1);
                if (cont && !pressImageFound)
                {
                    PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)Keys.Space, IntPtr.Zero);
                    Console.Title = "TowerUniteOCRAuto - Times Clicked: " + ++timesClicked;
                }
            }
            return cont;
        }

        // ------------------------------------------------------------------
        //  OCR scanning thread
        // ------------------------------------------------------------------
        private async void CallRunOCRAsync(int ms) =>
            await Task.Factory.StartNew(() => RunOCR(ms));

        private const int WM_KEYDOWN = 0x100; // Windows message for key down

        /// <summary>
        /// Main OCR loop. Captures the defined screen area, preprocesses the image,
        /// runs Tesseract multiple times (to handle bouncing prompt), uses majority vote,
        /// and presses the recognised letter if enough votes agree.
        /// </summary>
        private bool RunOCR(int ms)
        {
            int lastTime = ms;
            Rectangle captureRect = GetCaptureRect();
            int x1 = captureRect.X;
            int y1 = captureRect.Y;
            int width = captureRect.Width;
            int height = captureRect.Height;

            while (cont)
            {
                Thread.Sleep(lastTime);
                if (!cont) continue;

                List<char> detectedChars = new List<char>();
                int numFrames = 12;       // number of screenshots per scan
                int delayBetween = 15;    // milliseconds between frames (fast capture)

                // Capture multiple frames in quick succession
                for (int frame = 0; frame < numFrames; frame++)
                {
                    using (Bitmap captured = new Bitmap(width, height))
                    using (Graphics g = Graphics.FromImage(captured))
                    {
                        // Copy the screen area into a bitmap
                        g.CopyFromScreen(x1, y1, 0, 0, captured.Size);
                        // If the area is uniform (no text), skip this frame
                        if (!IsImageVariant(captured)) continue;

                        // --- Image preprocessing (clean black-on-white) ---
                        using (Bitmap gray = GrayScale(captured))                      // convert to grayscale
                        using (Bitmap contrast = AdjustContrast(gray, 120))          // gentle contrast boost
                        using (Bitmap binary = new Bitmap(contrast.Width, contrast.Height))
                        {
                            // Binarization: pixels darker than threshold become black; lighter become white.
                            // USER ADJUSTED THRESHOLD: changed from 50 to 37 (works better)
                            for (int y = 0; y < contrast.Height; y++)
                                for (int x = 0; x < contrast.Width; x++)
                                {
                                    Color c = contrast.GetPixel(x, y);
                                    int lum = (c.R + c.G + c.B) / 3;
                                    binary.SetPixel(x, y, lum > 41 ? Color.White : Color.Black);
                                }
                            // Run Tesseract on the binary image
                            string text = api.GetTextFromImage(binary).Trim();
                            // Keep only letters, ignore spaces and punctuation
                            string cleaned = new string(text.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
                            if (cleaned.Length == 1 && char.IsLetter(cleaned[0]))
                                detectedChars.Add(cleaned[0]);
                        }
                    }
                    Thread.Sleep(delayBetween);
                }

                // --- Majority vote ---
                char finalKey = '\0';
                if (detectedChars.Count > 0)
                {
                    var groups = detectedChars.GroupBy(c => c).OrderByDescending(g => g.Count());
                    var top = groups.First();
                    // Require at least 5 identical letters out of 12 frames
                    if (top.Count() >= 4)
                        finalKey = top.Key;
                }

                // --- Act on the decision ---
                if (finalKey != '\0')
                {
                    Keys keyToPress = (Keys)Enum.Parse(typeof(Keys), finalKey.ToString().ToUpper());
                    PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)keyToPress, IntPtr.Zero);
                    Console.WriteLine($"Pressed {finalKey} (votes: {string.Join(",", detectedChars)})");
                    pressImageFound = true;
                    timesBypassed++;

                    // Update the bypass counter on the GUI (thread-safe)
                    this.Invoke(new Action(() => bypassCounterLabel.Text = $"Bypasses: {timesBypassed}"));

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(DateTime.Now.ToShortTimeString() + ": ");
                    Console.ResetColor();
                    Console.WriteLine($"Bypassed AFK check x{timesBypassed}");

                    // Save a sample screenshot for debugging
                    using (Bitmap sample = new Bitmap(width, height))
                    using (Graphics g = Graphics.FromImage(sample))
                    {
                        g.CopyFromScreen(x1, y1, 0, 0, sample.Size);
                        sample.Save("lastSuccessfulBypassImageTaken.png");
                    }

                    // 10-second cooldown after a successful bypass (prevents double-tap)
                    lastTime = 10000;
                    Task.Delay(10000).ContinueWith(_ => { pressImageFound = false; });
                }
                else
                {
                    Console.WriteLine($"No clear key (detected: {string.Join(",", detectedChars)})");
                    pressImageFound = false;
                    lastTime = ms; // normal scan interval (3 seconds)
                }
                api.Clear(); // reset Tesseract engine between scans
            }
            return cont;
        }

        // ------------------------------------------------------------------
        //  Keyboard hook UI handling
        // ------------------------------------------------------------------
        private void keybindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            hook.UnregisterHotKeys();
            lastKeyPressed = e.KeyCode;
            hook.RegisterHotKey(lastKeyPressed);
            changed = true;
            label4.Focus(); // move focus away to avoid further key events in the textbox
        }

        private void keybindTextBox_TextChanged(object sender, EventArgs e)
        {
            if (changed)
            {
                keybindTextBox.Text = lastKeyPressed.ToString();
                changed = false;
            }
        }

        // ------------------------------------------------------------------
        //  Image processing helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Converts a colour image to grayscale using the average of R, G, B.
        /// </summary>
        private Bitmap GrayScale(Bitmap Bmp)
        {
            var result = new Bitmap(Bmp.Width, Bmp.Height);
            for (int y = 0; y < Bmp.Height; y++)
                for (int x = 0; x < Bmp.Width; x++)
                {
                    Color c = Bmp.GetPixel(x, y);
                    int rgb = (c.R + c.G + c.B) / 3;
                    result.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            return result;
        }

        /// <summary>
        /// Adjusts the contrast of an image. Value = 0 gives no change,
        /// positive values increase contrast, negative values decrease contrast.
        /// </summary>
        private Bitmap AdjustContrast(Bitmap Image, float Value)
        {
            Value = (100.0f + Value) / 100.0f;
            Value *= Value;
            Bitmap NewBitmap = (Bitmap)Image.Clone();
            BitmapData data = NewBitmap.LockBits(
                new Rectangle(0, 0, NewBitmap.Width, NewBitmap.Height),
                ImageLockMode.ReadWrite,
                NewBitmap.PixelFormat);
            int Height = NewBitmap.Height;
            int Width = NewBitmap.Width;

            unsafe
            {
                for (int y = 0; y < Height; ++y)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    int columnOffset = 0;
                    for (int x = 0; x < Width; ++x)
                    {
                        byte B = row[columnOffset];
                        byte G = row[columnOffset + 1];
                        byte R = row[columnOffset + 2];

                        float Red = R / 255.0f;
                        float Green = G / 255.0f;
                        float Blue = B / 255.0f;
                        Red = (((Red - 0.5f) * Value) + 0.5f) * 255.0f;
                        Green = (((Green - 0.5f) * Value) + 0.5f) * 255.0f;
                        Blue = (((Blue - 0.5f) * Value) + 0.5f) * 255.0f;

                        int iR = (int)Red;
                        iR = iR > 255 ? 255 : iR;
                        iR = iR < 0 ? 0 : iR;
                        int iG = (int)Green;
                        iG = iG > 255 ? 255 : iG;
                        iG = iG < 0 ? 0 : iG;
                        int iB = (int)Blue;
                        iB = iB > 255 ? 255 : iB;
                        iB = iB < 0 ? 0 : iB;

                        row[columnOffset] = (byte)iB;
                        row[columnOffset + 1] = (byte)iG;
                        row[columnOffset + 2] = (byte)iR;

                        columnOffset += 4;
                    }
                }
            }
            NewBitmap.UnlockBits(data);
            return NewBitmap;
        }

        /// <summary>
        /// Determines if an image has enough variation (not a solid colour).
        /// Used to avoid processing when the AFK prompt is not visible.
        /// </summary>
        private bool IsImageVariant(Bitmap bmp)
        {
            double sum = 0, sumSq = 0;
            int count = 0;
            // Sample every 5th pixel for performance
            for (int y = 0; y < bmp.Height; y += 5)
                for (int x = 0; x < bmp.Width; x += 5)
                {
                    Color c = bmp.GetPixel(x, y);
                    int lum = (c.R + c.G + c.B) / 3;
                    sum += lum;
                    sumSq += lum * lum;
                    count++;
                }
            if (count == 0) return false;
            double mean = sum / count;
            double variance = (sumSq / count) - (mean * mean);
            double stdDev = Math.Sqrt(variance);
            // If the standard deviation is below 5, the image is nearly uniform -> skip
            return stdDev > 5.0;
        }
    }

    // ------------------------------------------------------------------
    // Live binary preview window – shows exactly what Tesseract sees
    // after grayscale, contrast, and thresholding.
    // ------------------------------------------------------------------
    public class PreviewForm : Form
    {
        private PictureBox pictureBox;
        private System.Windows.Forms.Timer refreshTimer;
        private Func<Rectangle> getCaptureRect; // function that returns the capture rectangle

        public PreviewForm(Func<Rectangle> getRectFunc)
        {
            getCaptureRect = getRectFunc;
            this.Text = "Binary Preview (What Tesseract Sees)";
            this.Size = new Size(600, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.Controls.Add(pictureBox);

            // Timer updates the preview 10 times per second
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 100;
            refreshTimer.Tick += (s, e) => UpdatePreview();
            refreshTimer.Start();
        }

        /// <summary>
        /// Captures the current screen area defined by getCaptureRect(),
        /// applies the same preprocessing as the OCR loop, and displays the binary image.
        /// </summary>
        private void UpdatePreview()
        {
            if (this.IsDisposed) return;
            try
            {
                Rectangle rect = getCaptureRect();
                if (rect.Width <= 0 || rect.Height <= 0) return;

                using (Bitmap captured = new Bitmap(rect.Width, rect.Height))
                using (Graphics g = Graphics.FromImage(captured))
                {
                    g.CopyFromScreen(rect.X, rect.Y, 0, 0, captured.Size);
                    // Apply the same preprocessing as the OCR loop
                    using (Bitmap gray = GrayScale(captured))
                    using (Bitmap contrast = AdjustContrast(gray, 120))
                    using (Bitmap binary = new Bitmap(contrast.Width, contrast.Height))
                    {
                        for (int y = 0; y < contrast.Height; y++)
                            for (int x = 0; x < contrast.Width; x++)
                            {
                                Color c = contrast.GetPixel(x, y);
                                int lum = (c.R + c.G + c.B) / 3;
                                binary.SetPixel(x, y, lum > 31 ? Color.White : Color.Black);
                            }
                        // Update the UI on the correct thread
                        if (pictureBox.InvokeRequired)
                            pictureBox.Invoke(new Action(() => SetImage(binary)));
                        else
                            SetImage(binary);
                    }
                }
            }
            catch { } // Silent fail – preview errors are not critical
        }

        private void SetImage(Bitmap bmp)
        {
            var old = pictureBox.Image;
            pictureBox.Image = (Bitmap)bmp.Clone();
            old?.Dispose();
        }

        // ---------- Duplicate processing helpers (same as in Form1) ----------
        private Bitmap GrayScale(Bitmap Bmp)
        {
            var result = new Bitmap(Bmp.Width, Bmp.Height);
            for (int y = 0; y < Bmp.Height; y++)
                for (int x = 0; x < Bmp.Width; x++)
                {
                    Color c = Bmp.GetPixel(x, y);
                    int rgb = (c.R + c.G + c.B) / 3;
                    result.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            return result;
        }

        private Bitmap AdjustContrast(Bitmap Image, float Value)
        {
            Value = (100.0f + Value) / 100.0f;
            Value *= Value;
            Bitmap NewBitmap = (Bitmap)Image.Clone();
            BitmapData data = NewBitmap.LockBits(new Rectangle(0, 0, NewBitmap.Width, NewBitmap.Height),
                ImageLockMode.ReadWrite, NewBitmap.PixelFormat);
            int Height = NewBitmap.Height, Width = NewBitmap.Width;
            unsafe
            {
                for (int y = 0; y < Height; y++)
                {
                    byte* row = (byte*)data.Scan0 + y * data.Stride;
                    for (int x = 0; x < Width; x++)
                    {
                        byte B = row[x * 4], G = row[x * 4 + 1], R = row[x * 4 + 2];
                        float red = R / 255.0f, green = G / 255.0f, blue = B / 255.0f;
                        red = ((red - 0.5f) * Value + 0.5f) * 255.0f;
                        green = ((green - 0.5f) * Value + 0.5f) * 255.0f;
                        blue = ((blue - 0.5f) * Value + 0.5f) * 255.0f;
                        int iR = (int)red; if (iR > 255) iR = 255; if (iR < 0) iR = 0;
                        int iG = (int)green; if (iG > 255) iG = 255; if (iG < 0) iG = 0;
                        int iB = (int)blue; if (iB > 255) iB = 255; if (iB < 0) iB = 0;
                        row[x * 4] = (byte)iB;
                        row[x * 4 + 1] = (byte)iG;
                        row[x * 4 + 2] = (byte)iR;
                    }
                }
            }
            NewBitmap.UnlockBits(data);
            return NewBitmap;
        }

        // Clean up the timer and image when the form closes
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            pictureBox.Image?.Dispose();
            base.OnFormClosing(e);
        }
    }
}