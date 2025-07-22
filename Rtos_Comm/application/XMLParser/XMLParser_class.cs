using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Rtos_Comm.application.Configuration;

namespace Rtos_Comm.application.XMLParser
{
    public class XML_Parser
    {
        public List<IDriverConfig> AllConfigs { get; } = new List<IDriverConfig>();

        private Dictionary<string, Type> moduleTypeMap = new Dictionary<string, Type>
        {
            // NEW DATA: add related driver class here.
            { "adc", typeof(ADC_Config_Class) },
            { "irq", typeof(IRQ_Config_Class) }
        };

        private Dictionary<string, IDriverConfig> configMap = new Dictionary<string, IDriverConfig>();

        public void ParseConfigurationXml(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            XmlNodeList moduleNodes = doc.GetElementsByTagName("module");

            foreach (XmlNode module in moduleNodes)
            {
                string moduleId = module.Attributes["id"]?.Value ?? "";
                var matchedKey = moduleTypeMap.Keys.FirstOrDefault(k => moduleId.Contains(k));
                if (matchedKey == null)
                    continue;

                // get the same ground framework and driver like
                // "module.framework.sf_adc_periodic_on_sf_adc_periodic.1591552232"  
                // "module.driver.adc_on_adc.841273083" as "adc"
                string commonId = matchedKey;

                IDriverConfig configInstance;
                if (!configMap.TryGetValue(commonId, out configInstance))
                {
                    configInstance = (IDriverConfig)Activator.CreateInstance(moduleTypeMap[commonId]);
                    configMap[commonId] = configInstance;
                    AllConfigs.Add(configInstance);
                }

                configInstance.Id = moduleId; 

                foreach (XmlNode property in module.ChildNodes)
                {
                    string propId = property.Attributes["id"]?.Value ?? "";
                    string propValue = property.Attributes["value"]?.Value ?? "";

                    configInstance.SetProperty(propId, propValue);
                }
            }
        }

        public List<T> GetConfigs<T>() where T : IDriverConfig
        {
            return AllConfigs.OfType<T>().ToList();
        }
    }
}
