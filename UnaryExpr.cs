namespace GraphEq
{
    delegate double UnaryFunc(double arg);

    record UnaryOp(UnaryFunc Invoke, string FormatString, Precedence Precedence);

    internal class UnaryExpr : Expr
    {
        public UnaryExpr(Expr arg, UnaryOp op)
        {
            Arg = arg;
            Operator = op;
        }
        public Expr Arg { get; }
        public UnaryOp Operator { get; }
        public override bool IsConstant => Arg.IsConstant;
        public override double Eval() => Operator.Invoke(Arg.Eval());

        public override Expr Simplify()
        {
            if (IsConstant)
            {
                return new ConstExpr(Eval());
            }

            var arg = Arg.Simplify();
            if (arg != Arg)
            {
                return new UnaryExpr(arg, Operator);
            }

            return this;
        }

        public override Precedence Precedence => Operator.Precedence;

        public static readonly UnaryOp NegativeOp = new UnaryOp(
            (double arg) => -arg,
            "-{0}",
            Precedence.UnaryNegative
            );
    }
}
