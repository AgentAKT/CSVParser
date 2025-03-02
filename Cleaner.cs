using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace CSVParser
{
    internal class Cleaner
    {
        private MainWindow _mainWindow;
        public Cleaner(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void Clean(
            Label processedFilesLabelValue = null,
            Label processedRowsLabelValue = null,
            Label writeToFileUIDsLabelValue = null,
            List<string> uidsList = null,
            List<string> measurementsList = null,
            List<string> pathFilesList = null,
            Dictionary<string, string> uids = null)
        {
            // Очистка элементов интерфейса
            if (processedFilesLabelValue != null)
                processedFilesLabelValue.Content = "0";
            if (processedRowsLabelValue != null)
                processedRowsLabelValue.Content = "0";
            if (writeToFileUIDsLabelValue != null)
                writeToFileUIDsLabelValue.Content = "0";

            // Очистка списков и словарей
            if (uidsList != null)
                uidsList.Clear();
            if (measurementsList != null)
                measurementsList.Clear();
            if (uids != null)
                uids.Clear();
            if (pathFilesList != null)
                pathFilesList.Clear();
        }
    }
}