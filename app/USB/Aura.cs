﻿using GHelper.Gpu;
using GHelper.Helpers;
using System.Text;

namespace GHelper.USB
{
    public class AuraPower
    {
        public bool BootLogo;
        public bool BootKeyb;
        public bool AwakeLogo;
        public bool AwakeKeyb;
        public bool SleepLogo;
        public bool SleepKeyb;
        public bool ShutdownLogo;
        public bool ShutdownKeyb;

        public bool BootBar;
        public bool AwakeBar;
        public bool SleepBar;
        public bool ShutdownBar;

        public bool BootLid;
        public bool AwakeLid;
        public bool SleepLid;
        public bool ShutdownLid;

        public bool BootRear;
        public bool AwakeRear;
        public bool SleepRear;
        public bool ShutdownRear;
    }

    public enum AuraMode : int
    {
        AuraStatic = 0,
        AuraBreathe = 1,
        AuraColorCycle = 2,
        AuraRainbow = 3,
        Star = 4,
        Rain = 5,
        Highlight = 6,
        Laser = 7,
        Ripple = 8,
        AuraStrobe = 10,
        Comet = 11,
        Flash = 12,
        HEATMAP = 20,
        GPUMODE = 21,
    }

    public enum AuraSpeed : int
    {
        Slow = 0,
        Normal = 1,
        Fast = 2,
    }


    public static class Aura
    {

        static byte[] MESSAGE_APPLY = { AsusHid.AURA_ID, 0xb4 };
        static byte[] MESSAGE_SET = { AsusHid.AURA_ID, 0xb5, 0, 0, 0 };

        private static AuraMode mode = AuraMode.AuraStatic;
        private static AuraSpeed speed = AuraSpeed.Normal;

        public static Color Color1 = Color.White;
        public static Color Color2 = Color.Black;

        static bool isACPI = AppConfig.IsTUF() || AppConfig.IsVivobook();
        static bool isStrix = AppConfig.IsStrix();

        static public bool isSingleColor = false;

        static bool isOldHeatmap = AppConfig.Is("old_heatmap");

        static System.Timers.Timer timer = new System.Timers.Timer(1000);

        private static Dictionary<AuraMode, string> _modesSingleColor = new Dictionary<AuraMode, string>
        {
            { AuraMode.AuraStatic, Properties.Strings.AuraStatic },
            { AuraMode.AuraBreathe, Properties.Strings.AuraBreathe },
            { AuraMode.AuraStrobe, Properties.Strings.AuraStrobe },
        };

        private static Dictionary<AuraMode, string> _modes = new Dictionary<AuraMode, string>
        {
            { AuraMode.AuraStatic, Properties.Strings.AuraStatic },
            { AuraMode.AuraBreathe, Properties.Strings.AuraBreathe },
            { AuraMode.AuraColorCycle, Properties.Strings.AuraColorCycle },
            { AuraMode.AuraRainbow, Properties.Strings.AuraRainbow },
            { AuraMode.AuraStrobe, Properties.Strings.AuraStrobe },
            { AuraMode.HEATMAP, "Heatmap"},
            { AuraMode.GPUMODE, "GPU Mode" }
        };

        private static Dictionary<AuraMode, string> _modesStrix = new Dictionary<AuraMode, string>
        {
            { AuraMode.AuraStatic, Properties.Strings.AuraStatic },
            { AuraMode.AuraBreathe, Properties.Strings.AuraBreathe },
            { AuraMode.AuraColorCycle, Properties.Strings.AuraColorCycle },
            { AuraMode.AuraRainbow, Properties.Strings.AuraRainbow },
            { AuraMode.Star, "Star" },
            { AuraMode.Rain, "Rain" },
            { AuraMode.Highlight, "Highlight" },
            { AuraMode.Laser, "Laser" },
            { AuraMode.Ripple, "Ripple" },
            { AuraMode.AuraStrobe, Properties.Strings.AuraStrobe},
            { AuraMode.Comet, "Comet" },
            { AuraMode.Flash, "Flash" },
            { AuraMode.HEATMAP, "Heatmap"},
        };

