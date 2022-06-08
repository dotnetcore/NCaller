﻿using NMS.Leo.Metadata;
using NMS.Leo.Typed.Core.Correct;
using NMS.Leo.Typed.Core.Members;
using NMS.Leo.Typed.Core.Repeat;
using NMS.Leo.Typed.Validation;

namespace NMS.Leo.Typed.Core;

internal class StaticTypeLeoVisitor<T> : ILeoVisitor<T>, ICoreVisitor<T>, ILeoGetter<T>, ILeoSetter<T>
{
    private readonly DictBase<T> _handler;
    private readonly AlgorithmKind _algorithmKind;

    private Lazy<MemberHandler> _lazyMemberHandler;

    public StaticTypeLeoVisitor(DictBase<T> handler, AlgorithmKind kind, bool liteMode = false, bool strictMode = false)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _algorithmKind = kind;

        SourceType = typeof(T);
        LiteMode = liteMode;

        _lazyMemberHandler = MemberHandler.Lazy(() => new MemberHandler(_handler, SourceType), liteMode);
        _validationContext = strictMode
            ? new CorrectContext<T>(this, true)
            : null;
    }

    public Type SourceType { get; }

    public bool IsStatic => true;

    public AlgorithmKind AlgorithmKind => _algorithmKind;

    object ILeoVisitor.Instance => default;

    public T Instance => default;

    public bool StrictMode
    {
        get => ValidationEntry.StrictMode;
        set => ValidationEntry.StrictMode = value;
    }

    private CorrectContext<T> _validationContext;

    public ILeoValidationContext<T> ValidationEntry => _validationContext ??= new CorrectContext<T>(this, false);

    ILeoValidationContext ILeoVisitor.ValidationEntry => ValidationEntry;

    public LeoVerifyResult Verify() => ((CorrectContext<T>) ValidationEntry).ValidValue();

    public void VerifyAndThrow() => Verify().Raise();

    public void SetValue(string name, object value)
    {
        if (StrictMode)
            ((CorrectContext<T>) ValidationEntry).ValidOne(name, value).Raise();
        SetValueImpl(name, value);
    }

    void ILeoVisitor.SetValue<TObj>(Expression<Func<TObj, object>> expression, object value)
    {
        if (expression is null)
            return;

        var name = PropertySelector.GetPropertyName(expression);

        if (StrictMode)
            ((CorrectContext<T>) ValidationEntry).ValidOne(name, value).Raise();
        SetValueImpl(name, value);
    }

    void ILeoVisitor.SetValue<TObj, TValue>(Expression<Func<TObj, TValue>> expression, TValue value)
    {
        if (expression is null)
            return;

        var name = PropertySelector.GetPropertyName(expression);

        if (StrictMode)
            ((CorrectContext<T>) ValidationEntry).ValidOne(name, value).Raise();
        SetValueImpl(name, value);
    }

    void ILeoSetter<T>.SetValue<TObj>(Expression<Func<TObj, object>> expression, object value)
        => ((ILeoVisitor) this).SetValue(expression, value);

    void ILeoSetter<T>.SetValue<TObj, TValue>(Expression<Func<TObj, TValue>> expression, TValue value)
        => ((ILeoVisitor) this).SetValue(expression, value);

    public void SetValue(Expression<Func<T, object>> expression, object value)
    {
        if (expression is null)
            return;

        var name = PropertySelector.GetPropertyName(expression);

        if (StrictMode)
            ((CorrectContext<T>) ValidationEntry).ValidOne(name, value).Raise();
        SetValueImpl(name, value);
    }

    public void SetValue<TValue>(Expression<Func<T, TValue>> expression, TValue value)
    {
        if (expression is null)
            return;

        var name = PropertySelector.GetPropertyName(expression);

        if (StrictMode)
            ((CorrectContext<T>) ValidationEntry).ValidOne(name, value).Raise();
        SetValueImpl(name, value);
    }

    public void SetValue(IDictionary<string, object> keyValueCollections)
    {
        if (keyValueCollections is null)
            throw new ArgumentNullException(nameof(keyValueCollections));
        if (StrictMode)
            ((CorrectContext<T>) ValidationEntry).ValidMany(keyValueCollections).Raise();
        foreach (var keyValue in keyValueCollections)
            SetValueImpl(keyValue.Key, keyValue.Value);
    }

    private void SetValueImpl(string name, object value)
    {
        _handler[name] = value;
    }

    public object GetValue(string name)
    {
        return _handler[name];
    }

    public object GetValue(Expression<Func<T, object>> expression)
    {
        if (expression is null)
            throw new ArgumentNullException(nameof(expression));

        var name = PropertySelector.GetPropertyName(expression);

        return _handler[name];
    }

    public TValue GetValue<TValue>(string name)
    {
        return _handler.Get<TValue>(name);
    }

    object ILeoVisitor.GetValue<TObj>(Expression<Func<TObj, object>> expression)
    {
        if (expression is null)
            throw new ArgumentNullException(nameof(expression));

        var name = PropertySelector.GetPropertyName(expression);

        return _handler[name];
    }

    TValue ILeoVisitor.GetValue<TObj, TValue>(Expression<Func<TObj, TValue>> expression)
    {
        if (expression is null)
            throw new ArgumentNullException(nameof(expression));

        var name = PropertySelector.GetPropertyName(expression);

        return _handler.Get<TValue>(name);
    }

    object ILeoGetter<T>.GetValue<TObj>(Expression<Func<TObj, object>> expression)
        => ((ILeoVisitor) this).GetValue(expression);

    TValue ILeoGetter<T>.GetValue<TObj, TValue>(Expression<Func<TObj, TValue>> expression)
        => ((ILeoVisitor) this).GetValue(expression);

    public TValue GetValue<TValue>(Expression<Func<T, TValue>> expression)
    {
        if (expression is null)
            throw new ArgumentNullException(nameof(expression));

        var name = PropertySelector.GetPropertyName(expression);

        return _handler.Get<TValue>(name);
    }

    public object this[string name]
    {
        get => GetValue(name);
        set => SetValue(name, value);
    }

    public HistoricalContext<T> ExposeHistoricalContext() => default;

    public Lazy<MemberHandler> ExposeLazyMemberHandler() => _lazyMemberHandler;

    public ILeoVisitor<T> Owner => this;

    public bool LiteMode { get; }

    public IEnumerable<string> GetMemberNames() => _lazyMemberHandler.Value.GetNames();

    public LeoMember GetMember(string name) => _lazyMemberHandler.Value.GetMember(name);

    public LeoMember GetMember<TValue>(Expression<Func<T, TValue>> expression)
    {
        if (expression is null)
            throw new ArgumentNullException(nameof(expression));

        var name = PropertySelector.GetPropertyName(expression);

        return _lazyMemberHandler.Value.GetMember(name);
    }

    public bool Contains(string name) => _lazyMemberHandler.Value.Contains(name);
}