using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherStation.ViewModel
{
    /// <summary>
    /// The view model base.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the property changed event on the properties changed.
        /// </summary>
        /// <param name="propertyNames">The property names.</param>
        public void OnPropertiesChanged(params string[] propertyNames)
        {
            if (propertyNames != null && this.PropertyChanged != null)
            {
                for (var i = 0; i < propertyNames.Length; i++)
                {
                    this.RaisePropertyChanged(propertyNames[i]);
                }
            }
        }
    }
}
