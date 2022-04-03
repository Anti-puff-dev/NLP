using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NLP
{
    public class Sanitize
    {
        private static string[] ignore_defaults = new string[] { "das", "da", "de", "do", " no "," nos ", " na ", " nas ", " a ", " e ", " em ", " com ", " que ", " ao ", " aos " };

        public static string RemoverAcentos(string texto)
        {
            string comAcentos = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇç";
            string semAcentos = "AAAAAAaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUuuuuCc";

            for (int i = 0; i < comAcentos.Length; i++)
            {
                texto = texto.Replace(comAcentos[i].ToString(), semAcentos[i].ToString());
            }
            return texto;
        }


        public static string Apply(string s)
        {
            s = Regex.Replace(s, @"\p{P}", "");
            s = Regex.Replace(s, "[^0-9a-zA-Zà-úÀ-Ú.]+", " ").ToLower();
            s = s.Replace("\r\n", " ");
            s = s.Replace("\n\r", " ");
            s = s.Replace("\n", " ");
            s = s.Replace("\r", " ");
            s = s.Replace(Environment.NewLine, " ");
            s = RemoverAcentos(s.Replace("  ", " "));
            s = s.Replace("  ", " ").ToLower();
            return s;
        }


        public static string HardApply(string s)
        {
            s = Regex.Replace(s, @"\p{P}", "");
            s = Regex.Replace(s, "[^0-9a-zA-Zà-úÀ-Ú]+", " ").ToLower();
            s = s.Replace("\r\n", " ");
            s = s.Replace("\n\r", " ");
            s = s.Replace("\n", " ");
            s = s.Replace("\r", " ");
            s = s.Replace(Environment.NewLine, " ");
            s = RemoverAcentos(s.Replace("  ", " "));
            s = s.Replace(" das ", " ");
            s = s.Replace(" da ", " ");
            s = s.Replace(" de ", " ");
            s = s.Replace(" do ", " ");
            s = s.Replace(" na ", " ");
            s = s.Replace(" nas ", " ");
            s = s.Replace(" no ", " ");
            s = s.Replace(" nos ", " ");
            s = s.Replace(" em ", " ");
            s = s.Replace(" o ", " ");
            s = s.Replace(" os ", " ");
            s = s.Replace(" é ", " ");
            s = s.Replace(" e ", " ");
            s = s.Replace(" com ", " ");
            s = s.Replace(" que ", " ");
            s = s.Replace(" ao ", " ");
            s = s.Replace(" aos ", " ");
            s = s.Replace("  ", " ").ToLower();
            return s;
        }


        public static string CustomApply(string s, string[] ignore)
        {
            s = Regex.Replace(s, @"\p{P}", "");
            s = Regex.Replace(s, "[^0-9a-zA-Zà-úÀ-Ú.]+", " ").ToLower();
            s = s.Replace("\r\n", " ");
            s = s.Replace("\n\r", " ");
            s = s.Replace("\n", " ");
            s = s.Replace("\r", " ");
            s = s.Replace(Environment.NewLine, " ");
            s = RemoverAcentos(s.Replace("  ", " "));
            s = s.Replace("  ", " ").ToLower();

            foreach(string ig in ignore)
            {
                s = s.Replace(" " + ig + " ", " ");
            }

            return s;
        }
    }
}

