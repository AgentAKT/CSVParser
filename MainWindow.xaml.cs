using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Linq;

namespace CSVParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            collectUIDs.IsEnabled = false;
        }
        //private string selectedFilePath; // Переменная для хранения пути к выбранному файлу
        List<string> pathFilesList = new List<string>() { }; //Создать список путей к файлам
        List<string> uidsList = new List<string>() { }; //Создать список со значениями
        List<string> measurementsList = new List<string>() { }; //Создать список со значениями
        Dictionary<string, string> uids = new Dictionary<string, string>(); //Создать словарь с итоговыми значениями
        string createdFilePath = "";
        int counterFiles = 0; //Найдено .csv файлов   //Удалено дубликатов    //Записано в файл
        int counterRows = 0; //Обработано строк    

        Random rnd = new Random();

        public void PathBtn_Click(object sender, RoutedEventArgs e)
        {
            CleanDictAndList();
            pathFilesList.Clear();
            processedFilesLabelValue.Content = 0;
            processedRowsLabelValue.Content = 0;
            writeToFileUIDsLabelValue.Content = 0;

            counterFiles = 0;
            counterRows = 0;
            pathTextBox.Text = "";
            pathTextBox.Text = ReceivePath(createdFilePath);  //  Получить путь к файлам и записать в текстбокс Путь
            try
            {
                var ListOfFiles = ReceiveListOfFiles(pathTextBox.Text);  //  Получить список файлов и записать список файлов в переменную
                WriteListOfFiles(ListOfFiles); //  Заполнить список файлов
                collectUIDs.IsEnabled = true;
            }
            catch { }
        }

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

            // Проверяем, что пользователь выбрал файл

            // Получаем путь к выбранному файлу и сохраняем его в переменной
            pathFileTextBox.Text = openFileDialog.FileName;

            // Выводим путь к файлу в текстовое поле (если нужно)
            pathFileTextBox.Text = pathFileTextBox.Text;

            
        }

        public void CollectUIDs_Click(object sender, RoutedEventArgs e)
        {

            processedRowsLabelValue.Content = 0;
            writeToFileUIDsLabelValue.Content = 0;
            counterRows = 0;

            MakeAll();  //Выболнить действия по сбору UIDов            
            processedRowsLabelValue.Content = counterRows;
        }

        //    Получить путь к папке 
        //public static string ReceivePath()
        //{
        //    FolderBrowserDialog dialog = new FolderBrowserDialog();
        //    dialog.ShowDialog();
        //    string folderCSV = dialog.SelectedPath; //выбранный путь в переменную
        //    return folderCSV;
        //}

        public static string ReceivePath(string initialPath = null)
        {
            // Создаем экземпляр FolderBrowserDialog
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            // Если начальный путь передан, устанавливаем его
            if (!string.IsNullOrEmpty(initialPath))
            {
                dialog.SelectedPath = initialPath;
            }

            // Отображаем диалоговое окно
            DialogResult result = dialog.ShowDialog();

            // Проверяем, что пользователь выбрал папку

                // Возвращаем выбранный путь
                return dialog.SelectedPath;

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
                System.Windows.MessageBox.Show("В выбранной папке .csv файлы не найдены");
                pathTextBox.Text = "";
            }
        }

        //При нажатии на Enter ошибка
        private void PathTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                System.Windows.MessageBox.Show("Не балуйся, я так пока не умею :)");
                pathTextBox.Text = "";
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

            if (uids.ContainsKey(uidsList[index]))
            {
                Console.WriteLine("Такой ключ уже есть. Запись отменена");
            }
            else
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
            System.Windows.MessageBox.Show("Файл TI.dat сохранен, получено " + counterUIDs + " UIDов");
            counterUIDs = 0;
            counterRows = 0;
            CleanDictAndList();

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

        public void CleanAll()
        {
            //  Обнулить счетчик списка файлов
            counterFiles = 0;
            counterRows = 0;
            processedFilesLabelValue.Content = 0;
            processedRowsLabelValue.Content = 0;
            writeToFileUIDsLabelValue.Content = 0;
            uidsList.Clear(); measurementsList.Clear();
            uids.Clear();
        }

        public void CleanDictAndList()
        {
            uidsList.Clear(); measurementsList.Clear();
            uids.Clear();
        }

        //  Посмотреть строчки в файле по очереди
        public void ViewAllRowsInFile(string s)
        {
            int columnNum = Convert.ToInt32(numberOfColumnTextBox.Text) - 1;  //  Номер колонки с UIDами переводим в индекс
            int columnNumMeasurements = Convert.ToInt32(numberOfColumnMeasurements.Text) - 1;  //  Номер колонки со значениями переводим в индекс
            using (var reader = new StreamReader(s))
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
                        System.Windows.MessageBox.Show($"В файле {s} выбранном столбце нет данных, либо файл пустой");
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


        public void ClearAllCounters()
        {
            counterRows = 0;
            processedRowsLabelValue.Content = 0;
            writeToFileUIDsLabelValue.Content = 0;

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
                System.Windows.MessageBox.Show("Заполни пустые поля");
                return true;
            }
            return false;
        }

        private bool CheckPeriods()
        {
            if (Convert.ToInt32(valuesRandomFromTextBox.Text) >= Convert.ToInt32(valuesRandomToTextBox.Text) || Convert.ToInt32(timeRandomFromTextBox.Text) >= Convert.ToInt32(timeRandomToTextBox.Text))
            {
                System.Windows.MessageBox.Show("Проверь начало и конец диапазонов");
                return true;
            }
            return false;
        }
        // проверка что время "от" меньше чем время "до"
        private bool CheckTimes()
        {
            if (Convert.ToInt32(timeRandomFromTextBox.Text) <= 0 || Convert.ToInt32(timeRandomToTextBox.Text) <= 0)
            {
                System.Windows.MessageBox.Show("Время не может быть меньше 1");
                return true;
            }
            return false;
        }

        //  проверка что путь к файлам не пустой
        private bool CheckPath()
        {
            if (string.IsNullOrWhiteSpace(pathTextBox.Text))
            {
                System.Windows.MessageBox.Show("Заполни путь к файлам");
                return false;
            }
            return true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //collectedUIDsLabel_Copy2.Visibility = Visibility.Visible;
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //collectedUIDsLabel_Copy2.Visibility = Visibility.Collapsed;
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

        private void sortedUIDs_Click(object sender, RoutedEventArgs e)
        {
            string path = pathFileTextBox.Text;
            ViewAllRowsInFileToSorting();
            CheckBoxMeasurements.IsChecked = true;
        }

        public void ViewAllRowsInFileToSorting()
        {
            string path = pathFileTextBox.Text; // Путь к файлу

            string dateTimeString = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // Получаем текущую дату и время в формате "yyyyMMdd_HHmmss"
                                                                              // Имя папки
            string folderName = $"CSVParser_{dateTimeString}";

            // Путь к папке (например, на рабочем столе)
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string folderPath = Path.Combine(desktopPath, folderName);

            // Создаем папку, если она не существует
            Directory.CreateDirectory(folderPath);

            // Имя файла
            string fileName = "Result.csv"; // Файл для записи результатов
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
            System.Windows.MessageBox.Show($"Файл успешно создан: {fullPath}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}