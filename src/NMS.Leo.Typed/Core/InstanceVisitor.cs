﻿using NMS.Leo.Metadata;
using NMS.Leo.Typed.Core.Correct;
using NMS.Leo.Typed.Core.Members;
using NMS.Leo.Typed.Core.Repeat;
using NMS.Leo.Typed.Validation;

namespace NMS.Leo.Typed.Core;

internal class InstanceVisitor : ILeoVisitor, ICoreVisitor, ILeoGetter, ILeoSetter
{
    private readonly DictBase _handler;
    private readonly object _instance;
    private readonly AlgorithmKind _algorithmKind;

    private Lazy<MemberHandler> _lazyMemberHandler;

    protected HistoricalContext NormalHistoricalContext { get; set; }

    public InstanceVisitor(DictBase handler, Type sourceType, object instance, AlgorithmKind kind, bool repeatable,
        bool liteMode = false, bool strictMode = false)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _instance = instance;
        _algorithmKind = kind;

        _handler.SetObjInstance(_instance);

        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        NormalHistoricalContext = repeatable
            ? new HistoricalContext(sourceType, kind)
            : null;
        LiteMode = liteMode;

        _lazyMemberHandler = MemberHandler.Lazy(() => new MemberHandler(_handler, SourceType), liteMode);
        _validationContext = strictMode
            ? new CorrectContext(this, true)
            : null;
    }

    public Type SourceType { get; }

    public bool IsStatic => false;

    public AlgorithmKind AlgorithmKind => _algorithmKind;

    public object Instance => _instance;

    public bool StrictMode
    {
        get => ValidationEntry.StrictMode;
        set => ValidationEntry.StrictMode = value;
    }

    private CorrectContext _validationContext;

    public ILeoValidationContext ValidationEntry => _validationContext ??= new CorrectContext(this, false);

    public LeoVerifyResult Verify() => ((CorrectContext) ValidationEntry).ValidValue();

    public void VerifyAndThrow() => Verify().Raise();

    public void SetValue(string name, object value)
    {
        if (StrictMode)
            ((CorrectContext) ValidationEntry).ValidOne(name, value).Raise();
        SetValueImpl(name, value);
    }

    public void SetValue<TObj>(Expression<Func<TObj, object>> expression, object value)
    {
        if (expression is null)
            return;

        var name = PropertySelector.GetPropertyName(expression);

        if (StrictMode)
            ((CorrectContext) ValidationEntry).ValidOne(name, value).Raise();
        SetValueImpl(name, value);
    }

    public void SetValue<TObj, TValue>(Expression<Func<TObj, TValue>> expression, TValue value)
    {
        if (expression is null)
            return;

        var name = PropertySelector.GetPropertyName(expression);

        if (StrictMode)
            ((CorrectContext) ValidationEntry).ValidOne(name, value).Raise();
        SetValueImpl(name, value);
    }

    public void SetValue(IDictionary<string, object> keyValueCollections)
    {
        if (keyValueCollections is null)
            throw new ArgumentNullException(nameof(keyValueCollections));
        if (StrictMode)
            ((CorrectContext) ValidationEntry).ValidMany(keyValueCollections).Raise();
        foreach (var keyValue in keyValueCollections)
            SetValueImpl(keyValue.Key, keyValue.Value);
    }

    private void SetValueImpl(string name, object value)
    {
        NormalHistoricalContext?.RegisterOperation(c => c[name] = value);
        _handler[name] = value;
    }

    public object GetValue(string name)
    {
        return _handler[name];
    }

    public TValue GetValue<TValue>(string name)
    {
        return _handler.Get<TValue>(name);
    }

    public object GetValue<TObj>(Expression<Func<TObj, object>> expression)
    {
        if (expression is null)
            throw new ArgumentNullException(nameof(expression));

        var name = PropertySelector.GetPropertyName(expression);

        return _handler[name];
    }

    public TValue GetValue<TObj, TValue>(Expression<Func<TObj, TValue>> expression)
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

    public HistoricalContext ExposeHistoricalContext() => NormalHistoricalContext;

    public Lazy<MemberHandler> ExposeLazyMemberHandler() => _lazyMemberHandler;

    public ILeoVisitor Owner => this;

    public bool LiteMode { get; }

    public IEnumerable<string> GetMemberNames() => _lazyMemberHandler.Value.GetNames();

    public LeoMember GetMember(string name) => _lazyMemberHandler.Value.GetMember(name);

    public bool Contains(string name) => _lazyMemberHandler.Value.Contains(name);
}