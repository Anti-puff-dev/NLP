using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MySQL;

namespace NLP
{
    public static class Extensions
    {
        public static int IndexOf<T>( this IEnumerable<T> list, Predicate<T> condition)
        {
            int i = -1;
            return list.Any(x => { i++; return condition(x); }) ? i : -1;
        }

        public static IEnumerable<TSource> DistinctBy<TSource>(this IEnumerable<TSource> source, params Func<TSource, object>[] keySelectors)
        {
            // initialize the table
            var seenKeysTable = keySelectors.ToDictionary(x => x, x => new HashSet<object>());

            // loop through each element in source
            foreach (var element in source)
            {
                // initialize the flag to true
                var flag = true;

                // loop through each keySelector a
                foreach (var (keySelector, hashSet) in seenKeysTable)
                {
                    // if all conditions are true
                    flag = flag && hashSet.Add(keySelector(element));
                }

                // if no duplicate key was added to table, then yield the list element
                if (flag)
                {
                    yield return element;
                }
            }
        }
    }

    public enum Train
    {
        Database = 0,
        Runtime = 1
    }



    public class Classify
    {
        public static string Experiment = "default";
        public static Train TrainType = Train.Database;
        public static string DbTable = "nlp_dataset";
        public static string DbConnection {
            get => MySQL.DbConnection.ConnString;
            set { MySQL.DbConnection.ConnString = value; }
        }
        public static Models.Token[] RuntimeTokens = new Models.Token[0];
        public static double TrainingRate = 10;
        public static double TrainingRateDecay = 1.1;

