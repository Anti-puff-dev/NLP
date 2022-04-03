using System;
using System.Collections.Generic;
using System.Text;

namespace NLP.Models
{
    public class Category
    {
        public string name { get; set; }
        public int count { get; set; }
        public double weigths_sum { get; set; }
        public double weigths_avg { get; set; }
        public double relevance_sum { get; set; }
        public double relevance_avg { get; set; }
    }
}
