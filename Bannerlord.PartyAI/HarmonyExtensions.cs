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

    public HarmonyPatchBuilder Prefix(
        Delegate target,
        Delegate prefix)
    {
        _harmony.TryPatch(
            target.Method,
            prefix: prefix.Method);

        return this;
    }

    public HarmonyPatchBuilder Postfix(
        Delegate target,
        Delegate postfix)
    {
        _harmony.TryPatch(
            target.Method,
            postfix: postfix.Method);

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
            // normal method call: x => x.DoThing()
            MethodCallExpression m => m.Method,

            // property access: x => x.SomeProperty
            MemberExpression member
                when member.Member is PropertyInfo prop
                => prop.GetMethod
                   ?? throw new InvalidOperationException(
                        $"Property '{prop.Name}' has no getter"),

            // value type boxing: x => (object)x.SomeProperty
            UnaryExpression u
                when u.Operand is MemberExpression member
                && member.Member is PropertyInfo prop
                => prop.GetMethod
                   ?? throw new InvalidOperationException(
                        $"Property '{prop.Name}' has no getter"),

            _ => throw new InvalidOperationException(
                "Expression must be a method or property access.")
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