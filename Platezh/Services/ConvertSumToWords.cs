using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platezh.Services
{
    internal class ConvertSumToWords
    {
        public static class NumberToWordsConverter
        {
            private static readonly string[] Units = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            private static readonly string[] Teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
            private static readonly string[] Tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
            private static readonly string[] Hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };
            private static readonly string[] Thousands = { "", "тысяча", "тысячи", "тысяч" };
            private static readonly string[] Millions = { "", "миллион", "миллиона", "миллионов" };

            public static string ConvertToWords(decimal number)
            {
                if (number == 0)
                    return "ноль рублей 00 копеек";

                long rubles = (long)Math.Floor(number);
                long kopecks = (long)Math.Round((number - rubles) * 100);

                string rublesStr = Convert(rubles) + " " + GetRublesWord(rubles);
                string kopecksStr = kopecks.ToString("00") + " " + GetKopecksWord(kopecks);

                return $"{rublesStr} {kopecksStr}";
            }

            private static string Convert(long number)
            {
                if (number == 0)
                    return "";

                string words = "";

                if ((number / 1000000) > 0)
                {
                    words += Convert(number / 1000000) + " " + GetScaleWord(number / 1000000, Millions) + " ";
                    number %= 1000000;
                }

                if ((number / 1000) > 0)
                {
                    words += ConvertThousand(number / 1000) + " " + GetScaleWord(number / 1000, Thousands) + " ";
                    number %= 1000;
                }

                if ((number / 100) > 0)
                {
                    words += Hundreds[number / 100] + " ";
                    number %= 100;
                }

                if (number > 0)
                {
                    if (number < 10)
                        words += Units[number] + " ";
                    else if (number < 20)
                        words += Teens[number - 10] + " ";
                    else
                    {
                        words += Tens[number / 10] + " ";
                        if ((number % 10) > 0) 
                            words += Units[number % 10] + " ";
                    }
                }

                return words.Trim();
            }

            private static string ConvertThousand(long number)
            {
                if (number == 0)
                    return "";

                string words = "";

                if ((number / 100) > 0)
                {
                    words += Hundreds[number / 100] + " ";
                    number %= 100;
                }

                if (number > 0)
                {
                    if (number < 10)
                    {
                        // Особый случай для тысяч (один -> одна, два -> две)
                        if (number == 1)
                            words += "одна ";
                        else if (number == 2)
                            words += "две ";
                        else
                            words += Units[number] + " ";
                    }
                    else if (number < 20)
                        words += Teens[number - 10] + " ";
                    else
                    {
                        words += Tens[number / 10] + " ";
                        if ((number % 10) > 0)
                        {
                            // Особый случай для тысяч (один -> одна, два -> две)
                            if (number % 10 == 1)
                                words += "одна ";
                            else if (number % 10 == 2)
                                words += "две ";
                            else
                                words += Units[number % 10] + " ";
                        }
                    }
                }

                return words.Trim();
            }

            private static string GetScaleWord(long number, string[] scale)
            {
                number %= 100;
                if (number > 20) number %= 10;

                switch (number)
                {
                    case 1: return scale[1];
                    case 2:
                    case 3:
                    case 4: return scale[2];
                    default: return scale[3];
                }
            }

            private static string GetRublesWord(long number)
            {
                number %= 100;
                if (number > 20) number %= 10;

                switch (number)
                {
                    case 1: return "рубль";
                    case 2:
                    case 3:
                    case 4: return "рубля";
                    default: return "рублей";
                }
            }

            private static string GetKopecksWord(long number)
            {
                number %= 100;
                if (number > 20) number %= 10;

                switch (number)
                {
                    case 1: return "копейка";
                    case 2:
                    case 3:
                    case 4: return "копейки";
                    default: return "копеек";
                }
            }
        }
    }
}
