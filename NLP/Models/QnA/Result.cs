using System;
using System.Collections.Generic;
using System.Text;

namespace NLP.Models.QnA
{
    public class Result
    {
        public int question_id { get; set; }
        public int answer_id { get; set; }
        public string phrase { get; set; }
        public double relevance { get; set; }
        public double distance { get; set; }
    }
}
