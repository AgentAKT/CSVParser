using System.Collections.Generic;

namespace CSVParser
{
    public class ErrorHandler
    {
        // Словарь для хранения шаблонов сообщений об ошибках
        private readonly Dictionary<int, string> _errorTemplates;

        // Конструктор, который инициализирует шаблоны сообщений об ошибках
        public ErrorHandler()
        {
            _errorTemplates = new Dictionary<int, string>
            {
                { 1, "Проверь начало и конец диапазонов" },
                { 2, "Время не может быть меньше 1" },
                { 3, "Заполни путь к файлам" },
                { 4, "Заполни пустые поля" },
                { 5, "В файле {0}, в выбранном столбце нет данных, либо файл пустой" },
                { 6, "В выбранной папке .csv файлы не найдены" },
                { 7, "" },
                { 8, "" }
            };
        }

        // Метод для получения сообщения об ошибке по индексу
        public string GetErrorMessage(int errorCode, string fileName = null)
        {
            if (_errorTemplates.TryGetValue(errorCode, out string template))
            {
                // Если шаблон содержит плейсхолдер {0}, подставляем fileName
                return string.Format(template, fileName);
            }
            return "Неизвестная ошибка.";
        }
    }
}