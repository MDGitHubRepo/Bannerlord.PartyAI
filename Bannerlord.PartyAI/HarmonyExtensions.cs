using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Bannerlord.PartyAI;

public static class HarmonyExtensions
{
    public static HarmonyPatchBuilder<T> Patch<T>(this Harmony harmony)
        => new HarmonyPatchBuilder<T>(harmony);
    public static HarmonyPatchBuilder Patch(this Harmony harmony)
        => new HarmonyPatchBuilder(harmony);
}

public class HarmonyPatchBuilder
{
    private readonly Harmony _harmony;

    private MethodBase? _method;

    public HarmonyPatchBuilder(Harmony harmony)
    {
        _harmony = harmony;
    }

    public HarmonyPatchBuilder Method<T>(
        Expression<Action<T>> target)
    {
        _method = ResolveMethod<T>(target);
        return this;
    }

    public HarmonyPatchBuilder Method<T>(
        Expression<Func<T, object>> target)
    {
        _method = ResolveMethod<T>(target);
        return this;
    }

    public HarmonyPatchBuilder Method<T>(
        string targetMethodName)
    {
        _method = AccessTools2.Method(typeof(T), targetMethodName);
        return this;
    }

    public HarmonyPatchBuilder Method(Expression<Action> target)
    {
        _method = ExtractMethod(target);
        return this;
    }

    public HarmonyPatchBuilder Method<TResult>(
        Expression<Func<TResult>> target)
    {
        _method = ExtractMethod(target);
        return this;
    }

    public HarmonyPatchBuilder Method(
        Type type,
        string targetMethodName)
    {
        _method = AccessTools2.Method(type, targetMethodName);
        return this;
    }

    public HarmonyPatchBuilder Constructor<T>(Type[]? parameters = null)
    {
        _method = AccessTools2.Constructor(typeof(T), parameters);
        return this;
    }

    public HarmonyPatchBuilder Prefix(Delegate prefix)
    {
        Apply(_method, prefix: prefix);
        return this;
    }

    public HarmonyPatchBuilder Postfix(Delegate postfix)
    {
        Apply(_method, postfix: postfix);
        return this;
    }

    protected void Apply(
        MethodBase? original,
        Delegate? prefix = null,
        Delegate? postfix = null,
        Delegate? transpiler = null,
        Delegate? finalizer = null)
    {
        var prefixMethod = prefix is null ? null : new HarmonyMethod(prefix);
        var postfixMethod = postfix is null ? null : new HarmonyMethod(postfix);
        var transpilerMethod = transpiler is null ? null : new HarmonyMethod(transpiler);
        var finalizerMethod = finalizer is null ? null : new HarmonyMethod(finalizer);

        _harmony.Patch(original, prefixMethod, postfixMethod, transpilerMethod, finalizerMethod);
    }

    private static MethodInfo ResolveMethod<T>(LambdaExpression expression)
    {
        var extracted = ExtractMethod(expression);

        if (!extracted.IsVirtual)
        {
            return extracted;
        }

        var parameters = extracted.GetParameters()
            .Select(p => p.ParameterType)
            .ToArray();

        return typeof(T).GetMethod(
            extracted.Name,
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic,
            null,
            parameters,
            null) ?? extracted;
    }

    private static MethodInfo ExtractMethod(LambdaExpression expression)
    {
        return expression.Body switch
        {
            // direct method call
            MethodCallExpression m
                => m.Method,

            // boxed method call
            UnaryExpression u
                when u.Operand is MethodCallExpression m
                => m.Method,

            // property access
            MemberExpression member
                when member.Member is PropertyInfo prop
                => prop.GetMethod
                   ?? throw new InvalidOperationException(
                        $"Property '{prop.Name}' has no getter"),

            // boxed property access
            UnaryExpression u
                when u.Operand is MemberExpression member
                && member.Member is PropertyInfo prop
                => prop.GetMethod
                   ?? throw new InvalidOperationException(
                        $"Property '{prop.Name}' has no getter"),

            _ => throw new InvalidOperationException(
                $"Expression must be a method or property access. Actual: {expression.Body.NodeType}")
        };
    }
}

public class HarmonyPatchBuilder<T> : HarmonyPatchBuilder
{
    public HarmonyPatchBuilder(Harmony harmony) : base(harmony) { }

    public HarmonyPatchBuilder<T> Method(
        Expression<Action<T>> target)
    {
        base.Method<T>(target);
        return this;
    }

    public HarmonyPatchBuilder<T> Method(
        Expression<Func<T, object>> target)
    {
        base.Method<T>(target);
        return this;
    }

    public HarmonyPatchBuilder<T> Method(
        string targetMethodName)
    {
        base.Method<T>(targetMethodName);
        return this;
    }

    public HarmonyPatchBuilder<T> Constructor(Type[]? parameters = null)
    {
        base.Constructor<T>(parameters);
        return this;
    }

    public HarmonyPatchBuilder<T> Prefix(Delegate prefix)
    {
        base.Prefix(prefix);
        return this;
    }

    public HarmonyPatchBuilder<T> Postfix(Delegate postfix)
    {
        base.Postfix(postfix);
        return this;
    }
}