        static Aura()
        {
            timer.Elapsed += Timer_Elapsed;
            isSingleColor = AppConfig.IsSingleColor(); // Mono Color

            if (AppConfig.ContainsModel("GA402X") || AppConfig.ContainsModel("GA402N"))
                using (var stream = AsusHid.FindHidStream(AsusHid.AURA_ID))
                {
                    if (stream is null) return;
                    if (stream.Device.ReleaseNumberBcd == 22 || stream.Device.ReleaseNumberBcd == 23) isSingleColor = true;
                    stream.Close();
                }
        }

        public static Dictionary<AuraSpeed, string> GetSpeeds()
        {
            return new Dictionary<AuraSpeed, string>
            {
                { AuraSpeed.Slow, Properties.Strings.AuraSlow },
                { AuraSpeed.Normal, Properties.Strings.AuraNormal },
                { AuraSpeed.Fast, Properties.Strings.AuraFast }
            };
        }


        public static Dictionary<AuraMode, string> GetModes()
        {
            if (isACPI)
            {
                _modes.Remove(AuraMode.AuraRainbow);
            }

            if (isSingleColor)
            {
                return _modesSingleColor;
            }

            if (AppConfig.IsAdvantageEdition())
            {
                return _modes;
            }

            if (AppConfig.IsStrix() && !AppConfig.IsStrixLimitedRGB())
            {
                return _modesStrix;
            }

            return _modes;
        }

        public static AuraMode Mode
        {
            get { return mode; }
            set
            {
                mode = GetModes().ContainsKey(value) ? value : AuraMode.AuraStatic;
            }
        }

        public static AuraSpeed Speed
        {
            get { return speed; }
            set
            {
                speed = GetSpeeds().ContainsKey(value) ? value : AuraSpeed.Normal;
            }

        }

        public static void SetColor(int colorCode)
        {
            Color1 = Color.FromArgb(colorCode);
        }

        public static void SetColor2(int colorCode)
        {
            Color2 = Color.FromArgb(colorCode);
        }

        public static bool HasSecondColor()
        {
            return mode == AuraMode.AuraBreathe && !isACPI;
        }

        private static void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            SetHeatmap();
        }

        static void SetHeatmap(bool init = false)
        {
            float cpuTemp = (float)HardwareControl.GetCPUTemp();
            int freeze = 20, cold = 40, warm = 65, hot = 90;
            Color color;

            //Debug.WriteLine(cpuTemp);

            if (cpuTemp < cold) color = ColorUtilities.GetWeightedAverage(Color.Blue, Color.Green, ((float)cpuTemp - freeze) / (cold - freeze));
            else if (cpuTemp < warm) color = ColorUtilities.GetWeightedAverage(Color.Green, Color.Yellow, ((float)cpuTemp - cold) / (warm - cold));
            else if (cpuTemp < hot) color = ColorUtilities.GetWeightedAverage(Color.Yellow, Color.Red, ((float)cpuTemp - warm) / (hot - warm));
            else color = Color.Red;

            ApplyColor(color, init);
        }


        public static byte[] AuraMessage(AuraMode mode, Color color, Color color2, int speed, bool mono = false)
        {

            byte[] msg = new byte[17];
            msg[0] = AsusHid.AURA_ID;
            msg[1] = 0xb3;
            msg[2] = 0x00; // Zone 
            msg[3] = (byte)mode; // Aura Mode
            msg[4] = color.R; // R
            msg[5] = mono ? (byte)0 : color.G; // G
            msg[6] = mono ? (byte)0 : color.B; // B
            msg[7] = (byte)speed; // aura.speed as u8;
            msg[8] = 0; // aura.direction as u8;
            msg[9] = mode == AuraMode.AuraBreathe ? (byte)1 : (byte)0;
            msg[10] = color2.R; // R
            msg[11] = mono ? (byte)0 : color2.G; // G
            msg[12] = mono ? (byte)0 : color2.B; // B
            return msg;
        }

        public static void Init()
        {
            Task.Run(async () =>
            {
                AsusHid.Write(new List<byte[]> {
                    new byte[] { AsusHid.AURA_ID, 0xb9 },
                    Encoding.ASCII.GetBytes("]ASUS Tech.Inc."),
                    new byte[] { AsusHid.AURA_ID, 0x05, 0x20, 0x31, 0, 0x1a },
                    Encoding.ASCII.GetBytes("^ASUS Tech.Inc."),
                    new byte[] { 0x5e, 0x05, 0x20, 0x31, 0, 0x1a }
                });
            });
        }


