namespace Iirc.Utils.Gurobi
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Iirc.Utils.Collections;
    using Iirc.Utils.SolverFoundations;
    using Iirc.Utils.Math;
    using global::Gurobi;

    public static class GRBModelExtensions
    {
        public static readonly Func<GRBVar, double> DefaultValueSelector = var => var.X;
        
        public static Status GetResultStatus(this GRBModel model)
        {
            if (model.Get(GRB.IntAttr.SolCount) <= 0)
            {
                return model.Get(GRB.IntAttr.Status) == GRB.Status.INFEASIBLE ? Status.Infeasible : Status.NoSolution;
            }
            else
            {
                return model.Get(GRB.IntAttr.Status) == GRB.Status.OPTIMAL ? Status.Optimal : Status.Heuristic;
            }
        }

        public static bool TimeLimitReached(this GRBModel model)
        {
            return model.Get(GRB.IntAttr.Status) == GRB.Status.TIME_LIMIT;
        }

        public static void SetTimeLimit(this GRBModel model, TimeSpan? timeLimit)
        {
            if (timeLimit.HasValue) {
                model.Parameters.TimeLimit = timeLimit.Value.TotalSeconds;
            }
        }

        public static void Remove(this GRBModel model, IEnumerable<GRBVar> variables)
        {
            foreach (var variable in variables)
            {
                model.Remove(variable);
            }
        }

        public static void Remove(this GRBModel model, IEnumerable<GRBConstr> constraints)
        {
            foreach (var constraint in constraints)
            {
                model.Remove(constraint);
            }
        }

        public static void Remove(this GRBModel model, IEnumerable<GRBSOS> constraints)
        {
            foreach (var constraint in constraints)
            {
                model.Remove(constraint);
            }
        }

        public static GRBLinExpr Quicksum(this IEnumerable<GRBVar> varsToAdd)
        {
            GRBLinExpr expr = new GRBLinExpr();

            foreach (var varToAdd in varsToAdd)
            {
                expr.AddTerm(1.0, varToAdd);
            }

            return expr;
        }

        public static GRBLinExpr Quicksum(this IEnumerable<GRBLinExpr> exprsToAdd)
        {
            GRBLinExpr expr = new GRBLinExpr();

            foreach (var exprToAdd in exprsToAdd)
            {
                expr.Add(exprToAdd);
            }

            return expr;
        }

        public static GRBLinExpr Quicksum<T>(this IEnumerable<T> varsToAdd, Func<T, GRBVar> selector)
        {
            return varsToAdd.Select(selector).Quicksum();
        }

        public static GRBLinExpr Quicksum<T>(this IEnumerable<T> exprsToAdd, Func<T, GRBLinExpr> selector)
        {
            return exprsToAdd.Select(selector).Quicksum();
        }

        public static GRBConstr SetLazy(this GRBConstr constr, bool isLazy)
        {
            constr.Lazy = isLazy ? 1 : 0;
            return constr;
        }

        public static bool ToBool(
            this GRBVar variable,
            Func<GRBVar, double> valueSelector = null)
        {
            return variable.ToDouble(valueSelector) > 0.5;
        }

        public static double ToDouble(this GRBVar variable, Func<GRBVar, double> valueSelector = null)
        {
            return valueSelector == null ? DefaultValueSelector(variable) : valueSelector(variable);
        }

        public static IEnumerable<double> ToDoubles(
            this IEnumerable<GRBVar> variables,
            Func<GRBVar, double> valueSelector = null)
        {
            return variables.Select(variable => variable.ToDouble(valueSelector));
        }

        public static int ToInt(
            this GRBVar variable,
            Func<GRBVar, double> valueSelector = null)
        {
            return (int) Math.Round(variable.ToDouble(valueSelector));
        }

        public static bool TryWhereOne(
            this IEnumerable<GRBVar> variables,
            out int index,
            Func<GRBVar, double> valueSelector = null)
        {
            index = 0;
            foreach (var variable in variables)
            {
                if (variable.ToInt(valueSelector) == 1)
                {
                    return true;
                }

                index++;
            }

            index = default(int);
            return false;
        }

        public static bool TryWhereNonZero(
            this IEnumerable<GRBVar> variables,
            out int index,
            Func<GRBVar, double> valueSelector = null)
        {
            return variables.Select(variable => variable.ToDouble(valueSelector)).TryWhereNonZero(out index);
        }

        public static bool TryWhereNonZero<T>(
            this IDictionary<T, GRBVar> dict,
            out KeyValuePair<T, GRBVar> pairNonZero,
            Func<GRBVar, double> valueSelector = null)
        {
            var comparer = NumericComparer.Default;

            foreach (var pair in dict)
            {
                if (comparer.AreEqual(pair.Value.ToDouble(valueSelector), 0.0) == false)
                {
                    pairNonZero = pair;
                    return true;
                }
            }

            pairNonZero = default(KeyValuePair<T, GRBVar>);
            return false;
        }

        public static IDictionary<T, GRBVar> WhereNonZero<T>(
            this IDictionary<T, GRBVar> dict,
            Func<GRBVar, double> valueSelector = null)
        {
            var comparer = NumericComparer.Default;
            return new Dictionary<T, GRBVar>(dict
                .Where(pair => comparer.AreEqual(pair.Value.ToDouble(valueSelector), 0.0) == false));
        }
    }
}