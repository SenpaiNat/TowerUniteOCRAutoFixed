using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Patagames.Ocr;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TowerUniteOCRAuto
{
    public partial class Form1 : Form
    {
        private int timesBypassed = 0, timesClicked = 0;
        public static bool cont = false, pressImageFound = false;
        private OcrApi api;
        Keys lastKeyPressed;
        bool changed = false;

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private IntPtr towerUniteHandle = IntPtr.Zero;
        KeyboardHook hook = new KeyboardHook();

        public Form1()
        {
            InitializeComponent();
            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);

            MaximizeBox = false;
            Console.Title = "TowerUniteOCRAuto";
            Console.WriteLine("Looking for Tower Unite handle...");
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Equals("Tower-Win64-Shipping"))
                {
                    towerUniteHandle = p.MainWindowHandle;
                    Console.WriteLine("FOUND HANDLE: " + towerUniteHandle);
                }
            }

            if (towerUniteHandle == IntPtr.Zero)
            {
                MessageBox.Show("Tower Unite is not running.\nPlease run Tower Unite before running this application.\nClosing application.");
                Application.Exit();
                Environment.Exit(0);
            }

            Console.WriteLine("Working with your Primary Screen with dimensions: " + Screen.PrimaryScreen.Bounds.Width + "x" +
                              Screen.PrimaryScreen.Bounds.Height);
            Console.WriteLine("I recommend you TURN OFF your ChatBox as to not mess with the OCR reading.\n\tGo into Tower Unite ingame, then: Settings->Content->Disable Chat");
            Console.WriteLine("Note that as of now, the default button that will be pressed is SPACE.\n\tIf you are on Video Blacjack, go into your Tower Unite ingame, then: Settings->Controls->Mouse 1 (Click, Putt, Etc) | Rebind to SPACE");
        }

        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (StartedStoppedLabel.Text.Equals("Stopped"))
            {
                cont = true;
                bool success = false;
                StartedStoppedLabel.Text = "Started";
                int delTime = 0, ranTime = 0;
                if (Int32.TryParse(delayTimeTextBox.Text, out delTime) && Int32.TryParse(randomTimeTextBox.Text, out ranTime))
                {
                    if (delTime > 0 && ranTime >= 0)
                    {
                        CallRunOCRAsync(3000);
                        CallRunAutoKeyPressAsync(Int32.Parse(delayTimeTextBox.Text), Int32.Parse(randomTimeTextBox.Text));
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Starting auto...");
                        Console.ResetColor();
                        success = true;
                    }
                }
                if (!success)
                {
                    MessageBox.Show("Only integer values are allowed in the time textboxes\nMake sure they are non-negative, and your delay must be a non-zero time.");
                }
            }
            else
            {
                cont = false;
                StartedStoppedLabel.Text = "Stopped";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Stopping auto...");
                Console.ResetColor();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            api = OcrApi.Create();
            api.Init(Patagames.Ocr.Enums.Languages.English);
            // Whitelist all lowercase letters (optional – remove if you want any character)
            api.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyz");
            // PSM 7: treat image as a single text line
            api.SetVariable("tessedit_pageseg_mode", "7");
        }

        ////////Key Press Here////////

        private async void CallRunAutoKeyPressAsync(int msDelay, int msRand)
        {
            await RunAutoKeypressAsync(msDelay, msRand);
        }

        private Task<Boolean> RunAutoKeypressAsync(int msDelay, int msRand)
        {
            return Task.Factory.StartNew(() => RunAutoKeyPress(msDelay, msRand));
        }

        private Boolean RunAutoKeyPress(int msDelay, int msRand)
        {
            Random rand = new Random();
            while (Form1.cont)
            {
                Thread.Sleep(msDelay + rand.Next(msRand) + 1);
                if (Form1.cont && !Form1.pressImageFound)
                {
                    PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)Keys.Space, IntPtr.Zero);
                    Console.Title = "TowerUniteOCRAuto - Times Clicked: " + ++timesClicked;
                }
            }
            return Form1.cont;
        }

        ////////Picture OCR Here////////

        private async void CallRunOCRAsync(int ms)
        {
            if (await RunOCRAsync(ms))
            {
                StartedStoppedLabel.Text = "Stopped";
            }
        }

        private Task<Boolean> RunOCRAsync(int ms)
        {
            return Task.Factory.StartNew(() => RunOCR(ms));
        }

        const int WM_KEYDOWN = 0x100;

        private Boolean RunOCR(int ms)
        {
            int lastTime = ms;
            // Your capture rectangle (adjust if needed)
            int x1 = 670, y1 = 420, x2 = 1169, y2 = 563;
            int width = x2 - x1, height = y2 - y1;

            while (Form1.cont)
            {
                Thread.Sleep(lastTime);
                if (!Form1.cont) continue;

                List<char> detectedChars = new List<char>();
                int numFrames = 9;          // More samples for majority vote
                int delayBetween = 50;

                for (int frame = 0; frame < numFrames; frame++)
                {
                    using (Bitmap captured = new Bitmap(width, height))
                    using (Graphics g = Graphics.FromImage(captured))
                    {
                        g.CopyFromScreen(x1, y1, 0, 0, captured.Size);

                        // ---- ADDED: skip uniform areas (no text) ----
                        if (!IsImageVariant(captured))
                        {
                            continue;
                        }
                        // --------------------------------------------

                        // Preprocess: grayscale + strong contrast + binary threshold
                        using (Bitmap gray = GrayScale(captured))
                        using (Bitmap contrast = AdjustContrast(gray, 150))
                        using (Bitmap binary = new Bitmap(contrast.Width, contrast.Height))
                        {
                            for (int y = 0; y < contrast.Height; y++)
                                for (int x = 0; x < contrast.Width; x++)
                                {
                                    Color c = contrast.GetPixel(x, y);
                                    int luminance = (c.R + c.G + c.B) / 3;
                                    // Threshold (80 works for light text on dark background)
                                    binary.SetPixel(x, y, luminance > 80 ? Color.White : Color.Black);
                                }
                            string text = api.GetTextFromImage(binary).Trim();
                            string cleaned = new string(text.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
                            if (cleaned.Length == 1 && char.IsLetter(cleaned[0]))
                                detectedChars.Add(cleaned[0]);
                        }
                    }
                    Thread.Sleep(delayBetween);
                }

                // Majority vote (requires at least 5 out of 9 to be the same)
                char finalKey = '\0';
                if (detectedChars.Count > 0)
                {
                    var groups = detectedChars.GroupBy(c => c).OrderByDescending(g => g.Count());
                    var top = groups.First();
                    if (top.Count() >= 5 || detectedChars.Count == top.Count())
                        finalKey = top.Key;
                }

                if (finalKey != '\0')
                {
                    Keys keyToPress = (Keys)Enum.Parse(typeof(Keys), finalKey.ToString().ToUpper());
                    PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)keyToPress, IntPtr.Zero);
                    Console.WriteLine($"Pressed {finalKey} (votes: {string.Join(",", detectedChars)})");
                    Form1.pressImageFound = true;
                    timesBypassed++;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(DateTime.Now.ToShortTimeString() + ": ");
                    Console.ResetColor();
                    Console.WriteLine($"Bypassed AFK check x{timesBypassed}");
                    using (Bitmap sample = new Bitmap(width, height))
                    using (Graphics g = Graphics.FromImage(sample))
                    {
                        g.CopyFromScreen(x1, y1, 0, 0, sample.Size);
                        sample.Save("lastSuccessfulBypassImageTaken.png");
                    }
                    // ---- CHANGED: replace 14.5 min delay with normal interval and add cooldown ----
                    lastTime = ms;   // scan again after normal interval (3 seconds)
                    // Reset pressImageFound after 4 seconds so spacebar can work again
                    Task.Delay(4000).ContinueWith(_ => { Form1.pressImageFound = false; });
                    // ----------------------------------------------------------------------------
                }
                else
                {
                    Console.WriteLine($"No clear key (detected: {string.Join(",", detectedChars)})");
                    Form1.pressImageFound = false;
                    lastTime = ms;
                }
                api.Clear();
            }
            return Form1.cont;
        }

        private void keybindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            hook.UnregisterHotKeys();
            lastKeyPressed = e.KeyCode;
            hook.RegisterHotKey(lastKeyPressed);
            changed = true;
            label4.Focus();
        }

        private void keybindTextBox_TextChanged(object sender, EventArgs e)
        {
            if (changed)
            {
                keybindTextBox.Text = lastKeyPressed.ToString();
            }
            else
            {
                changed = false;
            }
        }

        private Bitmap GrayScale(Bitmap Bmp)
        {
            int rgb;
            Color c;
            for (int y = 0; y < Bmp.Height; y++)
                for (int x = 0; x < Bmp.Width; x++)
                {
                    c = Bmp.GetPixel(x, y);
                    rgb = (int)((c.R + c.G + c.B) / 3);
                    Bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            return Bmp;
        }

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

        // ---- ADDED: variance check to ignore solid colors ----
        /// <summary>
        /// Returns true if the image has sufficient variance (not a solid color).
        /// </summary>
        private bool IsImageVariant(Bitmap bmp)
        {
            double sum = 0, sumSq = 0;
            int count = 0;
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
            return stdDev > 5.0;
        }
        // ----------------------------------------------------
    }
}