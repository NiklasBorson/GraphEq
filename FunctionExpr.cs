using System;
using System.Collections.Generic;

namespace GraphEq
{
    internal class UnaryFunctionExpr : Expr
    {
        public UnaryFunctionExpr(string name, Expr arg, UnaryFunc func)
        {
            Name = name;
            Arg = arg;
            Func = func;
        }

        public string Name { get; }
        public Expr Arg { get; }
        public UnaryFunc Func { get; }
        public override bool IsConstant => Arg.IsConstant;
        public override double Eval() => Func.Invoke(Arg.Eval());

        public override Expr Simplify()
        {
            if (IsConstant)
            {
                return new ConstExpr(Eval());
            }

            var arg = Arg.Simplify();
            if (arg != Arg)
            {
                return new UnaryFunctionExpr(Name, arg, Func);
            }

            return this;
        }

        public override Precedence Precedence => Precedence.Atomic;

        public static readonly KeyValuePair<string, UnaryFunc>[] Functions =
        {
            new KeyValuePair<string, UnaryFunc>("sqrt", (double arg) => Math.Sqrt(arg)),
            new KeyValuePair<string, UnaryFunc>("ln", (double arg) => Math.Log(arg)),
            new KeyValuePair<string, UnaryFunc>("log10", (double arg) => Math.Log10(arg)),
            new KeyValuePair<string, UnaryFunc>("log2", (double arg) => Math.Log2(arg)),
            new KeyValuePair<string, UnaryFunc>("sin", (double arg) => Math.Sin(arg)),
            new KeyValuePair<string, UnaryFunc>("cos", (double arg) => Math.Cos(arg)),
            new KeyValuePair<string, UnaryFunc>("tan", (double arg) => Math.Tan(arg)),
        };
    }
}
