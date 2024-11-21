﻿using System.Collections.Generic;

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

    sealed class BinaryExpr : Expr
    {
        public delegate double Op(double a, double b);

        Op m_op;
        Expr m_a;
        Expr m_b;

        public BinaryExpr(Op op, Expr a, Expr b)
        {
            m_op = op;
            m_a = a;
            m_b = b;
        }

        public override double Eval(double[] args)
        {
            double a = m_a.Eval(args);
            double b = m_b.Eval(args);
            return m_op(a, b);
        }

        public override bool IsConstant => m_a.IsConstant && m_b.IsConstant;

        public override Expr Simplify()
        {
            if (IsConstant)
            {
                return new ConstExpr(Eval(null));
            }

            var a = m_a.Simplify();
            var b = m_b.Simplify();

            if (a != m_a || b != m_b)
            {
                return new BinaryExpr(m_op, a, b);
            }

            return this;
        }

        public override bool IsEquivalent(Expr other)
        {
            var expr = other as BinaryExpr;
            return expr != null &&
                expr.m_op == m_op &&
                expr.m_a.IsEquivalent(m_a) &&
                expr.m_b.IsEquivalent(m_b);
        }
    }

    record BinaryOp(string Description, Precedence Precedence, BinaryExpr.Op Op);

    static class BinaryOps
    {
        public static readonly Dictionary<SymbolId, BinaryOp> Operators = new Dictionary<SymbolId, BinaryOp>
        {
            { SymbolId.Plus, new BinaryOp(
                " +   Plus",
                Precedence.AddSub,
                (double a, double b) => a + b
                ) },
            { SymbolId.Minus, new BinaryOp(
                " -   Minus",
                Precedence.AddSub,
                (double a, double b) => a - b
                ) },
            { SymbolId.Multiply, new BinaryOp(
                " *   Multiply",
                Precedence.MulDiv,
                (double a, double b) => a * b
                ) },
            { SymbolId.Divide, new BinaryOp(
                " /   Divide",
                Precedence.MulDiv,
                (double a, double b) => a / b
                ) },
            { SymbolId.Percent, new BinaryOp(
                " %   Modulo",
                Precedence.MulDiv,
                (double a, double b) => a % b
                ) },
            { SymbolId.Caret, new BinaryOp(
                " ^   Power",
                Precedence.Power,
                (double a, double b) => double.Pow(a, b)
                ) },
            { SymbolId.BoolOr, new BinaryOp(
                " ||  Logical OR",
                Precedence.BoolOr,
                (double a, double b) => Expr.FromBool(Expr.ToBool(a) || Expr.ToBool(b))
                ) },
            { SymbolId.BoolAnd, new BinaryOp(
                " &&  Logical AND",
                Precedence.BoolAnd,
                (double a, double b) => Expr.FromBool(Expr.ToBool(a) && Expr.ToBool(b))
                ) },
            { SymbolId.Equals, new BinaryOp(
                " =   Equal to",
                Precedence.Comparison,
                (double a, double b) => Expr.FromBool(a == b)
                ) },
            { SymbolId.NotEqual, new BinaryOp(
                " !=  Not equal to",
                Precedence.Comparison,
                (double a, double b) => Expr.FromBool(a != b)
                ) },
            { SymbolId.LessThan, new BinaryOp(
                " <   Less than",
                Precedence.Comparison,
                (double a, double b) => Expr.FromBool(a < b)
                ) },
            { SymbolId.LessThanOrEqual, new BinaryOp(
                " <=  Less than or equal to",
                Precedence.Comparison,
                (double a, double b) => Expr.FromBool(a <= b)
                ) },
            { SymbolId.GreaterThan, new BinaryOp(
                " >   Greater than",
                Precedence.Comparison,
                (double a, double b) => Expr.FromBool(a > b)
                ) },
            { SymbolId.GreaterThanOrEqual, new BinaryOp(
                " >=  Greater than or equal to",
                Precedence.Comparison,
                (double a, double b) => Expr.FromBool(a >= b)
                ) },
        };
    }
}
