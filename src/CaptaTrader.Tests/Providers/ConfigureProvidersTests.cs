using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Credentials;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Integrations.Brokerage.Alpaca;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CaptaTrader.Tests.Providers;

[TestClass]
public sealed class ConfigureProvidersTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Defaults_AreTheFirstRegisteredProviders()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await CreateAsync(directory.Path);
        ConfigureProviders configure = services.GetRequiredService<ConfigureProviders>();

        ProviderDescriptor brokerage = await configure.GetSelectedAsync(ProviderKind.Brokerage, TestContext.CancellationToken);

        Assert.AreEqual("paper", brokerage.Id);
        Assert.IsTrue(brokerage.IsSimulated);
    }

    [TestMethod]
    public async Task SetCredential_StoresSecretInCredentialStore_AndOnlyAReferenceInSettings()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await CreateAsync(directory.Path);
        ConfigureProviders configure = services.GetRequiredService<ConfigureProviders>();
        IApplicationSettingStore settings = services.GetRequiredService<IApplicationSettingStore>();
        ICredentialStore credentials = services.GetRequiredService<ICredentialStore>();

        CapabilityResult<bool> result = await configure.SetCredentialAsync(AlpacaPaperBrokerageProvider.PROVIDER_ID, AlpacaPaperBrokerageProvider.SECRET_KEY_FIELD, " s3cret ", TestContext.CancellationToken);

        Assert.IsTrue(result.IsSuccess);
        string? reference = await settings.GetAsync($"credential.{AlpacaPaperBrokerageProvider.PROVIDER_ID}.{AlpacaPaperBrokerageProvider.SECRET_KEY_FIELD}", TestContext.CancellationToken);
        Assert.IsNotNull(reference);
        Assert.DoesNotContain("s3cret", reference);
        Assert.AreEqual("s3cret", (await credentials.GetAsync(new CredentialReference(reference), TestContext.CancellationToken))!.Value);
    }

    [TestMethod]
    public async Task ResolveConnection_ReportsMissingCredentials_AsNotConfigured()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await CreateAsync(directory.Path);
        ConfigureProviders configure = services.GetRequiredService<ConfigureProviders>();
        ProviderDescriptor alpaca = services.GetRequiredService<ProviderCatalog>().FindDescriptor(AlpacaPaperBrokerageProvider.PROVIDER_ID)!;

        await configure.SetCredentialAsync(alpaca.Id, AlpacaPaperBrokerageProvider.KEY_ID_FIELD, "key", TestContext.CancellationToken);
        CapabilityResult<ProviderConnection> missingSecret = await configure.ResolveConnectionAsync(alpaca, TestContext.CancellationToken);

        Assert.AreEqual(CapabilityStatus.NotConfigured, missingSecret.Status);

        await configure.SetCredentialAsync(alpaca.Id, AlpacaPaperBrokerageProvider.SECRET_KEY_FIELD, "secret", TestContext.CancellationToken);
        CapabilityResult<ProviderConnection> complete = await configure.ResolveConnectionAsync(alpaca, TestContext.CancellationToken);

        Assert.IsTrue(complete.IsSuccess);
        Assert.AreEqual("secret", complete.Value!.GetRequired(AlpacaPaperBrokerageProvider.SECRET_KEY_FIELD));
    }

    [TestMethod]
    public async Task ClearCredentials_RemovesSecretAndReference()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await CreateAsync(directory.Path);
        ConfigureProviders configure = services.GetRequiredService<ConfigureProviders>();
        InMemoryCredentialStore credentials = (InMemoryCredentialStore)services.GetRequiredService<ICredentialStore>();

        await configure.SetCredentialAsync(AlpacaPaperBrokerageProvider.PROVIDER_ID, AlpacaPaperBrokerageProvider.KEY_ID_FIELD, "key", TestContext.CancellationToken);
        await configure.ClearCredentialsAsync(AlpacaPaperBrokerageProvider.PROVIDER_ID, TestContext.CancellationToken);

        Assert.AreEqual(0, credentials.Count);
        ProviderStatus status = (await configure.GetStatusesAsync(ProviderKind.Brokerage, TestContext.CancellationToken))
            .Single(candidate => candidate.Descriptor.Id == AlpacaPaperBrokerageProvider.PROVIDER_ID);
        Assert.IsFalse(status.IsReady);
    }

    [TestMethod]
    public async Task SelectSimulatedProviders_PicksKeylessSimulatedProviders()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await CreateAsync(directory.Path);
        ConfigureProviders configure = services.GetRequiredService<ConfigureProviders>();

        Assert.IsTrue((await configure.SelectSimulatedProvidersAsync(TestContext.CancellationToken)).IsSuccess);

        Assert.IsTrue((await configure.GetSelectedAsync(ProviderKind.Brokerage, TestContext.CancellationToken)).IsSimulated);
        Assert.IsTrue((await configure.GetSelectedAsync(ProviderKind.MarketData, TestContext.CancellationToken)).IsSimulated);
    }

    private static Task<ServiceProvider> CreateAsync(string directory)
    {
        return TestServices.CreateAsync(directory, services =>
        {
            services.AddHttpClient();
            services.AddSingleton<Business.Modules.Brokerage.IBrokerageProvider>(provider =>
                new AlpacaPaperBrokerageProvider(provider.GetRequiredService<IHttpClientFactory>(), TimeProvider.System));
        });
    }
}
