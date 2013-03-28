using System;
using System.IO;
using System.Collections;
using System.Threading;
using Microsoft.SPOT.IO;
using Microsoft.SPOT;

namespace NetduinoPlus.Controler
{
    public sealed class ConfigFile
    {
        #region Private Variables
        private static readonly ConfigFile _instance = new ConfigFile();
        private Hashtable _sections = new Hashtable();
        private String _configFileName = "Configuration.ini";
        private bool _sdCardAvailable = false;
        #endregion


        #region Constructors
        private ConfigFile() 
        {
            RemovableMedia.Insert += new InsertEventHandler(RemovableMedia_Insert);
            RemovableMedia.Eject += new EjectEventHandler(RemovableMedia_Eject);
        }
        #endregion


        #region Events
        #endregion


        #region Public Properties
        public static ConfigFile Instance
        {
            get { return _instance; }
        }

        public bool SDCardAvailable
        {
            get { return _sdCardAvailable; }
        }
        #endregion


        #region Public Methods
        private void RemovableMedia_Insert(object sender, MediaEventArgs e)
        {
            _sdCardAvailable = true;
            LogFile.Application("SD card detected.");
            ProcessControl.Instance.InitFromConfigFile();
        }
        private void RemovableMedia_Eject(object sender, MediaEventArgs e)
        {
            _sdCardAvailable = false;
            Debug.Print("SD card ejected.");
        }

        /// <summary>
        /// Loads the Reads the data in the ini file into the IniFile object
        /// </summary>
        /// <param name="filename"></param>
        public void Load(bool append)
        {
            if (!append)
                DeleteSections();

            string Section = "";
            string[] keyvaluepair;

            using (FileStream inFileStream = new FileStream(Path.Combine("\\SD", _configFileName), FileMode.Open))
            {
                using (StreamReader inStreamReader = new StreamReader(inFileStream))
                {
                    string[] lines = inStreamReader.ReadToEnd().ToLower().Split(new char[] { '\n', '\r' });

                    foreach (string line in lines)
                    {
                        if (line == string.Empty)
                            continue;
                        if (';' == line[0])
                            continue;

                        if ('[' == line[0] && ']' == line[line.Length - 1])
                            Section = line.Substring(1, line.Length - 2);

                        else if (-1 != line.IndexOf("="))
                        {
                            keyvaluepair = line.Split(new char[] { '=' });
                            SetValue(Section, keyvaluepair[0], keyvaluepair[1]);
                        }
                    }
                    inStreamReader.Close();
                    inFileStream.Close();
                }
            }

        }

        /// <summary>
        /// Used to save the data back to the file or your choice
        /// </summary>
        /// <param name="FileName"></param>
        public void Save()
        {
            using (FileStream outFileStream = new FileStream(Path.Combine("\\SD", _configFileName), FileMode.Create))
            {
                using (StreamWriter outStreamWriter = new StreamWriter(outFileStream))
                {
                    foreach (object Sections in _sections.Keys)
                    {
                        outStreamWriter.WriteLine("[" + Sections.ToString() + "]");
                        Hashtable keyvalpair = (Hashtable)_sections[Sections];

                        foreach (object key in keyvalpair.Keys)
                            outStreamWriter.WriteLine(key.ToString() + "=" + keyvalpair[key].ToString());
                    }

                    outStreamWriter.Close();
                    outFileStream.Close();
                }
            }

        }

        public string GetValue(string Section, string key, string defkey = "")
        {
            key = key.ToLower();
            Section = Section.ToLower();

            Hashtable keyvalpair = (Hashtable)_sections[Section];

            if ((null != keyvalpair) && (keyvalpair.Contains(key)))
                defkey = keyvalpair[key].ToString();

            return defkey;
        }
        public float GetValue(string Section, string key, float defkey)
        {
            try { defkey = (float)double.Parse(GetValue(Section, key, defkey.ToString())); }
            catch (Exception) { }
            return defkey;
        }
        public double GetValue(string Section, string key, double defkey)
        {
            try { defkey = double.Parse(GetValue(Section, key, defkey.ToString())); }
            catch (Exception) { }
            return defkey;
        }
        public UInt64 GetValue(string Section, string key, UInt64 defkey)
        {
            try { defkey = UInt64.Parse(GetValue(Section, key, defkey.ToString())); }
            catch (Exception) { }
            return defkey;
        }
        public UInt32 GetValue(string Section, string key, UInt32 defkey)
        {
            try { defkey = UInt32.Parse(GetValue(Section, key, defkey.ToString())); }
            catch (Exception) { }
            return defkey;
        }
        public UInt16 GetValue(string Section, string key, UInt16 defkey)
        {
            try { defkey = UInt16.Parse(GetValue(Section, key, defkey.ToString())); }
            catch (Exception) { }
            return defkey;
        }
        public byte GetValue(string Section, string key, byte defkey)
        {
            try { defkey = byte.Parse(GetValue(Section, key, defkey.ToString())); }
            catch (Exception) { }
            return defkey;
        }
        public Int64 GetValue(string Section, string key, Int64 defkey)
        {
            try { defkey = Int64.Parse(GetValue(Section, key, defkey.ToString())); }
            catch (Exception) { }
            return defkey;
        }
        public Int32 GetValue(string Section, string key, Int32 defkey)
        {
            try { defkey = Int32.Parse(GetValue(Section, key, defkey.ToString())); }
            catch (Exception) { }
            return defkey;
        }
        public Int16 GetValue(string Section, string key, Int16 defkey)
        {
            try { defkey = Int16.Parse(GetValue(Section, key, defkey.ToString())); }
            catch (Exception) { }
            return defkey;
        }

        public void SetValue(string Section, string key, string value)
        {
            key = key.ToLower();
            Section = Section.ToLower();

            if (!_sections.Contains(Section))
                _sections.Add(Section, new Hashtable());

            Hashtable keyvalpair = (Hashtable)_sections[Section];

            if (keyvalpair.Contains(key))
                keyvalpair[key] = value;
            else
                keyvalpair.Add(key, value);
        }
        public void SetValue(string Section, string key, float value)
        {
            SetValue(Section, key, value.ToString());
        }
        public void SetValue(string Section, string key, double value)
        {
            SetValue(Section, key, value.ToString());
        }
        public void SetValue(string Section, string key, byte value)
        {
            SetValue(Section, key, value.ToString());
        }
        public void SetValue(string Section, string key, Int16 value)
        {
            SetValue(Section, key, value.ToString());
        }
        public void SetValue(string Section, string key, Int32 value)
        {
            SetValue(Section, key, value.ToString());
        }
        public void SetValue(string Section, string key, Int64 value)
        {
            SetValue(Section, key, value.ToString());
        }
        public void SetValue(string Section, string key, char value)
        {
            SetValue(Section, key, value.ToString());
        }
        public void SetValue(string Section, string key, UInt16 value)
        {
            SetValue(Section, key, value.ToString());
        }
        public void SetValue(string Section, string key, UInt32 value)
        {
            SetValue(Section, key, value.ToString());
        }
        public void SetValue(string Section, string key, UInt64 value)
        {
            SetValue(Section, key, value.ToString());
        }

        public void DeleteSection(string Section)
        {
            Section = Section.ToLower();

            if (_sections.Contains(Section))
                _sections.Remove(Section);
        }
        public void DeleteSections()
        {
            _sections.Clear();
        }
        #endregion
    }
}