using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace AForge.WindowsForms
{
    public static class MorzeGenerator
    {

        private static void DrawDot(Graphics gr, double x, double y, double width) => gr.FillEllipse(Brushes.Black, (float)x, (float)y, (float)width, (float)width);
        private static void DrawDash(Graphics gr, double x, double y, double width, double height) => gr.FillRectangle(Brushes.Black, (float)x, (float)y, (float)width, (float)height);

        private static void DrawRandomCode(Graphics gr, Random rnd, int width, string code)
        {
            var code_width = (rnd.Next(60, 90) / 100.0 * width) / 5 * code.Length;  //кол-во пикселей на кодировку
            var dots_in_dashes = rnd.Next(20, 50) / 10.0;  //размер точки относительно тире
            var dash_height_coef = rnd.Next(6, 12) / 100.0;  //высота тире относительно ширины

            var dot_count = code.Count(c => c == '.');  //кол-во точек
            var dash_count = code.Count(c => c == '-'); //кол-во тире

            var dot_width = code_width * 1.0 / ((dot_count + code.Length - 1) + dots_in_dashes * dash_count);
            var dash_width = dot_width * dots_in_dashes;
            var dash_height = dash_height_coef * dash_width;

            var max_height = System.Math.Max(dot_width, dash_height);  //максимальная возможная высота кода
            var x_space = (width-code_width)/2 + rnd.Next(-(int)(width - code_width)/4, (int)(width - code_width)/4); //отступ по оси Х
            var y_space = rnd.Next(0, (int)(width - max_height)); //отступ по оси Y

            double x = x_space, y = y_space;
            foreach (var c in code)
            {
                if (c == '.')
                {
                    DrawDot(gr, x, y, dot_width);
                    x += dot_width * 2;
                }
                else if (c == '-')
                {
                    DrawDash(gr, x, y, dash_width, dash_height);
                    x += dot_width + dash_width;
                }
                else
                    throw new InvalidDataException("не точка и не тире!");
            }
        }

        public static void MakeExamples(int width = 100,int count_examples = 10)
        {
            Random rnd = new Random();
            char[] alphabet = { 'а', 'б', 'в', 'г', 'д', 'е', 'ж', 'з', 'и', 'к' };// 'й','л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ш', 'щ', 'ъ', 'ы', 'ь', 'э', 'ю', 'я' };

            Bitmap img = new Bitmap(width, width);
            Graphics gr = Graphics.FromImage(img);
            var p = new Pen(Brushes.Black);
            Directory.CreateDirectory("Morze");
            foreach (var letter in alphabet)
            {
                Directory.CreateDirectory($"Morze\\{letter}");
                for (int i = 0; i < count_examples; i++)
                {
                    gr.Clear(Color.White);
                    switch (letter)
                    {
                        case 'а': DrawRandomCode(gr, rnd, width, ".-"); break;
                        case 'б': DrawRandomCode(gr, rnd, width, "-..."); break;
                        case 'в': DrawRandomCode(gr, rnd, width, ".--"); break;
                        case 'г': DrawRandomCode(gr, rnd, width, "--."); break;
                        case 'д': DrawRandomCode(gr, rnd, width, "-.."); break;
                        case 'е': DrawRandomCode(gr, rnd, width, "."); break;
                        case 'ж': DrawRandomCode(gr, rnd, width, "...-"); break;
                        case 'з': DrawRandomCode(gr, rnd, width, "--.."); break;
                        case 'и': DrawRandomCode(gr, rnd, width, ".."); break;
                        case 'й': DrawRandomCode(gr, rnd, width, ".---"); break;
                        case 'к': DrawRandomCode(gr, rnd, width, "-.-"); break;
                        case 'л': DrawRandomCode(gr, rnd, width, ".-.."); break;
                        case 'м': DrawRandomCode(gr, rnd, width, "--"); break;
                        case 'н': DrawRandomCode(gr, rnd, width, "-."); break;
                        case 'о': DrawRandomCode(gr, rnd, width, "---"); break;
                        case 'п': DrawRandomCode(gr, rnd, width, ".--."); break;
                        case 'р': DrawRandomCode(gr, rnd, width, ".-."); break;
                        case 'с': DrawRandomCode(gr, rnd, width, "..."); break;
                        case 'т': DrawRandomCode(gr, rnd, width, "-"); break;
                        case 'у': DrawRandomCode(gr, rnd, width, "..-"); break;
                        case 'ф': DrawRandomCode(gr, rnd, width, "..-."); break;
                        case 'х': DrawRandomCode(gr, rnd, width, "...."); break;
                        case 'ш': DrawRandomCode(gr, rnd, width, "----"); break;
                        case 'щ': DrawRandomCode(gr, rnd, width, "--.-"); break;
                        case 'ь': DrawRandomCode(gr, rnd, width, "-..-"); break;
                        case 'ы': DrawRandomCode(gr, rnd, width, "-.--"); break;
                        case 'ъ': DrawRandomCode(gr, rnd, width, "--.--"); break;
                        case 'ц': DrawRandomCode(gr, rnd, width, "-.-."); break;
                        case 'э': DrawRandomCode(gr, rnd, width, "..-.."); break;
                        case 'ю': DrawRandomCode(gr, rnd, width, "..--"); break;
                        case 'я': DrawRandomCode(gr, rnd, width, ".-.-"); break;
                        case 'ч': DrawRandomCode(gr, rnd, width, "---."); break;
                    }
                    img.Save($"Morze\\{letter}\\{i}.jpg");
                }

            }
        }
    }
}
