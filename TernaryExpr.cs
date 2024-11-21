namespace GraphEq
{
    // Represents expression of the form: condition ? first : second.
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

        public override double Eval(double[] args) => ToBool(m_condition.Eval(args)) ? 
            m_first.Eval(args) : 
            m_second.Eval(args);

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
