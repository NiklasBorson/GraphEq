using System;

namespace GraphEq
{
    delegate double BinaryFunc(double arg1, double arg2);

    record BinaryOp(BinaryFunc Invoke, string FormatString, Precedence Precedence);

    class BinaryExpr : Expr
    {
        public BinaryExpr(Expr arg1, Expr arg2, BinaryOp op)
        {
            Arg1 = arg1;
            Arg2 = arg2;
            Operator = op;
        }
        public Expr Arg1 { get; }
        public Expr Arg2 { get; }
        public BinaryOp Operator { get; }
        public override bool IsConstant => Arg1.IsConstant && Arg2.IsConstant;
        public override double Eval() => Operator.Invoke(Arg1.Eval(), Arg2.Eval());

        public override Expr Simplify()
        {
            if (IsConstant)
            {
                return new ConstExpr(Eval());
            }

            var arg1 = Arg1.Simplify();
            var arg2 = Arg2.Simplify();

            if (arg1 != Arg1 || arg2 != Arg2)
            {
                return new BinaryExpr(arg1, arg2, Operator);
            }

            return this;
        }

        public override Precedence Precedence => Operator.Precedence;

        public static readonly BinaryOp AddOp = new BinaryOp(
            (double arg1, double arg2) => arg1 + arg2,
            "{0} + {1}",
            Precedence.AddSub
            );
        public static readonly BinaryOp SubtractOp = new BinaryOp(
            (double arg1, double arg2) => arg1 - arg2,
            "{0} - {1}",
            Precedence.AddSub
            );
        public static readonly BinaryOp MultiplyOp = new BinaryOp(
            (double arg1, double arg2) => arg1 * arg2,
            "{0}*{1}",
            Precedence.MulDiv
            );
        public static readonly BinaryOp DivideOp = new BinaryOp(
            (double arg1, double arg2) => arg1 / arg2,
            "{0}/{1}",
            Precedence.MulDiv
            );
        public static readonly BinaryOp PowerOp = new BinaryOp(
            (double arg1, double arg2) => Math.Pow(arg1, arg2),
            "{0}^{1}",
            Precedence.Power
            );
    }
}
