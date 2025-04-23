using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace ConwayGameOfLife_NET9.Tests;

public class AutoMoqDataAttribute() : AutoDataAttribute(() => new Fixture()
    .Customize(new AutoMoqCustomization
    {
        ConfigureMembers = true,
        GenerateDelegates = true
    })
    .Customize(new SupportMutableValueTypesCustomization()));