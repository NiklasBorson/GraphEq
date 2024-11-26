using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Windows.UI.Text;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace GraphEq
{
    class HelpBuilder
    {
        BlockCollection Blocks { get; }
        Dictionary<string, string> HelpStrings { get; }

        const double ListIndent = 30;
        const double ParaGap = 10;

        public static void InitializeHelp(BlockCollection blocks)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var builder = new HelpBuilder(assembly, blocks);

            using (var stream = assembly.GetManifestResourceStream("GraphEq.Assets.Help.txt"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    builder.ReadHelp(reader);
                }
            }
        }

        HelpBuilder(Assembly assembly, BlockCollection blocks)
        {
            HelpStrings = new Dictionary<string, string>();
            Blocks = blocks;

            using (var stream = assembly.GetManifestResourceStream("GraphEq.Assets.HelpStrings.txt"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        int i = line.IndexOf(',');
                        if (i > 0)
                        {
                            HelpStrings[line.Substring(0, i)] = line.Substring(i + 1);
                        }
                    }
                }
            }
        }

        void ReadHelp(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line[0] == '@')
                {
                    int i = line.IndexOf(' ');
                    string cmd = line.Substring(0, i);
                    string arg = line.Substring(i + 1);

                    switch (cmd)
                    {
                        case "@h":
                            AddHeading(arg);
                            break;

                        case "@term":
                            AddTerm(arg);
                            break;

                        case "@def":
                            AddDef(arg);
                            break;

                        case "@li":
                            AddListItem(arg);
                            break;

                        case "@inc":
                            ProcessInclude(arg);
                            break;
                    }
                }
                else
                {
                    AddParagraph(line);
                }
            }
        }

        void AddParagraph(string text)
        {
            var par = NewParagraph(text);
            par.Margin = new Thickness(0, 0, 0, ParaGap);
            Blocks.Add(par);
        }

        void AddHeading(string text)
        {
            var par = NewParagraph(text);
            par.FontWeight = new FontWeight(700);
            par.FontSize = 16;

            int c = Blocks.Count;
            if (c != 0)
            {
                double whitespace = Blocks[c - 1].Margin.Bottom;
                if (whitespace < ParaGap)
                {
                    par.Margin = new Thickness(0, ParaGap - whitespace, 0, 0);
                }
            }

            Blocks.Add(par);
        }

        void AddTerm(string text)
        {
            var par = NewParagraph(text);
            Blocks.Add(par);
        }

        void AddDef(string text)
        {
            var par = NewParagraph(text);
            par.Margin = new Thickness(ListIndent, 0, 0, ParaGap);
            Blocks.Add(par);
        }

        void AddListItem(string text)
        {
            AddListItem("  \x2022", text);
        }

        void AddListItem(string symbol, string text)
        {
            var par = NewParagraph($"{symbol}\t{text}");
            par.Margin = new Thickness(ListIndent, 0, 0, 0);
            par.TextIndent = -ListIndent;
            Blocks.Add(par);
        }

        void ProcessInclude(string arg)
        {
            switch (arg)
            {
                case "unops":
                    AddListItem("-", HelpStrings["unary-"]);
                    AddListItem("!", HelpStrings["unary!"]);
                    break;

                case "binops":
                    foreach (var op in BinaryOps.Operators.Values)
                    {
                        AddListItem(op.Symbol, HelpStrings[op.Symbol]);
                    }
                    break;

                case "funcs":
                    foreach (var item in FunctionDefs.Functions)
                    {
                        AddTerm(item.Value.Signature);
                        AddDef(HelpStrings[item.Key]);
                    }
                    break;

                case "consts":
                    foreach (var item in Constants.NamedConstants)
                    {
                        AddListItem(item.Key);
                    }
                    break;
            }
        }

        Paragraph NewParagraph(string text)
        {
            var par = new Paragraph();
            par.Inlines.Add(new Run { Text = text });
            return par;
        }
    }
}
