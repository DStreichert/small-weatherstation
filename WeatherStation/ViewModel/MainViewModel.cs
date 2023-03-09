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
                var args = e.Argument as BackgroundWorker_DataContext;
                this.TaskWorker.ReportProgress(0);

                if (!(args.timerState is TimerState state) || state.TimerCanceled)
                {
                    this.StopTimerForBackgroundWorker();
                }
                else
                {
                    bool? result = null;
                    if (!this.TaskWorker.CancellationPending && !this._isRunning)
                    {
                        try
                        {
                            this._isRunning = true;
                            if (!this.TaskWorker.CancellationPending)
                            {
                                // Do work
                                e.Result = result = this.GetWeatherData();
                                // Work end
                                if (this.TaskWorker.CancellationPending)
                                {
                                    e.Cancel = true;
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
                    this._isRunning = false;
                }
            };
            this.TaskWorker.RunWorkerCompleted += (object RunWorkerCompletedSender, System.ComponentModel.RunWorkerCompletedEventArgs eventargs) =>
            {
                this._isRunning = false;
                //BackgroundWorker_DataContext args = null;
                //if (!eventargs.Cancelled)
                //{
                //    args = eventargs.Result as BackgroundWorker_DataContext;
                //}
            };
            this.StartTimerForBackgroundWorker();
        }

        /// <summary>
        /// Gets or sets a value indicating whether backgroundworker is running.
        /// </summary>
        private bool _isRunning;

        private string _apiKey;
        /// <summary>
        /// The api key for weatherdata api
        /// </summary>
        private string APIKey
        {
            get
            {
                if (this._apiKey == null)
                {
                    using (var fs = new FileStream("apikey.config", FileMode.OpenOrCreate))
                    using (var sr = new StreamReader(fs))
                    {
                        this._apiKey = sr.ReadToEnd();
                    }
                }
                return this._apiKey;
            }
        }

        private bool _isTodayNotified;
        /// <summary>
        /// Has the user already been notified today?
        /// </summary>
        public bool IsTodayNotified
        {
            get { return _isTodayNotified; }
            set { _isTodayNotified = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// The backgroundworker data context.
        /// </summary>
        private class BackgroundWorker_DataContext
        {
            protected internal TimerState timerState;
        }

        /// <summary>
        /// Starts the timer for background worker.
        /// </summary>
        protected internal void StartTimerForBackgroundWorker()
        {
            this.timerState = new TimerState
            {
                TimerCanceled = false
            };

            // And save a reference for Dispose.
            this.timerState.TimerReference = new Timer(new TimerCallback((object timerState) =>
            {
                if (!this._isRunning)
                {
                    this.TaskWorker.RunWorkerAsync(new BackgroundWorker_DataContext() { timerState = timerState as TimerState });
                }
            }), this.timerState, new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 30));
        }

        /// <summary>
        /// Stops the timer for background worker.
        /// </summary>
        protected internal void StopTimerForBackgroundWorker()
        {
            try
            {
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
        /// The background worker state
        /// </summary>
        internal TimerState timerState;

        /// <summary>
        /// A backgroundworker timer state.
        /// </summary>
        internal class TimerState
        {
            // Used to hold parameters for calls to TimerTask.
            /// <summary>
            /// Gets or sets the timer reference.
            /// </summary>
            protected internal Timer TimerReference { get; set; }
            /// <summary>
            /// Gets or sets a value indicating whether timer canceled.
            /// </summary>
            protected internal bool TimerCanceled { get; set; }
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

        /// <summary>
        /// Gets or sets the humidity.
        /// </summary>
        public double Humidity
        {
            get { return humidity; }
            set
            {
                humidity = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the latitude of the location of the weather data.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the longitude of the location of the weather data.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the zip code.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the country code.
        /// </summary>
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
        /// <summary>
        /// The last successful weatherdata update
        /// </summary>
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

        private ICommand btnStartGetWeatherData_ClickCommand;
        /// <summary>
        /// Gets the button BtnStartGetWeatherData click command.
        /// </summary>
        public ICommand BtnStartGetWeatherData_ClickCommand
            => btnStartGetWeatherData_ClickCommand ?? (btnStartGetWeatherData_ClickCommand = new RelayCommand(this.BtnStartGetWeatherData_Click, _ => this.BtnStartGetWeatherData_CanExecute));

        /// <summary>
        /// Gets a value indicating whether the command of Button BtnStartGetWeatherData can execute.
        /// </summary>
        public bool BtnStartGetWeatherData_CanExecute
            => this.timerState.TimerCanceled;

        private ICommand btnStopGetWeatherData_ClickCommand;
        /// <summary>
        /// Gets the button BtnStopGetWeatherData click command.
        /// </summary>
        public ICommand BtnStopGetWeatherData_ClickCommand
            => btnStopGetWeatherData_ClickCommand ?? (btnStopGetWeatherData_ClickCommand = new RelayCommand(this.BtnStopGetWeatherData_Click, _ => this.BtnStopGetWeatherData_CanExecute));

        /// <summary>
        /// Gets a value indicating whether the command of Button BtnStopGetWeatherData can execute.
        /// </summary>
        public bool BtnStopGetWeatherData_CanExecute
            => !this.timerState.TimerCanceled;

        private ICommand btnGetCoordinates_ClickCommand;
        /// <summary>
        /// Gets the button BtnGetCoordinates click command.
        /// </summary>
        public ICommand BtnGetCoordinates_ClickCommand
            => btnGetCoordinates_ClickCommand ?? (btnGetCoordinates_ClickCommand = new RelayCommand(this.BtnGetCoordinates_Click, _ => this.BtnGetCoordinates_CanExecute));

        private bool btnGetCoordinates_CanExecute = true;
        /// <summary>
        /// Gets a value indicating whether the command of Button BtnGetCoordinates can execute.
        /// </summary>
        public bool BtnGetCoordinates_CanExecute
            => this.btnGetCoordinates_CanExecute;

        /// <summary>
        /// Will execute if button BtnStartGetWeatherData clicked and starting the timer for backgroundworker.
        /// </summary>
        /// <param name="commandParameter">The commandParameter.</param>
        public void BtnStartGetWeatherData_Click(object commandParameter)
        {
            this.StartTimerForBackgroundWorker();
        }

        /// <summary>
        /// Will execute if button BtnStopGetWeatherData clicked.
        /// </summary>
        /// <param name="commandParameter">The commandParameter.</param>
        public void BtnStopGetWeatherData_Click(object commandParameter)
        {
            this.StopTimerForBackgroundWorker();
        }

        /// <summary>
        /// Will execute if button BtnGetCoordinates clicked. 
        /// If latitude and longitude not within parameters seted will the coordinates from the location of zip and country within the parameters geted from the api. 
        /// Then will the coordinates within the viewmodel seted.
        /// </summary>
        /// <param name="commandParameter">The commandParameter.</param>
        public void BtnGetCoordinates_Click(object commandParameter)
        {
            try
            {
                this.btnGetCoordinates_CanExecute = false;
                var parameter = (object[])commandParameter;
                var plz = (string)parameter[0];
                var country = (string)parameter[1];
                var accessUri = "http://api.openweathermap.org/geo/1.0/zip";
                if (!string.IsNullOrWhiteSpace(parameter[2].ToString()) && !string.IsNullOrWhiteSpace(parameter[3].ToString()))
                {
                    this.Latitude = Convert.ToDecimal(parameter[2].ToString());
                    this.Longitude = Convert.ToDecimal(parameter[3].ToString());
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
            this.btnGetCoordinates_CanExecute = true;
        }

        /// <summary>
        /// Gets the weather data from the api
        /// </summary>
        /// <returns>True if successful getted the weatherdatas, false if not successful</returns>
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
    }
}
