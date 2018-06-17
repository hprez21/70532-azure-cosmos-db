using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureCosmosDBDemo.Models
{
    public class Student
    {
        public string Name { get; set; }
        public List<Subject> Subjects { get; set; }
    }
}
