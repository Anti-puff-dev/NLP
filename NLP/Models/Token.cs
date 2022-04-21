using System;
using System.Collections.Generic;
using System.Text;

namespace NLP.Models
{
    public class Token
    {
        public int id { get; set; }
        public string word { get; set; }
        public int category_id { get; set; }
        public int count { get; set; }
        public double weight { get; set; }
        public double relevance { get; set; }
    }

    public class DbToken
    {
        public int id { get; set; }
        public string word { get; set; }
        public string soundex_t { get; set; }
        public string soundex_full { get; set; }
        public string grammar { get; set; }
        public string gender { get; set; }
        public int verbo { get; set; }
        public float weight { get; set; }
        public int isname { get; set; }
    }
}
