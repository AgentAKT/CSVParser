using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CSVParser
{
    internal class WorkingWithFiles
    {
        private Label _processedFilesLabelValue;
        private List<string> _pathFilesList;
        private int _counterFiles;

        // Единственный конструктор
        public WorkingWithFiles(Label processedFilesLabelValue, List<string> pathFilesList)
        {
            _processedFilesLabelValue = processedFilesLabelValue;
            _pathFilesList = pathFilesList;
            _counterFiles = 0;
        }

        public void ProcessFiles(List<string> selectedFiles)
        {
            foreach (string s in selectedFiles)
            {
                _pathFilesList.Add(s);
                Console.WriteLine(s);
                _counterFiles++;
                _processedFilesLabelValue.Content = _counterFiles;
            }
        }
    }
}
