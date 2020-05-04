using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace CalloutsPlus
{
    public class IniFile
    {
        public string path;

        [DllImport("KERNEL32.DLL")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("KERNEL32.DLL")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public IniFile(string INIPath)
        {
            path = INIPath;
        }

        public void IniWriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, this.path);
        }

        public string IniReadValue(string section, string key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, " ", temp, 255, this.path);
            return temp.ToString();
        }
    }
}
