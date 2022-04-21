using System;
using NLP;
using MySQL;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Spec
{
    class Program
    {
        static void Main(string[] args)
        {
            RunClassify(); //Text Categorization
            //RunQnA(); //Question-Answer 

            Console.ReadKey();
        }



        
        static void RunClassify()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            NLP.Classify.Experiment = "Test1";
            NLP.Classify.DbConnection = configuration["DefaultConnectionString"];
            NLP.Classify.ClearDb();
            NLP.Classify.Instance(1, 4, true);

            string[] list = new string[] {
                "Abaco Abobora Alopécia Tarturfo Mágico Malabarista Genótipo Abaco Abobora",
                "Abobora Coiso Tesoura Mágico Metro Beneficio Metro Abobora Janela Estrelar",
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


            NLP.Models.Category[] categories = NLP.Classify.Predict(tests[4], true);
            foreach (NLP.Models.Category category in categories)
            {
                Console.WriteLine($"category: {category.name} \t count: {category.count} \t weight_sum: {category.weigths_sum} \t weight_avg: {category.weigths_avg}  \t relevance_sum: {category.relevance_sum} \t relevance_avg: {category.relevance_avg}");
            }
        }




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
    }
}