        #region Train
        #region Train.Category
        public static void TrainCategory(string text, string word)
        {
            Models.Token[] category_tokens = null;
            Models.Token[] tokens = Relevances(Weights(Tokenize.Apply(text)));

            foreach (Models.Token token in tokens)
            {
                token.category = word;
            }


           if (TrainType == Train.Database)
            {
                category_tokens = MySQL.Json.Select.Fill($"SELECT * FROM {DbTable} WHERE category=?word AND experiment=?experiment ORDER BY word ASC", new string[] { word, Experiment }).Multiple<Models.Token>();

            } else if(TrainType == Train.Runtime)
            {
                category_tokens = RuntimeTokens.Where(c => c.category == word).OrderBy(i => i.word).ToArray();
            }

            bool isfirst = false;

            if(category_tokens != null)
            {
                if(category_tokens.Count() > 0)
                {
                    isfirst = false;
                    //string[] words = category_tokens.Select(item => item.word).ToArray<string>();
                    Models.Token[] intersect_tokens = Intersect(tokens, category_tokens);//tokens.Where(item => words.Contains(item.word)).ToArray<Models.Token>();
                    Models.Token[] different_tokens = Diference(tokens, category_tokens);//tokens.Where(item => !words.Contains(item.word)).ToArray<Models.Token>();

                    int maxCount = intersect_tokens.OrderByDescending(i => i.count).First().count;

                    foreach (Models.Token token in intersect_tokens)
                    {
                        //Console.WriteLine($"intersect word: {token.word}");
                        token.weight *= (TrainingRate) * (1 + (token.count / maxCount));
                        token.relevance *= (TrainingRate) * (1 + (token.count / maxCount));
                    }

                    foreach (Models.Token token in different_tokens)
                    {
                        //Console.WriteLine($"different word: {token.word}");
                        token.weight /= (TrainingRateDecay);
                        token.relevance /= (TrainingRateDecay);
                    }


                    if (TrainType == Train.Database)
                    {
                        string query = "";
                        List<string> parms = new List<string>();

                        int c = 0;

                        //RuntimeTokens = RuntimeTokens.Concat(tokens).GroupBy(x => new { x.category, x.word }).Select(x => x.First()).ToArray();

                        foreach (Models.Token token in RuntimeTokens)
                        {
                            query += $"INSERT INTO {DbTable} (experiment, word, category, {DbTable}.count, weight, relevance) VALUES (?experiment_i{c}, ?word_i{c}, ?category_i{c}, ?count_i{c}, ?weight_i{c}, ?relevance_i{c}) ON DUPLICATE KEY UPDATE {DbTable}.count=?count_u{c}, weight=?weight_u{c}, relevance=?relevance_u{c};";
                            parms.Add(Experiment);
                            parms.Add(token.word);
                            parms.Add(word);
                            parms.Add(token.count.ToString());
                            parms.Add(token.weight.ToString().Replace(",", "."));
                            parms.Add(token.relevance.ToString().Replace(",", "."));
                            parms.Add(token.count.ToString());
                            parms.Add(token.weight.ToString().Replace(",", "."));
                            parms.Add(token.relevance.ToString().Replace(",", "."));
                            c++;
                        }

                        Data.Query(query, parms.ToArray());
                    }
                    else if (TrainType == Train.Runtime)
                    {
                        RuntimeTokens = RuntimeTokens.Concat(tokens).GroupBy(x => new { x.category, x.word } ).Select(x => x.First()).ToArray();
                    }
                }
                else
                {
                    isfirst = true;
                }
            } else
            {
                isfirst = true;
            }

            //Console.WriteLine("First: " + isfirst);


            if (isfirst)
            {
                if (TrainType == Train.Database)
                {
                    string query = "";
                    List<string> parms = new List<string>();

                    int c = 0;
                    foreach (Models.Token token in tokens)
                    {
                        query += $"INSERT INTO {DbTable} (experiment, word, category, {DbTable}.count, weight, relevance) VALUES (?experiment_i{c}, ?word_i{c}, ?category_i{c}, ?count_i{c}, ?weight_i{c}, ?relevance_i{c}) ON DUPLICATE KEY UPDATE {DbTable}.count=?count_u{c}, weight=?weight_u{c}, relevance=?relevance_u{c};";
                        parms.Add(Experiment);
                        parms.Add(token.word);
                        parms.Add(word);
                        parms.Add(token.count.ToString());
                        parms.Add(token.weight.ToString().Replace(",","."));
                        parms.Add(token.relevance.ToString().Replace(",", "."));
                        parms.Add(token.count.ToString());
                        parms.Add(token.weight.ToString().Replace(",", "."));
                        parms.Add(token.relevance.ToString().Replace(",", "."));
                        c++;
                    }

                    Data.Query(query, parms.ToArray());
                    RuntimeTokens = RuntimeTokens.Concat(tokens).ToArray();
                }
                else if (TrainType == Train.Runtime)
                {
                    RuntimeTokens = RuntimeTokens.Concat(tokens).ToArray();
                }
            }



            /*#region Debug
            Console.WriteLine(">>> ");
            foreach (Models.Token token in RuntimeTokens.OrderByDescending(o => o.weight))
            {     
                Console.WriteLine($"word: {token.word} \t category: { token.category} \t count: {token.count} \t weight: {token.weight} \t relevance: {token.relevance}");
            }
            #endregion Debug*/
        }


        public static void TrainCategory(string text, string word, string[] ignore)
        {
            text = Sanitize.CustomApply(text, ignore);
            TrainCategory(text, word);
        }


        public static void TrainCategory(string text, string word, bool ignore)
        {
            if (ignore)
            {
                text = Sanitize.HardApply(text);
            }
            else
            {
                text = Sanitize.Apply(text);
            }

            TrainCategory(text, word);
        }


        public static void TrainCategoryGroup(string[] texts, string word, int epochs = 1)
        {
            Console.WriteLine("Start Training...");
            TrainingRate = 1 + ((TrainingRate - 1) / epochs);

            for (int j = 0; j < epochs; j++)
            {
                //Console.WriteLine($"\nEphoc {(j + 1)}");

                for (int i = 0; i < texts.Length; i++)
                {
                    texts[i] = Sanitize.Apply(texts[i]);
                    TrainCategory(texts[i], word);
                }
            }
        }


