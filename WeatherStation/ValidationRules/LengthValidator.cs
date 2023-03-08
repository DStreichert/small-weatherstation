using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using resx_ns = WeatherStation.Properties.Resources;

namespace WeatherStation.ValidationRules
{
    public class LengthValidator : ValidationRule
    {
        public LengthValidator()
        {
        }

        private int _min = 0;

        public int Min
        {
            get { return this._min; }
            set { this._min = value; }
        }

        private int? _max = null;

        public int? Max
        {
            get { return this._max; }
            set { this._max = value; }
        }

        private string _minErrorContent = resx_ns.validation_warning_valueMustLongerAs;

        public string MinErrorContent
        {
            get { return this._minErrorContent; }
            set { this._minErrorContent = value; }
        }

        private string _maxErrorContent = resx_ns.validation_warning_valueCanNotLongerThan;

        public string MaxErrorContent
        {
            get { return this._maxErrorContent; }
            set { this._maxErrorContent = value; }
        }

        private string _errorContent = resx_ns.validation_warning_valueLengthMustBe;

        public string ErrorContent
        {
            get
            {
                return this._errorContent;
            }
            set { this._errorContent = value; }
        }

        private string _FieldLabel = null;

        public string FieldLabel
        {
            get { return this._FieldLabel; }
            set { this._FieldLabel = value; }
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            try
            {
                if (this.Min > 0 && this.Min == this.Max && (value.ToString().Trim().Length) != this.Min)
                {
                    return new ValidationResult(false, string.Format(this.ErrorContent, this.Min, this.FieldLabel == null ? null : " '" + this.FieldLabel + "'"));
                }
                else if ((value == null || (value.ToString().Trim().Length) < this.Min))
                {
                    return new ValidationResult(false, string.Format(this.MinErrorContent, this.Min, this.FieldLabel == null ? null : " '" + this.FieldLabel + "'"));
                }
                else if (this.Max.HasValue && value != null && value.ToString().Trim().Length > this.Max)
                {
                    return new ValidationResult(false, string.Format(this.MaxErrorContent, this.Max, this.FieldLabel == null ? null : " '" + this.FieldLabel + "'"));
                }
            }
            catch (Exception e)
            {
                return new ValidationResult(false, e.Message);
            }

            return ValidationResult.ValidResult;
        }
    }
}
