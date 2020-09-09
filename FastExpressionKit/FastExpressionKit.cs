// FastExpressionKit
// License: MIT: Copyright (c) 2017 Ville M. Vainio <vivainio@gmail.com>
// See details at https://github.com/vivainio/FastExpressionKit

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
        
        public static Expression Mul(this Expression left, int right) => Expression.Multiply(left, Expression.Constant(right));
        public static Expression MulAssign(this Expression left, int right) => Expression.MultiplyAssign(left, Expression.Constant(right));

        public static Expression Add(this Expression left, int right) => Expression.Add(left, Expression.Constant(right));
        public static Expression Add(this Expression left, Expression right) => Expression.Add(left, right);

        public static Expression Mul(this Expression left, Expression right) => Expression.Multiply(left, right);
        public static Expression Let(this Expression left, Expression right) => Expression.Assign(left, right);
        public static MethodInfo Method(Expression<Action> exp) =>
            (exp.Body as MethodCallExpression).Method;
        public static Expression Box(Expression e) => Expression.Convert(e, typeof(object));
        public static Expression NotNull(this MemberExpression left) => Expression.NotEqual(left, Expression.Constant(null));
        public static Expression Call(this Expression left, string methodName) => 
            Expression.Call(left, typeof(object).GetMethod(methodName) );
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

            var l = Expression.Lambda<Func<T1, T2, DifferReturnValueType[]>>(resultArr, t1param, t2param);
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

            var assignList = fields.Select(f =>
            {
                var propA = typeof(TTarget).GetProperty(f);
                var propB = typeof(TSrc).GetProperty(f);
                if (propA.PropertyType == propB.PropertyType)
                {
                    return Expression.Assign(t1param.Dot(f), t2param.Dot(f));
                }
                else if (propA.PropertyType == typeof(int) && propB.PropertyType.IsEnum || 
                         propB.PropertyType == typeof(int) && propA.PropertyType.IsEnum)
                {
                    return Expression.Assign(t1param.Dot(f), Expression.Convert(t2param.Dot(f), propA.PropertyType));
                }
                else if (Nullable.GetUnderlyingType(propB.PropertyType) == propA.PropertyType ||
                         Nullable.GetUnderlyingType(propA.PropertyType) == propB.PropertyType)
                {
                    return Expression.Assign(t1param.Dot(f), Expression.Convert(t2param.Dot(f), propA.PropertyType));
                }

                throw new InvalidOperationException($"Invalid operation, cannot copy {propB} to {propA}.");
            });
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

    public class FieldHasherOptions
    {
        public Func<MemberExpression, Expression> StringNormalizer { get; set; }
        // early evaluate to zero if any nulls found
        public bool ZeroIfNulls { get; set; } = false;

    }
    public class FieldHasher<T1>
    {
        public readonly PropertyInfo[] Props;
    
        private readonly Func<T1, int> expr;

        private Func<T1, int> CreateExpression(PropertyInfo[] fields, FieldHasherOptions options)
        {
            /*
            $hash = 17;
            $hash += $hash * 23 + .Call ($obj.a).GetHashCode();
            $hash += $hash * 23 + .Call ($obj.b).GetHashCode();
            .If ($obj.s != null) {
                $hash += $hash * 23 + .Call (.Call ($obj.s).Trim()).GetHashCode()
            } .Else {
                $hash *= 23
            };
            $hash += $hash * 23 + .Call ($obj.date).GetHashCode();
            $hash += $hash * 23 + .Call ($obj.mynullable).GetHashCode();
            $hash
            */
            const int prim = 17;
            const int prim2 = 23;
            var t1param = EE.Param<T1>("obj");
            var resultHash = EE.Param<int>("hash");
            var ass = Expression.Assign(resultHash, Expression.Constant(prim));
            var block = new List<Expression>();
            block.Add(ass);
            var returnLabel = Expression.Label(typeof(int));

            var whenNull = options.ZeroIfNulls ? Expression.Return(returnLabel, Expression.Constant(0)) 
                : resultHash.MulAssign(prim2);

            block.AddRange(fields.Select(pi =>
            {
                var dotted = t1param.Dot(pi.Name);
                var propType = pi.PropertyType;

                var addAssignHashCode = Expression.AddAssign(resultHash,
                    resultHash.Mul(prim2).Add(dotted.Call("GetHashCode")));
                
                // nullable
                if (options.ZeroIfNulls && Nullable.GetUnderlyingType(propType) != null)
                {
                    return Expression.IfThenElse(
                        dotted.NotNull(),
                        addAssignHashCode,
                        whenNull);
                }
                
                // value type but not nullabl
                if (propType.IsValueType)
                {
                    return (Expression) addAssignHashCode;
                }

                
                // normalized string
                var normalized = propType == typeof(string) && options.StringNormalizer != null
                    ? options.StringNormalizer(dotted)
                    : dotted;

                var iffed = Expression.IfThenElse(dotted.NotNull(),
                    Expression.AddAssign(resultHash,
                        resultHash.Mul(prim2).Add(normalized.Call("GetHashCode"))),
                    whenNull);
                    
                return iffed;
            }));

            block.Add(Expression.Label(returnLabel, resultHash));
            var eblock = Expression.Block(new [] { resultHash} , block);
            var l = Expression.Lambda<Func<T1, int>>(eblock, t1param);
            return l.Compile();
        }

        public FieldHasher(PropertyInfo[] props)
        {
            this.Props = props;
            this.expr = CreateExpression(props, null);
        }
        public FieldHasher(PropertyInfo[] props, FieldHasherOptions options)
        {
            this.Props = props;
            this.expr = CreateExpression(props, options);
        }
        public int ComputeHash(T1 obj) => unchecked(expr.Invoke(obj));

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

        // zip {}
        public IEnumerable<Tuple<string, TP>> ResultsAsZip<TP>(ICollection<TP> hits)
        {
            var r = Enumerable.Zip(Props, hits, (p, h) => Tuple.Create(p, h));
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
    // Usable with FieldExtract instances
    public static class FieldExtractUtil
    {
        // emit array of object arrays, usable e.g. for sql bulk copy (one array per extracted property)
        public static object[][] ExtractToObjectArrays<T1>(FieldExtract<T1, object> extractor, IReadOnlyList<T1> entries)
        {
            // extract everything to jagged arrays
            // then transpose it
            var ecount = entries.Count;
            var extracted = new object[entries.Count][];
            for (var i = 0; i < entries.Count; i++)
            {
                extracted[i] = extractor.Extract(entries.ElementAt(i));

            }
            object[][] jagged = new object[extractor.Props.Length][];
            for (var i = 0; i < extractor.Props.Length; i++)
            {
                jagged[i] = new object[ecount];

            }

            for (var i = 0; i < entries.Count; i++)
            {
                for (var j = 0; j < extractor.Props.Count(); j++)
                {
                    jagged[j][i] = extracted[i][j];
                }

            }
            return jagged;
        }
    }
    public static class ReflectionHelper
    {
        // create cache for GetExtractorFor by reflecting on object
        public static PropertyInfo[] GetProps<T>() => typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public);
        public static PropertyInfo[] GetPropsByNames<T>(IEnumerable<string> propNames) =>
            propNames.Select(i => typeof(T).GetProperty(i)).ToArray();

        public static string[] PropNames<T>() => GetProps<T>().Select(p => p.Name).ToArray();
        public static string[] WriteablePropNames<T>() => GetProps<T>().Where(p => p.CanWrite).Select(p => p.Name).ToArray();
        public static IEnumerable<Tuple<Type, string[]>> CollectProps<T>() =>
                GetProps<T>()
                .GroupBy(prop => prop.PropertyType)
                .Select(g => Tuple.Create(g.Key, g.Select(pi => pi.Name).ToArray()));

        // use after CollectProps
        public static FieldExtract<T1, T2> GetExtractorFor<T1, T2>(IEnumerable<Tuple<Type, string[]>> propsCollection)
        {
            var proplist = propsCollection.First(el => el.Item1 == typeof(T2));
            if (proplist == null)
                return null;
            return new FieldExtract<T1, T2>(proplist.Item2);
        }

    }

}