using System;
using System.Collections.Generic;
using System.Linq;
using StringUtils;

namespace NLP
{
    public class Tokenize
    {
        public static double word_pooling = 1d;
        public static int maxlength = 0;
        public static bool soundex = false;


        public Tokenize()
        {

        }

        public static Tokenize Instance()
        {
            return new Tokenize();
        }

        public static Tokenize Instance(double word_pooling, int maxlength, bool sondex = false)
        {
            Tokenize.word_pooling = word_pooling;
            Tokenize.maxlength = maxlength;
            Tokenize.soundex = sondex;
            return new Tokenize();
        }


        #region Text
        public Models.Token[] Apply(string text)
        {
            string[] list = text.Split(new char[] { ' ', '\t' });
            var result = list.GroupBy(k => k, StringComparer.InvariantCultureIgnoreCase);

            List<Models.Token> tokens = new List<Models.Token>();
            foreach (var value in result)
            {
                string key = word_pooling < 1 ? WordPooling(value.Key, word_pooling) : value.Key;
                if(soundex)
                {
                    key = Soundex.Apply(key, maxlength);
                }

                if(maxlength > 0)
                {
                    if(key.Length > maxlength)
                    {
                        key = key.Substring(0, maxlength);
                    }
                }
                tokens.Add(new Models.Token() { word = key, count = value.Count() });
            }

            return tokens.Distinct().ToArray();
        }


        public Models.Token[] Apply(string text, string[] ignore)
        {
            text = Sanitize.CustomApply(text, ignore);
            return Apply(text);
        }


        public Models.Token[] Apply(string text, bool ignore)
        {
            if (ignore)
            {
                text = Sanitize.HardApply(text);
            }
            else
            {
                text = Sanitize.Apply(text);
            }
            return Apply(text);
        }
        #endregion Text




        #region Functions
        public static string WordPooling(string word, double rate)
        {
            int len = word.Length;
            if (len <= 4) return word;

            int pad = (int)Math.Ceiling((double)len * rate);
            return word.Substring(0, pad);
        }


        public static double TextWeights(string text, Models.Token[] tokens, double pooling_rate)
        {
            text = Sanitize.Apply(text);

            double sum = 0d;
            for (int c = 0; c < tokens.Length; c++)
            {
                //tokens[c].weight = (float)Regex.Matches(text, removerAcentos(tokens[c].word)).Count;
                if (tokens[c].weight > 0) Console.WriteLine(">>>> " + tokens[c].word + " " + tokens[c].weight);
            }


            for (int c = 0; c < tokens.Length; c++)
            {
                //if(tokens[c].weight > 0) Console.WriteLine(">>>> " + tokens[c].word + " " + tokens[c].weight);
                sum += tokens[c].weight > 0 ? 1 : 0;

                if (c < tokens.Length - 1)
                {
                    for (int i = c; i < tokens.Length; i++)
                    {
                        if (i != c && tokens[c].weight > 0 && tokens[i].weight > 0)
                        {
                            int dist = EmbedDistance(text, WordPooling(tokens[c].word, pooling_rate), WordPooling(tokens[i].word, pooling_rate));
                            //if (tokens[c].weight > 0) Console.WriteLine(">>>> " + tokens[c].word + " " + tokens[i].word + " " + tokens[c].weight + " Distance: " + dist);
                            sum += ((double)tokens.Length / (double)(dist + 1));
                        }
                    }
                }
            }

            return sum;
        }


        public static int EmbedDistance(string s, string w1, string w2)
        {
            if (w1.Equals(w2))
            {
                return 0;
            }

            string[] words = s.Split(new char[] { ' ', '\t' });
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = WordPooling(words[i], 0.6f);
            }


            int p1 = Array.IndexOf(words, w1, 0);
            if (p1 == -1) return 0;

            int p2 = Array.IndexOf(words, w2, p1);
            int p3 = Array.LastIndexOf(words, w2, p1);
            if (p2 == -1 && p3 == -1) return 0;

            int a1 = Math.Abs(p2 - p1);
            int a2 = Math.Abs(p3 - p1);
            int dist = a1 < a2 ? a1 : a2;
            return dist;
        }


        public static int Distance(string s, string w1, string w2)
        {
            if (w1.Equals(w2))
            {
                return 0;
            }
            String[] words = s.Split(new char[] { ' ', '\t' });
            int n = words.Length;
            int min_dist = n + 1;

            int prev = 0, i = 0;
            for (i = 0; i < n; i++)
            {

                if (words[i].Equals(w1) || words[i].Equals(w2))
                {
                    prev = i;
                    break;
                }
            }

            // Traverse after the first occurrence  
            while (i < n)
            {
                if (words[i].Equals(w1) || words[i].Equals(w2))
                {

                    // If the current element matches with  
                    // any of the two then check if current  
                    // element and prev element are different  
                    // Also check if this value is smaller than  
                    // minimum distance so far  
                    if ((!words[prev].Equals(words[i])) &&
                                    (i - prev) < min_dist)
                    {
                        min_dist = i - prev - 1;
                        prev = i;
                    }
                    else
                    {
                        prev = i;
                    }
                }
                i += 1;

            }
            return min_dist;
        }

        #endregion Functions
    }
}
