using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GraphEq
{
    enum Precedence
    {
        None,
        AddSub,
        MulDiv,
        Power,
        UnaryPrefix,
        Atomic
    }

    abstract class Expr
    {
        public abstract double Eval(double[] paramValues);
        public abstract bool IsConstant { get; }
        public virtual Expr Simplify() => this;
        public abstract bool IsEquivalent(Expr other);
        public virtual Precedence Precedence => Precedence.Atomic;
    }

    sealed class ConstExpr : Expr
    {
        double m_value;

        public ConstExpr(double value)
        {
            m_value = value;
        }

        public override double Eval(double[] paramValues) => m_value;
        public override bool IsConstant => true;

        public override bool IsEquivalent(Expr other)
        {
            return (other as ConstExpr)?.m_value == m_value;
        }

        public static readonly Dictionary<string, ConstExpr> NameConstants = new Dictionary<string, ConstExpr>
        {
            { "e", new ConstExpr(double.E) },
            { "pi", new ConstExpr(double.Pi) }
        };
    }

    sealed class VariableExpr : Expr
    {
        int m_index;

        public VariableExpr(int index)
        {
            m_index = index;
        }

        public override double Eval(double[] paramValues) => paramValues[m_index];
        public override bool IsConstant => false;

        public override bool IsEquivalent(Expr other)
        {
            return (other as VariableExpr)?.m_index == m_index;
        }
    }

    delegate double Function(double[] paramValues);

    record FunctionDef(string Name, int ParamCount, Function Func);

    record BinaryOp(SymbolId Symbol, Precedence Precedence, Function Func);

    sealed class FunctionExpr : Expr
    {
        Function m_func;
        Precedence m_precedence;
        IList<Expr> m_args;

        public FunctionExpr(Function func, Precedence precedence, IList<Expr> args)
        {
            m_func = func;
            m_precedence = precedence;
            m_args = args;
        }

        public FunctionExpr(BinaryOp op, Expr left, Expr right) : this(
            op.Func, op.Precedence, new Expr[] { left, right }
            )
        {
        }

        public override double Eval(double[] paramValues)
        {
            var argValues = new double[m_args.Count];
            for (int i = 0; i < m_args.Count; i++)
            {
                argValues[i] = m_args[i].Eval(paramValues);
            }
            return m_func(argValues);
        }

        public override bool IsConstant
        {
            get
            {
                foreach (var arg in m_args)
                {
                    if (!arg.IsConstant)
                        return false;
                }
                return true;
            }
        }

        public override Expr Simplify()
        {
            if (IsConstant)
            {
                return new ConstExpr(Eval(null));
            }

            Expr[] newArgs = null;

            for (int i = 0; i < m_args.Count; i++)
            {
                var arg = m_args[i];
                var newArg = arg.Simplify();
                if (newArg != arg)
                {
                    if (newArgs == null)
                    {
                        newArgs = new Expr[m_args.Count];
                    }
                    newArgs[i] = newArg;
                }
            }

            if (newArgs == null)
            {
                return this;
            }

            for (int i = 0; i < m_args.Count; i++)
            {
                if (newArgs[i] == null)
                {
                    newArgs[i] = m_args[i];
                }
            }

            return new FunctionExpr(m_func, m_precedence, newArgs);
        }

        public override bool IsEquivalent(Expr other)
        {
            var expr = other as FunctionExpr;
            if (expr == null)
                return false;

            if (expr.m_func != m_func || expr.m_args.Count != m_args.Count)
                return false;

            for (int i = 0; i < m_args.Count; i++)
            {
                if (!expr.m_args[i].IsEquivalent(m_args[i]))
                    return false;
            }

            return true;
        }

        public override Precedence Precedence => m_precedence;

        // Built-in functions.
        public static readonly FunctionDef[] Functions =
        {
            new FunctionDef(
                "sqrt",
                1,
                (double[] args) => double.Sqrt(args[0])
                ),
            new FunctionDef(
                "ln",
                1,
                (double[] args) => double.Log(args[0])
                ),
            new FunctionDef(
                "log10",
                1,
                (double[] args) => double.Log10(args[0])
                ),
            new FunctionDef(
                "log2",
                1,
                (double[] args) => double.Log2(args[0])
                ),
            new FunctionDef(
                "sin",
                1,
                (double[] args) => double.Sin(args[0])
                ),
            new FunctionDef(
                "cos",
                1,
                (double[] args) => double.Cos(args[0])
                ),
            new FunctionDef(
                "tan",
                1,
                (double[] args) => double.Tan(args[0])
                )
        };

        // Binary operators.
        public static readonly BinaryOp[] BinaryOperators = new BinaryOp[]
        {
            new BinaryOp(
                SymbolId.Plus,
                Precedence.AddSub,
                (double[] args) => args[0] + args[1]
                ),
            new BinaryOp(
                SymbolId.Minus,
                Precedence.AddSub,
                (double[] args) => args[0] - args[1]
                ),
            new BinaryOp(
                SymbolId.Multiply,
                Precedence.MulDiv,
                (double[] args) => args[0] * args[1]
                ),
            new BinaryOp(
                SymbolId.Divide,
                Precedence.MulDiv,
                (double[] args) => args[0] / args[1]
                ),
            new BinaryOp(
                SymbolId.Caret,
                Precedence.Power,
                (double[] args) => double.Pow(args[0], args[1])
                )
        };
    }
}
