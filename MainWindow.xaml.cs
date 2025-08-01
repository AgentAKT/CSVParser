using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Linq;
using MessageBox = System.Windows.MessageBox;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace CSVParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly ErrorHandler _errorHandler;
        private readonly Checker _checker;
        private Cleaner _cleaner;
        private WorkingWithFiles _workingWithFiles;

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //collectUIDs.IsEnabled = false;
            _errorHandler = new ErrorHandler(); // Инициализация ErrorHandler
            _cleaner = new Cleaner(this);

        }
        
        List<string> pathFilesList = new List<string>() { }; //Создать список путей к файлам
        List<string> uidsList = new List<string>() { }; //Создать список со значениями
        List<string> measurementsList = new List<string>() { }; //Создать список со значениями
        Dictionary<string, string> uids = new Dictionary<string, string>(); //Создать словарь с итоговыми значениями
        string createdFilePath;

        int counterFiles; //Найдено .csv файлов   //Удалено дубликатов    //Записано в файл
        int counterRows; //Обработано строк    

        Random rnd = new Random();

        [STAThread] // Требуется для использования OpenFileDialog

        public void PathToFileBtn_Click(object sender, RoutedEventArgs e)
        {
            // Создаем экземпляр OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv", // Фильтр для CSV-файлов
                Title = "Выберите файл" // Заголовок окна
            };

            // Отображаем диалоговое окно
            openFileDialog.ShowDialog();

            // Получаем путь к выбранному файлу и сохраняем его в переменной
            pathFileTextBox.Text = openFileDialog.FileName;
        }

        private void sortedUIDs_Click(object sender, RoutedEventArgs e)
        {
            string path = pathFileTextBox.Text;
            if (IsFileOpen(path))
            {
                // Уведомление пользователя
                MessageBox.Show("Файл уже открыт. Закройте файл перед выполнением операции.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Прерываем выполнение метода
            }

            ViewAllRowsInFileToSorting();
            CheckBoxMeasurements.IsChecked = true;
        }

        private bool IsFileOpen(string filePath)
        {
            try
            {
                // Пытаемся открыть файл для записи с исключительным доступом
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // Если файл не открыт, возвращаем false
                    return false;
                }
            }
            catch (IOException)
            {
                // Если файл открыт, будет выброшено исключение
                return true;
            }
        }

        public void PathBtn_Click(object sender, RoutedEventArgs e)
        {
            // Очистка полей и списков перед запуском
            _cleaner.Clean(
                processedFilesLabelValue,
                processedRowsLabelValue,
                writeToFileUIDsLabelValue,
                uidsList,
                measurementsList,
                pathFilesList,
                uids);

            // Сброс счетчиков
            counterFiles = 0;
            counterRows = 0;

            // Получение выбранных файлов

            List<string> selectedFiles;

            if (createdFilePath == null)
            {
                // Если createdFilePath равен null, открываем рабочий стол
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                selectedFiles = ReceivePath(desktopPath);
            }
            else
            {
                // Иначе используем путь из createdFilePath
                selectedFiles = ReceivePath(Path.GetDirectoryName(Path.GetFullPath(createdFilePath)));
            }

            // Создание экземпляра StringProcessing (если он еще не создан)
            if (_workingWithFiles == null)
            {
                _workingWithFiles = new WorkingWithFiles(processedFilesLabelValue, pathFilesList);
            }

            // Обработка файлов через StringProcessing
            _workingWithFiles.ProcessFiles(selectedFiles);
            selectedFiles = null;
        }


        public void CollectUIDs_Click(object sender, RoutedEventArgs e)
        {
            _cleaner.Clean(processedRowsLabelValue, writeToFileUIDsLabelValue);
            counterRows = 0;
            if (CheckPath() || CheckMeasurements() || CheckTimes() || CheckPeriods())
            {
                // Обработать файлы и получить словарь uids
                Dictionary<string, string> uids = ViewFilesInFolder();

                // Построить файл с использованием словаря uids
                string filePath = BuildFile(uids);

                // Открыть папку с файлом
                OpenFolderWithFile(Path.GetDirectoryName(Path.GetFullPath(filePath)));
                ResetApplicationState();
            }

            
            processedRowsLabelValue.Content = counterRows;
        }


        public static List<string> ReceivePath(string initialPath = null)
        {
            // Создаем экземпляр OpenFileDialog
            OpenFileDialog dialog = new OpenFileDialog();

            // Настройка диалога
            dialog.Multiselect = true; // Разрешаем выбор нескольких файлов
            dialog.Title = "Выберите файлы"; // Заголовок диалогового окна
            dialog.Filter = "CSV files (*.csv)|*.csv"; // Фильтр файлов 

            // Если начальный путь передан, устанавливаем его
            if (!string.IsNullOrEmpty(initialPath))
            {
                dialog.InitialDirectory = initialPath;
            }

            // Отображаем диалоговое окно
            DialogResult result = dialog.ShowDialog();

            // Если пользователь нажал "ОК", возвращаем выбранные файлы
                // Преобразуем массив FileNames в список и возвращаем
            return new List<string>(dialog.FileNames);

        }

        //    Получить список файлов
        public static string[] ReceiveListOfFiles(string x)
        {
            string[] files = Directory.GetFiles(x, "*.csv");
            return files;
        }

        //  Собрать список с файлами .csv
        public void WriteListOfFiles(string[] x)
        {
            foreach (string s in x)
            {
                pathFilesList.Add(s);
                counterFiles++;
                processedFilesLabelValue.Content = counterFiles;
            }
            if (counterFiles == 0)
            {
                string errorMessage = _errorHandler.GetErrorMessage(6); // "В выбранной папке .csv файлы не найдены"
                MessageBox.Show(errorMessage); // Показываем сообщение          
                return;
            }
        }

        //При нажатии на Enter ошибка
        private void PathTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PathBtn_Click(this, new RoutedEventArgs());
            }
        }

        //  Открыть файлы в папке по очереди
        public Dictionary<string, string> ViewFilesInFolder()
        {
            // Открыть файлы в папке по очереди
            foreach (string filePath in pathFilesList)
            {
                ViewAllRowsInFile(filePath);
            }

            // Создать и заполнить словарь uids
            //Dictionary<string, string> uids = new Dictionary<string, string>();
            for (int index = 0; index < uidsList.Count; index++)
            {
                ProcessUid(index, uids);
            }

            // Вернуть словарь uids
            return uids;
        }

        private void ProcessUid(int index, Dictionary<string, string> uids)
        {
            List<string> resultMeasurementList = new List<string>();
            //bool isDuplicate = false;
            int countUids = 0;
            double lastMeasurement = 0;

            // Обрабатываем "поток" с одинаковыми UID
            while (index < uidsList.Count - 1 && uidsList[index] == uidsList[index + 1])
            {
                resultMeasurementList.Add(measurementsList[index]);
                lastMeasurement = Convert.ToDouble(measurementsList[index + 1]);
                index++;
                countUids++;
                //isDuplicate = true;
            }

            if (countUids < Convert.ToInt16(numberOfValuesTextBox.Text))
            {
                int requiredCount = Convert.ToInt16(numberOfValuesTextBox.Text);
                double lastValue = Convert.ToDouble(measurementsList[index]);
                Random random = new Random();

                while (countUids < requiredCount - 1)
                {
                    double randomValue = lastValue * (1 + (random.NextDouble() * 0.04 - 0.02)); // ±5%
                    resultMeasurementList.Add(Math.Round(randomValue, 2).ToString());
                    countUids++;
                }
            }

            resultMeasurementList.Add(measurementsList[index]); // Добавляем последний UID

            string measurementString = GenerateMeasurementString(resultMeasurementList);

            if (!uids.ContainsKey(uidsList[index]))
            {
                uids.Add(uidsList[index], measurementString);
            }

           

        }

        private string GenerateMeasurementString(List<string> measurements)
        {
            int requiredCount = Convert.ToInt32(numberOfValuesTextBox.Text);
            bool isSmartNTFile = CheckBoxSmartNTFile.IsChecked == true;
            
            // Модификация существующих измерений
            for (int i = 0; i < measurements.Count; i++)
            {
                int timeRnd = RandomTime(rnd);
                if (isSmartNTFile && i == 0) // Добавляем суффикс только к первому элементу
                {
                    measurements[i] = $"{measurements[i]}[Q:0x70000002]";
                }
                else if (!isSmartNTFile) // Стандартный формат для всех элементов, если SmartNTFile не выбран
                {
                    measurements[i] = $"{measurements[i]}*{timeRnd}*70000002";
                }
            }

            // Добавление дополнительных измерений, если их недостаточно
            while (measurements.Count < requiredCount)
            {
                string valueVar = Convert.ToString(RandomValue(rnd));
                int timeRnd = RandomTime(rnd);
                if (isSmartNTFile)
                {
                    measurements.Add(valueVar); // Просто добавляем значение без суффикса
                }
                else
                {
                    measurements.Add($"{valueVar}*{timeRnd}*70000002");
                }
            }

            // Объединение всех значений через точку с запятой
            return string.Join(";", measurements).Replace(" ", "");
        }

        private string BuildFile(Dictionary<string, string> uids)
        {
            string filePath = "TI.dat";
            bool isSmartNTFile = CheckBoxSmartNTFile.IsChecked == true;

            using (var writer = new StreamWriter(filePath))
            {
                foreach (var kvp in uids)
                {
                    string line = isSmartNTFile
                        ? $"UID:{kvp.Key}[F:600,T:{RandomTime(rnd)}]={kvp.Value};"
                        : $"{kvp.Key}={kvp.Value};!";
                    writer.WriteLine(line);
                }
            }

            int counterUIDs = uids.Count;
            writeToFileUIDsLabelValue.Content = counterUIDs;
            MessageBox.Show($"Файл TI.dat сохранен, получено {counterUIDs} UIDов");

            // Очистка данных
            _cleaner.Clean(
                uidsList: uidsList,
                measurementsList: measurementsList,
                pathFilesList: pathFilesList,
                uids: uids
            );

            // Возвращаем путь к файлу
            return filePath;
        }

        // Метод для открытия папки
        private void OpenFolderWithFile(string folderPath)
        {
            // Проверяем, существует ли папка
            if (Directory.Exists(folderPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", folderPath);
            }
        }
       

        //  Посмотреть строчки в файле по очереди
        public void ViewAllRowsInFile(string fileName)
        {
            int columnNum = Convert.ToInt32(numberOfColumnTextBox.Text) - 1;  //  Номер колонки с UIDами переводим в индекс
            int columnNumMeasurements = Convert.ToInt32(numberOfColumnMeasurements.Text) - 1;  //  Номер колонки со значениями переводим в индекс
            using (var reader = new StreamReader(fileName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();  //   Прочитать текущую линию
                    string[] values = line.Split(';');  //   Разделить строку на массив значений                   
                    string uidVar = values[columnNum]; // Получить UID
                    // Если в файле есть измерения
                    bool isCheckBoxMesasurement = CheckBoxMeasurements.IsChecked == true;
                    try
                    {
                        string valueVar = isCheckBoxMesasurement 
                            ? values[columnNumMeasurements]
                            : Convert.ToString(RandomValue(rnd));

                        AddInUidAndMeasurementList(uidVar, valueVar);
                    }
                    catch
                    {
                        string errorMessage = _errorHandler.GetErrorMessage(5, fileName); // Получаем сообщение об ошибке
                        MessageBox.Show(errorMessage); // Показываем сообщение          
                        break;
                    }
                    finally
                    {
                        counterRows++;
                    }
                }
            }
            processedRowsLabelValue.Content = counterRows;
        }

        // если валидный UID, записать его и значение в словари
        public void AddInUidAndMeasurementList(string uidVar, string valueVar)
        {
            if (IsValidGuid(uidVar))
            {
                uidsList.Add(uidVar);
                measurementsList.Add(valueVar);
            }
        }

        //    Проверка UIDа
        internal bool IsValidGuid(string uid)
        {
            return Guid.TryParse(uid, out _);
        }

        // Общий метод для получения случайного целого числа в заданном диапазоне
        private int RandomInRange(Random rnd, int from, int to)
        {
            return rnd.Next(from, to);
        }

        // Рандомное время
        public int RandomTime(Random rnd)
        {
            int t1 = Convert.ToInt32(timeRandomFromTextBox.Text);
            int t2 = Convert.ToInt32(timeRandomToTextBox.Text);
            return RandomInRange(rnd, t1, t2);
        }

        // Рандомное значение
        public int RandomValue(Random rnd)
        {
            int v1 = Convert.ToInt32(valuesRandomFromTextBox.Text);
            int v2 = Convert.ToInt32(valuesRandomToTextBox.Text);
            return RandomInRange(rnd, v1, v2);
        }

        private bool CheckMeasurements()
        {
            var textBoxes = new[]
            {
            numberOfColumnTextBox.Text,
            numberOfValuesTextBox.Text,
            valuesRandomFromTextBox.Text,
            valuesRandomToTextBox.Text,
            timeRandomFromTextBox.Text,
            timeRandomToTextBox.Text
            };

            if (textBoxes.Any(string.IsNullOrEmpty))
            {
                string errorMessage = _errorHandler.GetErrorMessage(4); // Заполни пустые поля
                MessageBox.Show(errorMessage); // Показываем сообщение          
                return true;
            }
            return false;
        }

        private bool CheckPeriods()
        {
            if (Convert.ToInt32(valuesRandomFromTextBox.Text) >= Convert.ToInt32(valuesRandomToTextBox.Text) || Convert.ToInt32(timeRandomFromTextBox.Text) >= Convert.ToInt32(timeRandomToTextBox.Text))
            {
                string errorMessage = _errorHandler.GetErrorMessage(1); // Получаем сообщение об ошибке
                MessageBox.Show(errorMessage); // Показываем сообщение                
                return true;
                
            }
            return false;
        }
        // проверка что время "от" меньше чем время "до"
        private bool CheckTimes()
        {
            if (Convert.ToInt32(timeRandomFromTextBox.Text) <= 0 || Convert.ToInt32(timeRandomToTextBox.Text) <= 0)
            {
                string errorMessage = _errorHandler.GetErrorMessage(2);
                return true;
            }
            return false;
        }

        //  проверка что путь к файлам не пустой
        private bool CheckPath()
        {

            return true;
        }

        public void ViewAllRowsInFileToSorting()
        {
            string path = pathFileTextBox.Text;
            string dateTimeString = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string folderName = $"CSVParser_Results\\CSVParser_Results_{dateTimeString}";
            int numberOfValues = Convert.ToInt16(numberOfValuesTextBox.Text);
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string folderPath = Path.Combine(desktopPath, folderName);
            Directory.CreateDirectory(folderPath);
            string fileName = "Result.csv";
            string fullPath = Path.Combine(folderPath, fileName);
            bool isSOChecked = RadioButtonSO.IsChecked == true;
            bool isSKChecked = RadioButtonSK.IsChecked == true;
            int counterRows = 0;
            int columnWithUIDs = 1;
            int columnWithText = isSOChecked ? 2 : 3; // Выбор столбца в зависимости от чекбокса

            var random = new Random();

            using (var writer = new StreamWriter(fullPath, false, Encoding.UTF8))
            {
                using (var reader = new StreamReader(path))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(';');

                        if (values.Length > columnWithText)
                        {
                            string uid = values[columnWithUIDs];
                            string pathFromTable = values[columnWithText];
                            string text1 = values[columnWithText];

                            // Определение уровня напряжения и типа значения
                            string voltageLevel = GetVoltageLevel(pathFromTable);
                            string valueType = GetValueType(pathFromTable);

                            if (valueType == "Unknown" && voltageLevel != "Unknown")
                            {
                                valueType = "Voltage"; // Принудительно считаем напряжением
                            }

                            for (int i = 0; i < numberOfValues; i++)
                            {
                                double value = 0.0;

                                switch (valueType)
                                {
                                    case "Frequency":
                                        value = random.NextDouble() * 0.1 + 49.95;
                                        break;
                                    case "Temperature":
                                        value = random.NextDouble() * 10 + 20;
                                        break;
                                    case "Voltage":
                                        value = GetVoltageValue(voltageLevel, random);
                                        break;
                                    case "Current":
                                        value = GetCurrentValue(voltageLevel, random);
                                        break;
                                    case "ActivePower":
                                        value = GetPowerValue(voltageLevel, random, isActive: true);
                                        break;
                                    case "ReactivePower":
                                        value = GetPowerValue(voltageLevel, random, isActive: false);
                                        break;
                                    case "PowerFactor":
                                        value = random.NextDouble() * 0.1 + 0.95;
                                        break;
                                    case "Capacity":
                                        value = random.NextDouble() * 5 + 95;
                                        break;
                                    case "SignalLevel":
                                        value = random.NextDouble() * 10 - 85;
                                        break;
                                    default:
                                        continue;
                                }

                                if (value != 0.0)
                                {
                                    writer.WriteLine($"{uid};{Math.Round(value, 2)};{text1}");
                                    counterRows++;
                                }
                            }
                        }
                    }
                }
            }

            createdFilePath = fullPath;
            MessageBox.Show($"Файл успешно создан: {fullPath}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetVoltageLevel(string path)
        {
            string[] pathParts = path.Split('\\');

            for (int i = pathParts.Length - 1; i >= 0; i--)
            {
                string part = pathParts[i];

                // Проверяем явные указания напряжения (500, 330, 220, 110, 35, 10, 6, 0.4)
                if (part.Contains("500 кВ") || part.Contains("500kV") || part.Contains("500кВ") || part.Contains("Факт U 500"))
                    return "500";
                if (part.Contains("330 кВ") || part.Contains("330kV") || part.Contains("330кВ") || part.Contains("Факт U 330"))
                    return "330";
                if (part.Contains("220 кВ") || part.Contains("220kV") || part.Contains("220кВ") || part.Contains("Факт U 220"))
                    return "220";
                if (part.Contains("110 кВ") || part.Contains("110kV") || part.Contains("110кВ") || part.Contains("Факт U 110"))
                    return "110";
                if (part.Contains("35 кВ") || part.Contains("35kV") || part.Contains("35кВ") || part.Contains("Факт U 35"))
                    return "35";
                if (part.Contains("10 кВ") || part.Contains("10kV") || part.Contains("10кВ") || part.Contains("Факт U 10"))
                    return "10";
                if (part.Contains("6 кВ") || part.Contains("6kV") || part.Contains("6кВ") || part.Contains("Факт U 6"))
                    return "6";
                if (part.Contains("0.4 кВ") || part.Contains("400V") || part.Contains("0,4кВ") || part.Contains("Факт U 0,4"))
                    return "0.4";

                // Затем проверяем ТСНР, но только если нет явного указания напряжения
                if (part.Contains("ТСНР-110") || part.Contains("ТСН-110"))
                    return "110"; // ТСНР-110 → 110 кВ, а не 0.4 кВ!
                if (part.Contains("ТСНР-35") || part.Contains("ТСН-35"))
                    return "35";
                if (part.Contains("ТСНР-6") || part.Contains("ТСН-6"))
                    return "6";
                if (part.Contains("ТСН") && !part.Contains("110") && !part.Contains("35") && !part.Contains("6"))
                    return "0.4"; // Только если нет указания напряжения
            }

            // Дополнительные проверки, если явного указания нет
            if (path.Contains("ОРУ-110") || path.Contains("ЗРУ-110") || path.Contains("РУ-110"))
                return "110";
            if (path.Contains("ОРУ-35") || path.Contains("ЗРУ-35") || path.Contains("РУ-35"))
                return "35";
            if (path.Contains("ЗРУ-6") || path.Contains("КРУН-6"))
                return "6";

            return "Unknown";
        }

        private double GetVoltageValue(string voltageLevel, Random random)
        {
            switch (voltageLevel)
            {
                case "500":
                    return random.NextDouble() * 10 + 500; // 500 ±5 kV
                case "330":
                    return random.NextDouble() * 10 + 330; // 330 ±5 kV
                case "220":
                    return random.NextDouble() * 10 + 220; // 220 ±5 kV
                case "110":
                    return random.NextDouble() * 5 + 110;  // 110 ±2.5 kV
                case "35":
                    return random.NextDouble() * 2 + 35;   // 35 ±1 kV
                case "10":
                    return random.NextDouble() * 0.5 + 10; // 10 ±0.25 kV
                case "6":
                    return random.NextDouble() * 0.3 + 6;  // 6 ±0.15 kV
                case "0.4":
                    return random.NextDouble() * 40 + 380; // 400V ±5% (380-420V)
                default:
                    return random.NextDouble() * 100 + 200;
            }
        }

        private double GetCurrentValue(string voltageLevel, Random random)
        {
            double baseCurrent = random.NextDouble() * (60 - 50) + 50;
            switch (voltageLevel)
            {
                case "500":
                    return baseCurrent * 1.1;
                case "330":
                    return baseCurrent * 1;
                case "220":
                    return baseCurrent * 0.8;
                case "110":
                    return baseCurrent * 0.6;
                case "35":
                    return baseCurrent * 0.4;
                case "10":
                    return baseCurrent * 0.2;
                case "6":
                    return baseCurrent * 0.2;
                case "0.4":
                    return baseCurrent * 0.1; // Меньшие токи для низкого напряжения
                default:
                    return baseCurrent;
            }
        }

        private double GetPowerValue(string voltageLevel, Random random, bool isActive)
        {
            double baseValue = isActive ?
                random.NextDouble() * (100 - 50) + 50 :
                random.NextDouble() * (50 - 20) + 20;
            switch (voltageLevel)
            {
                case "500":
                    return baseValue * 10;
                case "330":
                    return baseValue * 5;
                case "220":
                    return baseValue * 2;
                case "110":
                    return baseValue * 1;
                case "35":
                    return baseValue * 0.3;
                case "10":
                    return baseValue * 0.1;
                case "6":
                    return baseValue * 0.05;
                case "0.4":
                    return baseValue * 0.01; // Меньшая мощность для 0.4 кВ
                default:
                    return baseValue;
            }
        }

        private string GetValueType(string path)
        {
            if (path.Contains("Частота") || path.Contains("частота") || path.Contains("Frequency") ||
                path.Contains("f") || path.Contains("F") || path.Contains("Гц") || path.Contains("Hz"))
                return "Frequency";

            if (path.Contains("Температура") || path.Contains("температура") || path.Contains("Temp") ||
                path.Contains("T°") || path.Contains("T ") || path.Contains("°C"))
                return "Temperature";

            if (path.Contains("Ua") || path.Contains("Ub") || path.Contains("Uc") ||
                path.Contains("Факт U") || path.Contains("Факт. U") ||
                path.Contains("Uab") || path.Contains("Ubc") || path.Contains("Uca") ||
                path.Contains("3U0") || path.Contains("Напряжение") || path.Contains("напряжение") ||
                path.Contains("Voltage") || path.Contains(" U ") || path.Contains("U_") ||
                path.EndsWith(" U") || path.EndsWith("U") || path.EndsWith("IСШ"))
                return "Voltage";

            if (path.Contains("Ia") || path.Contains("Ib") || path.Contains("Ic") ||
                path.Contains("3I0") || path.Contains("Ток") || path.Contains("ток") ||
                path.Contains("Current") || path.Contains("I ") || path.Contains("I_") ||
                path.Contains("Ток фаза"))
                return "Current";

            if (path.Contains("P") || path.Contains("Активная") || path.Contains("активная") ||
                path.Contains("Active") || path.Contains("Pсумм") || path.Contains("P3ф") ||
                path.Contains("Pa") || path.Contains("Pb") || path.Contains("Pc"))
                return "ActivePower";

            if (path.Contains("Q") || path.Contains("Реактивная") || path.Contains("реактивная") ||
                path.Contains("Reactive") || path.Contains("Qсумм") || path.Contains("Q3ф") ||
                path.Contains("Qa") || path.Contains("Qb") || path.Contains("Qc"))
                return "ReactivePower";

            if (path.Contains("cos") || path.Contains("Cos") || path.Contains("косинус") ||
                path.Contains("Коэффициент мощности") || path.Contains("PF") || path.Contains("power factor"))
                return "PowerFactor";

            if (path.Contains("Ёмкость") || path.Contains("Емкость") || path.Contains("Capacity") ||
                path.Contains("C ") || path.Contains("C_"))
                return "Capacity";

            if (path.Contains("Уровень GSM") || path.Contains("Уровень сигнала") || path.Contains("Signal level"))
                return "SignalLevel";

            return "Unknown";
        }


        public void ResetApplicationState()
        {
            // Очистка списков
            pathFilesList.Clear();
            uidsList.Clear();
            measurementsList.Clear();
            uids.Clear();

            // Сброс счетчиков
            counterFiles = 0;
            counterRows = 0;

            // Очистка текстовых полей
            pathFileTextBox.Text = string.Empty;
            
            // Очистка меток
            processedFilesLabelValue.Content = "0";
            processedRowsLabelValue.Content = "0";
            writeToFileUIDsLabelValue.Content = "0";

            // Сброс флажков
            CheckBoxMeasurements.IsChecked = false;
            CheckBoxSmartNTFile.IsChecked = false;

            // Очистка других элементов управления, если необходимо
            // Например, если у вас есть другие текстовые поля или метки, их тоже нужно очистить
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void numberOfColumnTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void pathFileTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void numberOfColumnTextBox_TextChanged_1(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void pathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void RadioButtonSO_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void RadioButtonSK_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}