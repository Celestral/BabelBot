using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabelBot
{
    public class Config
    {
        public Server Server { get; set; }
        public List<Role> Roles { get; set; }
    }

    public class Server
    {
        public string Name { get; set; }
        public ulong GuildID { get; set; }
        public bool LimitEncryption { get; set; }
        public bool LimitDecryption { get; set; }
        public bool LimitButtonDecryption { get;set; }
    }

    public class Role
    {
        public string Name { get; set; }
        public ulong ID { get; set; }
        public bool CanEncrypt { get; set; }
        public bool CanDecrypt { get; set; }
        public bool CanButtonDecrypt { get; set; }
    }
}
