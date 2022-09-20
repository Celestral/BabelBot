using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelBot
{
    internal class AlphabetBabelDictionary
    {
        public static Dictionary<char, string> alphaBabelDictionary = new Dictionary<char, string>();

        public static void FillDictionary()
        {
            for (int i = 1; i <= 26; i++)
            {
                alphaBabelDictionary.Add((char)(i+96), "babel" + i);
            }
            alphaBabelDictionary.Add(',', "babel27");
            alphaBabelDictionary.Add('?', "babel28");
            alphaBabelDictionary.Add(';', "babel29");
            alphaBabelDictionary.Add('.', "babel30");
            alphaBabelDictionary.Add(':', "babel31");
            alphaBabelDictionary.Add('!', "babel32");
            alphaBabelDictionary.Add('\"', "babel33");
            alphaBabelDictionary.Add('\'', "babel34");

        }
    }
}
