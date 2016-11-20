using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace FastExpressionKit
{
    public static class EE
    {
        public static ParameterExpression Var<T>(string name) => Expression.Variable(typeof(T), name);
        public static ParameterExpression Param<T>(string name) => Expression.Parameter(typeof(T), name);
        public static MemberExpression Dot(this Expression exp, string fieldName) => Expression.PropertyOrField(exp, fieldName);
        public static Expression IsEq(this Expression left, Expression right) => Expression.Equal(left, right);
    }

    public class FastObjectDiffer<T1, T2>
    {
        public readonly String[] Props;
        private readonly Func<T1, T2, bool[]> comparisonFunc;
        private Func<T1, T2, bool[]> CreateExpression(IEnumerable<string> fields)
        {
            var t1param = EE.Param<T1>("left");
            var t2param = EE.Param<T2>("right");

            var cmplist = fields.Select(f => t1param.Dot(f).IsEq(t2param.Dot(f)));
            var resultArr = Expression.NewArrayInit(typeof(bool), cmplist);
            var l = Expression.Lambda<Func<T1, T2, bool[]>>(resultArr, t1param, t2param);
            return l.Compile();
        }
        public FastObjectDiffer(string[] props)
        {
            this.Props = props;
            this.comparisonFunc = CreateExpression(props);
        }

        public bool[] Compare(T1 left, T2 right) => comparisonFunc.Invoke(left, right);
    }
}