        public static void ApplyBrightness(int brightness, string log = "Backlight", bool delay = false)
        {
            Task.Run(async () =>
            {
                if (delay) await Task.Delay(TimeSpan.FromSeconds(1));
                if (isACPI) Program.acpi.TUFKeyboardBrightness(brightness);

                AsusHid.Write(new byte[] { AsusHid.AURA_ID, 0xba, 0xc5, 0xc4, (byte)brightness }, AsusHid.AURA_ID, log);
                if (AppConfig.ContainsModel("GA503")) 
                    AsusHid.Write(new byte[] { AsusHid.INPUT_ID, 0xba, 0xc5, 0xc4, (byte)brightness }, AsusHid.INPUT_ID, log);
            });


        }

        static byte[] AuraPowerMessage(AuraPower flags)
        {
            byte keyb = 0, bar = 0, lid = 0, rear = 0;

            if (flags.BootLogo) keyb |= 1 << 0;
            if (flags.BootKeyb) keyb |= 1 << 1;
            if (flags.AwakeLogo) keyb |= 1 << 2;
            if (flags.AwakeKeyb) keyb |= 1 << 3;
            if (flags.SleepLogo) keyb |= 1 << 4;
            if (flags.SleepKeyb) keyb |= 1 << 5;
            if (flags.ShutdownLogo) keyb |= 1 << 6;
            if (flags.ShutdownKeyb) keyb |= 1 << 7;

            if (flags.BootBar) bar |= 1 << 1;
            if (flags.AwakeBar) bar |= 1 << 2;
            if (flags.SleepBar) bar |= 1 << 3;
            if (flags.ShutdownBar) bar |= 1 << 4;

            if (flags.BootLid) lid |= 1 << 0;
            if (flags.AwakeLid) lid |= 1 << 1;
            if (flags.SleepLid) lid |= 1 << 2;
            if (flags.ShutdownLid) lid |= 1 << 3;

            if (flags.BootLid) lid |= 1 << 4;
            if (flags.AwakeLid) lid |= 1 << 5;
            if (flags.SleepLid) lid |= 1 << 6;
            if (flags.ShutdownLid) lid |= 1 << 7;

            if (flags.BootRear) rear |= 1 << 0;
            if (flags.AwakeRear) rear |= 1 << 1;
            if (flags.SleepRear) rear |= 1 << 2;
            if (flags.ShutdownRear) rear |= 1 << 3;

            if (flags.BootRear) rear |= 1 << 4;
            if (flags.AwakeRear) rear |= 1 << 5;
            if (flags.SleepRear) rear |= 1 << 6;
            if (flags.ShutdownRear) rear |= 1 << 7;

            return new byte[] { 0x5d, 0xbd, 0x01, keyb, bar, lid, rear, 0xFF };
        }

        public static void ApplyPower()
        {

            AuraPower flags = new();

            // Keyboard
            flags.AwakeKeyb = AppConfig.IsNotFalse("keyboard_awake");
            flags.BootKeyb = AppConfig.IsNotFalse("keyboard_boot");
            flags.SleepKeyb = AppConfig.IsNotFalse("keyboard_sleep");
            flags.ShutdownKeyb = AppConfig.IsNotFalse("keyboard_shutdown");

            // Logo
            flags.AwakeLogo = AppConfig.IsNotFalse("keyboard_awake_logo");
            flags.BootLogo = AppConfig.IsNotFalse("keyboard_boot_logo");
            flags.SleepLogo = AppConfig.IsNotFalse("keyboard_sleep_logo");
            flags.ShutdownLogo = AppConfig.IsNotFalse("keyboard_shutdown_logo");

            // Lightbar
            flags.AwakeBar = AppConfig.IsNotFalse("keyboard_awake_bar");
            flags.BootBar = AppConfig.IsNotFalse("keyboard_boot_bar");
            flags.SleepBar = AppConfig.IsNotFalse("keyboard_sleep_bar");
            flags.ShutdownBar = AppConfig.IsNotFalse("keyboard_shutdown_bar");

            // Lid
            flags.AwakeLid = AppConfig.IsNotFalse("keyboard_awake_lid");
            flags.BootLid = AppConfig.IsNotFalse("keyboard_boot_lid");
            flags.SleepLid = AppConfig.IsNotFalse("keyboard_sleep_lid");
            flags.ShutdownLid = AppConfig.IsNotFalse("keyboard_shutdown_lid");

            // Rear Bar
            flags.AwakeRear = AppConfig.IsNotFalse("keyboard_awake_lid");
            flags.BootRear = AppConfig.IsNotFalse("keyboard_boot_lid");
            flags.SleepRear = AppConfig.IsNotFalse("keyboard_sleep_lid");
            flags.ShutdownRear = AppConfig.IsNotFalse("keyboard_shutdown_lid");

            AsusHid.Write(AuraPowerMessage(flags));

            if (isACPI)
                Program.acpi.TUFKeyboardPower(
                    flags.AwakeKeyb,
                    flags.BootKeyb,
                    flags.SleepKeyb,
                    flags.ShutdownKeyb);

        }

