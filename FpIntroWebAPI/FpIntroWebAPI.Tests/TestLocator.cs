using System;
using CommonServiceLocator;
using DeFuncto.Extensions;
using Unity;
using Unity.ServiceLocation;

namespace FpIntroWebAPI.Tests;

public static class TestLocator
{
    private static readonly Lazy<IServiceLocator> LazyLocator = new(GetLocator);
    private static readonly Lazy<IUnityContainer> LazyContainer = new(GetContainer);
    private static IUnityContainer GetContainer() => BuilderPrimer.GetUnityContainer();
    private static IServiceLocator GetLocator() => LazyContainer.Value.Apply(c => new UnityServiceLocator(c));
    public static T Get<T>() => LazyLocator.Value.GetInstance<T>();
}
