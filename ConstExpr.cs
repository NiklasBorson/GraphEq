using System.Collections.Generic;

namespace GraphEq
{
    // Expression with a constant value.
    sealed class ConstExpr : Expr
    {
        double m_value;

        public ConstExpr(double value)
        {
            m_value = value;
        }

        public override double Eval(double[] args) => m_value;
        public override bool IsConstant => true;

        public override bool IsEquivalent(Expr other)
        {
            return (other as ConstExpr)?.m_value == m_value;
        }
    }

    // Predefined named constants.
    static class Constants
    {
        public static readonly ConstExpr E = new ConstExpr(double.E);
        public static readonly ConstExpr Pi = new ConstExpr(double.Pi);
        public static readonly ConstExpr NaN = new ConstExpr(double.NaN);
        public static readonly ConstExpr PositiveInfinity = new ConstExpr(double.PositiveInfinity);
        public static readonly ConstExpr True = new ConstExpr(1);
        public static readonly ConstExpr False = NaN;

        public static readonly Dictionary<string, ConstExpr> NamedConstants = new Dictionary<string, ConstExpr>
        {
            { "e", E },
            { "pi", Pi },
            { "NaN", NaN },
            { "inf", PositiveInfinity },
            { "True", True },
            { "False", False },
        };
    }
}
