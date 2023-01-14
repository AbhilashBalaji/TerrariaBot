using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Meina
{
    internal class LoggerController
    {
        private string m_exePath = string.Empty;

        public LoggerController()
        {
        }

        public void Log(string logMessage)
        {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                Console.WriteLine(m_exePath + "\\" + "TerrariaBot.txt");
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + "TerrariaBot.txt"))
                {
                    LogToFile(logMessage, w);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void LogToFile(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.Write("\r\nLog Entry : ");
                txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                txtWriter.WriteLine("  :");
                txtWriter.WriteLine("  :{0}", logMessage);
                txtWriter.WriteLine("-------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
