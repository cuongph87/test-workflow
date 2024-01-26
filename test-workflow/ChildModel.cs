using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test_workflow
{
    /// <summary>
    /// A class for child objects.
    /// </summary>
    internal class ChildModel
    {
        public bool Growable { get; set; }
        
        // Age of the child
        public int Age { get; set; }

        // Salary
        private int Salary { get; set; }

        // Secrets of the child
        private string Secrets { get; set; }
    }
}
