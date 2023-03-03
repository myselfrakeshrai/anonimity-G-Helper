using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using Starlight.AnimeMatrix;

public class HardwareMonitor
{

    public static float? cpuTemp = -1;
    public static float? batteryDischarge = -1;


    public static void ReadSensors()
    {
        cpuTemp = -1;
        batteryDischarge = -1;

        try
        {
            var ct = new PerformanceCounter("Thermal Zone Information", "Temperature", @"\_TZ.THRM", true);
            cpuTemp = ct.NextValue() - 273;
            ct.Dispose();

            var cb = new PerformanceCounter("Power Meter", "Power", "Power Meter (0)", true);
            batteryDischarge = cb.NextValue() / 1000;
            cb.Dispose();
        }
        catch
        {
            Debug.WriteLine("Failed reading sensors");
        }
    }

}

namespace GHelper
{
    static class Program
    {
        public static NotifyIcon trayIcon = new NotifyIcon
        {
            Text = "G-Helper",
            Icon = Properties.Resources.standard,
            Visible = true
        };

        public static ASUSWmi wmi = new ASUSWmi();
        public static AppConfig config = new AppConfig();

        public static SettingsForm settingsForm = new SettingsForm();
        public static ToastForm toast = new ToastForm();

        // The main entry point for the application
        public static void Main()
        {

            trayIcon.MouseClick += TrayIcon_MouseClick; ;

            wmi.SubscribeToEvents(WatcherEventArrived);

            settingsForm.InitGPUMode();
            settingsForm.InitBoost();
            settingsForm.InitAura();

            settingsForm.VisualiseGPUAuto(config.getConfig("gpu_auto"));
            settingsForm.VisualiseScreenAuto(config.getConfig("screen_auto"));
            settingsForm.SetStartupCheck(Startup.IsScheduled());

            SetAutoModes();

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            IntPtr ds = settingsForm.Handle;

            Application.Run();

        }

        private static void SetAutoModes()
        {
            PowerLineStatus isPlugged = SystemInformation.PowerStatus.PowerLineStatus;
            settingsForm.AutoGPUMode(isPlugged);
            settingsForm.AutoScreen(isPlugged);
            settingsForm.AutoPerformance(isPlugged);
            settingsForm.SetBatteryChargeLimit(config.getConfig("charge_limit"));
        }

        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            SetAutoModes();
        }


        static void LaunchProcess(string fileName = "")
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = fileName;
            start.WindowStyle = ProcessWindowStyle.Hidden;
            start.CreateNoWindow = true;
            try
            {
                Process proc = Process.Start(start);
            }
            catch
            {
                Debug.WriteLine("Failed to run " + fileName);
            }


        }

        static void WatcherEventArrived(object sender, EventArrivedEventArgs e)
        {
            var collection = (ManagementEventWatcher)sender;

            if (e.NewEvent is null) return;

            int EventID = int.Parse(e.NewEvent["EventID"].ToString());

            Debug.WriteLine(EventID);

            switch (EventID)
            {
                case 124:    // M3
                    switch (config.getConfig("m3"))
                    {
                        case 1:
                            NativeMethods.KeyPress(NativeMethods.VK_MEDIA_PLAY_PAUSE);
                            break;
                        case 2:
                            settingsForm.BeginInvoke(settingsForm.CycleAuraMode);
                            break;
                        case 3:
                            LaunchProcess(config.getConfigString("m3_custom"));
                            break;
                        default:
                            NativeMethods.KeyPress(NativeMethods.VK_VOLUME_MUTE);
                            break;
                    }
                    return;
                case 56:    // M4 / Rog button
                    switch (config.getConfig("m4"))
                    {
                        case 1:
                            settingsForm.BeginInvoke(SettingsToggle);
                            break;
                        case 2:
                            LaunchProcess(config.getConfigString("m4_custom"));
                            break;
                        default:
                            settingsForm.BeginInvoke(settingsForm.CyclePerformanceMode);
                            break;
                    }
                    return;
                case 174:   // FN+F5
                    settingsForm.BeginInvoke(settingsForm.CyclePerformanceMode);
                    return;
                case 179:   // FN+F4
                    settingsForm.BeginInvoke(delegate
                    {
                        settingsForm.CycleAuraMode();
                    });
                    return;
                case 87:  // Battery
                    /*
                    settingsForm.BeginInvoke(delegate
                    {
                        settingsForm.AutoGPUMode(0);
                        settingsForm.AutoScreen(0);
                    });
                    */
                    return;
                case 88:  // Plugged
                    /*
                    settingsForm.BeginInvoke(delegate
                    {
                        settingsForm.AutoScreen(1);
                        settingsForm.AutoGPUMode(1);
                    });
                    */
                    return;

            }


        }

        static void SettingsToggle()
        {
            if (settingsForm.Visible)
                settingsForm.Hide();
            else
            {
                settingsForm.Show();
                settingsForm.Activate();
            }

            settingsForm.VisualiseGPUMode();

        }

        static void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                SettingsToggle();
            }
        }



        static void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }

}