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

    public class Differ<T1, T2>
    {
        public readonly string[] Props;
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

        public Differ(string[] props)
        {
            this.Props = props;
            this.comparisonFunc = CreateExpression(props);
        }

        public bool[] Compare(T1 left, T2 right) => comparisonFunc.Invoke(left, right);
    }

    public class FieldExtract<T1, T2>
    {
        public readonly string[] Props;
        private readonly Func<T1, T2[]> expr;

        private Func<T1, T2[]> CreateExpression(IEnumerable<string> fields)
        {
            var t1param = EE.Param<T1>("obj");
            var elist = fields.Select(f => t1param.Dot(f)).Cast<Expression>();
            if (typeof(T2) == typeof(object))
                elist = elist.Select(e => Expression.Convert(e, typeof(object)));
            var resultArr = Expression.NewArrayInit(typeof(T2), elist);
            var l = Expression.Lambda<Func<T1, T2[]>>(resultArr, t1param);
            return l.Compile();
        }

        public FieldExtract(string[] props)
        {
            this.Props = props;
            this.expr = CreateExpression(props);
        }

        public T2[] Extract(T1 obj) => expr.Invoke(obj);

        // zip
        public IEnumerable<Tuple<string, TP>> ResultsAsZip<TP>(ICollection<TP> hits)
        {
            var r =  Enumerable.Zip(Props, hits, (p,h) => Tuple.Create(p, h));
            return r;
        }

        // hits can be any enumerable, as long as it can be zipped with Props
        // 
        public Dictionary<string, TP> ResultsAsDict<TP>(ICollection<TP> hits)
        {
            var d = new Dictionary<string, TP>();
            for (var i = 0; i < hits.Count; i++)
            {
                d[Props[i]] = hits.ElementAt(i);
            }
            return d;
        }
    }

    public static class ReflectionHelper
    {
        // create cache for GetExtractorFor by reflecting on object
        public static IEnumerable<Tuple<Type, string[]>> CollectProps<T>() =>
            typeof(T).GetProperties()
                .GroupBy(prop => prop.PropertyType)
                .Select(g => Tuple.Create(g.Key, g.Select(pi => pi.Name).ToArray()));

        public static FieldExtract<T1, T2> GetExtractorFor<T1,T2>(IEnumerable<Tuple<Type, string[]>> propsCollection)
        {
            var proplist = propsCollection.First(el => el.Item1 == typeof(T2));
            if (proplist == null)
                return null;
            return new FieldExtract<T1, T2>(proplist.Item2);
        }

    }

}