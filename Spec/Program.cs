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


            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            NLP.Classify.Experiment = "Test1";
            NLP.Classify.TrainType = NLP.Train.Database;
            NLP.Classify.DbConnection = configuration["DefaultConnectionString"];
            NLP.Classify.ClearDb();


            /*
            NLP.Classify.TrainCategory(list[0], "test1", true);
            NLP.Classify.TrainCategory(list[1], "test1", true);
            NLP.Classify.TrainCategory(list[2], "test1", true);
            */
            Console.ReadKey();

            NLP.Classify.TrainCategoryGroup(list, "test1", true);
            NLP.Classify.TrainCategoryGroup(list1, "test2", true);

            NLP.Models.Category[] categories = NLP.Classify.PredictCategory(tests[2], true);
            foreach (NLP.Models.Category category in categories)
            {
                Console.WriteLine($"category: {category.name} \t count: {category.count} \t weight_sum: {category.weigths_sum} \t weight_avg: {category.weigths_avg}  \t relevance_sum: {category.relevance_sum} \t relevance_avg: {category.relevance_avg}");
            }


           Console.ReadKey();
        }
    }
}
