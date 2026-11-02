using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace CaptaTrader.Tests.Architecture;

/// <summary>
/// Business is the center: Data and Integrations depend on it, never the reverse, and the two
/// implementation tiers never depend on each other.
/// </summary>
[TestClass]
public sealed class ArchitectureTests
{
    private const string DATA = "CaptaTrader.Data";
    private const string INTEGRATIONS = "CaptaTrader.Integrations";
    private const string CLI_APP = "CaptaTrader";

    [TestMethod]
    [DataRow(DATA)]
    [DataRow(INTEGRATIONS)]
    [DataRow(CLI_APP)]
    public void Business_DoesNotReferenceOuterTiers(string assemblyName)
    {
        Assert.IsFalse(References(typeof(CaptaTrader.Business.DependencyInjection), assemblyName));
    }

    [TestMethod]
    public void Data_DoesNotReferenceIntegrations()
    {
        Assert.IsFalse(References(typeof(CaptaTrader.Data.DependencyInjection), INTEGRATIONS));
    }

    [TestMethod]
    public void Integrations_DoesNotReferenceData()
    {
        Assert.IsFalse(References(typeof(CaptaTrader.Integrations.DependencyInjection), DATA));
    }

    private static bool References(Type typeInAssembly, string assemblyName)
    {
        Assembly assembly = typeInAssembly.Assembly;

        return assembly.GetReferencedAssemblies().Any(reference => reference.Name == assemblyName);
    }
}
