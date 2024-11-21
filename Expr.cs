using System;
using System.Collections.Generic;

namespace GraphEq
{
    /// <summary>
    /// Abstract base class for all expressions.
    /// </summary>
    abstract class Expr
    {
        /// <summary>
        /// Evaluates the expression.
        /// </summary>
        /// <param name="args">
        /// Array of parameter values for the current scope. Variable expressions index into this array.
        /// </param>
        /// <returns>
        /// Returns the value of the expression.
        /// </returns>
        public abstract double Eval(double[] args);

        /// <summary>
        /// True if the expression is constant (i.e., does not depend on args pass to Eval).
        /// </summary>
        public abstract bool IsConstant { get; }

        /// <summary>
        /// Attempts to simplify the expression.
        /// </summary>
        /// <returns>
        /// Returns the simplified expression or 'this' if the expression cannot be simplified.
        /// </returns>
        public virtual Expr Simplify() => this;
        
        /// <summary>
        /// Determines whether this expression is equivalent to the specified expression.
        /// </summary>
        /// <param name="other">Other expression to compare with.</param>
        /// <returns>
        /// Returns true if the expressions are equivalent, or false otherwise.
        /// </returns>
        public abstract bool IsEquivalent(Expr other);

        // Boolean expressions return these values for True and False.
        public const double True = 1;
        public const double False = double.NaN;

        // Conversions between bool and double.
        // Any real nonzero value is evaluated as true.
        public static double FromBool(bool value) => value ? True : False;
        public static bool ToBool(double n) => double.IsRealNumber(n) && n != 0.0;
    }

    // Expression that references a variable by indexing into the args array.
    sealed class VariableExpr : Expr
    {
        int m_index;

        public VariableExpr(int index)
        {
            m_index = index;
        }

        public override double Eval(double[] args) => args[m_index];
        public override bool IsConstant => false;

        public override bool IsEquivalent(Expr other)
        {
            return (other as VariableExpr)?.m_index == m_index;
        }
    }
}
