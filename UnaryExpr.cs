namespace GraphEq
{
    sealed class UnaryExpr : Expr
    {
        public delegate double Op(double arg);

        Op m_op;
        Expr m_arg;

        public UnaryExpr(Op op, Expr arg)
        {
            m_op = op;
            m_arg = arg;
        }

        public override double Eval(double[] args)
        {
            double value = m_arg.Eval(args);
            return m_op(value);
        }

        public override bool IsConstant => m_arg.IsConstant;

        public override Expr Simplify()
        {
            if (IsConstant)
            {
                return new ConstExpr(Eval(null));
            }

            var arg = m_arg.Simplify();
            if (arg != m_arg)
            {
                return new UnaryExpr(m_op, arg);
            }

            return this;
        }

        public override bool IsEquivalent(Expr other)
        {
            var expr = other as UnaryExpr;
            return expr != null && 
                expr.m_op == m_op && 
                expr.m_arg.IsEquivalent(m_arg);
        }
    }

    static class UnaryOps
    {
        public static readonly UnaryExpr.Op UnaryMinus = (double arg) => -arg;
        public static readonly UnaryExpr.Op UnaryNot = (double arg) => Expr.FromBool(!Expr.ToBool(arg));
    }
}
