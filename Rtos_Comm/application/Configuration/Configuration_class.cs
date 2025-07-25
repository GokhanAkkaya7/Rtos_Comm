using Rtos_Comm.application.XMLParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rtos_Comm.application.Configuration
{
    public enum GptUnit
    {
        Unknown_unit,
        Seconds,
        Milliseconds,
        Microseconds,
        Hertz
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class XmlMapAttribute : Attribute
    {
        public string XmlId { get; }
        public XmlMapAttribute(string xmlId) => XmlId = xmlId;
    }

    public interface IDriverConfig
    {
        string Id { get; set; }
        void SetProperty(string propId, string propValue);
    }

    public class ADC_Config_Class : IDriverConfig
    {
        public string Id { get; set; }

        [XmlMap("module.framework.sf_adc_periodic.name")]
        public string FrameworkName { get; set; }

        [XmlMap("module.framework.sf_adc_periodic.data_buffer_length")]
        public int BufferLength { get; set; }

        [XmlMap("module.driver.adc.resolution")]
        public int ResolutionBits { get; set; }

        private List<int> _usedChannels = new List<int>();
        public List<int> UsedChannels
        {
            get { return _usedChannels; }
            private set { _usedChannels = value; }
        }

        public void SetProperty(string propId, string propValue)
        {
            if (propId.StartsWith("module.driver.adc.channel_") && propValue.EndsWith(".used"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(propId, @"channel_(\d+)");
                if (match.Success)
                {
                    int channel;
                    if (int.TryParse(match.Groups[1].Value, out channel))
                    {
                        if (!_usedChannels.Contains(channel))
                            _usedChannels.Add(channel);
                    }
                }
                return;
            }

            var props = this.GetType().GetProperties();
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<XmlMapAttribute>();
                if (attr != null && attr.XmlId == propId)
                {
                    if (prop.PropertyType == typeof(int))
                    {
                        int directInt;
                        if (int.TryParse(propValue, out directInt))
                        {
                            prop.SetValue(this, directInt);
                        }
                        else
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(propValue, @"\d+");
                            if (match.Success)
                            {
                                int extractedInt;
                                if (int.TryParse(match.Value, out extractedInt))
                                {
                                    prop.SetValue(this, extractedInt);
                                }
                            }
                        }
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        var lastPart = propValue.Split('.').Last();
                        prop.SetValue(this, lastPart);
                    }
                    break;
                }
            }
        }
    }

    public class IRQ_Config_Class : IDriverConfig
    {
        public string Id { get; set; }

        [XmlMap("module.driver.external_irq.name")]
        public string Name { get; set; }

        [XmlMap("module.driver.external_irq.channel")]
        public int Used_Irq_Channels { get; set; }

        public void SetProperty(string propId, string propValue)
        {
            var props = this.GetType().GetProperties();
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<XmlMapAttribute>();
                if (attr != null && attr.XmlId == propId)
                {
                    if (prop.PropertyType == typeof(int))
                    {
                        if (int.TryParse(propValue, out int val))
                            prop.SetValue(this, val);
                        else
                        {
                            var match = Regex.Match(propValue, @"\d+");
                            if (match.Success && int.TryParse(match.Value, out int extractedInt))
                                prop.SetValue(this, extractedInt);
                        }
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(this, propValue.Split('.').Last());
                    }
                    break;
                }
            }
        }
    }

    public class GPT_Config_Class : IDriverConfig
    {
        public string Id { get; set; }

        [XmlMap("module.driver.timer.name")]
        public string Name { get; set; }

        [XmlMap("module.driver.timer.channel")]
        public int Channel { get; set; }

        [XmlMap("module.driver.timer.period")]
        public int Period { get; set; }

        public GptUnit Unit { get; set; }

        public void SetProperty(string propId, string propValue)
        {
            if (propId == "module.driver.timer.unit")
            {
                switch (propValue)
                {
                    case "module.driver.timer.unit.unit_period_sec":
                        this.Unit = GptUnit.Seconds;
                        break;
                    case "module.driver.timer.unit.unit_period_msec":
                        this.Unit = GptUnit.Milliseconds;
                        break;
                    case "module.driver.timer.unit.unit_period_usec":
                        this.Unit = GptUnit.Microseconds;
                        break;
                    case "module.driver.timer.unit.unit_period_Hertz":
                        this.Unit = GptUnit.Hertz;
                        break;
                    default:
                        this.Unit = GptUnit.Unknown_unit;
                        break;
                }
                return; 
            }

            var props = this.GetType().GetProperties();
            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<XmlMapAttribute>();
                if (attr != null && attr.XmlId == propId)
                {
                    if (prop.PropertyType == typeof(int))
                    {
                        if (int.TryParse(propValue, out int val))
                        {
                            prop.SetValue(this, val);
                        }
                        else
                        {
                            var match = Regex.Match(propValue, @"\d+");
                            if (match.Success && int.TryParse(match.Value, out int extractedInt))
                            {
                                prop.SetValue(this, extractedInt);
                            }
                        }
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(this, propValue.Split('.').Last());
                    }
                    break;
                }
            }
        }
    }

    // NEW DATA: add related driver class here.
}