        public static void TrainCategoryGroup(string[] texts, string word, string[] ignore, int epochs = 1)
        {
            Console.WriteLine("Start Training...");
            TrainingRate = 1 + ((TrainingRate - 1) / epochs);

            for (int j = 0; j < epochs; j++)
            {
                //Console.WriteLine($"\nEphoc {(j + 1)}");

                for (int i = 0; i < texts.Length; i++)
                {
                    texts[i] = Sanitize.CustomApply(texts[i], ignore);
                    TrainCategory(texts[i], word);
                }
            }  
        }


        public static void TrainCategoryGroup(string[] texts, string word, bool ignore, int epochs = 1)
        {
            Console.WriteLine("Start Training...");
            TrainingRate = 1 + ((TrainingRate - 1) / epochs );

            for (int j = 0; j < epochs; j++)
            {
                //Console.WriteLine($"\nEphoc {(j+1)}");

                for (int i = 0; i < texts.Length; i++)
                {
                    if (ignore)
                    {
                        texts[i] = Sanitize.HardApply(texts[i]);
                    }
                    else
                    {
                        texts[i] = Sanitize.Apply(texts[i]);
                    }
                    TrainCategory(texts[i], word);
                }
            }
        }
        #endregion Train.Category
        #endregion Train


        #region Predict
        public static Models.Category[] Predict(string text, int results = 10)
        {
            Models.Token[] tokens = Relevances(Weights(Tokenize.Apply(text)));
            List<Models.Token[]> list = new List<Models.Token[]>();
            List<Models.Category> list_categories = new List<Models.Category>();

            Models.Token[] _tokens = tokens.OrderByDescending(i => i.weight).ToArray();

            if (TrainType == Train.Database)
            {
                foreach (Models.Token token in _tokens)
                {
                    Models.Token[] word_tokens = MySQL.Json.Select.Fill($"SELECT * FROM {DbTable} WHERE experiment=?experiment AND word=?word ORDER BY weight DESC LIMIT 30", new string[] { Experiment, token.word }).Multiple<Models.Token>();
                    //Console.WriteLine(word_tokens.Length); 
                    if (word_tokens != null && word_tokens.Length > 0) list.Add(word_tokens);
                }
            }
            else if (TrainType == Train.Runtime)
            {
                foreach (Models.Token token in _tokens)
                {
                    Models.Token[] word_tokens = RuntimeTokens.Where(i => i.word == token.word).OrderByDescending(i => i.weight).Take(30).ToArray();
                    //Console.WriteLine(word_tokens.Length);
                    if (word_tokens != null && word_tokens.Length > 0) list.Add(word_tokens);
                }
            }

            int c = 1;
            foreach (Models.Token[] token_list in list)
            {
                Console.WriteLine($"LIST {c++} ------------------------------------------");

                foreach (Models.Token token in token_list)
                {
                    Models.Category cat = list_categories.Find(v => v.name == token.category);
                    if (cat != null)
                    {
                        cat.weigths_avg = (cat.weigths_avg + token.weight) / 2;
                        cat.weigths_sum += token.weight;
                        cat.relevance_avg = (cat.relevance_avg + token.relevance) / 2;
                        cat.relevance_sum += token.relevance;
                        cat.count++;
                    }
                    else
                    {
                        list_categories.Add(new Models.Category() { name = token.category, count = 1, weigths_sum = token.weight, weigths_avg = token.weight, relevance_sum = token.relevance, relevance_avg = token.relevance });
                    }

                    Console.WriteLine($"category: {token.category} \tword: {token.word} \t count: {token.count} \t weight: {token.weight} \t relevance: {token.relevance} \n");
                }
            }

            list_categories = list_categories.OrderByDescending(item => item.count).ThenByDescending(item => item.relevance_avg).Take(results).ToList();


            #region Debug
            /*Console.WriteLine(">>> ");
            foreach (Models.Category category in list_categories)
            {
                Console.WriteLine($"category: {category.name} \t count: {category.count} \t weight_sum: {category.weigths_sum} \t weight_avg: {category.weigths_avg}  \t relevance_sum: {category.relevance_sum} \t relevance_avg: {category.relevance_avg}");
            }*/
            #endregion Debug

            return list_categories.ToArray();
        }


