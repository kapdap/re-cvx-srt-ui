using SRTPluginBase;
using SRTPluginProviderRECVX.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SRTPluginUIRECVXWinForms
{
    public class PluginUI : IPluginUI
    {
        internal static PluginInfo _info = new PluginInfo();
        public IPluginInfo Info => _info;
        public string RequiredProvider => "SRTPluginProviderRECVX";

        private IPluginHostDelegates _hostDelegates;
        private ApplicationContext applicationContext;
        private Task applicationTask;
        public static ContextMenuStrip contextMenuStrip;

        private bool oneTimeInit = false;

        [STAThread]
        public int Startup(IPluginHostDelegates hostDelegates)
        {
            _hostDelegates = hostDelegates;

            if (!oneTimeInit)
            {
                // Must be before any rendering happens, including creation of ContextMenuStrip in Program class.
                Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                oneTimeInit = true;
            }

            // Context menu.
            contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add(new ToolStripMenuItem("Options", null, (object sender, EventArgs e) =>
            {
                using (OptionsUI optionsForm = new OptionsUI())
                    optionsForm.ShowDialog();
            }));
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, (object sender, EventArgs e) =>
            {
                // Call Application.Exit() within the context of the Form.
                applicationContext?.MainForm?.Invoke(new Action(() => Application.Exit()));
            }));

            // Call the legacy code initialization.
            Program.Main();

            // Create and start the form.
            applicationContext = new ApplicationContext(new MainUI());
            applicationTask = Task.Run(() =>
            {
                Application.Run(applicationContext);
            }).ContinueWith((Task t) =>
            {
                Shutdown();
            });

            return 0;
        }

        public int Shutdown()
        {
            // Clean up the context.
            if (applicationContext != null)
            {
                // Clean up the form.
                if (applicationContext.MainForm != null)
                {
                    applicationContext.MainForm.Invoke(new Action(() =>
                    {
                        applicationContext.MainForm.Close();
                        applicationContext.MainForm.Dispose();
                    }));
                }

                applicationContext.Dispose();
                applicationContext = null;
            }

            return 0;
        }

        public int ReceiveData(object gameMemory)
        {
            if (applicationContext?.MainForm != null)
                ((MainUI)applicationContext?.MainForm).ReceiveData(gameMemory);
            return 0;
        }
    }

    public static class Program
    {
        public static Options programSpecialOptions;

        public static readonly string srtVersion = string.Format("v{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
        public static readonly string srtTitle = string.Format("RE: CVX SRT - {0}", srtVersion);

        public static int INV_SLOT_WIDTH;
        public static int INV_SLOT_HEIGHT;

        public static IReadOnlyDictionary<ItemEnumeration, System.Drawing.Rectangle> ItemToImageTranslation;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            try
            {
                // Handle command-line parameters.
                programSpecialOptions = new Options();
                programSpecialOptions.GetOptions();

                // Set item slot sizes after scaling is determined.
                INV_SLOT_WIDTH = (int)Math.Round(112d * programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero); // Individual inventory slot width.
                INV_SLOT_HEIGHT = (int)Math.Round(112d * programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero); // Individual inventory slot height.

                GenerateClipping();
            }
            catch (Exception ex)
            {
                FailFast(string.Format("[{0}] An unhandled exception has occurred. Please see below for details.\r\n\r\n[{1}] {2}\r\n{3}.", srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace), ex);
            }
        }

        public static void FailFast(string message, Exception ex)
        {
            ShowError(message);
            Environment.FailFast(message, ex);
        }

        public static void ShowError(string message) =>
            MessageBox.Show(message, srtTitle, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);

        public static string GetExceptionMessage(Exception ex) =>
            String.Format("[{0}] An unhandled exception has occurred. Please see below for details.\r\n\r\n[{1}] {2}\r\n{3}.", srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace);

        public static void GenerateClipping()
        {
            int itemColumnInc = -1;
            int itemRowInc = -1;

            ItemToImageTranslation = new Dictionary<ItemEnumeration, System.Drawing.Rectangle>()
            {
                // Row 1
                { ItemEnumeration.None, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RocketLauncher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 1, Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AssaultRifle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 3, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SniperRifle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 5, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },

                // Row 2
                { ItemEnumeration.Shotgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 1), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HandgunGlock17, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GrenadeLauncher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CombatKnife, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Handgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 3
                { ItemEnumeration.CustomHandgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.LinearLauncher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HandgunBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH *  ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MagnumBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MagnumBulletsInsideCase, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ShotgunShells, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GrenadeRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AcidRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 4
                { ItemEnumeration.FlameRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGunArrows, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.M93RPart, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.FAidSpray, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GreenHerb, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RedHerb, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BlueHerb, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 5
                { ItemEnumeration.MixedHerb2Green, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerbRedGreen, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerbBlueGreen, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerb2GreenBlue, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerb3Green, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MixedHerbGreenBlueRed, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 6
                { ItemEnumeration.InkRibbon, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Magnum, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldLugers, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SubMachineGun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 4, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGunPowder, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 6, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 7
                { ItemEnumeration.GunPowderArrow, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BOWGasRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MGunBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GasMask, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RifleBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseUnused, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ARifleBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 8
                { ItemEnumeration.AlexandersPierce, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexandersJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlfredsRing, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlfredsJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.LugerReplica, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.FamilyPicture, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CalicoBullets, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 9
                { ItemEnumeration.Lockpick, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GlassEye, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PianoRoll, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SteeringWheel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CraneKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Lighter, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EaglePlate, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 10
                { ItemEnumeration.SidePack, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MapRoll, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HawkEmblem, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QueenAntObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.KingAntObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BiohazardCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseM93RParts, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseBowGunPowder, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DuraluminCaseMagnumRounds, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 11
                { ItemEnumeration.Detonator, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ControlLever, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SilverKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ArmyProof, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.NavyProof, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 12
                { ItemEnumeration.AirForceProof, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.KeyWithTag, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.IDCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Map, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AirportKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EmblemCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SkeletonPicture, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 13
                { ItemEnumeration.MusicBoxPlate, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonflyNoWings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Album, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Halberd, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Extinguisher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Briefcase, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PadlockKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 14
                { ItemEnumeration.TG01, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SpAlloyEmblem, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ValveHandle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.OctaValveHandle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MachineRoomKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.MiningRoomKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BarCodeSticker, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 15
                { ItemEnumeration.SterileRoomKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DoorKnob, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BatteryPack, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.HemostaticWire, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.TurnTableKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ChemStorageKey, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ClementAlpha, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 16
                { ItemEnumeration.ClementSigma, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.TankObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SpAlloyEmblemUnused, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.ClementMixture, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RustedSword, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Hemostatic, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SecurityCard, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 17
                { ItemEnumeration.SecurityFile, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexiasChoker, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlexiasJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QueenAntRelief, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.KingAntRelief, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RedJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BlueJewel, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 18
                { ItemEnumeration.Socket, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SqValveHandle, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Serum, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EarthenwareVase, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PaperWeight, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SilverDragonflyNoWings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SilverDragonfly, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 19
                { ItemEnumeration.WingObject, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Crystal, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly1Wing, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly2Wings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.GoldDragonfly3Wings, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.File, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PlantPot, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Row 20
                { ItemEnumeration.PictureB, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 0), Program.INV_SLOT_HEIGHT * ++itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.M1P, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH * 2, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BowGunPowderUnused, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * (itemColumnInc = 3), Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EnhancedHandgun, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * ++itemColumnInc, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.PlayingManual, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 6, Program.INV_SLOT_HEIGHT * itemRowInc, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // Shares Icon (Unused Content)
                { ItemEnumeration.PrisonersDiary, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 4, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.DirectorsMemo, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 5, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Instructions, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 6, Program.INV_SLOT_HEIGHT * 7, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.AlfredsMemo, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 3, Program.INV_SLOT_HEIGHT * 15, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.BoardClip, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 6, Program.INV_SLOT_HEIGHT * 19, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },

                // No Icon (Unused Content)
                { ItemEnumeration.Card, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CrestKeyS, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.CrestKeyG, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.EmptyExtinguisher, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.FileFolders, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.Memo, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.NewspaperClip, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.SquareSocket, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.RemoteController, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QueenAntReliefComplete, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QuestionA, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QuestionB, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QuestionC, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QuestionD, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
                { ItemEnumeration.QuestionE, new System.Drawing.Rectangle(Program.INV_SLOT_WIDTH * 0, Program.INV_SLOT_HEIGHT * 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT) },
            };
        }
    }
}