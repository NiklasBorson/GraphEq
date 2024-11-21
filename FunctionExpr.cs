using System.Collections.Generic;

namespace GraphEq
{
    // Abstract base class for expressions that invoke either user-defined
    // or intrinsic functions.
    abstract class FunctionExprBase : Expr
    {
        IList<Expr> m_args;

        public FunctionExprBase(IList<Expr> args)
        {
            m_args = args;
        }

        // Evaluates the argument expressions to compute argument values,
        // which the caller passes to the inner function.
        protected double[] GetInnerArgs(double[] args)
        {
            var innerArgs = new double[m_args.Count];
            for (int i = 0; i < m_args.Count; i++)
            {
                innerArgs[i] = m_args[i].Eval(args);
            }
            return innerArgs;
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

        // Determines if any of the argument expressions can be simplified.
        // If so, returns an array of simplified arguments.
        // Otherwise, returns null.
        protected Expr[] GetSimplifiedArgs()
        {
            Expr[] newArgs = null;

            for (int i = 0; i < m_args.Count; i++)
            {
                // Can this expression be simpilfied?
                var arg = m_args[i];
                var newArg = arg.Simplify();
                if (newArg != arg)
                {
                    // Lazily allocate the array.
                    if (newArgs == null)
                    {
                        newArgs = new Expr[m_args.Count];
                    }

                    // Store the simplified expression.
                    newArgs[i] = newArg;
                }
            }

            if (newArgs != null)
            {
                // At least one expression was simplified.
                // Copy any non-simplified expressions.
                for (int i = 0; i < m_args.Count; i++)
                {
                    if (newArgs[i] == null)
                    {
                        newArgs[i] = m_args[i];
                    }
                }
            }

            return newArgs;
        }

        protected bool AreArgsEquivalent(FunctionExprBase other)
        {
            if (other.m_args.Count != m_args.Count)
            {
                return false;
            }

            for (int i = 0; i < m_args.Count; i++)
            {
                if (!other.m_args[i].IsEquivalent(m_args[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // Expression that invokes a user-defined function.
    // The arguments are passed to an expression representing the function body.
    sealed class UserFunctionExpr : FunctionExprBase
    {
        Expr m_body;

        public UserFunctionExpr(Expr body, IList<Expr> args) : base(args)
        {
            m_body = body;
        }

        public override double Eval(double[] args) => m_body.Eval(GetInnerArgs(args));

        public override Expr Simplify()
        {
            if (IsConstant)
            {
                return new ConstExpr(Eval(null));
            }

            var newArgs = GetSimplifiedArgs();
            if (newArgs != null)
            {
                return new UserFunctionExpr(m_body, newArgs);
            }

            return this;
        }

        public override bool IsEquivalent(Expr other)
        {
            var expr = other as UserFunctionExpr;

            return expr != null &&
                expr.m_body.IsEquivalent(m_body) &&
                AreArgsEquivalent(expr);
        }
    }

    // Expression that invokes an intrinsic function.
    // The arguments are passed to a delegate.
    sealed class FunctionExpr : FunctionExprBase
    {
        public delegate double Op(double[] args);

        Op m_op;

        public FunctionExpr(Op op, IList<Expr> args) : base(args)
        {
            m_op = op;
        }

        public override double Eval(double[] args) => m_op(GetInnerArgs(args));

        public override Expr Simplify()
        {
            if (IsConstant)
            {
                return new ConstExpr(Eval(null));
            }

            var newArgs = GetSimplifiedArgs();
            if (newArgs != null)
            {
                return new FunctionExpr(m_op, newArgs);
            }

            return this;
        }

        public override bool IsEquivalent(Expr other)
        {
            var expr = other as FunctionExpr;

            return expr != null &&
                expr.m_op == m_op &&
                AreArgsEquivalent(expr);
        }
    }

    record UserFunctionDef(int ParamCount, Expr Body);

    record FunctionDef(string Signature, int ParamCount, FunctionExpr.Op Op);

    static class FunctionDefs
    {
        public static readonly Dictionary<string, FunctionDef> Functions = new Dictionary<string, FunctionDef>
        {
            { "sqrt", new FunctionDef(
                "sqrt(n)",
                1,
                (double[] args) => double.Sqrt(args[0])
                ) },
            { "sqr", new FunctionDef(
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
            { "in_range", new FunctionDef(
                "in_range(x,min,max)",
                3,
                (double[] args) => InRange(args[0], args[1], args[2])
                ) },
        };

        static double Clamp(double x, double min, double max)
        {
            return x < min ? min : x > max ? max : x;
        }
        static double Clip(double x, double min, double max)
        {
            return x >= min && x <= max ? x : double.NaN;
        }
        static double InRange(double x, double min, double max)
        {
            return Expr.FromBool(x >= min && x <= max);
        }
    }
}
