using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class DirectoryFilteringTests
    {
        [Test]
        public void DirectoryFiltering_Rules_Nulls()
        {
            var nullRule = new DirectoryFiltering.Rule(FilteringMode.Allow, null);
            Assert.Null(nullRule.filterPath);
            Assert.AreEqual(nullRule.precedence, -1);

            var emptyRule = new DirectoryFiltering.Rule(FilteringMode.Allow, "");
            Assert.Null(emptyRule.filterPath);
            Assert.AreEqual(emptyRule.precedence, -1);

            var notWalkedRule = new DirectoryFiltering.Rule(FilteringMode.Allow, ".//.////");
            Assert.Null(notWalkedRule.filterPath);
            Assert.AreEqual(notWalkedRule.precedence, -1);

            var walkedBackRule = new DirectoryFiltering.Rule(FilteringMode.Allow, "Path/Another/../..");
            Assert.Null(walkedBackRule.filterPath);
            Assert.AreEqual(walkedBackRule.precedence, -1);

            var nullDefault = DirectoryFiltering.Rule.CreateFilterPredicateForRules(null);
            Assert.NotNull(nullDefault);
            Assert.False(nullDefault("AnyRandomString"));
            Assert.False(nullDefault("_______________"));
            Assert.Null(DirectoryFiltering.Rule.CreateFilterPredicateForRules(null, true));

            Assert.NotNull(DirectoryFiltering.Rule.CreateFilterPredicateForRules(new DirectoryFiltering.Rule[] {}));
            var emptyDefault = DirectoryFiltering.Rule.CreateFilterPredicateForRules(new DirectoryFiltering.Rule[] {});
            Assert.NotNull(emptyDefault);
            Assert.False(emptyDefault("AnyRandomString"));
            Assert.False(emptyDefault("_______________"));
            Assert.Null(DirectoryFiltering.Rule.CreateFilterPredicateForRules(new DirectoryFiltering.Rule[] {}, true));

            Assert.NotNull(DirectoryFiltering.Rule.CreateFilterPredicateForRules(new[] { nullRule }));
            var noValidRulesDefault = DirectoryFiltering.Rule.CreateFilterPredicateForRules(new[] { nullRule });
            Assert.NotNull(noValidRulesDefault);
            Assert.False(noValidRulesDefault("AnyRandomString"));
            Assert.False(noValidRulesDefault("_______________"));
            Assert.Null(DirectoryFiltering.Rule.CreateFilterPredicateForRules(new[] { nullRule }, true));
        }

        [Test]
        public void DirectoryFiltering_Rules_Precedence()
        {
            var ruleAssetsTextures = new DirectoryFiltering.Rule(FilteringMode.Allow, "Assets//Models/../Something/else/../../Textures/./");
            Assert.AreEqual(ruleAssetsTextures.filterPath, "/assets/textures");
            Assert.AreEqual(ruleAssetsTextures.precedence, 2);

            var ruleAssetsModelsTextures = new DirectoryFiltering.Rule(FilteringMode.Allow, "Assets/Models/Something/else/../../Textures/./");
            Assert.AreEqual(ruleAssetsModelsTextures.filterPath, "/assets/models/textures");
            Assert.AreEqual(ruleAssetsModelsTextures.precedence, 3);
        }

        [Test]
        public void DirectoryFiltering_Rules_Ordering()
        {
            var rules = new[]
            {
                new DirectoryFiltering.Rule(FilteringMode.Block, "Assets/Textures/UI"),
                new DirectoryFiltering.Rule(FilteringMode.Allow, "Assets/Textures"),
                new DirectoryFiltering.Rule(FilteringMode.Allow, "Assets/Models"),
                new DirectoryFiltering.Rule(FilteringMode.Block, "Assets/Models/Textures"),
                new DirectoryFiltering.Rule(FilteringMode.Allow, "Assets/G*"),
            };

            var defaultFalse = rules.CreateFilterPredicate();
            Assert.NotNull(defaultFalse);

            Assert.False(defaultFalse("Assets/Textures/UI/SomeUITexture.png"));
            Assert.False(defaultFalse("Assets/Textures/UI/SomeUITexture.png"));
            Assert.True(defaultFalse("Assets/Textures/World/SomeWorldTexture.png"));
            Assert.True(defaultFalse("Assets/Models/someModelFile.obj"));
            Assert.True(defaultFalse("Assets/Models/Textures/ThisShouldBeAllowedBecauseRuleOrdering.png"));

            Assert.True(defaultFalse("Assets/GettingCoverageOnTheCacheToo"));
            Assert.True(defaultFalse("Assets/GettingCoverageOnTheCacheToo"));

            Assert.False(defaultFalse("Packages/DefaultReturnTest"));

            var defaultTrue = rules.CreateFilterPredicate(true);
            Assert.NotNull(defaultTrue);

            Assert.False(defaultTrue("Assets/Textures/UI/SomeUITexture.png"));
            Assert.False(defaultTrue("Assets/Textures/UI/SomeUITexture.png"));
            Assert.True(defaultTrue("Assets/Textures/World/SomeWorldTexture.png"));
            Assert.True(defaultTrue("Assets/Models/someModelFile.obj"));
            Assert.True(defaultTrue("Assets/Models/Textures/ThisShouldBeAllowedBecauseRuleOrdering.png"));

            Assert.True(defaultTrue("Assets/GettingCoverageOnTheCacheToo"));
            Assert.True(defaultTrue("Assets/GettingCoverageOnTheCacheToo"));

            Assert.True(defaultTrue("Packages/DefaultReturnTest"));
        }

        [Test]
        public void DirectoryFiltering_DirectoryWalking()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets//Models/../Something/else/../../Textures/./");
            Assert.False(predicate("Assets/Models"));
            Assert.False(predicate("Assets/Models/ARandomTestString"));
            Assert.True(predicate("Assets/Textures"));
            Assert.True(predicate("Assets/Textures/ARandomTestString"));
        }

        [Test]
        public void DirectoryFiltering_NullPredicates()
        {
            Assert.Null(DirectoryFiltering.CreateMatchesPredicate((string)null));
            Assert.Null(DirectoryFiltering.CreateMatchesPredicate(""));
            Assert.Null(DirectoryFiltering.CreateMatchesPredicate("/././//"));
            Assert.Null(DirectoryFiltering.CreateMatchesPredicate("Path/.."));
            Assert.Null(DirectoryFiltering.CreateMatchesPredicate("Path/Another/../.."));

            Assert.Null(DirectoryFiltering.CreateMatchesPredicate((string[])null));
            Assert.Null(DirectoryFiltering.CreateMatchesPredicate(Array.Empty<string>()));
            Assert.Null(DirectoryFiltering.CreateMatchesPredicate(new[] { "ThisIsValidBut", null , "Isn't" }));
        }

        [Test]
        public void DirectoryFiltering_MatchEverythingFastPath()
        {
            // Normally created predicates are expected to be different objects as each creation will create a unique array
            // the fast path for '*' is expected to not capture that and instead return the same object every time

            var normalPath1 = DirectoryFiltering.CreateMatchesPredicate("Assets");
            var normalPath2 = DirectoryFiltering.CreateMatchesPredicate("Assets");
            if (normalPath1 == normalPath2)
            {
                Assert.Inconclusive();
            }

            var fastPath1 = DirectoryFiltering.CreateMatchesPredicate("*");
            var fastPath2 = DirectoryFiltering.CreateMatchesPredicate("*");
            Assert.True(fastPath1 == fastPath2);

            Assert.True(fastPath1("RandomString"));
            Assert.True(fastPath1("\u00af\\_(ツ)_/\u00af"));
        }

        [Test]
        public void DirectoryFiltering_TrailingAsterisk()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models/*");
            Assert.False(predicate("Assets/Models"));
            Assert.True(predicate("Assets/Models/ARandomTestString"));
        }

        [Test]
        public void DirectoryFiltering_MultiTrailingAsterisk()
        {
            var predicate2Asterisks = DirectoryFiltering.CreateMatchesPredicate("a/**");
            Assert.True(predicate2Asterisks("a/AnotherRandomTestString"));
            var predicate3Asterisks = DirectoryFiltering.CreateMatchesPredicate("a/**");
            // a path shorter than the asterisks did cause an issue
            Assert.True(predicate3Asterisks("a/b"));
        }

        [Test]
        public void DirectoryFiltering_MatchOnlyAtEndEndComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models/*Thing");
            Assert.False(predicate("Assets/Models/NoIDontThinkIllSayIt"));
            Assert.True(predicate("Assets/Models/ThisHasSomeTextBeforeThing"));
            Assert.False(predicate("Assets/Models/ILikeToSayThingSoIWill"));
            Assert.True(predicate("Assets/Models/ThingThingThingThingThingThing"));
            Assert.True(predicate("Assets/Models/YesThereWasAProblemWithThisPatternThiThing"));

            var predicate1 = DirectoryFiltering.CreateMatchesPredicate("Assets/Models/*TToo");
            Assert.True(predicate1("Assets/Models/MultipleCharactersCausedAProblemTTToo"));
        }

        [Test]
        public void DirectoryFiltering_MatchOnlyAtEndMiddleComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/*Mod/*");
            Assert.False(predicate("Assets/Models/ThisWontMatch"));
            Assert.False(predicate("Assets/Textures/ThisWontEither"));
            Assert.True(predicate("Assets/Mod/ThisWillThough"));
            Assert.True(predicate("Assets/ModMod/ThisWillThough"));
            Assert.True(predicate("Assets/MyMod/AndThisWillToo"));
        }

        [Test]
        public void DirectoryFiltering_MatchAnywhereEndComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models/*Thing*");
            Assert.True(predicate("Assets/Models/Thing"));
            Assert.False(predicate("Assets/Models/NoIDontThinkIllSayIt"));
            Assert.True(predicate("Assets/Models/ThisHasSomeTextBeforeThing"));
            Assert.True(predicate("Assets/Models/ILikeToSayThingSoIWill"));
            Assert.True(predicate("Assets/Models/ThingThingThingThingThingThing"));
            Assert.True(predicate("Assets/Models/YesThereWasAProblemWithThisPatternThiThing"));
        }

        [Test]
        public void DirectoryFiltering_MatchAnywhereMiddleComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/*Mod*/T*");
            Assert.True(predicate("Assets/Models/T"));
            Assert.True(predicate("Assets/Models/ThisWillMatch"));
            Assert.False(predicate("Assets/Textures/ThisWont"));
            Assert.True(predicate("Assets/Mod/ThisWillToo"));
            Assert.True(predicate("Assets/GameMods/ThereCanBeStuffBeforeItToo"));
        }

        [Test]
        public void DirectoryFiltering_MatchBeginningOfComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models/A*");
            Assert.True(predicate("Assets/Models/A"));
            Assert.True(predicate("Assets/Models/ARandomTestString"));
            Assert.True(predicate("Assets/Models/AnotherRandomTestString"));
            Assert.False(predicate("Assets/Models/YouExpectedAnotherRandomStringButItWasMeDio"));
        }

        [Test]
        public void DirectoryFiltering_MatchBeginningAndAnywhereOfComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models/A*String");
            Assert.True(predicate("Assets/Models/AString"));
            Assert.True(predicate("Assets/Models/ARandomTestString"));
            Assert.True(predicate("Assets/Models/AnotherRandomTestString"));
            Assert.False(predicate("Assets/Models/YouExpectedAnotherRandomStringButItWasMeDio"));
        }

        [Test]
        public void DirectoryFiltering_MatchChainedAnywhereInComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models/A*Random*String");
            Assert.True(predicate("Assets/Models/ARandomTestString"));
            Assert.True(predicate("Assets/Models/AnotherRandomTestString"));
            Assert.True(predicate("Assets/Models/AnotherRandomThingString"));
            Assert.False(predicate("Assets/Models/AnotherStringThatIsRandom"));
            Assert.False(predicate("Assets/Models/YouExpectedAnotherRandomStringButItWasMeDio"));
        }

        [Test]
        public void DirectoryFiltering_WildcardComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/*/Models");
            Assert.True(predicate("Assets/Kono/Models"));
            Assert.False(predicate("Assets/Dio"));
            Assert.True(predicate("Assets/Da/Models"));
            Assert.False(predicate("Assets/Models"));
        }

        [Test]
        public void DirectoryFiltering_LeadingForwardSlash()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models");
            Assert.True(predicate("/Assets/Models"));
        }

        [Test]
        public void DirectoryFiltering_StopShortMiddleComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/ModelsUnused");
            Assert.False(predicate("Assets/Models/Models"));
        }

        [Test]
        public void DirectoryFiltering_MoreSpecificMatches()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models");
            Assert.True(predicate("Assets/Models/Unused"));
            Assert.False(predicate("Assets"));
        }

        [Test]
        public void DirectoryFiltering_ComponentTooLong()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models");
            Assert.False(predicate("Assets/ModelsUnused"));
        }

        [Test]
        public void DirectoryFiltering_IncorrectCharacter()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Modei");
            Assert.False(predicate("Assets/Model"));
        }

        [Test]
        public void DirectoryFiltering_StopShort()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models/Unused");
            Assert.False(predicate("Assets/Models"));
        }

        [Test]
        public void DirectoryFiltering_StopShortEndComponent()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/ModelsUnused");
            Assert.False(predicate("Assets/Models"));
        }

        [Test]
        public void DirectoryFiltering_Capitalization()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("aSsEtS/mOdElS");
            Assert.True(predicate("AsSeTs/mOdElS"));
        }

        [Test]
        public void DirectoryFiltering_ExactMatch()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/Models");
            Assert.True(predicate("Assets/Models"));
        }

        [Test]
        public void DirectoryFiltering_NonASCIICharacter()
        {
            var predicate = DirectoryFiltering.CreateMatchesPredicate("Assets/À");
            Assert.True(predicate("Assets/À"));
            Assert.True(predicate("Assets/à"));
        }
    }
}
