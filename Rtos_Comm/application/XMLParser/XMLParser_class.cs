// File: Rtos_Comm/application/XMLParser/XML_Parser.cs
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

        // Bu harita sizin istediğiniz gibi basit kalacak.
        private readonly Dictionary<string, Type> moduleTypeMap = new Dictionary<string, Type>
        {
            { "adc", typeof(ADC_Config_Class) },
            { "irq", typeof(IRQ_Config_Class) },
            { "gpt", typeof(GPT_Config_Class) }
        };

        // Bu harita, ADC gibi birleşik yapılar için kullanılmaya devam edecek.
        private readonly Dictionary<string, IDriverConfig> configMap = new Dictionary<string, IDriverConfig>();

        // *** YENİ KURAL LİSTESİ ***
        // Bu listedeki anahtarlar ("gpt" gibi), her bulunduğunda yeni bir nesne oluşturulmasını sağlar.
        private readonly List<string> multiInstanceKeys = new List<string>
        {
            "gpt",
            "irq" // Eğer birden fazla IRQ olursa, onların da ayrı işlenmesi için eklenebilir.
        };

        public void ParseConfigurationXml(string path)
        {
            // Her çalıştırmadan önce tüm listeleri ve haritaları temizle
            AllConfigs.Clear();
            configMap.Clear();

            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            XmlNodeList moduleNodes = doc.GetElementsByTagName("module");

            foreach (XmlNode module in moduleNodes)
            {
                string moduleId = module.Attributes["id"]?.Value ?? "";
                var matchedKey = moduleTypeMap.Keys.FirstOrDefault(k => moduleId.Contains(k));
                if (matchedKey == null)
                    continue;

                string commonId = matchedKey;
                IDriverConfig configInstance;

                // *** ANA MANTIK DEĞİŞİKLİĞİ ***
                if (multiInstanceKeys.Contains(commonId))
                {
                    // Eğer anahtar "gpt" gibi çoklu bir türe aitse:
                    // HER ZAMAN yeni bir nesne oluştur.
                    configInstance = (IDriverConfig)Activator.CreateInstance(moduleTypeMap[commonId]);
                    // Ve bu yeni nesneyi doğrudan ana listeye ekle.
                    AllConfigs.Add(configInstance);
                }
                else
                {
                    // Eğer anahtar "adc" gibi birleşik bir türe aitse:
                    // SİZİN ORİJİNAL KODUNUZDAKİ GİBİ DAVRAN.
                    if (!configMap.TryGetValue(commonId, out configInstance))
                    {
                        configInstance = (IDriverConfig)Activator.CreateInstance(moduleTypeMap[commonId]);
                        configMap[commonId] = configInstance;
                        AllConfigs.Add(configInstance);
                    }
                }

                // İster yeni oluşturulsun, ister haritadan bulunsun, nesneyi doldur.
                configInstance.Id = string.IsNullOrEmpty(configInstance.Id) ? moduleId : configInstance.Id + ";" + moduleId;

                foreach (XmlNode property in module.SelectNodes("property"))
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