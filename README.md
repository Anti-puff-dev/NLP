# NLP - Natural Language Processing
### Simplification Methods with Database MySQL Query-Object Class (https://github.com/Anti-puff-dev/MySQL)

# Important Dlls Imports  
MySQL.dll  (https://github.com/Anti-puff-dev/MySQL)
StringUtils.dll  (https://github.com/Anti-puff-dev/StringUtils)

# Nuget Packs  
MySQL.Data 8.0.28  
Newtonsoft.Json 13.0.1  

# NLP version 1 Functions  
Text Classification / Categorization  - Modes: Realtime Memory / Database (MySQL/MariaDB)  
QnA - Answer the Questions  - Database (MySQL/MariaDB)  

Create in bin appsettings.json  
```
{
  "DefaultConnectionString": "server=localhost; user id=root; password=pwd; port=3306; database=dbname; Allow Zero Datetime=True;Allow User Variables=True;CharSet=utf8;",
  "CacheConnectionString": "server=localhost; user id=root; password=pwd; port=3306; database=dbname; Allow Zero Datetime=True;Allow User Variables=True;CharSet=utf8;"
}
```


# Run Spec Project to Tests

### Text Classify
```
static void RunClassify()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            NLP.Classify.Experiment = "Test1";
            NLP.Classify.TrainType = NLP.Train.Runtime;
            NLP.Classify.DbConnection = configuration["DefaultConnectionString"];
            NLP.Classify.ClearDb();

            string[] list = new string[] {
                "Abaco Abobora Alopécia Tarturfo Mágico Malabarista Genótipo Abaco Abobora",
                "Metro Abobora Coiso Tesoura Mágico Beneficio Metro Abobora Janela Estrelar",
                "Casa Metro Abobora Dinamite Determinante Alecrim Metrica",
            };

            string[] list1 = new string[] {
                "Abaco Amora Bronco Setembro Janela Betoneira Joelho Maça Bronco",
                "Amarelo Jardineira Bronco Cabeça Peça Ovo Orelha Telhado Fone",
                "Ardosia Branco Azul Gorjeta Amora Gelado Quente Amora Janela",
            };


            string[] tests = new string[] {
                "Pedra Janela Cabana Peça Abraço Verde Azul Amarelo Feijão",
                "Geladeira Amora Caçamba Pedregulho Veneza Jardim Acustico",
                "Viola Casa Alegre Perto Longe Inato Zebra Sapo Telhado",
                "Badejo Abobora Leitão Pedra Ovo Magico Geraldo Casa",
                "Brometo Jato Metro Trágico Abaco Estrelar Navio Queijo"
            };



            //NLP.Classify.TrainCategory(list[0], "test1", true);
            //NLP.Classify.TrainCategory(list[1], "test1", true);
            //NLP.Classify.TrainCategory(list[2], "test1", true);
            //Console.ReadKey();

            NLP.Classify.TrainCategoryGroup(list, "test1", true);
            NLP.Classify.TrainCategoryGroup(list1, "test2", true);

            Console.WriteLine("-------------------------------------------------------------------------");


            NLP.Models.Category[] categories = NLP.Classify.Predict(tests[0], true);
            foreach (NLP.Models.Category category in categories)
            {
                Console.WriteLine($"category: {category.name} \t count: {category.count} \t weight_sum: {category.weigths_sum} \t weight_avg: {category.weigths_avg}  \t relevance_sum: {category.relevance_sum} \t relevance_avg: {category.relevance_avg}");
            }
        }
```

### QnA
```
static void RunQnA()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            NLP.QnA.Experiment = "Test1";
            NLP.QnA.DbConnection = configuration["DefaultConnectionString"];
            NLP.QnA.ClearDb();



            string[] list = new string[] {
                "qual o seu nome?",
                "qual seu nome",
                "como você se chama?",
                "como vc se chama?"
            };

            string[] list1 = new string[] {
                "qual a melhor maneira de voar?",
                "como eu posso voar?",
                "como posso fazer para voar?"
            };


            string[] tests = new string[] {
                "qual seu nome?",
                "diga seu nome",
                "quero voar",
                "como faço pra voar",
            };



            NLP.QnA.Train(list, "Meu nome é Anti-puff");
            NLP.QnA.Train(list1, "Use um avião");

            Console.WriteLine($">>> {NLP.QnA.Predict(tests[0])}");
            Console.WriteLine($">>> {NLP.QnA.Predict(tests[1])}");
            Console.WriteLine($">>> {NLP.QnA.Predict(tests[2])}");
            Console.WriteLine($">>> {NLP.QnA.Predict(tests[3])}");
        }
```

