using System.Reflection;
using NetArchTest.Rules;

namespace OpsMessaging.ArchitectureTests;

// CONCEPT (Architecture test): The dependency rule from DESIGN.md §2.1 / §6.6 is not just a
// convention in a document: these tests inspect the compiled assemblies and fail the build
// when a layer points the wrong way (e.g. Domain using Infrastructure).
// CONCEPT (Quality gate): Because CI runs these tests, an architecture violation blocks the PR.
//
// Note: while a layer is still empty (phase 0) a rule passes trivially. The rules start to
// bite as soon as code is added in later phases.
public sealed class DependencyRuleTests
{
    private const string DomainNamespace = "OpsMessaging.Domain";
    private const string ApplicationNamespace = "OpsMessaging.Application";
    private const string InfrastructureNamespace = "OpsMessaging.Infrastructure";
    private const string ApiNamespace = "OpsMessaging.Api";
    private const string PortsNamespace = "OpsMessaging.Application.Abstractions";

    // Assemblies are loaded by name, so production code needs no "marker" types for the tests.
    private static readonly Assembly DomainAssembly = Assembly.Load(DomainNamespace);
    private static readonly Assembly ApplicationAssembly = Assembly.Load(ApplicationNamespace);
    private static readonly Assembly ApiAssembly = Assembly.Load(ApiNamespace);

    [Fact]
    public void Domain_Dependencies_OnlyBaseClassLibrary()
    {
        // CONCEPT (Clean Architecture): The domain depends on nothing: no other layer and no
        // NuGet package. Only "System.*" (the .NET base class library) and itself are allowed.
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespaceStartingWith(DomainNamespace)
            .Should().OnlyHaveDependenciesOn("System", DomainNamespace)
            .GetResult();

        AssertSuccessful(result, "Domain may only depend on the .NET base class library.");
    }

    [Fact]
    public void Domain_Dependencies_NoOuterLayersOrAspNetCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespaceStartingWith(DomainNamespace)
            .ShouldNot().HaveDependencyOnAny(
                ApplicationNamespace, InfrastructureNamespace, ApiNamespace, "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccessful(result, "Domain must not depend on Application, Infrastructure, Api or ASP.NET Core.");
    }

    [Fact]
    public void Application_Dependencies_NoInfrastructureOrApi()
    {
        // CONCEPT (Clean Architecture): Application defines ports; it never knows which
        // adapter (Infrastructure) or which delivery mechanism (Api) is used.
        var result = Types.InAssembly(ApplicationAssembly)
            .That().ResideInNamespaceStartingWith(ApplicationNamespace)
            .ShouldNot().HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccessful(result, "Application must not depend on Infrastructure, Api or ASP.NET Core.");
    }

    [Fact]
    public void PortImplementations_OutsideInfrastructure_NoneExist()
    {
        // CONCEPT (Clean Architecture): A port (interface in Application/Abstractions) is
        // implemented by an adapter, and adapters live in Infrastructure. Test doubles in the
        // test projects are not checked here: they are not part of the production assemblies.
        var ports = Types.InAssembly(ApplicationAssembly)
            .That().ResideInNamespace(PortsNamespace)
            .And().AreInterfaces()
            .GetTypes();

        var offenders = ports
            .SelectMany(port => Types.InAssemblies([DomainAssembly, ApplicationAssembly, ApiAssembly])
                .That().ImplementInterface(port)
                .GetTypes()
                .Select(implementation => $"{implementation.FullName} implements {port.Name}"))
            .ToList();

        offenders.ShouldBeEmpty(
            "Port implementations belong in OpsMessaging.Infrastructure. Offending types:\n" +
            string.Join('\n', offenders));
    }

    [Fact]
    public void Api_TypesOutsideCompositionRoot_DoNotUseInfrastructure()
    {
        // CONCEPT (Clean Architecture): Api references Infrastructure only to register the
        // adapters in the composition root (Program.cs, top-level statements in the global
        // namespace). Everything in the OpsMessaging.Api namespaces (endpoints, DTOs,
        // security) must go through Application.
        var result = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespaceStartingWith(ApiNamespace)
            .ShouldNot().HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        AssertSuccessful(result, "Only the composition root (Program.cs) may use Infrastructure.");
    }

    private static void AssertSuccessful(NetArchTest.Rules.TestResult result, string rule)
    {
        var failingTypes = result.FailingTypeNames ?? [];

        result.IsSuccessful.ShouldBeTrue(
            $"{rule} Offending types:\n{string.Join('\n', failingTypes)}");
    }
}
