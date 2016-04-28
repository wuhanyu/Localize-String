using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizeString
{
    class Program
    {
        private const string RESOURCE_PATH = "-respath";
        private const string LOCALIZATION_FILE = "-localizefile";
        private static readonly char[] SEPERATE_CHARS = {'\t'};
        private static readonly char[] TRIM_CHARS = { '"', '„', '”', '“', };

        static void Main(string[] args)
        {
            string resPath = null;
            string localizeFile = null;

            if (!ReadCommandLineParamater(args, ref resPath, ref localizeFile))
            {
                return;
            }

            //try
            //{
                ProcessLocalizationResources(resPath, localizeFile);
            //}
            /*catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }*/
            
        }

        static void ProcessLocalizationResources(string resPath, string localizeFilePath)
        {
            Dictionary<string, Dictionary<string, string>> localizations = GetLocalizations(localizeFilePath);
            Dictionary<string, XmlNode> templates = GetTemplates(localizations["zh-cn"].Keys, resPath + "en-us.resx");
            foreach (var filepath in Directory.EnumerateFiles(resPath))
            {
                string locale = NormalizeLocale(Path.GetFileNameWithoutExtension(filepath));
                if (localizations.Keys.Contains(locale))
                {
                    XmlDocument xml = new XmlDocument();
                    try
                    {
                        xml.Load(filepath);
                    }
                    catch
                    {
                        throw new Exception("Can not find resource file locale " + locale + " in " + filepath);
                    }
                    
                    foreach (var key in templates.Keys)
                    {
                        XmlNode node = templates[key].Clone();
                        node.SelectNodes("//value")[0].InnerText = localizations[locale][key];
                        XmlNode importNode = xml.ImportNode(node, true);
                        var xpath = string.Format("{0}[@name='{1}']", node.Name, node.Attributes["name"].Value);
                        var oldNode = xml.DocumentElement.SelectSingleNode(xpath);
                        if (oldNode == null)
                        {
                            xml.DocumentElement.AppendChild(importNode);
                        }
                        else
                        {
                            xml.DocumentElement.ReplaceChild(importNode, oldNode);
                        }
                        
                    }
                    xml.Save(filepath);
                }
            }
        }

        static Dictionary<string, XmlNode> GetTemplates(IEnumerable<string> keys, string enusFilePath)
        {
            Dictionary<string, XmlNode> templates = new Dictionary<string, XmlNode>();
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(enusFilePath);
            }
            catch
            {
                throw new Exception("Can not find en-us resource in " + enusFilePath);
            }
            
            XmlNodeList dataNodes = xml.SelectNodes("//data//value"); 
            foreach (XmlNode node in dataNodes)
            {
                if (keys.Contains(node.InnerText))
                {
                    templates.Add(node.InnerText, node.ParentNode);
                }
            }
            return templates;
        }

        static Dictionary<string, Dictionary<string, string>> GetLocalizations(string localizeFile)
        {
            Dictionary<string, Dictionary<string, string>> localizations = new Dictionary<string, Dictionary<string, string>>();
            string[] lines;
            try
            {
                lines = File.ReadAllLines(localizeFile);
            }
            catch
            {
                throw new Exception("Can not find localized file in " + localizeFile);
            }
            string[] locales = lines[0].Split(SEPERATE_CHARS);
            for (int i = 1; i < locales.Length; i++)
            {
                localizations.Add(NormalizeLocale(locales[i]), new Dictionary<string, string>());
            }
            for (int i = 1; i < lines.Length; i++)
            {
                string[] localizaionStrings = lines[i].Split(SEPERATE_CHARS);
                for (int j = 1; j < locales.Length; j++)
                {
                    localizations[NormalizeLocale(locales[j])].Add(
                        NormalizeString(localizaionStrings[0]), NormalizeString(localizaionStrings[j]));
                }
            }
            return localizations;
        }

        static string NormalizeLocale(string locale)
        {
            return locale.Split(' ')[0].Trim().ToLowerInvariant();
        }

        static string NormalizeString(string str)
        {
            return str.Trim(TRIM_CHARS);
        }

        private static bool ReadCommandLineParamater(string[] args, ref string resPath, ref string localizeFile)
        {
            if (args == null || !args.Any())
            {
                PrintHelpMessage();
                return false;
            }

            resPath = ReadCommandLineParamater(args, RESOURCE_PATH);
            localizeFile = ReadCommandLineParamater(args, LOCALIZATION_FILE);

            if (!(string.IsNullOrEmpty(resPath) || string.IsNullOrEmpty(localizeFile)))
                return true;

            PrintHelpMessage();
            return false;
        }

        private static string ReadCommandLineParamater(string[] args, string paramaterName)
        {
            string result = null;

            if (args == null || !args.Any())
                return null;

            var sourcePathParameter = args.FirstOrDefault(g => g.ToLowerInvariant().StartsWith(paramaterName.ToLowerInvariant()));
            if (!string.IsNullOrEmpty(sourcePathParameter))
                result = sourcePathParameter.Substring(paramaterName.Length + 1);

            return result;
        }

        private static void PrintHelpMessage()
        {
            Console.Write(@"
Usage:
LocalizeString.exe -respath:resPath -localizefile:localizeFile

Comment:
The resPath stands for the directory where the localize string is store.
The localizeFile needs to be saved as .txt(Unicode Text option in Excel) format with '\t' seperation.
The column name of localizeFile should be locale like zh-cn, ja-jp.
            ");
        }
    }
}
