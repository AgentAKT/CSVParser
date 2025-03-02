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
        private StringProcessing _stringProcessing;

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
            List<string> selectedFiles = ReceivePath(Path.GetDirectoryName(Path.GetFullPath(createdFilePath)));

            // Создание экземпляра StringProcessing (если он еще не создан)
            if (_stringProcessing == null)
            {
                _stringProcessing = new StringProcessing(processedFilesLabelValue, pathFilesList);
            }

            // Обработка файлов через StringProcessing
            _stringProcessing.ProcessFiles(selectedFiles);
        }


        public void CollectUIDs_Click(object sender, RoutedEventArgs e)
        {
            _cleaner.Clean(processedRowsLabelValue, writeToFileUIDsLabelValue);
            counterRows = 0;

            MakeAll();  //Выболнить действия по сбору UIDов            
            processedRowsLabelValue.Content = counterRows;
        }

        private void sortedUIDs_Click(object sender, RoutedEventArgs e)
        {
            string path = pathFileTextBox.Text;
            ViewAllRowsInFileToSorting();
            CheckBoxMeasurements.IsChecked = true;
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

        public void MakeAll()
        {

            if (CheckPath() | CheckMeasurements() | CheckTimes() | CheckPeriods())
            {
                ViewFilesInFolder();
            }
        }

        //  Открыть файлы в папке по очереди
        public void ViewFilesInFolder()
        {
            foreach (string filePath in pathFilesList)
            {
                ViewAllRowsInFile(filePath);
            }

            Dictionary<string, string> uids = new Dictionary<string, string>();

            for (int index = 0; index < uidsList.Count; index++)
            {
                ProcessUid(index, uids);
            }
            BuildFile(uids);
        }

        private void ProcessUid(int index, Dictionary<string, string> uids)
        {
            List<string> resultMeasurementList = new List<string>();
            bool isDuplicate = false;

            // Обрабатываем "поток" с одинаковыми UID
            while (index < uidsList.Count - 1 && uidsList[index] == uidsList[index + 1])
            {
                resultMeasurementList.Add(measurementsList[index]);
                index++;
                isDuplicate = true;
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
            for (int i = 0; i < measurements.Count; i++)
            {
                int timeRnd = RandomTime(rnd);
                if (CheckBoxSmartNTFile.IsChecked == true)
                {
                    measurements[i] = $"{measurements[i]}[Q:0x70000002]";
                }
                else
                {
                    measurements[i] = $"{measurements[i]}*{timeRnd}*70000002";
                }
            }

            // Добавление дополнительных замеров, если их недостаточно
            int requiredCount = Convert.ToInt32(numberOfValuesTextBox.Text);
            while (measurements.Count < requiredCount)
            {
                string valueVar = Convert.ToString(RandomValue(rnd));
                int timeRnd = RandomTime(rnd);
                if (CheckBoxSmartNTFile.IsChecked == true)
                {
                    measurements.Add($"{valueVar}");
                }
                else
                {
                    measurements.Add($"{valueVar}*{timeRnd}*70000002");
                }
            }

            return string.Join(";", measurements).Replace(" ", "");
        }


        private void BuildFile(Dictionary<string, string> uids)
        {
            int counterUIDs = 0;

            string filePath = "TI.dat"; // Путь к файлу

            using (var writer = new StreamWriter(filePath))
            {
                foreach (var kvp in uids)
                {
                    if (CheckBoxSmartNTFile.IsChecked == true)
                    {
                        int timeRnd = RandomTime(rnd);
                        writer.WriteLine($"UID:{kvp.Key}[F:600,T:{timeRnd}]={kvp.Value};");
                    }
                    else
                    {
                        writer.WriteLine($"{kvp.Key}={kvp.Value};!");
                    }
                    counterUIDs++;
                }
                writeToFileUIDsLabelValue.Content = counterUIDs;
            }
            MessageBox.Show("Файл TI.dat сохранен, получено " + counterUIDs + " UIDов");
            counterUIDs = 0;
            counterRows = 0;
            _cleaner.Clean(
                uidsList: uidsList,
                measurementsList: measurementsList,
                pathFilesList: pathFilesList,
                uids: uids
            );

            // Открыть папку с файлом
            OpenFolderWithFile(Path.GetDirectoryName(Path.GetFullPath(filePath)));
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
                    string uidVar = values[columnNum];
                    try
                    {
                        string valueVar;

                        if (CheckBoxMeasurements.IsChecked == true)
                        {
                            valueVar = values[columnNumMeasurements];
                        }
                        else
                        {
                            valueVar = Convert.ToString(RandomValue(rnd));
                        }

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
            string path = pathFileTextBox.Text; // Путь к файлу

            string dateTimeString = DateTime.Now.ToString("ddMMyyyy_HHmmss"); // Получаем текущую дату и время в формате "yyyyMMdd_HHmmss"
                                                                              // Имя папки
            string folderName = $"CSVParser_Results_{dateTimeString}";

            // Путь к папке (например, на рабочем столе)
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string folderPath = Path.Combine(desktopPath, folderName);

            // Создаем папку, если она не существует
            Directory.CreateDirectory(folderPath);

            // Имя файла
            string fileName = $"Result.csv"; // Файл для записи результатов
            string fullPath = Path.Combine(folderPath, fileName); // Полный путь к файлу

            int counterRows = 0; // Счетчик строк для отображения
            int columnWithUIDs = 1; // Столбец 2 (индекс 1) - UIDы
            int columnWithText = 2; // Столбец 3 (индекс 2) - текст
            var random = new Random();

            using (var writer = new StreamWriter(fullPath)) // Используем полный путь к файлу
            {
                using (var reader = new StreamReader(path))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine(); // Прочитать текущую линию
                        string[] values = line.Split(';'); // Разделить строку на массив значений

                        // Проверка на корректность количества столбцов
                        if (values.Length > columnWithText)
                        {
                            string uid = values[columnWithUIDs]; // Получаем UID
                            string text = values[columnWithText]; // Получаем текст

                            // Проверяем, что текст соответствует одному из требований и записываем 4 случайных значения
                            for (int i = 0; i < 4; i++)
                            {
                                double value = 0.0;

                                // Определяем, какое значение записывать на основе текста
                                // Напряжение
                                if (text.Contains("Частота"))
                                    value = random.NextDouble() * 0.01 + 49.9; // 6кВ +- 5%
                                else if (text.Contains(" 6кВ U"))
                                    value = random.NextDouble() * 0.1 + 5.95; // 6кВ +- 5%
                                else if (text.Contains(" 10кВ U"))
                                    value = random.NextDouble() * 0.1 + 9.95; // 10кВ +- 5%
                                else if (text.Contains(" 35кВ U"))
                                    value = random.NextDouble() * (36 - 34) + 34; // 35кВ +- 5%
                                else if (text.Contains(" 110кВ U"))
                                    value = random.NextDouble() * (111 - 108) + 108; // 110кВ +- 5%
                                else if (text.Contains(" 220кВ U"))
                                    value = random.NextDouble() * (218 - 222) + 108; // 110кВ +- 5%
                                else if (text.Contains(" 500кВ U"))
                                    value = random.NextDouble() * (502 - 498) + 498; // 500кВ +- 5%

                                // Ток
                                else if (text.Contains(" 6кВ I"))
                                    value = random.NextDouble() * (205 - 195) + 195; // 500кВ +- 5%
                                else if (text.Contains(" 10кВ I"))
                                    value = random.NextDouble() * (105 - 95) + 95; // 500кВ +- 5%
                                else if (text.Contains(" 35кВ I"))
                                    value = random.NextDouble() * (85 - 75) + 75; // 500кВ +- 5%
                                else if (text.Contains(" 110кВ I"))
                                    value = random.NextDouble() * (105 - 95) + 95; // 500кВ +- 5%
                                else if (text.Contains(" 500кВ I"))
                                    value = random.NextDouble() * (505 - 495) + 495; // 500кВ +- 5%
                                else
                                    continue; // Пропускаем строки, которые не соответствуют
                                              // Записываем UID и случайное значение
                                writer.WriteLine($"{uid};{Math.Round(value, 2)}");
                            }

                            counterRows++; // Увеличиваем счетчик строк
                        }
                    }
                }
            }

            // Сохраняем путь к созданному файлу в переменную
            createdFilePath = fullPath;
            //processedRowsLabelValue.Content = counterRows; // Отображаем количество обработанных строк
            MessageBox.Show($"Файл успешно создан: {fullPath}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}