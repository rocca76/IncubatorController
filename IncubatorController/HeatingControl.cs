using System;
using Microsoft.SPOT;

namespace NetduinoPlus.Controler
{
    class HeatingControl
    {
        #region Private Variables
        private static HeatingControl _instance = null;
        private static readonly object LockObject = new object();
        #endregion


        #region Public Properties
        public static HeatingControl GetInstance()
        {
            lock (LockObject)
            {
                if (_instance == null)
                {
                    _instance = new HeatingControl();
                }

                return _instance;
            }
        }
        #endregion


        #region Constructors
        #endregion


        #region Public Methods
        #endregion


        #region Private Methods
        #endregion
    }
}
