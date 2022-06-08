﻿using System.Collections;
using NMS.Leo.Metadata;

namespace NMS.Leo.Typed.Core.Correct.Token;

internal class ValueAllToken : ValueToken
{
    // ReSharper disable once InconsistentNaming
    public const string NAME = "ValueAllToken";

    private readonly Func<object, bool> _func;

    public ValueAllToken(LeoMember member, Func<object, bool> func) : base(member)
    {
        _func = func;
    }

    public override CorrectValueOps Ops => CorrectValueOps.All;

    public override string TokenName => NAME;

    public override bool MutuallyExclusive => false;

    public override int[] MutuallyExclusiveFlags => NoMutuallyExclusiveFlags;

    public override CorrectVerifyVal ValidValue(object value)
    {
        var val = new CorrectVerifyVal {NameOfExecutedRule = NAME};
        var flag = true;

        if (value is ICollection collection)
        {
            if (collection.Cast<object>().Any(one => !_func.Invoke(one)))
            {
                flag = false;
            }

            if (!flag)
            {
                UpdateVal(val, value);
            }
        }
        else
        {
            UpdateVal(val, value, "The type is not a collection or an array, and an exception occurs when using AllToken.");
        }

        return val;
    }

    private void UpdateVal(CorrectVerifyVal val, object obj, string message = null)
    {
        val.IsSuccess = false;
        val.VerifiedValue = obj;
        val.ErrorMessage = MergeMessage(message ?? "There is at least one unsatisfied member in the array or collection.");
    }

    public override string ToString() => NAME;
}