        public static void ApplyColor(Color color, bool init = false)
        {

            if (isACPI)
            {
                Program.acpi.TUFKeyboardRGB(0, color, 0, null);
                return;
            }

            if (isStrix && !isOldHeatmap)
            {
                byte[] msg = new byte[0x40];

                byte start = 9;
                byte maxLeds = 0x93;

                msg[0] = AsusHid.AURA_ID;
                msg[1] = 0xbc;
                msg[2] = 0;
                msg[3] = 1;
                msg[4] = 1;
                msg[5] = 1;
                msg[6] = 0;
                msg[7] = 0x10;

                for (byte i = 0; i < 0x12; i++)
                {
                    msg[start + i * 3] = color.R; // R
                    msg[start + 1 + i * 3] = color.G; // G
                    msg[start + 2 + i * 3] = color.B; // B
                }

                if (init)
                {
                    Init();
                    AsusHid.WriteAura(new byte[] { AsusHid.AURA_ID, 0xbc });
                }

                for (byte b = 0; b < maxLeds; b += 0x10)
                {
                    msg[6] = b;
                    AsusHid.WriteAura(msg);
                }

                msg[6] = maxLeds;
                AsusHid.WriteAura(msg);

                msg[4] = 4;
                msg[5] = 0;
                msg[6] = 0;
                msg[7] = 0;
                AsusHid.WriteAura(msg);
            }

            else
            {
                AsusHid.WriteAura(AuraMessage(0, color, color, 0));
                AsusHid.WriteAura(MESSAGE_SET);
            }

        }


        public static void ApplyGPUColor()
        {
            if ((AuraMode)AppConfig.Get("aura_mode") != AuraMode.GPUMODE) return;

            switch (GPUModeControl.gpuMode)
            {
                case AsusACPI.GPUModeUltimate:
                    ApplyColor(Color.Red, true);
                    break;
                case AsusACPI.GPUModeEco:
                    ApplyColor(Color.Green, true);
                    break;
                default:
                    ApplyColor(Color.Yellow, true);
                    break;
            }
        }


        public static void ApplyAura()
        {

            Mode = (AuraMode)AppConfig.Get("aura_mode");
            Speed = (AuraSpeed)AppConfig.Get("aura_speed");
            SetColor(AppConfig.Get("aura_color"));
            SetColor2(AppConfig.Get("aura_color2"));

            timer.Enabled = false;

            if (Mode == AuraMode.HEATMAP)
            {
                SetHeatmap(true);
                timer.Enabled = true;
                return;
            }

            if (Mode == AuraMode.GPUMODE)
            {
                ApplyGPUColor();
                return;
            }

            int _speed = (Speed == AuraSpeed.Normal) ? 0xeb : (Speed == AuraSpeed.Fast) ? 0xf5 : 0xe1;

            AsusHid.Write(new List<byte[]> { AuraMessage(Mode, Color1, Color2, _speed, isSingleColor), MESSAGE_APPLY, MESSAGE_SET });

            if (isACPI)
                Program.acpi.TUFKeyboardRGB(Mode, Color1, _speed);

        }


    }

}