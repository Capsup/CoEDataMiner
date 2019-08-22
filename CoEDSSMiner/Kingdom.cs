using System;
using System.Collections.Generic;
using System.Text;

namespace CoEDataMiner
{
    public class Kingdom
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _id;
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }
    }
}
