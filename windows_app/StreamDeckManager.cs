// =============================================================================
// Seeed Studio reTerminal Sticky - Stream Deck Host Manager
//
// Official Brand Colors:
// - Seeed Green: RGB(143, 195, 31)  / HEX #8FC31F / Pantone 376C
// - Seeed Blue:  RGB(0, 58, 74)    / HEX #003A4A / Pantone 7546C
//
// Features:
// - Native Dark Theme with Seeed Studio Green & Blue accents
// - 15 Interactive Squircle Keys with live 800x480 display preview
// - Simplified Button Inspector (Program .exe path, URL, Media, Command)
// - Whole 800x480 E-Paper Display Customizer:
//     * Browse and load any 800x480 image (.png, .jpg, .bmp)
//     * High-quality Floyd-Steinberg 1-bit monochrome dithering
//     * Live USB Serial streaming (48,000 bytes directly to reTerminal Sticky)
//     * One-click "Save StreamDeck_Bitmap.h" for Arduino flash
//     * Export 800x480 PNG
// - 100% Serial CDC Triggered (No global keyboard hooks or Alt+1..0 keystrokes)
// - Hardware Control (Screen Refresh, Fast Refresh, Buzzer Test)
// - Zero external dependencies (Compiles with native Windows csc.exe)
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ReTerminalStreamDeck
{
    // =========================================================================
    // Official Seeed Studio Theme & UI Palette
    // =========================================================================
    public static class Theme
    {
        // Official Seeed Studio Brand Identity
        public static readonly Color SeeedGreen      = Color.FromArgb(143, 195, 31);     // #8FC31F (Pantone 376C)
        public static readonly Color SeeedBlue       = Color.FromArgb(0, 58, 74);        // #003A4A (Pantone 7546C)

        // Dark Theme Canvas Colors (Deep teal-obsidian darks)
        public static readonly Color BgWindow        = Color.FromArgb(9, 18, 23);        // #091217 Deepest Dark
        public static readonly Color BgHeader        = Color.FromArgb(13, 26, 33);       // #0d1a21
        public static readonly Color BgSidebar       = Color.FromArgb(13, 26, 33);       // #0d1a21
        public static readonly Color BgCard          = Color.FromArgb(17, 35, 45);       // #11232d
        public static readonly Color BgCardHover     = Color.FromArgb(23, 48, 61);       // #17303d
        public static readonly Color BgCardActive    = Color.FromArgb(0, 58, 74);        // Seeed Blue active
        public static readonly Color BorderCard      = Color.FromArgb(25, 54, 69);       // #193645
        public static readonly Color BorderActive    = Color.FromArgb(143, 195, 31);     // Seeed Green active
        public static readonly Color BorderSubtle    = Color.FromArgb(20, 43, 55);       // #142b37

        public static readonly Color BgInput         = Color.FromArgb(10, 21, 27);       // #0a151b
        public static readonly Color BorderInput     = Color.FromArgb(29, 60, 77);       // #1d3c4d

        public static readonly Color TextPrimary     = Color.FromArgb(248, 250, 252);    // #f8fafc Crisp White
        public static readonly Color TextSecondary   = Color.FromArgb(156, 185, 196);    // #9cb9c4 Cool Cyan-Gray
        public static readonly Color TextMuted       = Color.FromArgb(93, 127, 141);     // #5d7f8d Muted Slate
        public static readonly Color AccentGreen     = Color.FromArgb(143, 195, 31);     // Seeed Green
        public static readonly Color AccentBlue      = Color.FromArgb(0, 58, 74);        // Seeed Blue
        public static readonly Color AccentSky       = Color.FromArgb(14, 165, 233);     // Web / Info
        public static readonly Color AccentAmber     = Color.FromArgb(245, 158, 11);     // Command / Warn
        public static readonly Color AccentRose      = Color.FromArgb(244, 63, 94);      // Danger
        public static readonly Color AccentPurple    = Color.FromArgb(168, 85, 247);     // Media

        public static readonly Font FontAppTitle     = new Font("Segoe UI Semibold", 13.5f, FontStyle.Bold);
        public static readonly Font FontSection      = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
        public static readonly Font FontBodyBold     = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        public static readonly Font FontBody         = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        public static readonly Font FontSmall        = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        public static readonly Font FontBadge        = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
        public static readonly Font FontMono         = new Font("Consolas", 9f, FontStyle.Regular);

        public static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (rect.Width < d) rect.Width = d;
            if (rect.Height < d) rect.Height = d;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // =========================================================================
    // Data Model
    // =========================================================================
    public class ButtonConfig
    {
        public int id { get; set; }
        public string name { get; set; }
        public string actionType { get; set; } // LaunchApp, OpenUrl, MediaControl, RunCommand
        public string target { get; set; }
        public string arguments { get; set; }
        public string workingDir { get; set; }

        public ButtonConfig()
        {
            name = "Unnamed";
            actionType = "LaunchApp";
            target = "";
            arguments = "";
            workingDir = "";
        }
    }

    public class AppConfig
    {
        public bool autoConnectSerial { get; set; }
        public string lastComPort { get; set; }
        public bool minimizeToTray { get; set; }
        public bool showNotifications { get; set; }
        public string customImagePath { get; set; }
        public List<ButtonConfig> buttons { get; set; }

        public AppConfig()
        {
            autoConnectSerial = true;
            lastComPort = "AUTO";
            minimizeToTray = false;
            showNotifications = false;
            customImagePath = "";
            buttons = new List<ButtonConfig>();
        }

        public static AppConfig CreateDefaults()
        {
            AppConfig cfg = new AppConfig();
            cfg.buttons.Add(new ButtonConfig { id = 1,  name = "Discord",            actionType = "LaunchApp",    target = @"C:\Users\%USERNAME%\AppData\Local\Discord\Update.exe", arguments = "--processStart Discord.exe" });
            cfg.buttons.Add(new ButtonConfig { id = 2,  name = "WhatsApp",           actionType = "OpenUrl",      target = "https://web.whatsapp.com" });
            cfg.buttons.Add(new ButtonConfig { id = 3,  name = "Autodesk / CAD",     actionType = "LaunchApp",    target = "" });
            cfg.buttons.Add(new ButtonConfig { id = 4,  name = "Google Chrome",      actionType = "LaunchApp",    target = "chrome.exe" });
            cfg.buttons.Add(new ButtonConfig { id = 5,  name = "Brave Browser",      actionType = "LaunchApp",    target = "brave.exe" });

            cfg.buttons.Add(new ButtonConfig { id = 6,  name = "Inkscape",           actionType = "LaunchApp",    target = @"C:\Program Files\Inkscape\bin\inkscape.exe" });
            cfg.buttons.Add(new ButtonConfig { id = 7,  name = "Visual Studio Code", actionType = "LaunchApp",    target = "code.cmd" });
            cfg.buttons.Add(new ButtonConfig { id = 8,  name = "DaVinci Resolve",    actionType = "LaunchApp",    target = "" });
            cfg.buttons.Add(new ButtonConfig { id = 9,  name = "KiCad",              actionType = "LaunchApp",    target = "" });
            cfg.buttons.Add(new ButtonConfig { id = 10, name = "WeChat",             actionType = "LaunchApp",    target = "" });

            cfg.buttons.Add(new ButtonConfig { id = 11, name = "Previous Track",     actionType = "MediaControl", target = "PREV_TRACK" });
            cfg.buttons.Add(new ButtonConfig { id = 12, name = "Play / Pause",       actionType = "MediaControl", target = "PLAY_PAUSE" });
            cfg.buttons.Add(new ButtonConfig { id = 13, name = "Next Track",         actionType = "MediaControl", target = "NEXT_TRACK" });
            cfg.buttons.Add(new ButtonConfig { id = 14, name = "Volume Down",        actionType = "MediaControl", target = "VOL_DOWN" });
            cfg.buttons.Add(new ButtonConfig { id = 15, name = "Volume Up",          actionType = "MediaControl", target = "VOL_UP" });
            return cfg;
        }
    }

    // =========================================================================
    // 800x480 E-Paper Display Bitmap Engine (Monochrome SSD1677)
    // =========================================================================
    public static class EpdBitmapEngine
    {
        // 15 hardware touch button rectangles on the 800x480 screen
        public static readonly Rectangle[] BUTTON_RECTS = new Rectangle[15] {
            new Rectangle( 12,  14, 147, 141),
            new Rectangle(167,  11, 147, 141),
            new Rectangle(326,  12, 147, 141),
            new Rectangle(480,  12, 147, 141),
            new Rectangle(637,  13, 147, 141),

            new Rectangle( 12, 170, 147, 141),
            new Rectangle(167, 167, 147, 141),
            new Rectangle(326, 168, 147, 141),
            new Rectangle(480, 168, 147, 141),
            new Rectangle(637, 168, 147, 141),

            new Rectangle( 13, 326, 147, 141),
            new Rectangle(168, 323, 147, 141),
            new Rectangle(327, 324, 147, 141),
            new Rectangle(481, 324, 147, 141),
            new Rectangle(638, 325, 147, 141)
        };

        // Load 800x480 bitmap from C header file (StreamDeck_Bitmap.h)
        public static Bitmap LoadBitmapFromHeader(string headerPath)
        {
            if (!File.Exists(headerPath)) return null;

            try
            {
                string text = File.ReadAllText(headerPath);
                MatchCollection matches = Regex.Matches(text, @"0x([0-9a-fA-F]{2})");
                if (matches.Count < 48000) return null;

                byte[] bytes = new byte[48000];
                for (int i = 0; i < 48000; i++)
                {
                    bytes[i] = Convert.ToByte(matches[i].Groups[1].Value, 16);
                }

                Bitmap bmp = new Bitmap(800, 480, PixelFormat.Format32bppArgb);
                for (int y = 0; y < 480; y++)
                {
                    for (int x = 0; x < 800; x++)
                    {
                        int byteIdx = (y * 100) + (x / 8);
                        int bit = (bytes[byteIdx] >> (7 - (x % 8))) & 1;
                        // 1 = White, 0 = Black
                        bmp.SetPixel(x, y, bit == 1 ? Color.White : Color.Black);
                    }
                }
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        // Load any image file and scale/dither to 800x480 1-bit monochrome
        public static Bitmap LoadAndDitherImage(string imagePath)
        {
            if (!File.Exists(imagePath)) return null;

            try
            {
                using (Bitmap src = new Bitmap(imagePath))
                {
                    return DitherTo800x480Monochrome(src);
                }
            }
            catch
            {
                return null;
            }
        }

        // Floyd-Steinberg error diffusion to generate crisp 800x480 monochrome image
        public static Bitmap DitherTo800x480Monochrome(Bitmap src)
        {
            int targetW = 800;
            int targetH = 480;

            float[,] gray = new float[targetW, targetH];

            using (Bitmap scaled = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.White);
                    g.DrawImage(src, 0, 0, targetW, targetH);
                }

                for (int y = 0; y < targetH; y++)
                {
                    for (int x = 0; x < targetW; x++)
                    {
                        Color c = scaled.GetPixel(x, y);
                        float lum = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B);
                        gray[x, y] = lum;
                    }
                }
            }

            Bitmap result = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            for (int y = 0; y < targetH; y++)
            {
                for (int x = 0; x < targetW; x++)
                {
                    float oldVal = gray[x, y];
                    float newVal = oldVal > 128f ? 255f : 0f;
                    float err = oldVal - newVal;
                    result.SetPixel(x, y, newVal > 128f ? Color.White : Color.Black);

                    if (x + 1 < targetW) gray[x + 1, y] += err * (7f / 16f);
                    if (x - 1 >= 0 && y + 1 < targetH) gray[x - 1, y + 1] += err * (3f / 16f);
                    if (y + 1 < targetH) gray[x, y + 1] += err * (5f / 16f);
                    if (x + 1 < targetW && y + 1 < targetH) gray[x + 1, y + 1] += err * (1f / 16f);
                }
            }
            return result;
        }

        // Convert 800x480 Bitmap into 48,000 raw 1-bit EPD bytes (1=White, 0=Black, MSB first)
        public static byte[] ConvertToEpd1BitBytes(Bitmap bmp)
        {
            byte[] buffer = new byte[48000];
            int width = Math.Min(800, bmp.Width);
            int height = Math.Min(480, bmp.Height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    int lum = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                    if (lum > 128)
                    {
                        int byteIdx = (y * 100) + (x / 8);
                        buffer[byteIdx] |= (byte)(0x80 >> (x % 8));
                    }
                }
            }
            return buffer;
        }

        // Export C Header file (StreamDeck_Bitmap.h)
        public static void ExportBitmapHeader(byte[] buffer, string filePath)
        {
            StringBuilder sb = new StringBuilder(280000);
            sb.AppendLine("#pragma once");
            sb.AppendLine("#include <Arduino.h>");
            sb.AppendLine();
            sb.AppendLine("// 800x480 Monochrome 1-bit Stream Deck UI Bitmap");
            sb.AppendLine("// 1 = White, 0 = Black, MSB first, 100 bytes per row, 480 rows = 48000 bytes");
            sb.AppendLine("static const unsigned char PROGMEM image_g1650_bits[48000] = {");

            for (int i = 0; i < 48000; i++)
            {
                if (i % 16 == 0) sb.Append("    ");
                sb.Append(string.Format("0x{0:x2}", buffer[i]));
                if (i < 47999) sb.Append(", ");
                if ((i + 1) % 16 == 0) sb.AppendLine();
            }
            sb.AppendLine("};");
            File.WriteAllText(filePath, sb.ToString());
        }
    }

    // =========================================================================
    // Win32 Interop
    // =========================================================================
    internal static class NativeMethods
    {
        public const byte VK_MEDIA_NEXT_TRACK = 0xB0;
        public const byte VK_MEDIA_PREV_TRACK = 0xB1;
        public const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        public const byte VK_VOLUME_MUTE = 0xAD;
        public const byte VK_VOLUME_DOWN = 0xAE;
        public const byte VK_VOLUME_UP = 0xAF;

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }

    // =========================================================================
    // Modern Custom Button Control (Seeed Green / Seeed Blue Styles)
    // =========================================================================
    public class ModernButton : Button
    {
        public enum ButtonStyle { Primary, Secondary, Danger, Ghost }
        public ButtonStyle Style { get; set; }

        private bool isHovered = false;
        private bool isPressed = false;

        public ModernButton()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                           ControlStyles.UserPaint |
                           ControlStyles.OptimizedDoubleBuffer |
                           ControlStyles.ResizeRedraw, true);
            this.Cursor = Cursors.Hand;
            this.Font = Theme.FontBodyBold;
            this.Style = ButtonStyle.Secondary;
            this.Height = 32;
        }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs mevent) { isPressed = true; Invalidate(); base.OnMouseDown(mevent); }
        protected override void OnMouseUp(MouseEventArgs mevent) { isPressed = false; Invalidate(); base.OnMouseUp(mevent); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color parentBg = Theme.BgHeader;
            Control p = this.Parent;
            while (p != null)
            {
                if (p.BackColor != Color.Transparent && p.BackColor.A == 255)
                {
                    parentBg = p.BackColor;
                    break;
                }
                p = p.Parent;
            }
            using (SolidBrush bgBrush = new SolidBrush(parentBg))
                g.FillRectangle(bgBrush, this.ClientRectangle);

            Color bg, fg, border;
            int radius = 6;

            if (Style == ButtonStyle.Primary)
            {
                // Seeed Green
                bg = isPressed ? Color.FromArgb(120, 168, 20) : (isHovered ? Color.FromArgb(158, 212, 38) : Theme.SeeedGreen);
                fg = Color.FromArgb(0, 38, 48); // Deep contrasting Seeed Blue text
                border = bg;
            }
            else if (Style == ButtonStyle.Danger)
            {
                bg = isPressed ? Color.FromArgb(153, 27, 27) : (isHovered ? Color.FromArgb(185, 28, 28) : Color.FromArgb(220, 38, 38));
                fg = Color.White;
                border = bg;
            }
            else if (Style == ButtonStyle.Ghost)
            {
                bg = isPressed ? Color.FromArgb(12, 24, 30) : (isHovered ? Color.FromArgb(20, 42, 54) : Color.FromArgb(14, 30, 38));
                fg = isHovered ? Theme.TextPrimary : Theme.TextSecondary;
                border = isHovered ? Color.FromArgb(35, 75, 96) : Color.FromArgb(24, 52, 66);
            }
            else // Secondary -> Seeed Blue
            {
                bg = isPressed ? Color.FromArgb(0, 45, 58) : (isHovered ? Color.FromArgb(0, 75, 96) : Theme.SeeedBlue);
                fg = Color.White;
                border = isHovered ? Color.FromArgb(0, 95, 122) : Color.FromArgb(0, 68, 86);
            }

            Rectangle r = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath path = Theme.CreateRoundedRectangle(r, radius))
            {
                using (SolidBrush brush = new SolidBrush(bg))
                    g.FillPath(brush, path);
                using (Pen pen = new Pen(border, 1))
                    g.DrawPath(pen, path);
            }

            TextRenderer.DrawText(g, this.Text, this.Font, r, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // =========================================================================
    // Flicker-Free Double-Buffered Controls
    // =========================================================================
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                           ControlStyles.UserPaint |
                           ControlStyles.OptimizedDoubleBuffer |
                           ControlStyles.ResizeRedraw, true);
        }
    }

    public class DoubleBufferedTableLayoutPanel : TableLayoutPanel
    {
        public DoubleBufferedTableLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                           ControlStyles.UserPaint |
                           ControlStyles.OptimizedDoubleBuffer |
                           ControlStyles.ResizeRedraw, true);
        }
    }

    public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                           ControlStyles.UserPaint |
                           ControlStyles.OptimizedDoubleBuffer |
                           ControlStyles.ResizeRedraw, true);
        }
    }

    // =========================================================================
    // Custom Owner-Drawn Stream Deck Key View (Grid Item)
    // =========================================================================
    public class StreamDeckKeyView : Control
    {
        public int Index { get; set; }
        public ButtonConfig Config { get; set; }
        public bool IsSelected { get; set; }
        public Bitmap MasterEpdBitmap { get; set; }

        private bool isHovered = false;

        public StreamDeckKeyView(int index, ButtonConfig config)
        {
            this.Index = index;
            this.Config = config;
            this.IsSelected = false;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                           ControlStyles.UserPaint |
                           ControlStyles.OptimizedDoubleBuffer |
                           ControlStyles.ResizeRedraw, true);
            this.Cursor = Cursors.Hand;
            this.Size = new Size(116, 126);
        }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Clear background with parent color
            Color parentBg = Theme.BgWindow;
            if (this.Parent != null && this.Parent.BackColor != Color.Transparent)
                parentBg = this.Parent.BackColor;
            using (SolidBrush bgBrush = new SolidBrush(parentBg))
                g.FillRectangle(bgBrush, this.ClientRectangle);

            Rectangle r = new Rectangle(2, 2, this.Width - 5, this.Height - 5);
            Color cardBg = IsSelected ? Theme.BgCardActive : (isHovered ? Theme.BgCardHover : Theme.BgCard);
            Color cardBorder = IsSelected ? Theme.BorderActive : (isHovered ? Color.FromArgb(40, 85, 108) : Theme.BorderCard);
            int borderWidth = IsSelected ? 2 : 1;

            using (GraphicsPath path = Theme.CreateRoundedRectangle(r, 10))
            {
                using (SolidBrush sb = new SolidBrush(cardBg))
                    g.FillPath(sb, path);
                using (Pen pen = new Pen(cardBorder, borderWidth))
                    g.DrawPath(pen, path);
            }

            int pad = 6;
            int bottomY = this.Height - pad - 16;

            // 1. Top Header: Button Badge (#01..#15)
            string keyNumber = string.Format("#{0:D2}", Index + 1);
            Color badgeFg = IsSelected ? Theme.SeeedGreen : Theme.TextSecondary;
            Rectangle numRect = new Rectangle(pad + 2, pad + 1, 44, 15);
            TextRenderer.DrawText(g, keyNumber, Theme.FontBadge, numRect, badgeFg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // 2. Cropped E-Paper Artwork Preview if master bitmap exists
            int iconY = pad + 17;
            int spaceForIcon = bottomY - 20 - iconY;
            int maxSquare = Math.Min(this.Width - (pad * 2) - 6, spaceForIcon);
            int iconSize = Math.Max(24, Math.Min(68, maxSquare));
            int iconX = (this.Width - iconSize) / 2;
            Rectangle artRect = new Rectangle(iconX, iconY, iconSize, iconSize);

            if (MasterEpdBitmap != null && Index >= 0 && Index < EpdBitmapEngine.BUTTON_RECTS.Length)
            {
                Rectangle srcRect = EpdBitmapEngine.BUTTON_RECTS[Index];
                using (GraphicsPath artPath = Theme.CreateRoundedRectangle(artRect, 8))
                {
                    g.SetClip(artPath);
                    g.DrawImage(MasterEpdBitmap, artRect, srcRect, GraphicsUnit.Pixel);
                    g.ResetClip();
                }
                using (GraphicsPath artPath = Theme.CreateRoundedRectangle(artRect, 8))
                {
                    using (Pen artPen = new Pen(Color.FromArgb(45, 75, 90), 1))
                        g.DrawPath(artPen, artPath);
                }
            }
            else
            {
                // Fallback squircle icon box
                using (GraphicsPath artPath = Theme.CreateRoundedRectangle(artRect, 8))
                {
                    using (SolidBrush blackBrush = new SolidBrush(Color.Black))
                        g.FillPath(blackBrush, artPath);
                }
            }

            // 3. Button Name
            int nameY = iconY + iconSize + 3;
            int availableNameH = Math.Max(12, bottomY - nameY - 1);
            Rectangle nameRect = new Rectangle(pad, nameY, this.Width - (pad * 2), Math.Min(16, availableNameH));
            string displayName = (Config != null && !string.IsNullOrEmpty(Config.name)) ? Config.name : string.Format("Button #{0}", Index + 1);
            TextRenderer.DrawText(g, displayName, Theme.FontBodyBold, nameRect, Theme.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // 4. Bottom Action Type Badge
            string tag = "APP";
            Color tagBg = Color.FromArgb(16, 40, 52);
            Color tagFg = Theme.AccentSky;

            if (Config != null)
            {
                if (Config.actionType == "OpenUrl")
                {
                    tag = "WEB";
                    tagBg = Color.FromArgb(15, 45, 30);
                    tagFg = Theme.SeeedGreen;
                }
                else if (Config.actionType == "MediaControl")
                {
                    tag = "MEDIA";
                    tagBg = Color.FromArgb(42, 22, 58);
                    tagFg = Theme.AccentPurple;
                }
                else if (Config.actionType == "RunCommand")
                {
                    tag = "CMD";
                    tagBg = Color.FromArgb(50, 32, 14);
                    tagFg = Theme.AccentAmber;
                }
            }

            Size tagSize = TextRenderer.MeasureText(tag, Theme.FontBadge);
            int tagW = tagSize.Width + 6;
            Rectangle tagRect = new Rectangle(pad, bottomY, tagW, 14);
            using (GraphicsPath tp = Theme.CreateRoundedRectangle(tagRect, 3))
            {
                using (SolidBrush tb = new SolidBrush(tagBg))
                    g.FillPath(tb, tp);
            }
            TextRenderer.DrawText(g, tag, Theme.FontBadge, tagRect, tagFg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            string targetPreview = "";
            if (Config != null)
            {
                if (Config.actionType == "MediaControl") targetPreview = Config.target ?? "";
                else if (Config.actionType == "OpenUrl") targetPreview = (Config.target ?? "").Replace("https://", "").Replace("http://", "");
                else
                {
                    targetPreview = Path.GetFileName(Config.target ?? "");
                    if (string.IsNullOrEmpty(targetPreview)) targetPreview = "Not configured";
                }
            }

            int targetX = pad + tagW + 4;
            int targetW = this.Width - targetX - pad;
            Rectangle targetRect = new Rectangle(targetX, bottomY, targetW, 14);
            TextRenderer.DrawText(g, targetPreview, Theme.FontSmall, targetRect, Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    // =========================================================================
    // Dedicated 800x480 E-Paper Image Manager Form
    // =========================================================================
    public class EpdImageForm : Form
    {
        private Bitmap currentBmp;
        private PictureBox picDisplay;
        private ProgressBar prgUpload;
        private Label lblUploadStatus;
        private Label lblImageInfo;
        private ModernButton btnUpload;
        private ModernButton btnSaveHeader;
        private ModernButton btnExportPng;
        private ModernButton btnBrowseImage;
        private ModernButton btnResetDefault;
        private Action<byte[], Action<int>> uploadAction;
        private string headerPath;
        private Action<Bitmap> onImageApplied;

        public EpdImageForm(Bitmap activeBmp, string hPath, Action<byte[], Action<int>> uploader, bool isConnected, Action<Bitmap> applyCallback)
        {
            this.currentBmp = activeBmp != null ? new Bitmap(activeBmp) : null;
            this.headerPath = hPath;
            this.uploadAction = uploader;
            this.onImageApplied = applyCallback;

            InitializeComponent(isConnected);
            UpdatePreview();
        }

        private void InitializeComponent(bool isConnected)
        {
            this.Text = "reTerminal Sticky — 800x480 E-Paper Display Image Manager";
            this.Size = new Size(880, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgWindow;
            this.ForeColor = Theme.TextPrimary;
            this.Font = Theme.FontBody;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Apply dark titlebar
            try
            {
                int darkMode = 1;
                NativeMethods.DwmSetWindowAttribute(this.Handle, 20, ref darkMode, sizeof(int));
                NativeMethods.DwmSetWindowAttribute(this.Handle, 19, ref darkMode, sizeof(int));
            }
            catch { }

            // Top Header Bar
            Panel pnlTop = new Panel();
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 62;
            pnlTop.BackColor = Theme.BgHeader;
            pnlTop.Padding = new Padding(20, 10, 20, 10);
            this.Controls.Add(pnlTop);

            Label lblTitle = new Label();
            lblTitle.Text = "800 x 480 Monochrome E-Paper UI (SSD1677)";
            lblTitle.Font = Theme.FontSection;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(20, 10);
            pnlTop.Controls.Add(lblTitle);

            lblImageInfo = new Label();
            lblImageInfo.Text = isConnected ? "● Connected to reTerminal Sticky (Ready for Live Sync)" : "● Device Disconnected (Can export StreamDeck_Bitmap.h or PNG)";
            lblImageInfo.Font = Theme.FontSmall;
            lblImageInfo.ForeColor = isConnected ? Theme.SeeedGreen : Theme.TextMuted;
            lblImageInfo.AutoSize = true;
            lblImageInfo.Location = new Point(22, 34);
            pnlTop.Controls.Add(lblImageInfo);

            btnBrowseImage = new ModernButton();
            btnBrowseImage.Text = "📂 Choose 800x480 Image...";
            btnBrowseImage.Style = ModernButton.ButtonStyle.Primary;
            btnBrowseImage.Width = 200;
            btnBrowseImage.Height = 36;
            btnBrowseImage.Location = new Point(pnlTop.Width - 225, 12);
            btnBrowseImage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseImage.Click += btnBrowseImage_Click;
            pnlTop.Controls.Add(btnBrowseImage);

            // Bottom Action Bar
            Panel pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 84;
            pnlBottom.BackColor = Theme.BgHeader;
            pnlBottom.Padding = new Padding(20, 10, 20, 12);
            this.Controls.Add(pnlBottom);

            prgUpload = new ProgressBar();
            prgUpload.Location = new Point(20, 10);
            prgUpload.Size = new Size(824, 8);
            prgUpload.Visible = false;
            prgUpload.Style = ProgressBarStyle.Continuous;
            pnlBottom.Controls.Add(prgUpload);

            lblUploadStatus = new Label();
            lblUploadStatus.Text = "";
            lblUploadStatus.Font = Theme.FontSmall;
            lblUploadStatus.ForeColor = Theme.SeeedGreen;
            lblUploadStatus.AutoSize = true;
            lblUploadStatus.Location = new Point(20, 24);
            pnlBottom.Controls.Add(lblUploadStatus);

            int btnY = 36;

            btnUpload = new ModernButton();
            btnUpload.Text = "⚡ Upload to reTerminal";
            btnUpload.Style = ModernButton.ButtonStyle.Primary;
            btnUpload.Width = 190;
            btnUpload.Height = 36;
            btnUpload.Location = new Point(20, btnY);
            btnUpload.Click += btnUpload_Click;
            pnlBottom.Controls.Add(btnUpload);

            btnSaveHeader = new ModernButton();
            btnSaveHeader.Text = "💾 Save StreamDeck_Bitmap.h";
            btnSaveHeader.Style = ModernButton.ButtonStyle.Secondary;
            btnSaveHeader.Width = 220;
            btnSaveHeader.Height = 36;
            btnSaveHeader.Location = new Point(220, btnY);
            btnSaveHeader.Click += btnSaveHeader_Click;
            pnlBottom.Controls.Add(btnSaveHeader);

            btnExportPng = new ModernButton();
            btnExportPng.Text = "📁 Export PNG";
            btnExportPng.Style = ModernButton.ButtonStyle.Ghost;
            btnExportPng.Width = 130;
            btnExportPng.Height = 36;
            btnExportPng.Location = new Point(450, btnY);
            btnExportPng.Click += btnExportPng_Click;
            pnlBottom.Controls.Add(btnExportPng);

            btnResetDefault = new ModernButton();
            btnResetDefault.Text = "↺ Reset Default";
            btnResetDefault.Style = ModernButton.ButtonStyle.Ghost;
            btnResetDefault.Width = 130;
            btnResetDefault.Height = 36;
            btnResetDefault.Location = new Point(590, btnY);
            btnResetDefault.Click += btnResetDefault_Click;
            pnlBottom.Controls.Add(btnResetDefault);

            ModernButton btnClose = new ModernButton();
            btnClose.Text = "Done";
            btnClose.Style = ModernButton.ButtonStyle.Ghost;
            btnClose.Width = 90;
            btnClose.Height = 36;
            btnClose.Location = new Point(730, btnY);
            btnClose.Click += delegate { this.Close(); };
            pnlBottom.Controls.Add(btnClose);

            // Center Preview Canvas
            Panel pnlCenter = new Panel();
            pnlCenter.Dock = DockStyle.Fill;
            pnlCenter.BackColor = Theme.BgWindow;
            this.Controls.Add(pnlCenter);

            picDisplay = new PictureBox();
            picDisplay.Size = new Size(800, 480);
            picDisplay.Location = new Point(34, 12);
            picDisplay.SizeMode = PictureBoxSizeMode.Normal;
            picDisplay.BackColor = Color.White;
            pnlCenter.Controls.Add(picDisplay);

            this.Controls.SetChildIndex(pnlCenter, 0);
            this.Controls.SetChildIndex(pnlBottom, 1);
            this.Controls.SetChildIndex(pnlTop, 2);
        }

        private void UpdatePreview()
        {
            if (currentBmp != null)
            {
                picDisplay.Image = currentBmp;
            }
        }

        private void btnBrowseImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select 800x480 Image for E-Paper Display";
                ofd.Filter = "Image Files (*.png;*.jpg;*.bmp;*.jpeg)|*.png;*.jpg;*.bmp;*.jpeg|All Files (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        Bitmap dithered = EpdBitmapEngine.LoadAndDitherImage(ofd.FileName);
                        if (dithered != null)
                        {
                            if (currentBmp != null) currentBmp.Dispose();
                            currentBmp = dithered;
                            UpdatePreview();

                            if (onImageApplied != null)
                            {
                                onImageApplied(currentBmp);
                            }

                            lblUploadStatus.Text = "Loaded and dithered: " + Path.GetFileName(ofd.FileName);
                            lblUploadStatus.ForeColor = Theme.SeeedGreen;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error processing image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            if (currentBmp == null) return;

            prgUpload.Value = 0;
            prgUpload.Visible = true;
            lblUploadStatus.Text = "Streaming 48KB bitmap to reTerminal Sticky...";
            lblUploadStatus.ForeColor = Theme.SeeedGreen;
            btnUpload.Enabled = false;

            byte[] epdBytes = EpdBitmapEngine.ConvertToEpd1BitBytes(currentBmp);

            ThreadPool.QueueUserWorkItem(delegate {
                if (uploadAction != null)
                {
                    uploadAction(epdBytes, (progress) => {
                        try
                        {
                            this.BeginInvoke((MethodInvoker)delegate {
                                prgUpload.Value = Math.Max(0, Math.Min(100, progress));
                                lblUploadStatus.Text = string.Format("Uploading... {0}%", progress);
                            });
                        }
                        catch { }
                    });

                    try
                    {
                        this.BeginInvoke((MethodInvoker)delegate {
                            prgUpload.Visible = false;
                            lblUploadStatus.Text = "Upload complete! Screen refreshed successfully.";
                            lblUploadStatus.ForeColor = Theme.SeeedGreen;
                            btnUpload.Enabled = true;
                        });
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        this.BeginInvoke((MethodInvoker)delegate {
                            prgUpload.Visible = false;
                            lblUploadStatus.Text = "Serial upload handler unavailable. Please check connection.";
                            lblUploadStatus.ForeColor = Theme.AccentRose;
                            btnUpload.Enabled = true;
                        });
                    }
                    catch { }
                }
            });
        }

        private void btnSaveHeader_Click(object sender, EventArgs e)
        {
            if (currentBmp == null) return;

            try
            {
                byte[] epdBytes = EpdBitmapEngine.ConvertToEpd1BitBytes(currentBmp);
                EpdBitmapEngine.ExportBitmapHeader(epdBytes, headerPath);

                lblUploadStatus.Text = "Successfully saved to " + Path.GetFileName(headerPath);
                lblUploadStatus.ForeColor = Theme.SeeedGreen;
                MessageBox.Show("StreamDeck_Bitmap.h successfully saved!\n\nCompile and reflash the firmware with PlatformIO or Arduino IDE to make this the permanent flash UI.",
                    "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save StreamDeck_Bitmap.h: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExportPng_Click(object sender, EventArgs e)
        {
            if (currentBmp == null) return;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Export 800x480 Monochrome E-Paper Bitmap";
                sfd.Filter = "PNG Image (*.png)|*.png|Bitmap Image (*.bmp)|*.bmp";
                sfd.FileName = "StreamDeck_UI_800x480.png";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        currentBmp.Save(sfd.FileName, ImageFormat.Png);
                        lblUploadStatus.Text = "Exported to " + Path.GetFileName(sfd.FileName);
                        lblUploadStatus.ForeColor = Theme.SeeedGreen;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnResetDefault_Click(object sender, EventArgs e)
        {
            Bitmap defaultBmp = EpdBitmapEngine.LoadBitmapFromHeader(headerPath);
            if (defaultBmp != null)
            {
                if (currentBmp != null) currentBmp.Dispose();
                currentBmp = defaultBmp;
                UpdatePreview();

                if (onImageApplied != null)
                {
                    onImageApplied(currentBmp);
                }

                lblUploadStatus.Text = "Restored factory default bitmap from StreamDeck_Bitmap.h";
                lblUploadStatus.ForeColor = Theme.SeeedGreen;
            }
        }
    }

    // =========================================================================
    // Main Host Application Window
    // =========================================================================
    public class MainForm : Form
    {
        private AppConfig config;
        private string configPath;
        private string headerPath;
        private Bitmap masterBmp;
        private JavaScriptSerializer jsonSerializer;

        // Serial Port
        private SerialPort serialPort;
        private System.Windows.Forms.Timer serialAutoConnectTimer;
        private bool isConnectingSerial = false;

        // UI Controls
        private Label lblAppTitle;
        private Label lblSerialStatus;
        private ComboBox cmbSerialPorts;
        private ModernButton btnSerialToggle;
        private ModernButton btnOpenImageManager;
        private ModernButton btnRefreshEpd;
        private ModernButton btnFastRefresh;
        private ModernButton btnBeep;

        // Keypad Grid Controls
        private StreamDeckKeyView[] keyViews = new StreamDeckKeyView[15];
        private int selectedButtonIndex = 0;

        // Inspector Controls
        private Label lblInspectorHeader;
        private TextBox txtName;
        private ComboBox cmbActionType;
        private Label lblTarget;
        private TextBox txtTarget;
        private ModernButton btnBrowseFile;
        private Label lblArgs;
        private TextBox txtArgs;
        private Label lblWorkingDir;
        private TextBox txtWorkingDir;
        private ModernButton btnBrowseDir;
        private ComboBox cmbMediaAction;
        private ModernButton btnSaveMapping;
        private ModernButton btnTestAction;

        // Console Controls
        private RichTextBox rtbLog;
        private ModernButton btnClearLog;
        private CheckBox chkMinimizeToTray;
        private CheckBox chkShowNotifications;
        private ModernButton btnResetDefaults;

        // Tray Icon
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        // Debounce tracking
        private long lastTriggerTicks = 0;
        private int lastTriggerButton = -1;

        public MainForm()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                           ControlStyles.UserPaint |
                           ControlStyles.OptimizedDoubleBuffer |
                           ControlStyles.ResizeRedraw, true);

            InitializeConfig();
            InitializeComponent();
            ApplyNativeDarkTitleBar();
            LoadMasterBitmap();
            InitializeTrayIcon();
            StartSerialManager();

            Log("SYSTEM", "reTerminal Sticky Host Manager initialized (Seeed Studio Edition).");
            Log("SYSTEM", "Listening for 15 buttons via Serial CDC (115200 baud).");
            SelectButton(0);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate(true);
        }

        private void ApplyNativeDarkTitleBar()
        {
            try
            {
                int darkMode = 1;
                NativeMethods.DwmSetWindowAttribute(this.Handle, 20, ref darkMode, sizeof(int));
                NativeMethods.DwmSetWindowAttribute(this.Handle, 19, ref darkMode, sizeof(int));
            }
            catch { }
        }

        private void LoadMasterBitmap()
        {
            headerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StreamDeck_Bitmap.h");
            if (!File.Exists(headerPath) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "StreamDeck_Bitmap.h")))
            {
                headerPath = Path.Combine(Directory.GetCurrentDirectory(), "StreamDeck_Bitmap.h");
            }

            // Check if user has a custom 800x480 image saved
            string customPath = config != null ? config.customImagePath : "";
            if (!string.IsNullOrEmpty(customPath))
            {
                if (!File.Exists(customPath))
                {
                    string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(customPath));
                    if (File.Exists(localFile)) customPath = localFile;
                    else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(customPath))))
                        customPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(customPath));
                }

                if (File.Exists(customPath))
                {
                    masterBmp = EpdBitmapEngine.LoadAndDitherImage(customPath);
                }
            }

            if (masterBmp == null)
            {
                string defaultCustom = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_ui_800x480.png");
                if (File.Exists(defaultCustom))
                {
                    masterBmp = EpdBitmapEngine.LoadAndDitherImage(defaultCustom);
                }
            }

            if (masterBmp == null)
            {
                masterBmp = EpdBitmapEngine.LoadBitmapFromHeader(headerPath);
                if (masterBmp != null)
                {
                    Log("SYSTEM", "Loaded default 800x480 E-Paper UI from StreamDeck_Bitmap.h");
                }
            }
            else
            {
                Log("SYSTEM", "Loaded custom 800x480 E-Paper UI from " + Path.GetFileName(customPath));
            }

            UpdateKeyViewsMasterBitmap();
        }

        private void UpdateKeyViewsMasterBitmap()
        {
            for (int i = 0; i < 15; i++)
            {
                if (keyViews[i] != null)
                {
                    keyViews[i].MasterEpdBitmap = masterBmp;
                    keyViews[i].Invalidate();
                }
            }
        }

        private void InitializeConfig()
        {
            jsonSerializer = new JavaScriptSerializer();
            configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "streamdeck_config.json");
            if (!File.Exists(configPath) && File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "streamdeck_config.json")))
            {
                configPath = Path.Combine(Directory.GetCurrentDirectory(), "streamdeck_config.json");
            }

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = jsonSerializer.Deserialize<AppConfig>(json);
                }
                catch
                {
                    config = AppConfig.CreateDefaults();
                }
            }
            else
            {
                config = AppConfig.CreateDefaults();
                SaveConfig();
            }

            while (config.buttons.Count < 15)
            {
                int newId = config.buttons.Count + 1;
                config.buttons.Add(new ButtonConfig { id = newId, name = string.Format("Button #{0}", newId), actionType = "LaunchApp" });
            }
        }

        private void SaveConfig()
        {
            try
            {
                string json = jsonSerializer.Serialize(config);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Log("ERROR", "Failed to save config: " + ex.Message);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "reTerminal Sticky — Stream Deck Host (Seeed Studio Edition)";
            this.Size = new Size(1220, 800);
            this.MinimumSize = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgWindow;
            this.ForeColor = Theme.TextPrimary;
            this.Font = Theme.FontBody;

            // 1. Header Toolbar
            DoubleBufferedPanel pnlHeader = new DoubleBufferedPanel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 70;
            pnlHeader.BackColor = Theme.BgHeader;
            pnlHeader.Padding = new Padding(24, 12, 24, 12);
            this.Controls.Add(pnlHeader);

            lblAppTitle = new Label();
            lblAppTitle.Text = "reTerminal Sticky";
            lblAppTitle.Font = Theme.FontAppTitle;
            lblAppTitle.ForeColor = Theme.TextPrimary;
            lblAppTitle.AutoSize = true;
            lblAppTitle.Location = new Point(24, 12);
            pnlHeader.Controls.Add(lblAppTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Seeed Studio Stream Deck Host • 15-Key USB Serial Controller";
            lblSubtitle.Font = Theme.FontSmall;
            lblSubtitle.ForeColor = Theme.TextSecondary;
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(26, 38);
            pnlHeader.Controls.Add(lblSubtitle);

            // Header Right Hardware Controls
            DoubleBufferedFlowLayoutPanel pnlHeaderRight = new DoubleBufferedFlowLayoutPanel();
            pnlHeaderRight.Dock = DockStyle.Right;
            pnlHeaderRight.AutoSize = true;
            pnlHeaderRight.FlowDirection = FlowDirection.LeftToRight;
            pnlHeaderRight.WrapContents = false;
            pnlHeaderRight.Padding = new Padding(0, 16, 10, 0);
            pnlHeader.Controls.Add(pnlHeaderRight);

            lblSerialStatus = new Label();
            lblSerialStatus.Text = "● Disconnected";
            lblSerialStatus.Font = Theme.FontBodyBold;
            lblSerialStatus.ForeColor = Theme.TextMuted;
            lblSerialStatus.AutoSize = true;
            lblSerialStatus.Margin = new Padding(0, 8, 12, 0);
            pnlHeaderRight.Controls.Add(lblSerialStatus);

            cmbSerialPorts = new ComboBox();
            cmbSerialPorts.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSerialPorts.BackColor = Theme.BgInput;
            cmbSerialPorts.ForeColor = Theme.TextPrimary;
            cmbSerialPorts.FlatStyle = FlatStyle.Flat;
            cmbSerialPorts.Font = Theme.FontBody;
            cmbSerialPorts.Width = 100;
            cmbSerialPorts.Height = 32;
            cmbSerialPorts.Margin = new Padding(0, 4, 8, 0);
            pnlHeaderRight.Controls.Add(cmbSerialPorts);

            btnSerialToggle = new ModernButton();
            btnSerialToggle.Text = "Connect";
            btnSerialToggle.Style = ModernButton.ButtonStyle.Primary;
            btnSerialToggle.Width = 96;
            btnSerialToggle.Margin = new Padding(0, 4, 10, 0);
            btnSerialToggle.Click += btnSerialToggle_Click;
            pnlHeaderRight.Controls.Add(btnSerialToggle);

            btnOpenImageManager = new ModernButton();
            btnOpenImageManager.Text = "🖼 800x480 E-Paper Image";
            btnOpenImageManager.Style = ModernButton.ButtonStyle.Primary;
            btnOpenImageManager.Width = 180;
            btnOpenImageManager.Margin = new Padding(0, 4, 10, 0);
            btnOpenImageManager.Click += btnOpenImageManager_Click;
            pnlHeaderRight.Controls.Add(btnOpenImageManager);

            btnRefreshEpd = new ModernButton();
            btnRefreshEpd.Text = "↻ Refresh";
            btnRefreshEpd.Style = ModernButton.ButtonStyle.Secondary;
            btnRefreshEpd.Width = 84;
            btnRefreshEpd.Margin = new Padding(0, 4, 6, 0);
            btnRefreshEpd.Click += delegate { SendSerialCommand("REFRESH"); };
            pnlHeaderRight.Controls.Add(btnRefreshEpd);

            btnFastRefresh = new ModernButton();
            btnFastRefresh.Text = "⚡ Fast";
            btnFastRefresh.Style = ModernButton.ButtonStyle.Secondary;
            btnFastRefresh.Width = 72;
            btnFastRefresh.Margin = new Padding(0, 4, 6, 0);
            btnFastRefresh.Click += delegate { SendSerialCommand("FAST_REFRESH"); };
            pnlHeaderRight.Controls.Add(btnFastRefresh);

            btnBeep = new ModernButton();
            btnBeep.Text = "🔔 Buzzer";
            btnBeep.Style = ModernButton.ButtonStyle.Secondary;
            btnBeep.Width = 80;
            btnBeep.Margin = new Padding(0, 4, 0, 0);
            btnBeep.Click += delegate { SendSerialCommand("BEEP"); };
            pnlHeaderRight.Controls.Add(btnBeep);

            // 2. Bottom Activity Console
            DoubleBufferedPanel pnlConsole = new DoubleBufferedPanel();
            pnlConsole.Dock = DockStyle.Bottom;
            pnlConsole.Height = 120;
            pnlConsole.BackColor = Theme.BgHeader;
            pnlConsole.Padding = new Padding(24, 8, 24, 10);
            this.Controls.Add(pnlConsole);

            DoubleBufferedPanel pnlConsoleHeader = new DoubleBufferedPanel();
            pnlConsoleHeader.Dock = DockStyle.Top;
            pnlConsoleHeader.Height = 26;
            pnlConsole.Controls.Add(pnlConsoleHeader);

            Label lblConsoleTitle = new Label();
            lblConsoleTitle.Text = "ACTIVITY CONSOLE";
            lblConsoleTitle.Font = Theme.FontBadge;
            lblConsoleTitle.ForeColor = Theme.TextSecondary;
            lblConsoleTitle.AutoSize = true;
            lblConsoleTitle.Location = new Point(0, 4);
            pnlConsoleHeader.Controls.Add(lblConsoleTitle);

            chkMinimizeToTray = new CheckBox();
            chkMinimizeToTray.Text = "Minimize to tray on close";
            chkMinimizeToTray.Font = Theme.FontSmall;
            chkMinimizeToTray.ForeColor = Theme.TextSecondary;
            chkMinimizeToTray.AutoSize = true;
            chkMinimizeToTray.Location = new Point(140, 4);
            chkMinimizeToTray.Checked = config.minimizeToTray;
            chkMinimizeToTray.CheckedChanged += delegate { config.minimizeToTray = chkMinimizeToTray.Checked; SaveConfig(); };
            pnlConsoleHeader.Controls.Add(chkMinimizeToTray);

            chkShowNotifications = new CheckBox();
            chkShowNotifications.Text = "Desktop notifications";
            chkShowNotifications.Font = Theme.FontSmall;
            chkShowNotifications.ForeColor = Theme.TextSecondary;
            chkShowNotifications.AutoSize = true;
            chkShowNotifications.Location = new Point(310, 4);
            chkShowNotifications.Checked = config.showNotifications;
            chkShowNotifications.CheckedChanged += delegate { config.showNotifications = chkShowNotifications.Checked; SaveConfig(); };
            pnlConsoleHeader.Controls.Add(chkShowNotifications);

            btnResetDefaults = new ModernButton();
            btnResetDefaults.Text = "Reset Defaults";
            btnResetDefaults.Style = ModernButton.ButtonStyle.Ghost;
            btnResetDefaults.Width = 110;
            btnResetDefaults.Height = 24;
            btnResetDefaults.Dock = DockStyle.Right;
            btnResetDefaults.Click += btnResetDefaults_Click;
            pnlConsoleHeader.Controls.Add(btnResetDefaults);

            btnClearLog = new ModernButton();
            btnClearLog.Text = "Clear";
            btnClearLog.Style = ModernButton.ButtonStyle.Ghost;
            btnClearLog.Width = 60;
            btnClearLog.Height = 24;
            btnClearLog.Dock = DockStyle.Right;
            btnClearLog.Margin = new Padding(0, 0, 8, 0);
            btnClearLog.Click += delegate { rtbLog.Clear(); };
            pnlConsoleHeader.Controls.Add(btnClearLog);

            rtbLog = new RichTextBox();
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.BackColor = Theme.BgWindow;
            rtbLog.ForeColor = Theme.TextSecondary;
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.Font = Theme.FontMono;
            rtbLog.ReadOnly = true;
            pnlConsole.Controls.Add(rtbLog);

            // 3. Right Sidebar: Button Inspector
            DoubleBufferedPanel pnlInspector = new DoubleBufferedPanel();
            pnlInspector.Dock = DockStyle.Right;
            pnlInspector.Width = 430;
            pnlInspector.BackColor = Theme.BgSidebar;
            pnlInspector.Padding = new Padding(24, 20, 24, 20);
            this.Controls.Add(pnlInspector);

            BuildInspectorControls(pnlInspector);

            // 4. Center Main Panel: 15 Hardware Keys (3x5 Grid)
            DoubleBufferedPanel pnlKeypadContainer = new DoubleBufferedPanel();
            pnlKeypadContainer.Dock = DockStyle.Fill;
            pnlKeypadContainer.BackColor = Theme.BgWindow;
            pnlKeypadContainer.Padding = new Padding(20, 10, 16, 10);
            this.Controls.Add(pnlKeypadContainer);

            Label lblKeypadTitle = new Label();
            lblKeypadTitle.Text = "HARDWARE KEYPAD (Select key to configure)";
            lblKeypadTitle.Font = Theme.FontBadge;
            lblKeypadTitle.ForeColor = Theme.TextSecondary;
            lblKeypadTitle.Dock = DockStyle.Top;
            lblKeypadTitle.Height = 28;
            lblKeypadTitle.Padding = new Padding(4, 0, 0, 4);

            DoubleBufferedTableLayoutPanel gridKeys = new DoubleBufferedTableLayoutPanel();
            gridKeys.Dock = DockStyle.Fill;
            gridKeys.ColumnCount = 5;
            gridKeys.RowCount = 3;
            gridKeys.BackColor = Theme.BgWindow;
            gridKeys.Padding = new Padding(0, 6, 0, 0);

            for (int col = 0; col < 5; col++)
                gridKeys.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            for (int row = 0; row < 3; row++)
                gridKeys.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

            for (int i = 0; i < 15; i++)
            {
                int btnIndex = i;
                StreamDeckKeyView keyView = new StreamDeckKeyView(btnIndex, config.buttons[btnIndex]);
                keyView.Dock = DockStyle.Fill;
                keyView.Margin = new Padding(4);
                keyView.MasterEpdBitmap = masterBmp;
                keyView.Click += delegate { SelectButton(btnIndex); };
                keyViews[btnIndex] = keyView;
                gridKeys.Controls.Add(keyView, btnIndex % 5, btnIndex / 5);
            }

            pnlKeypadContainer.Controls.Add(gridKeys);
            pnlKeypadContainer.Controls.Add(lblKeypadTitle);
            pnlKeypadContainer.Controls.SetChildIndex(gridKeys, 0);
            pnlKeypadContainer.Controls.SetChildIndex(lblKeypadTitle, 1);

            this.Controls.SetChildIndex(pnlKeypadContainer, 0);
            this.Controls.SetChildIndex(pnlInspector, 1);
            this.Controls.SetChildIndex(pnlConsole, 2);
            this.Controls.SetChildIndex(pnlHeader, 3);
        }

        private void BuildInspectorControls(Panel pnl)
        {
            Label lblSection = new Label();
            lblSection.Text = "BUTTON INSPECTOR";
            lblSection.Font = Theme.FontBadge;
            lblSection.ForeColor = Theme.TextSecondary;
            lblSection.Location = new Point(24, 12);
            lblSection.AutoSize = true;
            pnl.Controls.Add(lblSection);

            lblInspectorHeader = new Label();
            lblInspectorHeader.Text = "Button #01";
            lblInspectorHeader.Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold);
            lblInspectorHeader.ForeColor = Theme.TextPrimary;
            lblInspectorHeader.Location = new Point(24, 30);
            lblInspectorHeader.AutoSize = true;
            pnl.Controls.Add(lblInspectorHeader);

            int curY = 72;

            // Button Name
            Label lblName = new Label();
            lblName.Text = "Button Name";
            lblName.Font = Theme.FontBodyBold;
            lblName.ForeColor = Theme.TextSecondary;
            lblName.Location = new Point(24, curY);
            lblName.AutoSize = true;
            pnl.Controls.Add(lblName);
            curY += 22;

            txtName = new TextBox();
            txtName.BackColor = Theme.BgInput;
            txtName.ForeColor = Theme.TextPrimary;
            txtName.BorderStyle = BorderStyle.FixedSingle;
            txtName.Font = Theme.FontBody;
            txtName.Location = new Point(24, curY);
            txtName.Width = 380;
            pnl.Controls.Add(txtName);
            curY += 34;

            // Action Type
            Label lblAction = new Label();
            lblAction.Text = "Action Type";
            lblAction.Font = Theme.FontBodyBold;
            lblAction.ForeColor = Theme.TextSecondary;
            lblAction.Location = new Point(24, curY);
            lblAction.AutoSize = true;
            pnl.Controls.Add(lblAction);
            curY += 22;

            cmbActionType = new ComboBox();
            cmbActionType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbActionType.BackColor = Theme.BgInput;
            cmbActionType.ForeColor = Theme.TextPrimary;
            cmbActionType.FlatStyle = FlatStyle.Flat;
            cmbActionType.Font = Theme.FontBody;
            cmbActionType.Location = new Point(24, curY);
            cmbActionType.Width = 380;
            cmbActionType.Items.Add("Launch Application (.exe / script)");
            cmbActionType.Items.Add("Open Website URL");
            cmbActionType.Items.Add("Media Control Shortcut");
            cmbActionType.Items.Add("Run Shell Command");
            cmbActionType.SelectedIndexChanged += cmbActionType_SelectedIndexChanged;
            pnl.Controls.Add(cmbActionType);
            curY += 36;

            // Target (File / URL / Media / Command)
            lblTarget = new Label();
            lblTarget.Text = "Program Executable Path";
            lblTarget.Font = Theme.FontBodyBold;
            lblTarget.ForeColor = Theme.TextSecondary;
            lblTarget.Location = new Point(24, curY);
            lblTarget.AutoSize = true;
            pnl.Controls.Add(lblTarget);
            curY += 22;

            txtTarget = new TextBox();
            txtTarget.BackColor = Theme.BgInput;
            txtTarget.ForeColor = Theme.TextPrimary;
            txtTarget.BorderStyle = BorderStyle.FixedSingle;
            txtTarget.Font = Theme.FontBody;
            txtTarget.Location = new Point(24, curY);
            txtTarget.Width = 300;
            pnl.Controls.Add(txtTarget);

            btnBrowseFile = new ModernButton();
            btnBrowseFile.Text = "Browse";
            btnBrowseFile.Style = ModernButton.ButtonStyle.Secondary;
            btnBrowseFile.Location = new Point(330, curY - 2);
            btnBrowseFile.Width = 74;
            btnBrowseFile.Height = 26;
            btnBrowseFile.Click += btnBrowseFile_Click;
            pnl.Controls.Add(btnBrowseFile);

            cmbMediaAction = new ComboBox();
            cmbMediaAction.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMediaAction.BackColor = Theme.BgInput;
            cmbMediaAction.ForeColor = Theme.TextPrimary;
            cmbMediaAction.FlatStyle = FlatStyle.Flat;
            cmbMediaAction.Font = Theme.FontBody;
            cmbMediaAction.Location = new Point(24, curY);
            cmbMediaAction.Width = 380;
            cmbMediaAction.Items.AddRange(new object[] { "PLAY_PAUSE", "NEXT_TRACK", "PREV_TRACK", "VOL_UP", "VOL_DOWN", "MUTE" });
            cmbMediaAction.Visible = false;
            pnl.Controls.Add(cmbMediaAction);
            curY += 36;

            // Command Arguments
            lblArgs = new Label();
            lblArgs.Text = "Command-line Arguments (Optional)";
            lblArgs.Font = Theme.FontBodyBold;
            lblArgs.ForeColor = Theme.TextSecondary;
            lblArgs.Location = new Point(24, curY);
            lblArgs.AutoSize = true;
            pnl.Controls.Add(lblArgs);
            curY += 22;

            txtArgs = new TextBox();
            txtArgs.BackColor = Theme.BgInput;
            txtArgs.ForeColor = Theme.TextPrimary;
            txtArgs.BorderStyle = BorderStyle.FixedSingle;
            txtArgs.Font = Theme.FontBody;
            txtArgs.Location = new Point(24, curY);
            txtArgs.Width = 380;
            pnl.Controls.Add(txtArgs);
            curY += 34;

            // Working Directory
            lblWorkingDir = new Label();
            lblWorkingDir.Text = "Working Directory (Optional)";
            lblWorkingDir.Font = Theme.FontBodyBold;
            lblWorkingDir.ForeColor = Theme.TextSecondary;
            lblWorkingDir.Location = new Point(24, curY);
            lblWorkingDir.AutoSize = true;
            pnl.Controls.Add(lblWorkingDir);
            curY += 22;

            txtWorkingDir = new TextBox();
            txtWorkingDir.BackColor = Theme.BgInput;
            txtWorkingDir.ForeColor = Theme.TextPrimary;
            txtWorkingDir.BorderStyle = BorderStyle.FixedSingle;
            txtWorkingDir.Font = Theme.FontBody;
            txtWorkingDir.Location = new Point(24, curY);
            txtWorkingDir.Width = 300;
            pnl.Controls.Add(txtWorkingDir);

            btnBrowseDir = new ModernButton();
            btnBrowseDir.Text = "Browse";
            btnBrowseDir.Style = ModernButton.ButtonStyle.Secondary;
            btnBrowseDir.Location = new Point(330, curY - 2);
            btnBrowseDir.Width = 74;
            btnBrowseDir.Height = 26;
            btnBrowseDir.Click += btnBrowseDir_Click;
            pnl.Controls.Add(btnBrowseDir);
            curY += 50;

            // Action Buttons
            btnSaveMapping = new ModernButton();
            btnSaveMapping.Text = "Save Changes";
            btnSaveMapping.Style = ModernButton.ButtonStyle.Primary;
            btnSaveMapping.Location = new Point(24, curY);
            btnSaveMapping.Width = 180;
            btnSaveMapping.Height = 38;
            btnSaveMapping.Click += btnSaveMapping_Click;
            pnl.Controls.Add(btnSaveMapping);

            btnTestAction = new ModernButton();
            btnTestAction.Text = "▶ Test Launch";
            btnTestAction.Style = ModernButton.ButtonStyle.Secondary;
            btnTestAction.Location = new Point(216, curY);
            btnTestAction.Width = 188;
            btnTestAction.Height = 38;
            btnTestAction.Click += delegate { ExecuteButtonAction(selectedButtonIndex, "Test Launch"); };
            pnl.Controls.Add(btnTestAction);
        }

        private void SelectButton(int index)
        {
            if (index < 0 || index >= config.buttons.Count) return;
            selectedButtonIndex = index;

            for (int i = 0; i < 15; i++)
            {
                keyViews[i].IsSelected = (i == index);
                keyViews[i].Invalidate();
            }

            ButtonConfig b = config.buttons[index];
            lblInspectorHeader.Text = string.Format("Button #{0:D2} — {1}", b.id, b.name);
            txtName.Text = b.name ?? "";

            if (b.actionType == "OpenUrl") cmbActionType.SelectedIndex = 1;
            else if (b.actionType == "MediaControl") cmbActionType.SelectedIndex = 2;
            else if (b.actionType == "RunCommand") cmbActionType.SelectedIndex = 3;
            else cmbActionType.SelectedIndex = 0;

            if (b.actionType == "MediaControl")
            {
                cmbMediaAction.SelectedItem = b.target;
            }
            else
            {
                txtTarget.Text = b.target ?? "";
            }

            txtArgs.Text = b.arguments ?? "";
            txtWorkingDir.Text = b.workingDir ?? "";
        }

        private void cmbActionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (txtTarget == null || cmbMediaAction == null || btnBrowseFile == null) return;
            int idx = cmbActionType.SelectedIndex;

            if (idx == 1) // Open URL
            {
                lblTarget.Text = "Website URL (e.g. https://...)";
                txtTarget.Visible = true;
                btnBrowseFile.Visible = false;
                cmbMediaAction.Visible = false;
                lblArgs.Visible = false;
                txtArgs.Visible = false;
                lblWorkingDir.Visible = false;
                txtWorkingDir.Visible = false;
                btnBrowseDir.Visible = false;
            }
            else if (idx == 2) // Media Control
            {
                lblTarget.Text = "Media Shortcut Function";
                txtTarget.Visible = false;
                btnBrowseFile.Visible = false;
                cmbMediaAction.Visible = true;
                lblArgs.Visible = false;
                txtArgs.Visible = false;
                lblWorkingDir.Visible = false;
                txtWorkingDir.Visible = false;
                btnBrowseDir.Visible = false;
            }
            else if (idx == 3) // Run Command
            {
                lblTarget.Text = "Command or Script Path";
                txtTarget.Visible = true;
                btnBrowseFile.Visible = false;
                cmbMediaAction.Visible = false;
                lblArgs.Visible = true;
                txtArgs.Visible = true;
                lblWorkingDir.Visible = true;
                txtWorkingDir.Visible = true;
                btnBrowseDir.Visible = true;
            }
            else // Launch App
            {
                lblTarget.Text = "Program Executable Path";
                txtTarget.Visible = true;
                btnBrowseFile.Visible = true;
                cmbMediaAction.Visible = false;
                lblArgs.Visible = true;
                txtArgs.Visible = true;
                lblWorkingDir.Visible = true;
                txtWorkingDir.Visible = true;
                btnBrowseDir.Visible = true;
            }
        }

        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Application to Map";
                ofd.Filter = "Applications & Shortcuts (*.exe;*.lnk;*.bat;*.cmd)|*.exe;*.lnk;*.bat;*.cmd|All Files (*.*)|*.*";
                ofd.CheckFileExists = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtTarget.Text = ofd.FileName;
                    if (string.IsNullOrEmpty(txtName.Text) || txtName.Text.StartsWith("Button "))
                    {
                        txtName.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                    }
                    if (string.IsNullOrEmpty(txtWorkingDir.Text))
                    {
                        txtWorkingDir.Text = Path.GetDirectoryName(ofd.FileName);
                    }
                }
            }
        }

        private void btnBrowseDir_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select Working Directory";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtWorkingDir.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnSaveMapping_Click(object sender, EventArgs e)
        {
            ButtonConfig b = config.buttons[selectedButtonIndex];
            b.name = txtName.Text.Trim();

            int typeIdx = cmbActionType.SelectedIndex;
            if (typeIdx == 1) b.actionType = "OpenUrl";
            else if (typeIdx == 2) b.actionType = "MediaControl";
            else if (typeIdx == 3) b.actionType = "RunCommand";
            else b.actionType = "LaunchApp";

            if (b.actionType == "MediaControl")
            {
                b.target = cmbMediaAction.SelectedItem != null ? cmbMediaAction.SelectedItem.ToString() : "PLAY_PAUSE";
            }
            else
            {
                b.target = txtTarget.Text.Trim();
            }

            b.arguments = txtArgs.Text.Trim();
            b.workingDir = txtWorkingDir.Text.Trim();

            SaveConfig();
            keyViews[selectedButtonIndex].Config = b;
            keyViews[selectedButtonIndex].Invalidate();
            lblInspectorHeader.Text = string.Format("Button #{0:D2} — {1}", b.id, b.name);

            Log("CONFIG", string.Format("Saved Button #{0} ({1}) -> {2} [{3}]", b.id, b.name, b.actionType, b.target));
        }

        private void btnOpenImageManager_Click(object sender, EventArgs e)
        {
            bool isConnected = (serialPort != null && serialPort.IsOpen);
            using (EpdImageForm form = new EpdImageForm(masterBmp, headerPath,
                (epdBytes, progressCallback) => UploadBitmapToDevice(epdBytes, progressCallback),
                isConnected,
                (newBmp) => {
                    if (masterBmp != null) masterBmp.Dispose();
                    masterBmp = new Bitmap(newBmp);
                    UpdateKeyViewsMasterBitmap();

                    // Save custom image to disk for auto-reloading
                    try
                    {
                        string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_ui_800x480.png");
                        masterBmp.Save(savePath, ImageFormat.Png);
                        config.customImagePath = savePath;
                        SaveConfig();
                    }
                    catch { }
                }))
            {
                form.ShowDialog(this);
            }
        }

        private void btnResetDefaults_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reset all 15 buttons to default factory configurations?", "Confirm Reset",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                config = AppConfig.CreateDefaults();
                SaveConfig();
                for (int i = 0; i < 15; i++)
                {
                    keyViews[i].Config = config.buttons[i];
                    keyViews[i].Invalidate();
                }
                SelectButton(0);
                Log("CONFIG", "All buttons reset to factory defaults.");
            }
        }

        // =====================================================================
        // Action Dispatcher (Triggered exclusively via Serial CDC)
        // =====================================================================
        public void ExecuteButtonAction(int index, string sourceTrigger)
        {
            if (index < 0 || index >= config.buttons.Count) return;

            long nowTicks = DateTime.UtcNow.Ticks;
            long elapsedMs = (nowTicks - lastTriggerTicks) / TimeSpan.TicksPerMillisecond;
            if (index == lastTriggerButton && elapsedMs < 250) return;
            lastTriggerTicks = nowTicks;
            lastTriggerButton = index;

            ButtonConfig b = config.buttons[index];

            this.BeginInvoke((MethodInvoker)delegate {
                Log("ACTION", string.Format("[{0}] Button #{1} ({2}) -> {3}", sourceTrigger, b.id, b.name, b.actionType));

                if (config.showNotifications && trayIcon != null)
                {
                    trayIcon.ShowBalloonTip(1000, "reTerminal Sticky",
                        string.Format("Button #{0}: {1}", b.id, b.name), ToolTipIcon.Info);
                }
            });

            ThreadPool.QueueUserWorkItem(delegate {
                try
                {
                    if (b.actionType == "MediaControl")
                    {
                        ExecuteMediaAction(b.target);
                    }
                    else if (b.actionType == "OpenUrl")
                    {
                        string url = b.target;
                        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            url = "https://" + url;
                        }
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    else if (b.actionType == "RunCommand")
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = "cmd.exe";
                        psi.Arguments = string.Format("/c {0} {1}", b.target, b.arguments);
                        if (!string.IsNullOrEmpty(b.workingDir) && Directory.Exists(b.workingDir))
                            psi.WorkingDirectory = b.workingDir;
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;
                        Process.Start(psi);
                    }
                    else // LaunchApp
                    {
                        string target = Environment.ExpandEnvironmentVariables(b.target ?? "");
                        if (!string.IsNullOrEmpty(target))
                        {
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = target;
                            if (!string.IsNullOrEmpty(b.arguments)) psi.Arguments = b.arguments;
                            if (!string.IsNullOrEmpty(b.workingDir) && Directory.Exists(b.workingDir))
                                psi.WorkingDirectory = b.workingDir;
                            psi.UseShellExecute = true;
                            Process.Start(psi);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate {
                        Log("ERROR", string.Format("Failed to execute action for Button #{0}: {1}", b.id, ex.Message));
                    });
                }
            });
        }

        private void ExecuteMediaAction(string target)
        {
            switch (target)
            {
                case "PLAY_PAUSE":
                    SendVirtualKey(NativeMethods.VK_MEDIA_PLAY_PAUSE);
                    break;
                case "NEXT_TRACK":
                    SendVirtualKey(NativeMethods.VK_MEDIA_NEXT_TRACK);
                    break;
                case "PREV_TRACK":
                    SendVirtualKey(NativeMethods.VK_MEDIA_PREV_TRACK);
                    break;
                case "VOL_UP":
                    SendVirtualKey(NativeMethods.VK_VOLUME_UP);
                    SendVirtualKey(NativeMethods.VK_VOLUME_UP);
                    break;
                case "VOL_DOWN":
                    SendVirtualKey(NativeMethods.VK_VOLUME_DOWN);
                    SendVirtualKey(NativeMethods.VK_VOLUME_DOWN);
                    break;
                case "MUTE":
                    SendVirtualKey(NativeMethods.VK_VOLUME_MUTE);
                    break;
            }
        }

        private void SendVirtualKey(byte vk)
        {
            NativeMethods.keybd_event(vk, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(vk, 0, 2, UIntPtr.Zero);
        }

        // =====================================================================
        // Serial CDC Manager & Live Bitmap Streamer
        // =====================================================================
        private void StartSerialManager()
        {
            UpdateSerialPortList();

            serialAutoConnectTimer = new System.Windows.Forms.Timer();
            serialAutoConnectTimer.Interval = 2500;
            serialAutoConnectTimer.Tick += delegate {
                if (serialPort == null || !serialPort.IsOpen)
                {
                    UpdateSerialPortList();
                    if (config.autoConnectSerial && !isConnectingSerial)
                    {
                        AutoConnectSerialPort();
                    }
                }
            };
            serialAutoConnectTimer.Start();

            if (config.autoConnectSerial)
            {
                AutoConnectSerialPort();
            }
        }

        private void UpdateSerialPortList()
        {
            string[] ports = SerialPort.GetPortNames();
            string curSel = cmbSerialPorts.SelectedItem != null ? cmbSerialPorts.SelectedItem.ToString() : "";

            cmbSerialPorts.Items.Clear();
            cmbSerialPorts.Items.Add("AUTO");
            foreach (string p in ports)
            {
                cmbSerialPorts.Items.Add(p);
            }

            if (!string.IsNullOrEmpty(curSel) && cmbSerialPorts.Items.Contains(curSel))
                cmbSerialPorts.SelectedItem = curSel;
            else
                cmbSerialPorts.SelectedIndex = 0;
        }

        private void AutoConnectSerialPort()
        {
            if (isConnectingSerial) return;

            string[] ports = SerialPort.GetPortNames();
            if (ports.Length == 0) return;

            string targetPort = null;
            if (!string.IsNullOrEmpty(config.lastComPort) && config.lastComPort != "AUTO")
            {
                foreach (string p in ports)
                {
                    if (p.Equals(config.lastComPort, StringComparison.OrdinalIgnoreCase))
                    {
                        targetPort = p;
                        break;
                    }
                }
            }

            if (targetPort == null && ports.Length > 0)
            {
                targetPort = ports[ports.Length - 1]; // pick highest COM port
            }

            if (targetPort != null)
            {
                ConnectSerial(targetPort);
            }
        }

        private void ConnectSerial(string portName)
        {
            if (isConnectingSerial) return;
            isConnectingSerial = true;

            try
            {
                if (serialPort != null)
                {
                    if (serialPort.IsOpen) serialPort.Close();
                    serialPort.Dispose();
                }

                serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();

                config.lastComPort = portName;
                SaveConfig();

                btnSerialToggle.Text = "Disconnect";
                btnSerialToggle.Style = ModernButton.ButtonStyle.Danger;
                btnSerialToggle.Invalidate();

                lblSerialStatus.Text = string.Format("● Connected: {0}", portName);
                lblSerialStatus.ForeColor = Theme.SeeedGreen;
                Log("SERIAL", string.Format("Connected to reTerminal Sticky on {0} (115200 baud)", portName));
            }
            catch
            {
                lblSerialStatus.Text = "● Disconnected";
                lblSerialStatus.ForeColor = Theme.TextMuted;
            }
            finally
            {
                isConnectingSerial = false;
            }
        }

        private void DisconnectSerial()
        {
            if (serialPort != null)
            {
                try { if (serialPort.IsOpen) serialPort.Close(); serialPort.Dispose(); } catch { }
                serialPort = null;
            }

            btnSerialToggle.Text = "Connect";
            btnSerialToggle.Style = ModernButton.ButtonStyle.Primary;
            btnSerialToggle.Invalidate();

            lblSerialStatus.Text = "● Disconnected";
            lblSerialStatus.ForeColor = Theme.TextMuted;
            Log("SERIAL", "Serial port disconnected.");
        }

        private void btnSerialToggle_Click(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                DisconnectSerial();
            }
            else
            {
                string sel = cmbSerialPorts.SelectedItem != null ? cmbSerialPorts.SelectedItem.ToString() : "AUTO";
                if (sel == "AUTO") AutoConnectSerialPort();
                else ConnectSerial(sel);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (serialPort == null || !serialPort.IsOpen) return;
                string line = serialPort.ReadLine().Trim();

                if (!string.IsNullOrEmpty(line))
                {
                    this.BeginInvoke((MethodInvoker)delegate {
                        if (line.Contains("[StreamDeck] Button "))
                        {
                            int startIdx = line.IndexOf("Button ") + 7;
                            int endIdx = line.IndexOf(" ", startIdx);
                            if (endIdx > startIdx)
                            {
                                string numStr = line.Substring(startIdx, endIdx - startIdx);
                                int btnNum;
                                if (int.TryParse(numStr, out btnNum) && btnNum >= 1 && btnNum <= 15)
                                {
                                    ExecuteButtonAction(btnNum - 1, "Serial CDC");
                                    return;
                                }
                            }
                        }
                        Log("HARDWARE", line);
                    });
                }
            }
            catch { }
        }

        private void SendSerialCommand(string cmd)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    serialPort.WriteLine(cmd);
                    Log("COMMAND", "Sent: " + cmd);
                }
                catch (Exception ex)
                {
                    Log("ERROR", "Serial write error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Serial port is not connected.\nPlease connect to the reTerminal Sticky COM port first.",
                    "Hardware Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void UploadBitmapToDevice(byte[] epdBytes, Action<int> onProgress)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                this.BeginInvoke((MethodInvoker)delegate {
                    MessageBox.Show("Serial port is not connected. Please connect to reTerminal Sticky first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
                return;
            }

            try
            {
                Log("SERIAL", "Initiating UPLOAD_BITMAP to reTerminal Sticky...");
                serialPort.WriteLine("UPLOAD_BITMAP 48000");

                Thread.Sleep(200);

                int chunkSize = 1000;
                for (int offset = 0; offset < epdBytes.Length; offset += chunkSize)
                {
                    int count = Math.Min(chunkSize, epdBytes.Length - offset);
                    serialPort.Write(epdBytes, offset, count);
                    Thread.Sleep(12);

                    if (onProgress != null)
                    {
                        onProgress((offset + count) * 100 / epdBytes.Length);
                    }
                }

                Log("SERIAL", "Streamed 48,000 bytes successfully. Screen refreshing...");
            }
            catch (Exception ex)
            {
                Log("ERROR", "Bitmap stream error: " + ex.Message);
            }
        }

        // =====================================================================
        // System Tray
        // =====================================================================
        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Open Manager", null, (s, e) => RestoreFromTray());
            trayMenu.Items.Add("Customize 800x480 Image...", null, (s, e) => btnOpenImageManager_Click(null, null));
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Refresh E-Paper", null, (s, e) => SendSerialCommand("REFRESH"));
            trayMenu.Items.Add("Fast Refresh", null, (s, e) => SendSerialCommand("FAST_REFRESH"));
            trayMenu.Items.Add("Test Buzzer", null, (s, e) => SendSerialCommand("BEEP"));
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

            trayIcon = new NotifyIcon();
            trayIcon.Text = "reTerminal Sticky Stream Deck";
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (config.minimizeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                if (config.showNotifications)
                {
                    trayIcon.ShowBalloonTip(1500, "reTerminal Stream Deck",
                        "App minimized to tray. Keypad shortcuts are still active!", ToolTipIcon.Info);
                }
                return;
            }

            if (serialPort != null)
            {
                try { if (serialPort.IsOpen) serialPort.Close(); serialPort.Dispose(); } catch { }
            }
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }

        // =====================================================================
        // Logging Helper
        // =====================================================================
        private void Log(string tag, string message)
        {
            if (rtbLog == null || rtbLog.IsDisposed) return;

            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            string line = string.Format("[{0}] [{1}] {2}\n", timeStamp, tag, message);

            Color tagColor = Theme.TextSecondary;
            if (tag == "ACTION") tagColor = Theme.SeeedGreen;
            else if (tag == "SERIAL") tagColor = Theme.AccentSky;
            else if (tag == "HARDWARE") tagColor = Theme.AccentPurple;
            else if (tag == "ERROR") tagColor = Theme.AccentRose;
            else if (tag == "COMMAND") tagColor = Theme.AccentAmber;

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = Color.FromArgb(70, 105, 120);
            rtbLog.AppendText(string.Format("[{0}] ", timeStamp));

            rtbLog.SelectionColor = tagColor;
            rtbLog.AppendText(string.Format("[{0}] ", tag));

            rtbLog.SelectionColor = Theme.TextPrimary;
            rtbLog.AppendText(message + "\n");
            rtbLog.ScrollToCaret();
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
