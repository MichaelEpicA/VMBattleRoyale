using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VM_Battle_Royale
{
    class VMBRFormatHandler
    {
        private static string substring;

        public static string GetValue(string vmbrdata, string key)
        {
            MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(vmbrdata));
            using (StreamReader reader = new StreamReader(stream))
            {

                string line = reader.ReadLine();
                if (line == null)
                {
                    return "dc";
                }
                int index = line.IndexOf(key);
                //int index = line.IndexOf(key);
                if (index == -1)
                {

                }
                else
                {
                    string removedkey = line.Remove(0, index + key.Length + 1);
                    int indexremoved = removedkey.IndexOf(" ");
                    if (indexremoved == -1 || key == "response")
                    {
                        return removedkey;
                    }
                    int lastcharacter = indexremoved - 1 + 1;
                    substring = removedkey.Substring(0, lastcharacter);
                }
                /*else
                {
                    int indexofspace = line.IndexOf(" ");
                    while (indexofspace > index)
                    {
                        line.Remove(indexofspace, 1);
                        indexofspace = line.IndexOf(" ");
                    }
                    int valuecorrect = (index + key.Length + 1);
                    if (valuecorrect <= -1)
                    {
                        int equation = index + key.Length + 1;
                        substring = line.Substring(equation);
                        if (substring.IndexOf(" ") != -1)
                        {
                            substring = substring.Remove(substring.Length - 1);
                        }
                    }
                    else
                    {
                        substring = line.Substring(valuecorrect, valuecorrect);

                    }
                }*/

            }
            return substring;
        }

        public static string CreateVMBRFormat(Dictionary<string, string> dictionary, bool multiline = false)
        {
            if (multiline)
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, string> kvp in dictionary)
                {
                    sb.Append(kvp.Key + "=" + kvp.Value + "\n");
                }
                return sb.ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, string> kvp in dictionary)
                {
                    sb.Append(kvp.Key + "=" + kvp.Value + " ");
                }
                return sb.ToString();
            }
        }

        public static string CreateVMBRFormat(string key, string value)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(key + "=" + value);
            return sb.ToString();
        }

        public static string GetValue(byte[] vmbrdata, string key)
        {
            MemoryStream stream = new MemoryStream(vmbrdata);
            using (StreamReader reader = new StreamReader(stream))
            {

                string line = reader.ReadLine();
                if (line == null)
                {
                    return "dc";
                }
                int index = line.IndexOf(key);
                //int index = line.IndexOf(key);
                if (index == -1)
                {

                }
                else
                {
                    string removedkey = line.Remove(0, index + key.Length + 1);
                    int indexremoved = removedkey.IndexOf(" ");
                    if (indexremoved == -1 || key == "response")
                    {
                        return removedkey;
                    }
                    int lastcharacter = indexremoved - 1 + 1;
                    substring = removedkey.Substring(0, lastcharacter);
                }
                /*else
                {
                    int indexofspace = line.IndexOf(" ");
                    while (indexofspace > index)
                    {
                        line.Remove(indexofspace, 1);
                        indexofspace = line.IndexOf(" ");
                    }
                    int valuecorrect = (index + key.Length + 1);
                    if (valuecorrect <= -1)
                    {
                        int equation = index + key.Length + 1;
                        substring = line.Substring(equation);
                        if (substring.IndexOf(" ") != -1)
                        {
                            substring = substring.Remove(substring.Length - 1);
                        }
                    }
                    else
                    {
                        substring = line.Substring(valuecorrect, valuecorrect);

                    }
                }*/

            }
            return substring;
        }
    }
}