        public static Models.Category[] Predict(string text, bool ignore = true, int results = 10)
        {
            if (ignore)
            {
                text = Sanitize.HardApply(text);
            }
            else
            {
                text = Sanitize.Apply(text);
            }

            return Predict(text, results);
        }


        public static Models.Category[] Predict(string text, string[] ignore, int results = 10)
        {
            text = Sanitize.CustomApply(text, ignore);
            return Predict(text, results);
        }
        #endregion Predict



        #region Functions
        public static Models.Token[] Weights(Models.Token[] tokens)
        {
            int maxCount = 7; // tokens.OrderByDescending(i => i.count).First().count;
            //maxCount = maxCount < 7 ? 7 : maxCount;


            for (int i=0; i<tokens.Length; i++)
            {
                tokens[i].weight = (double)tokens[i].count / (double)maxCount;
                //Console.WriteLine($"{tokens[i].word} {tokens[i].weight} {tokens[i].count} {maxCount}");
            }

            return tokens;
        }


        public static Models.Token[] Relevances(Models.Token[] tokens, int maxDistance = 10)
        {
            Models.Token[] _tokens = tokens.OrderByDescending(i => i.weight).ToArray();

            for (int i = 0; i < _tokens.Length-1; i++)
            {
                int p1 = tokens.IndexOf(item => item.word.Equals(_tokens[i].word));
                int p2 = tokens.IndexOf(item => item.word.Equals(_tokens[i+1].word));


                int distance = Math.Abs(p2 - p1);
                _tokens[i].relevance = (double)_tokens[i].weight * ((double)maxDistance / (double)distance);
                _tokens[i+1].relevance = (double)_tokens[i+1].weight * ((double)maxDistance / (double)distance);
                //Console.WriteLine($">>> word: {_tokens[i].word} {tokens[i].word} \t word1: {_tokens[i+1].word} {tokens[i+1].word} \t distance: {distance}");
            }

            return tokens;
        }


        public static Models.Token[] Intersect(Models.Token[] arr1, Models.Token[] arr2)
        {
            List<Models.Token> result = new List<Models.Token>();
            foreach(Models.Token token1 in arr1)
            {
                foreach (Models.Token token2 in arr2)
                {
                    if(token1.word == token2.word)
                    {
                        token1.count += token2.count;
                        result.Add(token1);
                    }
                }
            }

            return result.ToArray();
        }


        public static Models.Token[] Diference(Models.Token[] arr1, Models.Token[] arr2)
        {
            List<Models.Token> result = new List<Models.Token>();


            foreach (Models.Token token1 in arr1)
            {
                bool has = false;
                Models.Token tmp = null;
                foreach (Models.Token token2 in arr2)
                {
                    if (token1.word == token2.word)
                    {
                        has = true;
                        tmp = null;
                        break;
                    }
                    else
                    {
                        tmp = token2;
                    }
                }

                if (!has)
                {
                    result.Add(token1);
                    if (tmp != null) result.Add(tmp);
                }
            }

            foreach (Models.Token token2 in arr2)
            {
                bool has = false;
                Models.Token tmp = null;

                foreach (Models.Token token1 in arr1)
                {
                    if (token1.word == token2.word)
                    {
                        has = true;
                        tmp = null;
                        break;
                    }
                    else
                    {
                        tmp = token1;
                    }
                }

                if (!has)
                {
                    result.Add(token2);
                    if (tmp != null) result.Add(tmp);
                }
            }




            return result.DistinctBy(t => t.word).ToArray();
        }


        public static void ClearDb()
        {
            Data.Query($"DELETE FROM {DbTable} WHERE experiment=?experiment", new string[] { Experiment });
        }
        #endregion Functions
    }
}
