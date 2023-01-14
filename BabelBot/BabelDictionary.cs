using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelBot
{
    public class BabelDictionary
    {
       public List<Pairing> Pairings { get; set; }
    }

    public class Pairing
    {
        public char Character { get; set; }
        public string EmoteName { get; set; }
    }
}
