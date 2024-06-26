﻿using System;
using System.Collections.Generic;
using System.IO;
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
            public const int DM_PELSWIDTH = 0x80000;
            public const int DM_PELSHEIGHT = 0x100000;
            public const int DM_DISPLAYFREQUENCY = 0x400000;
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
        private readonly HashSet<string> uniqueResolutions = new HashSet<string>();
        private void LoadSavedResolutions()
        {
            string filePath = "savedResolutions.txt";
            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    savedResolutionsListBox.Items.Add(line);
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            LoadSavedResolutions();
            comboBox.SelectedIndex = 0;
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = screen.DeviceName;
                comboBox.Items.Add(item);
            }
            listBox.SelectionChanged += listBox_SelectionChanged;
        }

        private void addResolutionButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedResolution = listBox.SelectedItem?.ToString();   
            string selectedFrequency = frequencyComboBox.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(selectedResolution) && !string.IsNullOrEmpty(selectedFrequency))
            {
                string resolutionPreset = $"{selectedResolution}, {selectedFrequency}";

                // Проверка на существование такого же разрешения с частотой обновления
                if (savedResolutionsListBox.Items.Cast<string>().Any(item => item.Equals(resolutionPreset)))
                {
                    MessageBox.Show("Такое разрешение с частотой обновления уже существует.");
                }
                else
                {
                    savedResolutionsListBox.Items.Add(resolutionPreset);
                    SaveResolutionPresetToFile(resolutionPreset);
                }
            }
            else
            {
                MessageBox.Show("Выберите разрешение и частоту обновления.");
            }
        }
        private void deleteResolutionButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = savedResolutionsListBox.SelectedItem;

            if (selectedItem != null)
            {
                // Диалоговое окно с подтверждением удаления
                var result = MessageBox.Show($"Вы действительно хотите удалить - {selectedItem}?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                // Проверка выбора пользователя
                if (result == MessageBoxResult.Yes)
                {
                    // Удаление выбранного элемента из ListBox
                    savedResolutionsListBox.Items.Remove(selectedItem);

                    // Удаление строки из файла
                    DeleteResolutionPresetFromFile(selectedItem.ToString());
                }
            }
            else
            {
                MessageBox.Show("Выберите разрешение для удаления.");
            }
        }

        private void DeleteResolutionPresetFromFile(string resolutionPreset)
        {
            string filePath = "savedResolutions.txt";
            if (File.Exists(filePath))
            {
                // Чтение всех строк из файла
                var lines = File.ReadAllLines(filePath).ToList();

                // Удаление строки, соответствующей выбранному разрешению
                lines.Remove(resolutionPreset);

                // Перезапись файла без удалённой строки
                File.WriteAllLines(filePath, lines);
            }
        }
        private void SaveResolutionPresetToFile(string resolutionPreset)
        {
            string filePath = "savedResolutions.txt";
            try
            {
                File.AppendAllText(filePath, resolutionPreset + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}");
            }
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedScreen = ((ComboBoxItem)comboBox.SelectedItem)?.Content.ToString();

            if (selectedScreen != null)
            {
                listBox.Items.Clear();
                uniqueResolutions.Clear(); // Очищаем HashSet при каждом выборе нового дисплея

                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    if (screen.DeviceName == selectedScreen)
                    {
                        DisplaySettings.DEVMODE dm = new DisplaySettings.DEVMODE();
                        int modeNum = 0;
                        List<string> resolutions = new List<string>();

                        while (DisplaySettings.EnumDisplaySettings(screen.DeviceName, modeNum, ref dm))
                        {
                            string resolution = $"{dm.dmPelsWidth}x{dm.dmPelsHeight}";
                            if (uniqueResolutions.Add(resolution)) // Условие будет истинным для всех разрешений выбранного дисплея
                            {
                                if (dm.dmBitsPerPel == 32) // Проверяем, является ли разрешение системным
                                {
                                    resolutions.Add(resolution);
                                }
                            }
                            modeNum++;
                        }

                        // Сортировка списка разрешений от большего к меньшему
                        var sortedResolutions = resolutions
                            .Distinct() // Убедитесь, что добавляете только уникальные значения
                            .Select(r => new
                            {
                                Resolution = r,
                                Width = int.Parse(r.Split('x')[0]),
                                Height = int.Parse(r.Split('x')[1])
                            })
                            .OrderByDescending(r => r.Width)
                            .ThenByDescending(r => r.Height)
                            .Select(r => r.Resolution);

                        foreach (var res in sortedResolutions)
                        {
                            listBox.Items.Add(res);
                        }

                        break;
                    }
                }
            }
        }
        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedResolution = listBox.SelectedItem?.ToString();
            if (selectedResolution != null)
            {
                frequencyComboBox.Items.Clear();

                string selectedScreen = ((ComboBoxItem)comboBox.SelectedItem)?.Content.ToString();
                DisplaySettings.DEVMODE dm = new DisplaySettings.DEVMODE();
                int modeNum = 0;
                HashSet<int> uniqueFrequencies = new HashSet<int>();

                while (DisplaySettings.EnumDisplaySettings(selectedScreen, modeNum, ref dm))
                {
                    if ($"{dm.dmPelsWidth}x{dm.dmPelsHeight}" == selectedResolution && uniqueFrequencies.Add(dm.dmDisplayFrequency))
                    {
                        frequencyComboBox.Items.Add($"{dm.dmDisplayFrequency}Hz");
                    }
                    modeNum++;
                }

                if (frequencyComboBox.Items.Count > 0)
                {
                    frequencyComboBox.SelectedIndex = 0;
                }
            }
        }
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();

            listBox.Items.Clear();
            var filteredResolutions = uniqueResolutions
                .Where(resolution => resolution.ToLower().Contains(searchText))
                .Select(r => new
                {
                    Resolution = r,
                    Width = int.Parse(r.Split('x')[0]),
                    Height = int.Parse(r.Split('x')[1])
                })
                .OrderByDescending(r => r.Width)
                .ThenByDescending(r => r.Height)
                .Select(r => r.Resolution);

            foreach (var resolution in filteredResolutions)
            {
                listBox.Items.Add(resolution);
            }
        }
        private void applyResolutionButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = savedResolutionsListBox.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedItem))
            {
                // Разбор строки с разрешением и частотой обновления
                var parts = selectedItem.Split(new string[] { ", " }, StringSplitOptions.None);
                var resolution = parts[0].Split('x');
                var frequency = parts[1].Replace("Hz", "");

                int width = int.Parse(resolution[0]);
                int height = int.Parse(resolution[1]);
                int displayFrequency = int.Parse(frequency);

                DisplaySettings.DEVMODE dm = new DisplaySettings.DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DisplaySettings.DEVMODE));
                dm.dmPelsWidth = width;
                dm.dmPelsHeight = height;
                dm.dmDisplayFrequency = displayFrequency;
                dm.dmFields = DisplaySettings.DM_PELSWIDTH | DisplaySettings.DM_PELSHEIGHT | DisplaySettings.DM_DISPLAYFREQUENCY;

                string selectedScreen = System.Windows.Forms.Screen.PrimaryScreen.DeviceName; // Используйте PrimaryScreen для примера, замените на выбранный экран, если нужно

                int result = DisplaySettings.ChangeDisplaySettingsEx(selectedScreen, ref dm, IntPtr.Zero, DisplaySettings.CDS_UPDATEREGISTRY, IntPtr.Zero);
                if (result == DisplaySettings.DISP_CHANGE_SUCCESSFUL)
                {

                }
                else
                {
                    MessageBox.Show("Не удалось изменить разрешение экрана.");
                }
            }
            else
            {
                MessageBox.Show("Выберите разрешение для применения.");
            }
        }
        private void changeResolutionButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedResolution = listBox.SelectedItem?.ToString();

            if (selectedResolution != null)
            {
                var match = Regex.Match(selectedResolution, @"(\d+)x(\d+)");
                if (match.Success)
                {
                    int width = int.Parse(match.Groups[1].Value);
                    int height = int.Parse(match.Groups[2].Value);

                    string selectedFrequencyItem = frequencyComboBox.SelectedItem?.ToString();
                    var frequencyMatch = Regex.Match(selectedFrequencyItem, @"(\d+)Hz");
                    int frequency; // Объявляем переменную заранее
                    if (frequencyMatch.Success && int.TryParse(frequencyMatch.Groups[1].Value, out frequency)) // Используем out для присвоения значения
                    {
                        // Теперь у вас есть переменная frequency, содержащая выбранную частоту обновления
                        // Используйте эту частоту при поиске соответствующего режима в EnumDisplaySettings
                    }
                    else
                    {
                        // Обработка ситуации, когда частота не была успешно извлечена
                        MessageBox.Show("Не удалось определить частоту обновления.");
                        return; // Выходим из метода, если частота не определена
                    }

                    string selectedScreen = ((ComboBoxItem)comboBox.SelectedItem)?.Content.ToString();
                    var screen = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s => s.DeviceName == selectedScreen);
                    if (screen != null)
                    {
                        DisplaySettings.DEVMODE dm = new DisplaySettings.DEVMODE();
                        int modeNum = 0;

                        while (DisplaySettings.EnumDisplaySettings(screen.DeviceName, modeNum, ref dm))
                        {
                            if (dm.dmPelsWidth == width && dm.dmPelsHeight == height && dm.dmDisplayFrequency == frequency)
                            {
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
        }
    }
}
