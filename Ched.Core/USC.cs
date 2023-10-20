using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core
{
    public class USC
    {
        public List<USCObject> objects;
        public double offset = 0;


        public USC(double offset) { 
            objects = new List<USCObject>();
        }


    }
}
