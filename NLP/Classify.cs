using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MySQL;
using StringUtils;

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



    public class Classify
    {
        public static string Experiment = "default";
        public static string ExperimentId = "";
        public static string DbTable = "nlp_dataset";
        public static double TrainingRate = Math.E;
        public static double TrainingRateDecay = 1.1;
        public static string DbConnection
        {
            get => MySQL.DbConnection.ConnString;
            set { MySQL.DbConnection.ConnString = value; }
        }

        public static double word_pooling = 1d;
        public static int maxlength = 0;
        public static bool soundex = false;



        public Classify()
        {
            
        }

        public static Classify Instance()
        {
            return new Classify();
        }

        public static Classify Instance(double word_pooling, int maxlength, bool sondex = false)
        {
            Classify.word_pooling = word_pooling;
            Classify.maxlength = maxlength;
            Classify.soundex = sondex;
            return new Classify();
        }



        #region Train
        #region Train.Category
        public static void TrainCategory(string text, string[] words)
        {
            Models.Token[] category_tokens = null;
            Models.Token[] tokensArr = Tokenize.Instance(word_pooling, maxlength, soundex).Apply(text);
            Models.Token[] tokens = Relevances(Weights(tokensArr));


            foreach(string word in words)
            {
                int category_id = Convert.ToInt32(Data.Query($"SELECT category_id FROM {DbTable}_categories WHERE name=?name", new string[] { word }).Tables[0].Rows[0][0]);

                foreach (Models.Token token in tokens)
                {
                    token.category_id = category_id;
                }

                category_tokens = MySQL.Json.Select.Fill($"SELECT * FROM {DbTable} WHERE category_id=?category_id AND experiment_id=?experiment_id ORDER BY word ASC", new string[] { category_id.ToString(), ExperimentId }).Multiple<Models.Token>();

                bool isfirst = false;

                if (category_tokens != null)
                {
                    if (category_tokens.Count() > 0)
                    {
                        isfirst = false;
                        Models.Token[] intersect_tokens = Intersect(tokens, category_tokens);
                        Models.Token[] different_tokens = Diference(tokens, category_tokens);

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


                        string query = "";
                        List<string> parms = new List<string>();

                        int c = 0;

                        foreach (Models.Token token in intersect_tokens.Concat(different_tokens))
                        {
                            query += $"INSERT INTO {DbTable} (experiment_id, word, category_id, {DbTable}.count, weight, relevance) VALUES (?experiment_i{c}, ?word_i{c}, ?category_i{c}, ?count_i{c}, ?weight_i{c}, ?relevance_i{c}) ON DUPLICATE KEY UPDATE {DbTable}.count=?count_u{c}, weight=?weight_u{c}, relevance=?relevance_u{c};";
                            parms.Add(ExperimentId);
                            parms.Add(token.word);
                            parms.Add(category_id.ToString());
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
                    else
                    {
                        isfirst = true;
                    }
                }
                else
                {
                    isfirst = true;
                }

                //Console.WriteLine("First: " + isfirst);


                if (isfirst)
                {
                    string query = "";
                    List<string> parms = new List<string>();

                    int c = 0;
                    foreach (Models.Token token in tokens)
                    {
                        query += $"INSERT INTO {DbTable} (experiment_id, word, category_id, {DbTable}.count, weight, relevance) VALUES (?experiment_i{c}, ?word_i{c}, ?category_i{c}, ?count_i{c}, ?weight_i{c}, ?relevance_i{c}) ON DUPLICATE KEY UPDATE {DbTable}.count=?count_u{c}, weight=?weight_u{c}, relevance=?relevance_u{c};";
                        parms.Add(ExperimentId);
                        parms.Add(token.word);
                        parms.Add(category_id.ToString());
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


                #region Debug
                /*Console.WriteLine(">>> ");
                foreach (Models.Token token in RuntimeTokens.OrderByDescending(o => o.weight))
                {     
                    Console.WriteLine($"word: {token.word} \t category: { token.category} \t count: {token.count} \t weight: {token.weight} \t relevance: {token.relevance}");
                }*/
                #endregion Debug
            }
        }


        public static void TrainCategory(string text, string[] words, string[] ignore)
        {
            text = Sanitize.CustomApply(text, ignore);
            TrainCategory(text, words);
        }


        public static void TrainCategory(string text, string[] words, bool ignore)
        {
            if (ignore)
            {
                text = Sanitize.HardApply(text);
            }
            else
            {
                text = Sanitize.Apply(text);
            }

            TrainCategory(text, words);
        }


        public static void TrainCategoryGroup(string[] texts, string[] words, int epochs = 1)
        {
            Console.WriteLine("Start Training Group...");
            TrainingRate = 1 + ((TrainingRate - 1) / epochs);

            DbPopulateExperimentsCategory(words);

            for (int j = 0; j < epochs; j++)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    texts[i] = Sanitize.Apply(texts[i]);
                    TrainCategory(texts[i], words);
                }
            }
        }


        public static void TrainCategoryGroup(string[] texts, string[] words, string[] ignore, int epochs = 1)
        {
            Console.WriteLine("Start Training Group...");
            TrainingRate = 1 + ((TrainingRate - 1) / epochs);

            DbPopulateExperimentsCategory(words);

            for (int j = 0; j < epochs; j++)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    texts[i] = Sanitize.CustomApply(texts[i], ignore);
                    TrainCategory(texts[i], words);
                }
            }  
        }


        public static void TrainCategoryGroup(string[] texts, string[] words, bool ignore, int epochs = 1)
        {
            Console.WriteLine("Start Training Group...");
            TrainingRate = 1 + ((TrainingRate - 1) / epochs );

            DbPopulateExperimentsCategory(words);


            for (int j = 0; j < epochs; j++)
            {
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
                    TrainCategory(texts[i], words);
                }
            }
        }

        #endregion Train.Category
        #endregion Train



        #region Predict
        public static Models.Category[] Predict(string text, int subcategories_levels = 1, int results = 10)
        {
            Models.Token[] tokens = Relevances(Weights(Tokenize.Instance(word_pooling, maxlength, soundex).Apply(text)));
            List<Models.Token[]> list = new List<Models.Token[]>();
            List<Models.Category> list_categories = new List<Models.Category>();

            Models.Token[] _tokens = tokens.OrderByDescending(i => i.weight).ToArray();
            if (String.IsNullOrEmpty(ExperimentId)) DbPopulateExperiment();

            if(subcategories_levels > 0)
            {
                subcategories_levels--;

                foreach (Models.Token token in _tokens)
                {
                    Models.Token[] word_tokens = MySQL.Json.Select.Fill($"SELECT nlp_dataset.*  FROM {DbTable} INNER JOIN {DbTable}_categories ON {DbTable}_categories.category_id={DbTable}.category_id AND {DbTable}_categories.experiment_id={DbTable}.experiment_id AND {DbTable}_categories.parent_id=0 WHERE {DbTable}.experiment_id=?experiment_id AND word=?word ORDER BY weight DESC LIMIT 30", new string[] { ExperimentId, token.word }).Multiple<Models.Token>();
                    if (word_tokens != null && word_tokens.Length > 0) list.Add(word_tokens);
                }

                int c = 1;
                foreach (Models.Token[] token_list in list)
                {
                    Console.WriteLine($"LIST {c++} ------------------------------------------");

                    foreach (Models.Token token in token_list)
                    {
                        Models.Category cat = list_categories.Find(v => v.category_id == token.category_id);
                        if (cat != null)
                        {
                            cat.weigths_avg = (cat.weigths_avg + token.weight) / 2;
                            cat.weigths_sum += token.weight;
                            cat.relevance_avg = (cat.relevance_avg + token.relevance) / 2;
                            cat.relevance_sum += token.relevance;
                            cat.count++;

                            //Console.WriteLine($">>> weigths_sum: " + cat.weigths_sum + " cat.relevance_sum: " + cat.relevance_sum);
                            //Console.WriteLine($">>> token.weight " + token.weight + " token.relevance: " + token.relevance);
                        }
                        else
                        {
                            string category_name = Data.Query($"SELECT name FROM {DbTable}_categories WHERE category_id=?category_id", new string[] { token.category_id.ToString() }).Tables[0].Rows[0][0].ToString();
                            list_categories.Add(new Models.Category() { category_id = token.category_id, name = category_name, count = 1, weigths_sum = token.weight, weigths_avg = token.weight, relevance_sum = token.relevance, relevance_avg = token.relevance, subcategories = PredictSubCategory(_tokens, token.category_id, subcategories_levels, results) });
                        }

                        Console.WriteLine($"category_id: {token.category_id} \tword: {token.word} \t count: {token.count} \t weight: {token.weight} \t relevance: {token.relevance} \n");
                    }
                }

                //list_categories = list_categories.OrderByDescending(item => item.count).ThenByDescending(item => item.relevance_avg).Take(results).ToList();   
                list_categories = list_categories.OrderByDescending(item => item.relevance_avg/(1 + item.weigths_avg)).Take(results).ToList();
            }


            return list_categories.ToArray();
        }


        private static Models.Category[] PredictSubCategory(Models.Token[] tokens, int parent_id, int subcategories_levels, int results)
        {
            List<Models.Token[]> list = new List<Models.Token[]>();
            List<Models.Category> list_categories = new List<Models.Category>();

            if (subcategories_levels > 0)
            {
                subcategories_levels--;

                foreach (Models.Token token in tokens)
                {
                    Models.Token[] word_tokens = MySQL.Json.Select.Fill($"SELECT nlp_dataset.*  FROM {DbTable} INNER JOIN {DbTable}_categories ON {DbTable}_categories.category_id={DbTable}.category_id AND {DbTable}_categories.experiment_id={DbTable}.experiment_id AND {DbTable}_categories.parent_id=?parent_id WHERE {DbTable}.experiment_id=?experiment_id AND word=?word ORDER BY weight DESC LIMIT 30", new string[] { parent_id.ToString(), ExperimentId, token.word }).Multiple<Models.Token>();
                    if (word_tokens != null && word_tokens.Length > 0) list.Add(word_tokens);
                }

                int c = 1;
                foreach (Models.Token[] token_list in list)
                {
                    Console.WriteLine($"LIST {c++} ------------------------------------------");

                    foreach (Models.Token token in token_list)
                    {
                        Models.Category cat = list_categories.Find(v => v.category_id == token.category_id);
                        if (cat != null)
                        {
                            cat.weigths_avg = (cat.weigths_avg + token.weight) / 2;
                            cat.weigths_sum += token.weight;
                            cat.relevance_avg = (cat.relevance_avg + token.relevance) / 2;
                            cat.relevance_sum += token.relevance;
                            cat.count++;

                            //Console.WriteLine($">>> weigths_sum: " + cat.weigths_sum + " cat.relevance_sum: " + cat.relevance_sum);
                            //Console.WriteLine($">>> token.weight " + token.weight + " token.relevance: " + token.relevance);
                        }
                        else
                        {
                            string category_name = Data.Query($"SELECT name FROM {DbTable}_categories WHERE category_id=?category_id", new string[] { token.category_id.ToString() }).Tables[0].Rows[0][0].ToString();
                            list_categories.Add(new Models.Category() { category_id = token.category_id, name = category_name, count = 1, weigths_sum = token.weight, weigths_avg = token.weight, relevance_sum = token.relevance, relevance_avg = token.relevance, subcategories = PredictSubCategory(tokens, token.category_id, subcategories_levels, results) });
                        }

                        Console.WriteLine($"\t subcategory_id: {token.category_id} \tword: {token.word} \t count: {token.count} \t weight: {token.weight} \t relevance: {token.relevance}");
                    }
                }

                //list_categories = list_categories.OrderByDescending(item => item.count).ThenByDescending(item => item.relevance_avg).Take(results).ToList();
                list_categories = list_categories.OrderByDescending(item => item.relevance_avg / (1 + item.weigths_avg)).Take(results).ToList();
            }

            return list_categories.ToArray();
        }



        /*
        public static Models.Category[] Predict(string text, int subcategories_levels = 1, int results = 10)
        {
            Models.Token[] tokens = Relevances(Weights(Tokenize.Instance(word_pooling, maxlength, soundex).Apply(text)));
            List<Models.Token[]> list = new List<Models.Token[]>();
            List<Models.Category> list_categories = new List<Models.Category>();

            Models.Token[] _tokens = tokens.OrderByDescending(i => i.weight).ToArray();
            if (String.IsNullOrEmpty(ExperimentId)) DbPopulateExperiment();

            foreach (Models.Token token in _tokens)
            {
                Models.Token[] word_tokens = MySQL.Json.Select.Fill($"SELECT * FROM {DbTable} WHERE experiment_id=?experiment_id AND word=?word ORDER BY weight DESC LIMIT 30", new string[] { ExperimentId, token.word }).Multiple<Models.Token>();
                if (word_tokens != null && word_tokens.Length > 0) list.Add(word_tokens);
            }
           

            int c = 1;
            foreach (Models.Token[] token_list in list)
            {
                Console.WriteLine($"LIST {c++} ------------------------------------------");

                foreach (Models.Token token in token_list)
                {
                    Models.Category cat = list_categories.Find(v => v.category_id == token.category_id);
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
                        string category_name = Data.Query($"SELECT name FROM {DbTable}_categories WHERE category_id=?category_id", new string[] { token.category_id.ToString() }).Tables[0].Rows[0][0].ToString();
                        list_categories.Add(new Models.Category() { category_id = token.category_id, name = category_name, count = 1, weigths_sum = token.weight, weigths_avg = token.weight, relevance_sum = token.relevance, relevance_avg = token.relevance });
                    }

                    Console.WriteLine($"category_id: {token.category_id} \tword: {token.word} \t count: {token.count} \t weight: {token.weight} \t relevance: {token.relevance} \n");
                }
            }

            list_categories = list_categories.OrderByDescending(item => item.count).ThenByDescending(item => item.relevance_avg).Take(results).ToList();


            #region Debug
            Console.WriteLine(">>> ");
            foreach (Models.Category category in list_categories)
            {
                Console.WriteLine($"category: {category.name} \t count: {category.count} \t weight_sum: {category.weigths_sum} \t weight_avg: {category.weigths_avg}  \t relevance_sum: {category.relevance_sum} \t relevance_avg: {category.relevance_avg}");
            }
            #endregion Debug

            return list_categories.ToArray();
        }*/


        public static Models.Category[] Predict(string text, bool ignore = true, int subcategories_levels = 1, int results = 10)
        {
            if (ignore)
            {
                text = Sanitize.HardApply(text);
            }
            else
            {
                text = Sanitize.Apply(text);
            }

            return Predict(text, subcategories_levels, results);
        }


        public static Models.Category[] Predict(string text, string[] ignore, int subcategories_levels = 1, int results = 10)
        {
            text = Sanitize.CustomApply(text, ignore);
            return Predict(text, subcategories_levels, results);
        }
        #endregion Predict



        #region Functions
        public static Models.Token[] Weights(Models.Token[] tokens)
        {
            int maxCount = tokens.OrderByDescending(i => i.count).First().count;
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
            Data.Query($"TRUNCATE TABLE {DbTable};TRUNCATE TABLE {DbTable}_experiments;TRUNCATE TABLE {DbTable}_categories;");
        }


        public static void CleaExperiment()
        {
            if (String.IsNullOrEmpty(ExperimentId)) DbPopulateExperiment();
            Data.Query($"DELETE FROM {DbTable} WHERE experiment_id=?experiment_id", new string[] { ExperimentId });
            Data.Query($"DELETE FROM {DbTable}_experiments WHERE experiment_id=?experiment_id", new string[] { ExperimentId });
            Data.Query($"DELETE FROM {DbTable}_categories WHERE experiment_id=?experiment_id", new string[] { ExperimentId });
        }


        private static void DbPopulateExperimentsCategory(string[] words)
        {
            if (String.IsNullOrEmpty(ExperimentId))
            {
                Data.Query($"INSERT IGNORE INTO {DbTable}_experiments (name) VALUES (?name)", new string[] { Experiment });
                ExperimentId = Data.Query($"SELECT experiment_id FROM {DbTable}_experiments WHERE name=?name", new string[] { Experiment }).Tables[0].Rows[0][0].ToString();
            }


            string last_id = "0";
            foreach (string word in words)
            {
                Data.Query($"INSERT IGNORE INTO {DbTable}_categories (experiment_id, parent_id, name) VALUES (?experiment_id, ?last_id, ?name)", new string[] { ExperimentId, last_id, word });
                last_id = Data.Query($"SELECT category_id FROM {DbTable}_categories WHERE experiment_id=?experiment_id AND name=?word", new string[] { ExperimentId, word }).Tables[0].Rows[0][0].ToString();
            }
  
        }


        private static void DbPopulateExperiment()
        {
            if (String.IsNullOrEmpty(ExperimentId))
            {
                Data.Query($"INSERT IGNORE INTO {DbTable}_experiments (name) VALUES (?name)", new string[] { Experiment });
                ExperimentId = Data.Query($"SELECT experiment_id FROM {DbTable}_experiments WHERE name=?name", new string[] { Experiment }).Tables[0].Rows[0][0].ToString();
            }
        }
        #endregion Functions
    }
}
