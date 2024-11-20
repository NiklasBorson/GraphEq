﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GraphEq
{
    enum Precedence
    {
        None,
        BoolOr,
        BoolAnd,
        Comparison,
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

        // Conversion of double to Boolean number.
        public static bool ToBool(double n) => double.IsRealNumber(n) && n != 0.0;
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

        public static readonly ConstExpr E = new ConstExpr(double.E);
        public static readonly ConstExpr Pi = new ConstExpr(double.Pi);
        public static readonly ConstExpr NaN = new ConstExpr(double.NaN);
        public static readonly ConstExpr PositiveInfinity = new ConstExpr(double.PositiveInfinity);

        public static readonly Dictionary<string, ConstExpr> NamedConstants = new Dictionary<string, ConstExpr>
        {
            { "e", E },
            { "pi", Pi },
            { "NaN", NaN },
            { "inf", PositiveInfinity },
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

    record FunctionDef(string Signature, int ParamCount, Function Func);

    record BinaryOp(SymbolId Symbol, Precedence Precedence, Function Func);

    sealed class FunctionExpr : Expr
    {
        Function m_func;
        IList<Expr> m_args;

        public FunctionExpr(Function func, IList<Expr> args)
        {
            m_func = func;
            m_args = args;
        }

        public FunctionExpr(BinaryOp op, Expr left, Expr right) : this(
            op.Func, new Expr[] { left, right }
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

            return new FunctionExpr(m_func, newArgs);
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

        // Built-in functions.
        public static readonly Dictionary<string, FunctionDef> Functions = new Dictionary<string, FunctionDef>
        {
            { "sqrt",
            new FunctionDef(
                "sqrt(n)",
                1,
                (double[] args) => double.Sqrt(args[0])
                ) },
            { "sqr",
            new FunctionDef(
                "sqr(n)",
                1,
                (double[] args) => args[0] * args[0]
                ) },
            { "log10", new FunctionDef(
                "log10(n)",
                1,
                (double[] args) => double.Log10(args[0])
                ) },
            { "log2", new FunctionDef(
                "log2(n)",
                1,
                (double[] args) => double.Log2(args[0])
                ) },
            { "ln", new FunctionDef(
                "ln(n)",
                1,
                (double[] args) => double.Log(args[0])
                ) },
            { "exp", new FunctionDef(
                "exp(n)",
                1,
                (double[] args) => double.Exp(args[0])
                ) },
            { "exp10", new FunctionDef(
                "exp10(n)",
                1,
                (double[] args) => double.Exp10(args[0])
                ) },
            { "exp2", new FunctionDef(
                "exp2(n)",
                1,
                (double[] args) => double.Exp2(args[0])
                ) },
            { "sin", new FunctionDef(
                "sin(n)",
                1,
                (double[] args) => double.Sin(args[0])
                ) },
            { "cos", new FunctionDef(
                "cos(n)",
                1,
                (double[] args) => double.Cos(args[0])
                ) },
            { "tan", new FunctionDef(
                "tan(n)",
                1,
                (double[] args) => double.Tan(args[0])
                ) },
            { "asin", new FunctionDef(
                "asin(n)",
                1,
                (double[] args) => double.Asin(args[0])
                ) },
            { "acos", new FunctionDef(
                "acos(n)",
                1,
                (double[] args) => double.Acos(args[0])
                ) },
            { "atan", new FunctionDef(
                "atan(n)",
                1,
                (double[] args) => double.Atan(args[0])
                ) },
            { "atan2", new FunctionDef(
                "atan2(x,y)",
                2,
                (double[] args) => double.Atan2(args[0], args[1])
                ) },
            { "abs", new FunctionDef(
                "abs(n)",
                1,
                (double[] args) => double.Abs(args[0])
                ) },
            { "max", new FunctionDef(
                "max(a,b)",
                2,
                (double[] args) => double.Max(args[0], args[1])
                ) },
            { "min", new FunctionDef(
                "min(a,b)",
                2,
                (double[] args) => double.Min(args[0], args[1])
                ) },
            { "round", new FunctionDef(
                "round(n)",
                1,
                (double[] args) => double.Round(args[0])
                ) },
            { "floor", new FunctionDef(
                "floor(n)",
                1,
                (double[] args) => double.Floor(args[0])
                ) },
            { "ceil", new FunctionDef(
                "ceil(n)",
                1,
                (double[] args) => double.Ceiling(args[0])
                ) },
            { "trunc", new FunctionDef(
                "trunc(n)",
                1,
                (double[] args) => double.Truncate(args[0])
                ) },
            { "clamp", new FunctionDef(
                "clamp(x,min,max)",
                3,
                (double[] args) => Clamp(args[0], args[1], args[2])
                ) },
            { "clip", new FunctionDef(
                "clip(x,min,max)",
                3,
                (double[] args) => Clip(args[0], args[1], args[2])
                ) },
        };

        static double Clamp(double x, double min, double max)
        {
            return x < min ? min : x > max ? max : x;
        }
        static double Clip(double x, double min, double max)
        {
            return x < min ? double.NaN : x > max ? double.NaN : x;
        }

        // Unary operators.
        public static readonly Function UnaryMinus = (double[] args) => -args[0];
        public static readonly Function UnaryNot = (double[] args) => (args[0] == 0) ? 1.0 : 0.0;

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
                ),
            new BinaryOp(
                SymbolId.BoolOr,
                Precedence.BoolOr,
                (double[] args) => ToBool(args[0]) || ToBool(args[1]) ? 1.0 : 0.0
                ),
            new BinaryOp(
                SymbolId.BoolAnd,
                Precedence.BoolAnd,
                (double[] args) => ToBool(args[0]) && ToBool(args[1]) ? 1.0 : 0.0
                ),
            new BinaryOp(
                SymbolId.LessThan,
                Precedence.Comparison,
                (double[] args) => (args[0] < args[1]) ? 1.0 : 0.0
                ),
            new BinaryOp(
                SymbolId.LessThanOrEqual,
                Precedence.Comparison,
                (double[] args) => (args[0] <= args[1]) ? 1.0 : 0.0
                ),
            new BinaryOp(
                SymbolId.Equals,
                Precedence.Comparison,
                (double[] args) => (args[0] == args[1]) ? 1.0 : 0.0
                ),
            new BinaryOp(
                SymbolId.NotEqual,
                Precedence.Comparison,
                (double[] args) => (args[0] != args[1]) ? 1.0 : 0.0
                ),
            new BinaryOp(
                SymbolId.GreaterThan,
                Precedence.Comparison,
                (double[] args) => (args[0] > args[1]) ? 1.0 : 0.0
                ),
            new BinaryOp(
                SymbolId.GreaterThanOrEqual,
                Precedence.Comparison,
                (double[] args) => (args[0] >= args[1]) ? 1.0 : 0.0
                ),
        };
    }

    sealed class TernaryExpr : Expr
    {
        Expr m_condition;
        Expr m_first;
        Expr m_second;

        public TernaryExpr(Expr condition, Expr first, Expr second)
        {
            m_condition = condition;
            m_first = first;
            m_second = second;
        }

        public override double Eval(double[] paramValues)
        {
            if (ToBool(m_condition.Eval(paramValues)))
            {
                return m_first.Eval(paramValues);
            }
            else
            {
                return m_second.Eval(paramValues);
            }
        }

        public override bool IsConstant =>
            m_condition.IsConstant &&
            m_first.IsConstant &&
            m_second.IsConstant;

        public override Expr Simplify()
        {
            var condition = m_condition.Simplify();
            var first = m_first.Simplify();
            var second = m_second.Simplify();

            if (condition == m_condition && first == m_first && second == m_second)
            {
                return this;
            }
            else
            {
                return new TernaryExpr(condition, first, second);
            }
        }

        public override bool IsEquivalent(Expr other)
        {
            var expr = other as TernaryExpr;
            if (expr == null)
                return false;

            return m_condition.IsEquivalent(expr.m_condition) &&
                m_first.IsEquivalent(expr.m_first) &&
                m_second.IsEquivalent(expr.m_second);
        }
    }
}
