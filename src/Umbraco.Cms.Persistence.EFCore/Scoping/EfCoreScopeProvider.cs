﻿using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.DistributedLocking;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Scoping;
using IScope = Umbraco.Cms.Infrastructure.Scoping.IScope;
using IScopeProvider = Umbraco.Cms.Infrastructure.Scoping.IScopeProvider;

namespace Umbraco.Cms.Persistence.EFCore.Scoping;

public class EfCoreScopeProvider : IEfCoreScopeProvider
{
    private readonly IAmbientEfCoreScopeStack _ambientEfCoreScopeStack;
    private readonly IUmbracoEfCoreDatabaseFactory _umbracoEfCoreDatabaseFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEFCoreScopeAccessor _efCoreScopeAccessor;
    private readonly IAmbientEFCoreScopeContextStack _ambientEfCoreScopeContextStack;
    private readonly IDistributedLockingMechanismFactory _distributedLockingMechanismFactory;
    private readonly IEventAggregator _eventAggregator;
    private readonly FileSystems _fileSystems;
    private readonly IScopeProvider _scopeProvider;

    // Needed for DI as IAmbientEfCoreScopeStack is internal
    public EfCoreScopeProvider()
        : this(
            StaticServiceProvider.Instance.GetRequiredService<IAmbientEfCoreScopeStack>(),
            StaticServiceProvider.Instance.GetRequiredService<IUmbracoEfCoreDatabaseFactory>(),
            StaticServiceProvider.Instance.GetRequiredService<ILoggerFactory>(),
            StaticServiceProvider.Instance.GetRequiredService<IEFCoreScopeAccessor>(),
            StaticServiceProvider.Instance.GetRequiredService<IAmbientEFCoreScopeContextStack>(),
            StaticServiceProvider.Instance.GetRequiredService<IDistributedLockingMechanismFactory>(),
            StaticServiceProvider.Instance.GetRequiredService<IEventAggregator>(),
            StaticServiceProvider.Instance.GetRequiredService<FileSystems>(),
            StaticServiceProvider.Instance.GetRequiredService<IScopeProvider>())
    {
    }

    internal EfCoreScopeProvider(
        IAmbientEfCoreScopeStack ambientEfCoreScopeStack,
        IUmbracoEfCoreDatabaseFactory umbracoEfCoreDatabaseFactory,
        ILoggerFactory loggerFactory,
        IEFCoreScopeAccessor efCoreScopeAccessor,
        IAmbientEFCoreScopeContextStack ambientEfCoreScopeContextStack,
        IDistributedLockingMechanismFactory distributedLockingMechanismFactory,
        IEventAggregator eventAggregator,
        FileSystems fileSystems,
        IScopeProvider scopeProvider)
    {
        _ambientEfCoreScopeStack = ambientEfCoreScopeStack;
        _umbracoEfCoreDatabaseFactory = umbracoEfCoreDatabaseFactory;
        _loggerFactory = loggerFactory;
        _efCoreScopeAccessor = efCoreScopeAccessor;
        _ambientEfCoreScopeContextStack = ambientEfCoreScopeContextStack;
        _distributedLockingMechanismFactory = distributedLockingMechanismFactory;
        _eventAggregator = eventAggregator;
        _fileSystems = fileSystems;
        _scopeProvider = scopeProvider;
        _fileSystems.IsScoped = () => efCoreScopeAccessor.AmbientScope != null && ((EfCoreScope)efCoreScopeAccessor.AmbientScope).ScopedFileSystems;
    }

