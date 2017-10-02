﻿using Microsoft.VisualBasic.FileIO;
using SQLiteCreation.Parsers.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteCreation.Parsers
{
    class Parser : IParser
    {
        public ConcurrentQueue<string[]> StringArrayQueue { get; private set; }
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource(); 
        public event Action<object, string> OnError = (object o, string s) => { };
        public event Action<object, string> OnFatalError = (object o, string s) => { };
        public Dictionary<string, int> ColumnNameAndPosition { get; private set; }

        private TextFieldParser tsvReader;
        private IDataVerificationStrategy dvs;
        private string[] parserDelimiters;
        private string[] headers;
        
        public Parser(string pathToFile, string[] parserDelimiters, string[] headers, IDataVerificationStrategy dvs)
        {
            
            try
            {
                tsvReader = new TextFieldParser(pathToFile);
            }
            catch (Exception ex)
            {
                string message = $"При обращении к текстовому файлу возникла ошибка.{Environment.NewLine}Подробности:" + ex.Message;
                OnFatalError(this, message);
            }
            this.parserDelimiters = parserDelimiters;
            this.headers = headers;
            this.dvs = dvs;
            SettingUp();
        }

        public void Parse()
        {
            string[] parsedStringArray;
            string errorMessage = "";
            
            //Счетчик всех считанных строк, нужен чтобы отображать, в какой строке текстового файла присутствует ошибка
            int sourceCounter = 0;

            while (!tsvReader.EndOfData)
            {
                try
                {
                    parsedStringArray = tsvReader.ReadFields();
                }
                catch (Exception ex)
                {
                    string message1 = "В процессе чтения текстового файла возникла фатальная ошибка.{Environment.NewLine}Подробности:";
                    string message2 = "База данных создана неполностью.";
                    OnFatalError(this, message1+ ex.Message+ Environment.NewLine+ message2);
                    return;
                }

                sourceCounter++;

                //Проверяем данные в считанной строке, при наличии ошибок выводим их на консоль и пишем в лог ошибок
                if (!dvs.Verify(ColumnNameAndPosition, parsedStringArray, sourceCounter, ref errorMessage))
                {
                    using (StreamWriter file = new StreamWriter(string.Format(@"errorlog_{0}.txt", DateTime.Now.ToString(@"dd-MM-yyyy_HH-mm.ss")), true))
                    {
                        OnError(this, errorMessage);
                        file.WriteLine(errorMessage);
                        continue;
                    }
                }

                //Если в строке нет ошибок, добавляем данные из нее к sql запросу
                StringArrayQueue.Enqueue(parsedStringArray);
            }
            Cts.Cancel();
        }

        public async Task ParseAsync()
        {
            await Task.Factory.StartNew(Parse).ContinueWith((x)=>Cts.Cancel());
        }

        private void SettingUp()
        {
            StringArrayQueue = new ConcurrentQueue<string[]>();

            //Настраиваем парсер
            tsvReader.SetDelimiters(parserDelimiters);
            tsvReader.HasFieldsEnclosedInQuotes = true;

            //Считываем из таблицы первую строку с заголовками и ищем в ней названия столбцов
            string[] parsedString = tsvReader.ReadFields();
            int[] headersPositions = new int[headers.Length];

            //Ставим в соответствие название столбца и его позицию в таблице.
            //Если какого-то столбца нет, то прекращаем работу программы,
            //и с помощью словаря показываем, каких именно столбцов не хватает
            ColumnNameAndPosition = new Dictionary<string, int>(headers.Length);
            for (int i = 0; i < headersPositions.Length; i++)
            {
                ColumnNameAndPosition.Add(headers[i], Array.IndexOf(parsedString, headers[i]));
            }

            if (ColumnNameAndPosition.ContainsValue(-1))
            {
                string message1 = $"В текстовом файле отсутствуют следующие столбцы:{Environment.NewLine}";
                string[] missingColumns = ColumnNameAndPosition.Where(x => x.Value == -1).Select(x => x.Key).ToArray();
                string message2 = string.Join(Environment.NewLine, missingColumns);
                OnFatalError(this, message1 + message2);
            }
        }
    }
}
