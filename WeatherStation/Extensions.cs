using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WeatherStation
{
    public static class Extensions
    {
        public static void SetValidationRules(
            this TextBox Box,
            ValidationRule[] rules,
            System.Windows.Data.UpdateSourceTrigger trigger = System.Windows.Data.UpdateSourceTrigger.Default)
        {
            var text = Box.Text;
            System.Windows.Data.Binding binding = SetValidationRules(Box, rules, "Text", trigger);
            System.Windows.Data.BindingOperations.SetBinding(Box, TextBox.TextProperty, binding);
            Box.Text = text;
        }

        public static System.Windows.Data.Binding SetValidationRules(
            object source,
            ValidationRule[] rules,
            string Path,
            System.Windows.Data.UpdateSourceTrigger trigger = System.Windows.Data.UpdateSourceTrigger.Default)
        {
            var binding = new System.Windows.Data.Binding(Path)
            {
                Source = source,
                Mode = System.Windows.Data.BindingMode.TwoWay,
                UpdateSourceTrigger = trigger
            };
            foreach (var rule in rules)
            {
                binding.ValidationRules.Add(rule);
            }

            return binding;
        }
    }
}
