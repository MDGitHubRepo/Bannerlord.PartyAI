using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using System;
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

    public HarmonyPatchBuilder(Harmony harmony)
    {
        _harmony = harmony;
    }

    public HarmonyPatchBuilder Prefix<T>(
        Expression<Action<T>> target,
        Delegate prefix)
    {
        Apply(target, prefix: prefix);
        return this;
    }

    public HarmonyPatchBuilder Prefix<T>(
        Expression<Func<T, object>> target,
        Delegate prefix)
    {
        Apply(target, prefix: prefix);
        return this;
    }

    public HarmonyPatchBuilder Postfix<T>(
        Expression<Action<T>> target,
        Delegate postfix)
    {
        Apply(target, postfix: postfix);
        return this;
    }

    public HarmonyPatchBuilder Postfix<T>(
        Expression<Func<T, object>> target,
        Delegate postfix)
    {
        Apply(target, postfix: postfix);
        return this;
    }

    protected void Apply(
        LambdaExpression target,
        Delegate? prefix = null,
        Delegate? postfix = null,
        Delegate? transpiler = null)
    {
        MethodInfo original = ExtractMethod(target);

        _harmony.TryPatch(
            original,
            prefix: prefix?.Method,
            postfix: postfix?.Method,
            transpiler: transpiler?.Method);
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

    public HarmonyPatchBuilder<T> Prefix(
        Expression<Func<T, object>> target,
        Delegate prefix)
    {
        Apply(target, prefix: prefix);
        return this;
    }

    public HarmonyPatchBuilder<T> Prefix(
        Expression<Action<T>> target,
        Delegate prefix)
    {
        Apply(target, prefix: prefix);
        return this;
    }

    public HarmonyPatchBuilder<T> Postfix(
        Expression<Func<T, object>> target,
        Delegate postfix)
    {
        Apply(target, postfix: postfix);
        return this;
    }

    public HarmonyPatchBuilder<T> Postfix(
        Expression<Action<T>> target,
        Delegate postfix)
    {
        Apply(target, postfix: postfix);
        return this;
    }
}