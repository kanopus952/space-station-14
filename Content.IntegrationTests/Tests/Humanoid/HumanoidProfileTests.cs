using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Speech.Components;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Humanoid;

[TestFixture]
[TestOf(typeof(HumanoidProfileSystem))]
public sealed class HumanoidProfileTests
{
    private static readonly EntProtoId BaseSpecies = "MobHuman";
    private static readonly ProtoId<SpeciesPrototype> Vox = "Vox";

    [Test]
    public async Task EnsureValidLoading()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entityManager = server.ResolveDependency<IEntityManager>();
            var humanoidProfile = entityManager.System<HumanoidProfileSystem>();
            var human = entityManager.Spawn(BaseSpecies);
            humanoidProfile.ApplyProfileTo(human,
                new HumanoidCharacterProfile()
                    .WithSex(Sex.Female)
                    .WithAge(67)
                    .WithGender(Gender.Neuter)
                    .WithSpecies(Vox));
            var humanoidComponent = entityManager.GetComponent<HumanoidProfileComponent>(human);
            var voiceComponent = entityManager.GetComponent<VocalComponent>(human);

            Assert.That(humanoidComponent.Age, Is.EqualTo(67));
            Assert.That(humanoidComponent.Sex, Is.EqualTo(Sex.Female));
            Assert.That(humanoidComponent.Gender, Is.EqualTo(Gender.Neuter));
            Assert.That(humanoidComponent.Species, Is.EqualTo(Vox));

            Assert.That(voiceComponent.Sounds, Is.Not.Null, message: "the MobHuman spawned by this test needs to have sex-specific sound set");
            Assert.That(voiceComponent.Sounds![Sex.Female], Is.EqualTo(voiceComponent.EmoteSounds));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    [TestOf(typeof(HumanoidCharacterProfile)), TestOf(typeof(VisualBodyComponent))]
    [Description("Tests that the game can generate a completely random profile with a completely random species and apply it to a blank body.")]
    public async Task EnsureValidRandom()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            LoadDependencies(server, out var body, out var humanoidComponent, out var humanoidProfile, out var markingManager, out var visualBody, out var bodySystem);
            var profile = HumanoidCharacterProfile.Random();
            humanoidProfile.ApplyProfileTo(body, profile);
            visualBody.ApplyProfileTo(body, profile);

            AssertValidProfile(server, (body, humanoidComponent), profile, markingManager, bodySystem);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    [TestOf(typeof(HumanoidCharacterProfile)), TestOf(typeof(VisualBodyComponent))]
    [Description("Tests that every species is able to randomly generate a valid appearance without issues.")]
    public async Task EnsureValidRandomSpecies()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            foreach (var speciesProto in server.ProtoMan.EnumeratePrototypes<SpeciesPrototype>())
            {
                LoadDependencies(server, out var body, out var humanoidComponent, out var humanoidProfile, out var markingManager, out var visualBody, out var bodySystem);

                var species = speciesProto.ID;
                var profile = HumanoidCharacterProfile.RandomWithSpecies(species);
                humanoidProfile.ApplyProfileTo(body, profile);
                visualBody.ApplyProfileTo(body, profile);

                Assert.That(humanoidComponent.Age, Is.LessThanOrEqualTo(speciesProto.MaxAge));
                Assert.That(humanoidComponent.Age, Is.GreaterThanOrEqualTo(speciesProto.MinAge));
                Assert.That(speciesProto.Sexes.Contains(humanoidComponent.Sex), Is.True);
                Assert.That(humanoidComponent.Species, Is.EqualTo(species));
                var strategy = server.ProtoMan.Index(speciesProto.SkinColoration).Strategy;
                Assert.That(strategy.VerifySkinColor(profile.Appearance.SkinColor), Is.True);

                AssertValidProfile(server, (body, humanoidComponent), profile, markingManager, bodySystem);
            }
        });

        await pair.CleanReturnAsync();
    }

    private static void LoadDependencies(
        RobustIntegrationTest.ServerIntegrationInstance server,
        out EntityUid body,
        out HumanoidProfileComponent humanoidComponent,
        out HumanoidProfileSystem humanoidProfile,
        out MarkingManager markingManager,
        out SharedVisualBodySystem visualBody,
        out BodySystem bodySystem)
    {
        var entityManager = server.ResolveDependency<IEntityManager>();
        humanoidProfile = entityManager.System<HumanoidProfileSystem>();
        markingManager = server.ResolveDependency<MarkingManager>();
        visualBody = entityManager.System<SharedVisualBodySystem>();
        bodySystem = entityManager.System<BodySystem>();
        body = entityManager.Spawn(BaseSpecies);
        humanoidComponent = entityManager.GetComponent<HumanoidProfileComponent>(body);
    }

    private static void AssertValidProfile(
        RobustIntegrationTest.ServerIntegrationInstance server,
        Entity<HumanoidProfileComponent> body,
        HumanoidCharacterProfile profile,
        MarkingManager markingManager,
        BodySystem bodySystem)
    {
        bodySystem.TryGetOrgansWithComponent<VisualOrganComponent>(body.Owner, out var organs);

        foreach (var (_, visualOrgan) in organs)
        {
            Assert.That(visualOrgan.Profile.Sex, Is.EqualTo(profile.Sex));
            Assert.That(visualOrgan.Profile.EyeColor, Is.EqualTo(profile.Appearance.EyeColor));
            Assert.That(visualOrgan.Profile.SkinColor, Is.EqualTo(profile.Appearance.SkinColor));
        }

        bodySystem.TryGetOrgansWithComponent<VisualOrganMarkingsComponent>(body.Owner, out var markings);

        foreach (var (_, markingOrgan) in markings)
        {
            var data = markingOrgan.MarkingData;
            var groupProto = server.ProtoMan.Index(data.Group);
            var counts = new Dictionary<HumanoidVisualLayers, int>();
            var freeMarkings = new List<Marking>();

            foreach (var marking in markingOrgan.AppliedMarkings)
            {
                var markingProto = server.ProtoMan.Index(marking.MarkingId);

                Assert.That(markingProto.Sprites.Count, Is.EqualTo(marking.MarkingColors.Count));
                Assert.That(markingManager.CanBeApplied(data.Group, profile.Sex, markingProto), Is.True);
                Assert.That(data.Layers.Contains(markingProto.BodyPart), Is.True);
                if (!markingProto.ForcedColoring && groupProto.Appearances.GetValueOrDefault(markingProto.BodyPart)?.MatchSkin != true)
                    freeMarkings.Add(marking);

                if (!groupProto.Limits.TryGetValue(markingProto.BodyPart, out var limits))
                    continue;

                var count = counts.GetValueOrDefault(markingProto.BodyPart);
                Assert.That(count, Is.LessThanOrEqualTo(limits.Limit));
                counts[markingProto.BodyPart] = count + 1;
            }

            if (freeMarkings.Count == markingOrgan.AppliedMarkings.Count)
                continue;

            foreach (var marking in markingOrgan.AppliedMarkings)
            {
                if (freeMarkings.Contains(marking))
                    continue;

                var markingProto = server.ProtoMan.Index(marking.MarkingId);

                Assert.That(marking.MarkingColors,
                    Is.EqualTo(MarkingColoring.GetMarkingLayerColors(markingProto, profile.Appearance.SkinColor, profile.Appearance.EyeColor, markingOrgan.AppliedMarkings)));

                if (markingProto.SexRestriction != null)
                    Assert.That(markingProto.SexRestriction, Is.EqualTo(profile.Sex));
            }
        }
    }
}
