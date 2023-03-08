using WeatherStation.Net;
using WeatherStation.Properties;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using resx_ns = WeatherStation.Properties.Resources;
using System.IO;

namespace WeatherStation.ViewModel
{
    /// <summary>
    /// The main view model.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            this.IsTodayNotified = DateTime.Compare(Settings.Default.lastNotificationAbove30Degrees.Date, DateTime.Today) == 0;
            this.TaskWorker = new System.ComponentModel.BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            this.TaskWorker.DoWork += (object sender, System.ComponentModel.DoWorkEventArgs e) =>
            {
                var args = e.Argument as TaskWorker_DataContext;
                this.TaskWorker.ReportProgress(0);

                if (!(args.workerState is TimerState state) || state.TimerCanceled)
                {
                    this.ExecuteWorkerStop();
                }
                else
                {
                    bool? result = null;
                    if (!this.TaskWorker.CancellationPending && !this.IsRunning)
                    {
                        try
                        {
                            this.IsRunning = true;
                            this.LastExecuted = DateTime.Now;
                            if (!this.TaskWorker.CancellationPending)
                            {
                                // Do work
                                args.Result = this.GetWeatherData();
                                // Work end
                                e.Result = args;
                                if (this.TaskWorker.CancellationPending)
                                {
                                    e.Cancel = true;
                                }

                                if (args.Result.GetType() == typeof(bool))
                                {
                                    result = (bool)args.Result;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show(ex.Message);
                        }
                    }

                    if (result == true)
                    {
                        this.LastSuccessful = DateTime.Now;
                    }
                }
                if (e.Cancel)
                {
                    this.IsRunning = false;
                }
            };
            this.TaskWorker.RunWorkerCompleted += (object RunWorkerCompletedSender, System.ComponentModel.RunWorkerCompletedEventArgs eventargs) =>
            {
                this.IsRunning = false;
                TaskWorker_DataContext args = null;
                if (!eventargs.Cancelled)
                {
                    args = eventargs.Result as TaskWorker_DataContext;
                }
            };
            this.ExecuteWorkerStart();
        }

        private string apiKey { get; set; }
        private string APIKey
        {
            get
            {
                if (apiKey == null)
                {
                    using (var fs = new FileStream("apikey.config", FileMode.OpenOrCreate))
                    using (var sr = new StreamReader(fs))
                    {
                        this.apiKey = sr.ReadToEnd();
                    }
                }
                return this.apiKey;
            }
        }

        public bool GetWeatherData()
        {
            var result = false;
            try
            {
                var accessUri = Settings.Default.ApiUrl;
                using (var WS = new RESTWebserviceConnection(accessUri, RESTWebserviceConnection.ProvidedContentTypes.json))
                {
                    WS.Accept = RESTWebserviceConnection.ProvidedContentTypes.json;
                    var response = WS.Get<API.CurrentWeatherData.Response.Get>("", new List<string> { "lat=" + this.Latitude, "lon=" + this.Longitude, "units=metric", "lang=de", "appid=" + this.APIKey });
                    this.Temperature = response.main.temp;
                    this.Pressure = response.main.pressure;
                    this.Humidity = response.main.humidity;
                    result = true;
                }
                if (this.Temperature >= 30 && DateTime.Compare(Settings.Default.lastNotificationAbove30Degrees.Date, DateTime.Today) < 0)
                {
                    System.Windows.MessageBox.Show(string.Format(resx_ns.notificationAbove30Degrees, this.Temperature));
                    Settings.Default.lastNotificationAbove30Degrees = DateTime.Today;
                    Settings.Default.Save();
                    this.IsTodayNotified = true;
                }
                else if (this.IsTodayNotified && DateTime.Compare(Settings.Default.lastNotificationAbove30Degrees.Date, DateTime.Today) < 0)
                {
                    this.IsTodayNotified = false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                result = false;
            }
            return result;
        }

        private bool isTodayNotified;

        public bool IsTodayNotified
        {
            get { return isTodayNotified; }
            set { isTodayNotified = value; RaisePropertyChanged(); }
        }


        private class TaskWorker_DataContext
        {
            protected internal object Result = false;
            protected internal TimerState workerState;
        }

        protected internal void ExecuteWorkerStart()
        {
            this.timerChangeState = new TimerState();
            this.timerChangeState.TimerCanceled = false;
            this.timerState = new TimerState();
            this.timerState.TimerCanceled = false;

            // And save a reference for Dispose.
            this.timerState.TimerReference = new Timer(new TimerCallback((object workerState) =>
            {
                this.Execute(workerState);
            }), this.timerState, new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 30));
        }

        internal void Execute(object workerState)
        {
            if (!this.IsRunning)
            {
                this.TaskWorker.RunWorkerAsync(new TaskWorker_DataContext() { workerState = workerState as TimerState });
            }
        }

        protected internal void ExecuteWorkerStop()
        {
            try
            {
                if (this.timerChangeState != null)
                {
                    this.timerChangeState.TimerCanceled = true;
                    this.timerChangeState.TimerReference?.Dispose();
                }
                if (this.timerState != null)
                {
                    this.timerState.TimerCanceled = true;
                    this.timerState.TimerReference?.Dispose();
                }
                if (this.TaskWorker != null)
                {
                    this.TaskWorker.CancelAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// The timer change state
        /// </summary>
        internal TimerState timerChangeState;

        /// <summary>
        /// The background worker state
        /// </summary>
        internal TimerState timerState;

        internal class TimerState
        {
            // Used to hold parameters for calls to TimerTask.
            protected internal Timer TimerReference;
            protected internal bool TimerCanceled;
        }

        /// <summary>
        /// Gets or sets the BackgroundWorker which update the Data.
        /// </summary>
        internal System.ComponentModel.BackgroundWorker TaskWorker { get; set; }

        private double temperature;
        /// <summary>
        /// Gets or sets the temperature.
        /// </summary>
        public double Temperature
        {
            get
            {
                return temperature;
            }
            set
            {
                temperature = value;
                this.RaisePropertyChanged();
            }
        }

        private double pressure = 2;

        /// <summary>
        /// Gets or sets the pressure.
        /// </summary>
        public double Pressure
        {
            get
            {
                return pressure;
            }
            set
            {
                pressure = value;
                this.RaisePropertyChanged();
            }
        }

        private double humidity;

        public double Humidity
        {
            get { return humidity; }
            set
            {
                humidity = value;
                this.RaisePropertyChanged();
            }
        }

        public decimal Latitude
        {
            get
            {
                return Settings.Default.Lat;
            }
            set
            {
                Settings.Default.Lat = value;
                Settings.Default.Save();
                this.RaisePropertyChanged();
            }
        }

        public decimal Longitude
        {
            get
            {
                return Settings.Default.Lon;
            }
            set
            {
                Settings.Default.Lon = value;
                Settings.Default.Save();
                this.RaisePropertyChanged();
            }
        }

        private string plz;

        public string Plz
        {
            get { return plz; }
            set
            {
                plz = value;
                this.RaisePropertyChanged();
            }
        }

        private string country;

        public string Country
        {
            get { return country; }
            set
            {
                country = value;
                this.RaisePropertyChanged();
            }
        }

        private DateTime lastSuccessful;
        public DateTime LastSuccessful
        {
            get
            {
                return lastSuccessful;
            }
            set
            {
                lastSuccessful = value;
                this.RaisePropertyChanged();
            }
        }

        private DateTime lastExecuted;
        public DateTime LastExecuted
        {
            get
            {
                return lastExecuted;
            }
            private set
            {
                lastExecuted = value;
            }
        }

        private bool isRunning;
        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
            private set
            {
                isRunning = value;
            }
        }

        private ICommand btnStartGetWeatherData_ClickCommand;
        public ICommand BtnStartGetWeatherData_ClickCommand
        {
            get
            {
                return btnStartGetWeatherData_ClickCommand ?? (btnStartGetWeatherData_ClickCommand = new RelayCommand(this.BtnStartGetWeatherData_Click, param => this.BtnStartGetWeatherData_CanExecute));
            }
        }

        public bool BtnStartGetWeatherData_CanExecute
        {
            get
            {
                // check if executing is allowed, i.e., validate, check if a process is running, etc. 
                return this.timerState.TimerCanceled;
            }
        }

        public void BtnStartGetWeatherData_Click(object obj)
        {
            this.ExecuteWorkerStart();
        }

        private ICommand btnStopGetWeatherData_ClickCommand;
        public ICommand BtnStopGetWeatherData_ClickCommand
        {
            get
            {
                return btnStopGetWeatherData_ClickCommand ?? (btnStopGetWeatherData_ClickCommand = new RelayCommand(this.BtnStopGetWeatherData_Click, param => this.BtnStopGetWeatherData_CanExecute));
            }
        }

        public bool BtnStopGetWeatherData_CanExecute
        {
            get
            {
                // check if executing is allowed, i.e., validate, check if a process is running, etc. 
                return !this.timerState.TimerCanceled;
            }
        }

        public void BtnStopGetWeatherData_Click(object obj)
        {
            this.ExecuteWorkerStop();
        }

        private ICommand btnGetCoordinates_ClickCommand;
        public ICommand BtnGetCoordinates_ClickCommand
        {
            get
            {
                return btnGetCoordinates_ClickCommand ?? (btnGetCoordinates_ClickCommand = new RelayCommand(this.BtnGetCoordinates_Click, param => this.BtnGetCoordinates_CanExecute));
            }
        }

        public bool BtnGetCoordinates_CanExecute
        {
            get
            {
                // check if executing is allowed, i.e., validate, check if a process is running, etc. 
                return true;
            }
        }

        public void BtnGetCoordinates_Click(object parameter)
        {
            try
            {
                var values = (object[])parameter;
                var plz = (string)values[0];
                var country = (string)values[1];
                var accessUri = "http://api.openweathermap.org/geo/1.0/zip";
                if (!string.IsNullOrWhiteSpace(values[2].ToString()) && !string.IsNullOrWhiteSpace(values[3].ToString()))
                {
                    this.Latitude = Convert.ToDecimal(values[2].ToString());
                    this.Longitude = Convert.ToDecimal(values[3].ToString());
                }
                else if (!string.IsNullOrWhiteSpace(plz) && !string.IsNullOrWhiteSpace(country))
                {
                    using (var WS = new RESTWebserviceConnection(accessUri, RESTWebserviceConnection.ProvidedContentTypes.json))
                    {
                        WS.Accept = RESTWebserviceConnection.ProvidedContentTypes.json;
                        var response = WS.Get<API.CoordinatesByZip.Response.Get>("", new List<string> { "zip=" + plz + ',' + country, "limit=1", "appid=" + this.APIKey });
                        this.Latitude = response.lat;
                        this.Longitude = response.lon;
                        this.Plz = plz;
                        this.Country = country;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
    }
}
