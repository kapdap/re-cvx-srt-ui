using SRTPluginProviderRECVX;
using SRTPluginProviderRECVX.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace SRTPluginUIRECVXWinForms
{
    public partial class MainUI : Form
    {
        // How often to perform more expensive operations.
        // 16 milliseconds for a slim scan.
        public const long FULL_UI_DRAW_TICKS = TimeSpan.TicksPerMillisecond * 333L;

        private long lastFullUIDraw;

        // Quality settings (high performance).
        private CompositingMode compositingMode = CompositingMode.SourceOver;
        private CompositingQuality compositingQuality = CompositingQuality.HighSpeed;
        private SmoothingMode smoothingMode = SmoothingMode.None;
        private PixelOffsetMode pixelOffsetMode = PixelOffsetMode.Half;
        private InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor;
        private TextRenderingHint textRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // Text alignment and formatting.
        private StringFormat invStringFormat = new StringFormat(StringFormat.GenericDefault) { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far };
        private StringFormat stdStringFormat = new StringFormat(StringFormat.GenericDefault) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };

        private Bitmap inventoryError; // An error image.
        private Bitmap inventoryImage;

        private GameMemoryRECVX gameMemory;

        public MainUI()
        {
            InitializeComponent();

            // Set titlebar.
            Text += string.Format(" {0}", Program.srtVersion);

            ContextMenuStrip = PluginUI.contextMenuStrip;
            playerHealthStatus.ContextMenuStrip = PluginUI.contextMenuStrip;
            statisticsPanel.ContextMenuStrip = PluginUI.contextMenuStrip;
            inventoryPanel.ContextMenuStrip = PluginUI.contextMenuStrip;

            //GDI+
            playerHealthStatus.Paint += playerHealthStatus_Paint;
            statisticsPanel.Paint += statisticsPanel_Paint;
            inventoryPanel.Paint += inventoryPanel_Paint;

            int titleHeight = 39;
            int borderWidth = 16;

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoTitleBar))
            {
                titleHeight = 0;
                borderWidth = 0;

                FormBorderStyle = FormBorderStyle.None;
            }

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.Transparent))
                TransparencyKey = Color.Black;

            // Only run the following code if we're rendering inventory.
            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
            {
                GenerateImages();

                // Set the width and height of the inventory display so it matches the maximum items and the scaling size of those items.
                inventoryPanel.Width = Program.INV_SLOT_WIDTH * 2 + (Program.INV_SLOT_WIDTH / 2);
                inventoryPanel.Height = Program.INV_SLOT_HEIGHT * 6;

                // Adjust main form width as well.
                Width = inventoryPanel.Width + (statisticsPanel.Width - 30) + borderWidth;

                // Only adjust form height if its greater than 545. We don't want it to go below this size.
                if (titleHeight + inventoryPanel.Height > 545)
                    Height = titleHeight + inventoryPanel.Height;
            }
            else
            {
                // Disable rendering of the inventory panel.
                inventoryPanel.Visible = false;

                Height = titleHeight + playerHealthStatus.Height + 70;

                if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.Debug))
                    Height += 56;

                if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoEnemyHealth))
                {
                    Height += 150;
                    Width = statisticsPanel.Width + borderWidth;
                }
                else
                    Width = playerHealthStatus.Width + borderWidth;
            }

            lastFullUIDraw = DateTime.UtcNow.Ticks;
        }

        public void GenerateImages()
        {
            // Create a black slot image for when side-pack is not equipped.
            inventoryError = new Bitmap(Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT, PixelFormat.Format32bppPArgb);
            using (Graphics grp = Graphics.FromImage(inventoryError))
            {
                grp.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 0, 0)), 0, 0, inventoryError.Width, inventoryError.Height);
                grp.DrawLine(new Pen(Color.FromArgb(150, 255, 0, 0), 3), 0, 0, inventoryError.Width, inventoryError.Height);
                grp.DrawLine(new Pen(Color.FromArgb(150, 255, 0, 0), 3), inventoryError.Width, 0, 0, inventoryError.Height);
            }

            // Transform the image into a 32-bit PARGB Bitmap.
            try
            {
                inventoryImage = Properties.Resources.ICONS.Clone(new Rectangle(0, 0, Properties.Resources.ICONS.Width, Properties.Resources.ICONS.Height), PixelFormat.Format32bppPArgb);
            }
            catch (Exception ex)
            {
                Program.FailFast(string.Format("[{0}] An unhandled exception has occurred. Please see below for details.\r\n\r\n[{1}] {2}\r\n{3}.\r\n\r\nPARGB Transform.", Program.srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace), ex);
            }

            // Rescales the image down if the scaling factor is not 1.
            if (Program.programSpecialOptions.ScalingFactor != 1d)
            {
                try
                {
                    inventoryImage = new Bitmap(inventoryImage, (int)Math.Round(inventoryImage.Width * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero), (int)Math.Round(inventoryImage.Height * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero));
                }
                catch (Exception ex)
                {
                    Program.FailFast(string.Format(@"[{0}] An unhandled exception has occurred. Please see below for details.
---
[{1}] {2}
{3}", Program.srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace), ex);
                }
            }
        }

        public void ReceiveData(object gameMemoryRECVX)
        {
            gameMemory = (GameMemoryRECVX)gameMemoryRECVX;

            try
            {
                if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.AlwaysOnTop))
                {
                    bool hasFocus;
                    if (InvokeRequired)
                        hasFocus = PInvoke.HasActiveFocus((IntPtr)Invoke(new Func<IntPtr>(() => Handle)));
                    else
                        hasFocus = PInvoke.HasActiveFocus(Handle);

                    if (!hasFocus)
                    {
                        if (InvokeRequired)
                            Invoke(new Action(() => TopMost = true));
                        else
                            TopMost = true;
                    }
                }

                // Only draw occasionally, not as often as the stats panel.
                if (DateTime.UtcNow.Ticks - lastFullUIDraw >= FULL_UI_DRAW_TICKS)
                {
                    // Update the last drawn time.
                    lastFullUIDraw = DateTime.UtcNow.Ticks;

                    // Only draw these periodically to reduce CPU usage.
                    playerHealthStatus.Invalidate();
                    if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
                        inventoryPanel.Invalidate();
                }

                // Always draw this as these are simple text draws and contains the IGT/frame count.
                statisticsPanel.Invalidate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[{0}] {1}\r\n{2}", ex.GetType().ToString(), ex.Message, ex.StackTrace);
            }
        }

        private void playerHealthStatus_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = smoothingMode;
            e.Graphics.CompositingQuality = compositingQuality;
            e.Graphics.CompositingMode = compositingMode;
            e.Graphics.InterpolationMode = interpolationMode;
            e.Graphics.PixelOffsetMode = pixelOffsetMode;
            e.Graphics.TextRenderingHint = textRenderingHint;

            // Draw health.
            Font healthFont = new Font("Consolas", 14, FontStyle.Bold);

            if (!gameMemory.Player.IsAlive) // Dead
            {
                e.Graphics.DrawString("DEAD", healthFont, Brushes.Red, 15, 37, stdStringFormat);
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.EMPTY, "EMPTY");

                return;
            }

            Brush brush = Brushes.LawnGreen;

            if (gameMemory.Player.IsGassed)
            {
                brush = Brushes.Purple;
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.POISON, "POISON");
            }
            else if(gameMemory.Player.IsPoison)
            {
                brush = Brushes.Violet;
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.POISON, "POISON");
            }
            else if (gameMemory.Player.IsDanger)
            {
                brush = Brushes.Red;
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.DANGER, "DANGER");
            }
            else if (gameMemory.Player.IsCautionOrange)
            {
                brush = Brushes.Gold;
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.CAUTION_YELLOW, "CAUTION_YELLOW");
            }
            else if (gameMemory.Player.IsCautionYellow)
            {
                brush = Brushes.Goldenrod;
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.CAUTION_YELLOW, "CAUTION_YELLOW");
            }
            else
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.FINE, "FINE");

            e.Graphics.DrawString(gameMemory.Player.CurrentHP.ToString(), healthFont, brush, 15, 37, stdStringFormat);
        }

        private void inventoryPanel_Paint(object sender, PaintEventArgs e)
        {
            int currentSlot = -1;

            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
            {
                e.Graphics.SmoothingMode = smoothingMode;
                e.Graphics.CompositingQuality = compositingQuality;
                e.Graphics.CompositingMode = compositingMode;
                e.Graphics.InterpolationMode = interpolationMode;
                e.Graphics.PixelOffsetMode = pixelOffsetMode;
                e.Graphics.TextRenderingHint = textRenderingHint;

                foreach (InventoryEntry inv in gameMemory.Player.Inventory)
                {
                    if ((inv.Slot == 0 || inv.Slot == 1) && inv.IsEmpty)
                        currentSlot++;

                    if (inv == default || inv.IsEmpty)
                        continue;

                    currentSlot++;

                    int slotColumn = currentSlot % 2;
                    int slotRow = currentSlot / 2;
                    int imageX = (slotColumn * Program.INV_SLOT_WIDTH) + (Program.INV_SLOT_WIDTH / 2);
                    int imageY = slotRow * Program.INV_SLOT_HEIGHT;
                    int textX = imageX + Program.INV_SLOT_WIDTH;
                    int textY = imageY + Program.INV_SLOT_HEIGHT;
                    bool evenSlotColumn = slotColumn % 2 == 0;
                    Brush textBrush = Brushes.White;

                    if (inv.Quantity == 0)
                        textBrush = Brushes.DarkRed;
                    else if (inv.IsAcid)
                        textBrush = Brushes.Yellow;
                    else if (inv.IsBOW)
                        textBrush = Brushes.Green;
                    else if (inv.IsFlame)
                        textBrush = Brushes.Red;

                    TextureBrush imageBrush;
                    if (Program.ItemToImageTranslation.ContainsKey(inv.Type))
                        imageBrush = new TextureBrush(inventoryImage, Program.ItemToImageTranslation[inv.Type]);
                    else
                        imageBrush = new TextureBrush(inventoryError, new Rectangle(0, 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT));

                    imageBrush.TranslateTransform(Program.INV_SLOT_WIDTH / 2, 0);

                    // Double-slot item.
                    if (imageBrush.Image.Width == Program.INV_SLOT_WIDTH * 2 && inv.Slot != 1)
                    {
                        if (inv.Slot == 0)
                        {
                            imageBrush.TranslateTransform(Program.INV_SLOT_WIDTH / 3 + Program.INV_SLOT_WIDTH, 0);
                            imageX -= Program.INV_SLOT_WIDTH / 2;
                        }
                        else
                        {
                            // Shift the quantity text over into the 2nd slot's area.
                            textX += Program.INV_SLOT_WIDTH;
                            currentSlot++;
                        }
                    }

                    e.Graphics.FillRectangle(imageBrush, imageX, imageY, imageBrush.Image.Width, imageBrush.Image.Height);
                    e.Graphics.DrawString(!inv.IsInfinite ? inv.Quantity.ToString() : "∞", new Font("Consolas", 14, FontStyle.Bold), textBrush, textX, textY, invStringFormat);
                }
            }
        }

        private void statisticsPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = smoothingMode;
            e.Graphics.CompositingQuality = compositingQuality;
            e.Graphics.CompositingMode = compositingMode;
            e.Graphics.InterpolationMode = interpolationMode;
            e.Graphics.PixelOffsetMode = pixelOffsetMode;
            e.Graphics.TextRenderingHint = textRenderingHint;

            // Additional information and stats.
            // Adjustments for displaying text properly.
            int heightGap = 15;
            int heightOffset = 0;
            int i = 1;

            // IGT Display.
            e.Graphics.DrawString(string.Format("{0}", gameMemory.IGT.FormattedString), new Font("Consolas", 16, FontStyle.Bold), Brushes.White, 0, 0, stdStringFormat);

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.Debug))
            {
                e.Graphics.DrawString("T:" + gameMemory.IGT.RunningTimer.ToString("0000000000"), new Font("Consolas", 9, FontStyle.Bold), Brushes.Gray, 0, 25, stdStringFormat);
                e.Graphics.DrawString("C:" + gameMemory.Version.Code, new Font("Consolas", 9, FontStyle.Bold), Brushes.Gray, 0, 38, stdStringFormat);
                e.Graphics.DrawString("P:" + gameMemory.Process.ProcessName, new Font("Consolas", 9, FontStyle.Bold), Brushes.Gray, 0, 51, stdStringFormat);
                e.Graphics.DrawString("I:" + gameMemory.Process.Id, new Font("Consolas", 9, FontStyle.Bold), Brushes.Gray, 0, 64, stdStringFormat);
                heightOffset = 56; // Adding an additional offset to accomdate Raw IGT.
            }

            e.Graphics.DrawString("Status: " + gameMemory.Player.StatusName, new Font("Consolas", 10, FontStyle.Bold), Brushes.White, 0, heightOffset + 25, stdStringFormat);
            e.Graphics.DrawString("Saves: " + gameMemory.Player.Saves, new Font("Consolas", 10, FontStyle.Bold), Brushes.White, 0, heightOffset + 38, stdStringFormat);
            e.Graphics.DrawString("Retry: " + gameMemory.Player.Retry, new Font("Consolas", 10, FontStyle.Bold), Brushes.White, 0, heightOffset + 51, stdStringFormat);
            heightOffset += 39;

            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoEnemyHealth))
            {
                e.Graphics.DrawString("Enemy HP", new Font("Consolas", 10, FontStyle.Bold), Brushes.Red, 0, heightOffset + (heightGap * ++i), stdStringFormat);

                List<EnemyEntry> enemyList = gameMemory.Enemy.ToList();

                if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.DebugEnemy))
                    enemyList = enemyList.Where(a => !a.IsEmpty).OrderBy(a => a.Slot).ToList();
                else
                    enemyList = enemyList.Where(a => a.IsAlive && !a.IsEmpty).OrderBy(a => a.Percentage).ThenByDescending(a => a.CurrentHP).ToList();

                foreach (EnemyEntry enemy in enemyList)
                {
                    int x = 1;
                    int y = heightOffset + (heightGap * ++i);

                    DrawProgressBarGDI(e, backBrushGDI, foreBrushGDI, x, y, 176, heightGap, enemy.Percentage * 100f, 100f);

                    if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.DebugEnemy))
                        e.Graphics.DrawString(enemy.DebugMessage, new Font("Consolas", 8, FontStyle.Regular), Brushes.Red, x + 2, y + 1, stdStringFormat);
                    else
                        e.Graphics.DrawString(enemy.HealthMessage, new Font("Consolas", 10, FontStyle.Bold), Brushes.Red, x, y, stdStringFormat);
                }
            }
        }

        private Brush backBrushGDI = new SolidBrush(Color.FromArgb(255, 60, 60, 60));
        private Brush foreBrushGDI = new SolidBrush(Color.FromArgb(255, 100, 0, 0));

        private void DrawProgressBarGDI(PaintEventArgs e, Brush bgBrush, Brush foreBrush, float x, float y, float width, float height, float value, float maximum = 100)
        {
            // Draw BG.
            e.Graphics.DrawRectangles(new Pen(bgBrush, 2f), new RectangleF[1] { new RectangleF(x, y, width, height) });

            // Draw FG.
            RectangleF foreRect = new RectangleF(
                x + 1f,
                y + 1f,
                (width * value / maximum) - 2f,
                height - 2f
                );
            e.Graphics.FillRectangle(foreBrush, foreRect);
        }

        private void inventoryPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
                if (e.Button == MouseButtons.Left)
                    PInvoke.DragControl(((DoubleBufferedPanel)sender).Parent.Handle);
        }

        private void statisticsPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PInvoke.DragControl(((DoubleBufferedPanel)sender).Parent.Handle);
        }

        private void playerHealthStatus_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PInvoke.DragControl(((PictureBox)sender).Parent.Handle);
        }

        private void MainUI_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PInvoke.DragControl(((Form)sender).Handle);
        }
    }
}