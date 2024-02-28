using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectRES
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class DisplaySettings
        {
            [DllImport("user32.dll")]
            public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

            [DllImport("user32.dll")]
            public static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, int dwflags, IntPtr lParam);

            public const int CDS_UPDATEREGISTRY = 0x01;
            public const int CDS_TEST = 0x02;
            public const int DISP_CHANGE_SUCCESSFUL = 0;
            public const int DISP_CHANGE_RESTART = 1;
            public const int DISP_CHANGE_FAILED = -1;

            [StructLayout(LayoutKind.Sequential)]
            public struct DEVMODE
            {
                private const int CCHDEVICENAME = 0x20;
                private const int CCHFORMNAME = 0x20;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string dmDeviceName;
                public short dmSpecVersion;
                public short dmDriverVersion;
                public short dmSize;
                public short dmDriverExtra;
                public int dmFields;
                public int dmPositionX;
                public int dmPositionY;
                public ScreenOrientation dmDisplayOrientation;
                public int dmDisplayFixedOutput;
                public short dmColor;
                public short dmDuplex;
                public short dmYResolution;
                public short dmTTOption;
                public short dmCollate;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string dmFormName;
                public short dmLogPixels;
                public int dmBitsPerPel;
                public int dmPelsWidth;
                public int dmPelsHeight;
                public int dmDisplayFlags;
                public int dmDisplayFrequency;
                public int dmICMMethod;
                public int dmICMIntent;
                public int dmMediaType;
                public int dmDitherType;
                public int dmReserved1;
                public int dmReserved2;
                public int dmPanningWidth;
                public int dmPanningHeight;
            }

            public enum ScreenOrientation : uint
            {
                DMDO_DEFAULT = 0,
                DMDO_90 = 1,
                DMDO_180 = 2,
                DMDO_270 = 3
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = screen.DeviceName;
                comboBox.Items.Add(item);
            }
        }
        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedScreen = ((ComboBoxItem)comboBox.SelectedItem).Content.ToString();

            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (screen.DeviceName == selectedScreen)
                {
                    listBox.Items.Clear();

                    DisplaySettings.DEVMODE dm = new DisplaySettings.DEVMODE();
                    int modeNum = 0;
                    var displayModes = new List<DisplaySettings.DEVMODE>();

                    // Перечисляем все режимы дисплея для выбранного экрана
                    while (DisplaySettings.EnumDisplaySettings(screen.DeviceName, modeNum, ref dm))
                    {
                        displayModes.Add(dm);
                        modeNum++;
                    }

                    // Упорядочиваем режимы дисплея по разрешению
                    var orderedDisplayModes = displayModes
                        .OrderByDescending(d => d.dmPelsWidth * d.dmPelsHeight)
                        .ThenByDescending(d => d.dmDisplayFrequency);

                    foreach (var mode in orderedDisplayModes)
                    {
                        listBox.Items.Add($"{mode.dmPelsWidth}x{mode.dmPelsHeight}, {mode.dmDisplayFrequency}Hz");
                    }

                    break;
                }
            }
        }
       private void changeResolutionButton_Click(object sender, RoutedEventArgs e)
       {
            // Получаем выбранное разрешение и частоту обновления
            string selectedResolution = listBox.SelectedItem.ToString();
            var match = Regex.Match(selectedResolution, @"(\d+)x(\d+), (\d+)Hz");
            if (!match.Success) return;
            int width = int.Parse(match.Groups[1].Value);
            int height = int.Parse(match.Groups[2].Value);
            int frequency = int.Parse(match.Groups[3].Value);

            // Находим выбранный экран
            string selectedScreen = ((ComboBoxItem)comboBox.SelectedItem).Content.ToString();
            var screen = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s => s.DeviceName == selectedScreen);
            if (screen == null) return;

            // Находим режим дисплея с выбранным разрешением и частотой обновления
            DisplaySettings.DEVMODE dm = new DisplaySettings.DEVMODE();
            int modeNum = 0;
            while (DisplaySettings.EnumDisplaySettings(screen.DeviceName, modeNum, ref dm))
            {
                if (dm.dmPelsWidth == width && dm.dmPelsHeight == height && dm.dmDisplayFrequency == frequency)
                {
                    // Меняем разрешение экрана
                    int result = DisplaySettings.ChangeDisplaySettingsEx(screen.DeviceName, ref dm, IntPtr.Zero, DisplaySettings.CDS_UPDATEREGISTRY, IntPtr.Zero);
                    if (result != DisplaySettings.DISP_CHANGE_SUCCESSFUL)
                    {
                        MessageBox.Show("Не удалось изменить разрешение экрана.");
                    }
                    break;
                }
                modeNum++;
            }
       }
    }
}
