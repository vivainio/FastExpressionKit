using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FastExpressionKit
{
    using DifferReturnValueType = Tuple<string, object>;

    public static class EE
    {
        public static ParameterExpression Var<T>(string name) => Expression.Variable(typeof(T), name);
        public static ParameterExpression Param<T>(string name) => Expression.Parameter(typeof(T), name);
        public static MemberExpression Dot(this Expression exp, string fieldName) => Expression.PropertyOrField(exp, fieldName);
        public static Expression IsEq(this Expression left, Expression right) => Expression.Equal(left, right);
        public static Expression Val<T>(T value) => Expression.Constant(value, typeof(T));
        public static MethodInfo Method(Expression<Action> exp) =>
            (exp.Body as MethodCallExpression).Method;
        public static Expression Box(Expression e) => Expression.Convert(e, typeof(object));

     }

    public class Differ<T1, T2>
    {
        public readonly string[] Props;
        private readonly Func<T1, T2, DifferReturnValueType[]> comparisonFunc;
        private Func<T1, T2, DifferReturnValueType[]> CreateExpression(IEnumerable<string> fields)
        {
            var t1param = EE.Param<T1>("left");
            var t2param = EE.Param<T2>("right");
            var NULL = EE.Val<DifferReturnValueType>(null);
            var TupleCreate = EE.Method(() => Tuple.Create<string, object>("", null));
            var cmplist2 = fields.Select(f =>
                Expression.Condition(t1param.Dot(f).IsEq(t2param.Dot(f)),
                    NULL,
                    Expression.Call(TupleCreate, EE.Val(f), EE.Box(t2param.Dot(f)))));
            var resultArr = Expression.NewArrayInit(typeof(DifferReturnValueType), cmplist2);
            
            var l = Expression.Lambda<Func<T1, T2, DifferReturnValueType[] >>(resultArr, t1param, t2param);
            return l.Compile();
        }

        public Differ(string[] props)
        {
            this.Props = props;
            this.comparisonFunc = CreateExpression(props);
        }

        public DifferReturnValueType[] Compare(T1 left, T2 right) => comparisonFunc.Invoke(left, right);
    }

    public class FieldCopier<TTarget, TSrc>
    {
        public readonly string[] Props;
        private readonly Action<TTarget, TSrc> assignExpr;
        private Action<TTarget, TSrc> CreateExpression(IEnumerable<string> fields)
        {
            var t1param = EE.Param<TTarget>("left");
            var t2param = EE.Param<TSrc>("right");

            var assignList = fields.Select(f => Expression.Assign(t1param.Dot(f), t2param.Dot(f)));
            //var resultArr = Expression.NewArrayInit(typeof(bool), cmplist);
            var block = Expression.Block(assignList);
            var l = Expression.Lambda<Action<TTarget, TSrc>>(block, t1param, t2param);
            return l.Compile();
        }

        public FieldCopier(string[] props)
        {
            this.Props = props;
            this.assignExpr = CreateExpression(props);
        }

        public void Copy(TTarget left, TSrc right) => assignExpr.Invoke(left, right);
    }

    public class FieldExtract<T1, TVal>
    {
        public readonly string[] Props;
        private readonly Func<T1, TVal[]> expr;

        private Func<T1, TVal[]> CreateExpression(IEnumerable<string> fields)
        {
            var t1param = EE.Param<T1>("obj");
            var elist = fields.Select(f => t1param.Dot(f)).Cast<Expression>();
            if (typeof(TVal) == typeof(object))
                elist = elist.Select(e => EE.Box(e));
            var resultArr = Expression.NewArrayInit(typeof(TVal), elist);
            var l = Expression.Lambda<Func<T1, TVal[]>>(resultArr, t1param);
            return l.Compile();
        }

        public FieldExtract(string[] props)
        {
            this.Props = props;
            this.expr = CreateExpression(props);
        }

        public TVal[] Extract(T1 obj) => expr.Invoke(obj);

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
        public static PropertyInfo[] GetProps<T>() => typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public);

        public static string[] PropNames<T>() => GetProps<T>().Select(p => p.Name).ToArray();
        public static IEnumerable<Tuple<Type, string[]>> CollectProps<T>() =>
                GetProps<T>()
                .GroupBy(prop => prop.PropertyType)
                .Select(g => Tuple.Create(g.Key, g.Select(pi => pi.Name).ToArray()));

        // use after CollectProps
        public static FieldExtract<T1, T2> GetExtractorFor<T1,T2>(IEnumerable<Tuple<Type, string[]>> propsCollection)
        {
            var proplist = propsCollection.First(el => el.Item1 == typeof(T2));
            if (proplist == null)
                return null;
            return new FieldExtract<T1, T2>(proplist.Item2);
        }

    }

}