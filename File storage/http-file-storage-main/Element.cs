using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace file_storage
{
    public class Element
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public Element(string Name, string Type)
        {
            this.Name = Name;
            this.Type = Type;
        }

    }
}
