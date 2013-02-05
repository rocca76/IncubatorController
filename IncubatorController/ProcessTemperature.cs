using System;
using Microsoft.SPOT;
using System.Threading;

namespace NetduinoPlus.Controler
{
  public delegate void TemperatureEventHandler(TemperatureThread requestTemperature);

  class ProcessTemperature
  {
    #region Private Variables
    private static ProcessTemperature _processTemperature = null;
    private static TemperatureThread _temperatureThread = null;
    #endregion

    #region Constructors
    private ProcessTemperature() { }
    #endregion

    #region Public Static Methods
    public static ProcessTemperature GetInstance()
    {
      if (_processTemperature == null)
      {
        _processTemperature = new ProcessTemperature();
      }

      return _processTemperature;
    }

    public void Control(double target)
    {
      _temperatureThread = new TemperatureThread(TemperatureEventHandler, target);
      _temperatureThread.Start();
    }

    #endregion

    #region Private Methods
    private static void TemperatureEventHandler(TemperatureThread requestTemperature)
    {
      //requestTemperature.Target;
    }
    #endregion
  }


    /// <summary>
    /// 
    /// </summary>

  public class TemperatureThread
  {
    #region Private Variables
    private Thread currentThread = null;
    private TemperatureEventHandler _temperatureEventHandler = null;
    private double _target;
    #endregion


    #region Constructors
    public TemperatureThread(TemperatureEventHandler temperatureEventHandler, double target)
    {
      _temperatureEventHandler = temperatureEventHandler;
      _target = target;
    }
    #endregion


    #region Events
    #endregion


    #region Public Properties
    public double Target
    {
      get { return _target; }
    }

    public bool IsAlive
    {
      get { return currentThread.IsAlive; }
    }
    #endregion

    public void Start()
    {
      this.currentThread = new Thread(ThreadMain);
      currentThread.Start();
    }

    public void Stop()
    {
      Debug.Print("Stopping this thread.");
      this.currentThread.Abort();
    }

    public void Dispose()
    {
      Stop();
    }

    private void ThreadMain()
    {
      try
      {
        //Target
        //_temperatureEventHandler(this);
      }
      catch (Exception ex)
      {
        Debug.Print(ex.ToString());
      }
    }
  }
}
