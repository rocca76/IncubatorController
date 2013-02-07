using System;
using Microsoft.SPOT;
using System.Threading;
using System.IO;

namespace NetduinoPlus.Controler
{
    public class WriteFile
    {
        #region Private Variables
        private Thread currentThread = null;
        private String _data;
        #endregion


        #region Constructors
        public WriteFile(String data)
        {
            _data = data;
        }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public String Data
        {
            get { return _data; }
        }
        #endregion

        public void Start()
        {
            this.currentThread = new Thread(ThreadMain);
            currentThread.Start();
        }

        private void ThreadMain()
        {
            try
            {
                using (var filestream = new FileStream(@"SD\Incubateur.txt", FileMode.Append))
                {
                    StreamWriter streamWriter = new StreamWriter(filestream);
                    streamWriter.WriteLine(Data);
                    streamWriter.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }
    }
}