    public IEfCoreScope CreateDetachedScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified,
        bool? scopeFileSystems = null) =>
        new EfCoreDetachableScope(
            _scopeProvider.CreateDetachedScope(IsolationLevel.Unspecified, repositoryCacheMode, null, null, scopeFileSystems),
            _distributedLockingMechanismFactory,
            _loggerFactory,
            _umbracoEfCoreDatabaseFactory,
            _efCoreScopeAccessor,
            _fileSystems,
            this,
            null,
            _eventAggregator,
            repositoryCacheMode,
            scopeFileSystems);

    public void AttachScope(IEfCoreScope other)
    {
        // IScopeProvider.AttachScope works with an IEFCoreScope
        // but here we can only deal with our own Scope class
        if (other is not EfCoreDetachableScope otherScope)
        {
            throw new ArgumentException("Not a Scope instance.");
        }

        if (otherScope.Detachable == false)
        {
            throw new ArgumentException("Not a detachable scope.");
        }

        if (otherScope.Attached)
        {
            throw new InvalidOperationException("Already attached.");
        }

        _scopeProvider.AttachScope(otherScope.ParentInfrastructureScope!);
        otherScope.Attached = true;
        otherScope.OriginalScope = (EfCoreScope)_ambientEfCoreScopeStack.AmbientScope!;
        otherScope.OriginalContext = AmbientScopeContext;

        PushAmbientScopeContext(otherScope.ScopeContext);
        _ambientEfCoreScopeStack.Push(otherScope);
    }

    public IEfCoreScope DetachScope()
    {
        if (_ambientEfCoreScopeStack.AmbientScope is not EfCoreDetachableScope ambientScope)
        {
            throw new InvalidOperationException("Ambient scope is not detachable");
        }

        if (ambientScope == null)
        {
            throw new InvalidOperationException("There is no ambient scope.");
        }

        if (ambientScope.Detachable == false)
        {
            throw new InvalidOperationException("Ambient scope is not detachable.");
        }

        PopAmbientScope();
        PopAmbientScopeContext();

        var originalScope = (EfCoreScope)_ambientEfCoreScopeStack.AmbientScope!;
        if (originalScope != ambientScope.OriginalScope)
        {
            throw new InvalidOperationException($"The detatched scope ({ambientScope.InstanceId}) does not match the original ({originalScope.InstanceId})");
        }

        IScopeContext? originalScopeContext = AmbientScopeContext;
        if (originalScopeContext != ambientScope.OriginalContext)
        {
            throw new InvalidOperationException($"The detatched scope context does not match the original");
        }

        _scopeProvider.DetachScope();

        ambientScope.OriginalScope = null;
        ambientScope.OriginalContext = null;
        ambientScope.Attached = false;
        return ambientScope;
    }


    public IScopeContext? AmbientScopeContext => _ambientEfCoreScopeContextStack.AmbientContext;

    public IEfCoreScope CreateScope(
        RepositoryCacheMode repositoryCacheMode = RepositoryCacheMode.Unspecified, bool? scopeFileSystems = null)
    {
        if (_ambientEfCoreScopeStack.AmbientScope is null)
        {
            ScopeContext? newContext = _ambientEfCoreScopeContextStack.AmbientContext == null ? new ScopeContext() : null;
            IScope parentScope = _scopeProvider.CreateScope(IsolationLevel.Unspecified, repositoryCacheMode, null, null, scopeFileSystems);
            var ambientScope = new EfCoreScope(
                parentScope,
                _distributedLockingMechanismFactory,
                _loggerFactory,
                _umbracoEfCoreDatabaseFactory,
                _efCoreScopeAccessor,
                _fileSystems,
                this,
                newContext,
                _eventAggregator,
                repositoryCacheMode,
                scopeFileSystems);

            if (newContext != null)
            {
                PushAmbientScopeContext(newContext);
            }

            _ambientEfCoreScopeStack.Push(ambientScope);
            return ambientScope;
        }

        var efCoreScope = new EfCoreScope(
            (EfCoreScope)_ambientEfCoreScopeStack.AmbientScope,
            _distributedLockingMechanismFactory,
            _loggerFactory,
            _umbracoEfCoreDatabaseFactory,
            _efCoreScopeAccessor,
            _fileSystems,
            this,
            null,
            _eventAggregator,
            repositoryCacheMode,
            scopeFileSystems);

        _ambientEfCoreScopeStack.Push(efCoreScope);
        return efCoreScope;
    }

    public void PopAmbientScope() => _ambientEfCoreScopeStack.Pop();

    public void PushAmbientScopeContext(IScopeContext? scopeContext)
    {
        if (scopeContext is null)
        {
            throw new ArgumentNullException(nameof(scopeContext));
        }
        _ambientEfCoreScopeContextStack.Push(scopeContext);
    }

    public void PopAmbientScopeContext() => _ambientEfCoreScopeContextStack.Pop();
}
