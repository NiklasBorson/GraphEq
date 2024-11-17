using System;
using System.Collections.Generic;

namespace GraphEq
{
    enum Precedence
    {
        None,
        AddSub,
        MulDiv,
        Power,
        UnaryNegative,
        Atomic
    }

    abstract class Expr
    {
        public abstract double Eval();
        public abstract bool IsConstant { get; }
        public virtual Expr Simplify() => this;
        public virtual Precedence Precedence => Precedence.Atomic;
    }

    sealed class ConstExpr : Expr
    {
        public ConstExpr(double value)
        {
            Value = value;
        }

        public double Value { get; }

        public override double Eval() => Value;
        public override bool IsConstant => true;
    }

    sealed class NamedConstExpr : Expr
    {
        public NamedConstExpr(string name, double value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public double Value { get; }
        public override double Eval() => Value;
        public override bool IsConstant => true;
        
        public static readonly NamedConstExpr[] Constants =
        {
            new NamedConstExpr("pi", Math.PI),
            new NamedConstExpr("e", Math.E)
        };
    }

    sealed class VariableExpr : Expr
    {
        public VariableExpr(string name, double value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public double Value { get; set; }
        public override double Eval() => Value;
        public override bool IsConstant => false;
    }
}
