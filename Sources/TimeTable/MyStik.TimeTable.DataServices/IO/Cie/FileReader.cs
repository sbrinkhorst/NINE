﻿using System;
using System.IO;
using log4net;
using MyStik.TimeTable.DataServices.IO.Cie.Data;
using Newtonsoft.Json;

namespace MyStik.TimeTable.DataServices.IO.Cie
{
    public class FileReader
    {
        private string directory;
        private readonly ImportContext ctx = new ImportContext();
        private CieModel model;

        private readonly ILog _Logger = LogManager.GetLogger("FileReader");


        public ImportContext Context
        {
            get
            {
                return ctx;
            }
        }


        public void ReadFiles(string path)
        {
            directory = path;

            _Logger.Info("Lese JSON_Datei");
            ReadJSON("import.json");

            // Komplette Konsistenzprüfung der Dateien
            Check();
        }

        private string GetFileContent(string gpuFile)
        {
            var path = Path.Combine(directory, gpuFile);

            if (File.Exists(path))
            {
                var lines = File.ReadAllText(Path.Combine(directory, gpuFile)); // hier ohne Encoding Default => warum auch immmer
                ctx.AddErrorMessage(gpuFile, string.Format("Anzahl Einträge: {0}", lines.Length), false);
                return lines;
            }

            ctx.AddErrorMessage(gpuFile, string.Format("Datei {0} nicht vorhanden", gpuFile), true);
            return String.Empty;
        }

        private void ReadJSON(string fileName)
        {
            var txt = GetFileContent(fileName);

            try
            {
                model = JsonConvert.DeserializeObject(txt, typeof(CieModel),new JsonSerializerSettings
                {
                    
                }) as CieModel;

            }
            catch (Exception exception)
            {
                ctx.AddErrorMessage(fileName, exception.Message, true);
            }

        }

        private void Check()
        {
            ctx.AddErrorMessage("JSON", $"# Kurse: {model.Courses.Count}", false);

            ctx.Model = model;
        }
    }
}
