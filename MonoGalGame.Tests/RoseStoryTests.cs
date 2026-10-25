using System.Text.RegularExpressions;
using MonoGalGame.Core.Storage;
using MonoGalGame.Core.Stories;
using Xunit;

namespace MonoGalGame.Tests;

public sealed class RoseStoryTests
{
    private static StoryDocument Story() => StoryDocument.Load(Path.Combine(AppContext.BaseDirectory, "Rose", "Stories", "main.json"));

    public static IEnumerable<object[]> Routes()
    {
        for (var first = 0; first < 2; first++)
            for (var second = 0; second < 2; second++)
                for (var route = 0; route < 3; route++)
                    yield return [first, second, route];
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public void EveryChoiceCombinationReachesItsIndependentEnding(int first, int second, int route)
    {
        var player = new StoryPlayer(Story());
        var choices = new[] { first, second, route };
        var choiceCount = 0;
        var dialogueCount = 0;
        var conditionalLines = new HashSet<string>();
        for (var step = 0; !player.IsEnded && step < 3000; step++)
        {
            if (player.CurrentNode.Kind == NodeKind.Choice)
            {
                Assert.InRange(choiceCount, 0, 2);
                Assert.Equal($"q{choiceCount + 1}", player.CurrentNodeId);
                Assert.Equal(choiceCount == 2 ? 3 : 2, player.AvailableChoices.Count);
                player.Choose(choices[choiceCount++]);
            }
            else
            {
                conditionalLines.Add(player.CurrentNode.Text);
                dialogueCount++;
                player.Advance();
            }
        }
        Assert.True(player.IsEnded);
        Assert.Equal(3, choiceCount);
        Assert.InRange(dialogueCount, 550, 800);
        Assert.Equal($"ending-{(char)('a' + route)}", player.CurrentNodeId);
        Assert.Equal(first + 1, player.Variables["q1"]);
        Assert.Equal(second + 1, player.Variables["q2"]);
        Assert.Equal(route + 1, player.Variables["q3"]);
        Assert.Equal(first == 0, conditionalLines.Contains("你听过我说他。你知道我留着那些东西，不只是舍不得。"));
        Assert.Equal(first == 1, conditionalLines.Contains("我想跟你说以前的事。有时候看你笑，我又觉得，你是不是不想听。"));
        Assert.Equal(second == 0, conditionalLines.Contains("我知道你拒绝了相亲。可住在哪儿、什么时候去看我，这些还是没说。"));
        Assert.Equal(second == 1, conditionalLines.Contains("周阿姨来的那天，我其实一点都没觉得无所谓。我说吃饭，是因为我不知道还能跟你要什么。"));
    }

    [Fact]
    public void EveryOriginalSpokenLineIsPreservedExactlyOnceAndDirectionsStayOutOfDialogue()
    {
        var story = Story();
        var expected = new List<string>();
        foreach (var filename in new[] { "01_共通剧情.txt", "03_分支A_两人的尺寸.txt", "04_分支B_合身的告别.txt", "05_分支C_没有日期的以后.txt" })
            foreach (var raw in File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Rose", "Scripts", filename)))
            {
                var line = raw.Trim();
                if (line.StartsWith('《') || line.StartsWith("入口：") || line.StartsWith("阅读入口：")) continue;
                var match = Regex.Match(line, @"^(?:【旁白·.+?】|[^：【]{1,24}：)(.*)$");
                if (match.Success) expected.Add(match.Groups[1].Value);
            }
        var actual = story.Nodes.Values.Where(n => n.Kind == NodeKind.Dialogue).Select(n => n.Text).ToList();
        Assert.Equal(1085, actual.Count);
        Assert.Equal(expected.Order(), actual.Order());
        Assert.DoesNotContain(actual, text => text.Contains("【演出】") || text.Contains("纱BG") || text.Contains("【制作备注】"));
    }

    [Fact]
    public void AllThreeEndingsAndAllReactionBlocksAreReachableFromTheOpening()
    {
        var story = Story();
        var visited = new HashSet<string>();
        var pending = new Stack<string>();
        pending.Push(story.Start);
        while (pending.TryPop(out var id))
        {
            if (!visited.Add(id)) continue;
            var node = story.Nodes[id];
            if (node.Next is { } next) pending.Push(next);
            if (node.Then is { } then) pending.Push(then);
            if (node.Else is { } otherwise) pending.Push(otherwise);
            foreach (var choice in node.Choices) pending.Push(choice.Next);
        }
        Assert.Equal(story.Nodes.Count, visited.Count);
        Assert.Equal(3, story.Nodes.Values.Count(n => n.Kind == NodeKind.End));
        Assert.Equal(3, story.Nodes.Values.Count(n => n.Kind == NodeKind.Choice));
    }

    [Fact]
    public void FirstMeetingAndSummerKissRespectTheScriptsSilenceDirections()
    {
        var story = Story();
        var illustrations = story.Nodes.Values.Where(n => n.Scene?.Background is "cg01" or "cg04").ToList();
        Assert.NotEmpty(illustrations);
        Assert.All(illustrations, node => Assert.Equal("", node.Scene!.Music));
    }

    [Fact]
    public void SavedDualPortraitScenePreservesChapterTimeAndViewpoint()
    {
        var story = Story();
        var player = new StoryPlayer(story);
        while (player.CurrentNode.Speaker != "唐见月") player.Advance();
        var scene = player.Scene;
        Assert.Equal("tang-jianyue-work", scene.Portrait);
        Assert.Equal("ye-zhao-work", scene.SecondaryPortrait);
        Assert.Contains("储物间里的灯", scene.Chapter);
        Assert.Contains("2029", scene.Time);
        Assert.Equal("叶照", scene.Viewpoint);
        var directory = Path.Combine(Path.GetTempPath(), "monogalgame-rose-test-" + Guid.NewGuid());
        try
        {
            var saves = new SaveStore(directory, "rose-and-old-wedding-dress");
            saves.Save(1, player.Capture());
            var restored = new StoryPlayer(story);
            restored.Restore(saves.Load(1).Snapshot);
            Assert.Equal(scene, restored.Scene);
            Assert.Equal(player.CurrentNodeId, restored.CurrentNodeId);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void EveryAuthoredCgHasItsOwnSceneAndClearsAllPortraitLayers()
    {
        var illustrations = Story().Nodes.Values.Where(n => n.Scene?.Background?.StartsWith("cg", StringComparison.Ordinal) == true).ToArray();
        Assert.Equal(36, illustrations.Select(n => n.Scene!.Background).Distinct().Count());
        Assert.All(illustrations, n => { Assert.Equal("", n.Scene!.Portrait); Assert.Equal("", n.Scene.SecondaryPortrait); Assert.Equal("", n.Scene.TertiaryPortrait); });
    }

    [Theory]
    [InlineData("纱S01", "新娘的裙摆破了", "bg02-hallway", "ye-zhao-work", "luo-xiaoan")]
    [InlineData("纱S02", "又坏了？", "bg03", "tang-jianyue", "ye-zhao")]
    [InlineData("纱S04", "她上楼拿布", "bg07", "tang-jianyue", "ye-zhao")]
    [InlineData("纱S06", "我买了一瓶水", "bg05-day", "ye-zhao", "")]
    [InlineData("纱S06", "你一直在？", "bg05-day", "tang-jianyue", "ye-zhao")]
    [InlineData("B04", "没有非要说什么", "bg09", "ye-zhao", "")]
    [InlineData("B05", "叶照去买菜", "bg04", "tang-jianyue", "")]
    [InlineData("B07a", "我今天有点想你", "bg09", "ye-zhao", "")]
    [InlineData("B07a", "我把电话放下", "bg07", "tang-jianyue", "")]
    [InlineData("C06", "信我写了", "bg09", "ye-zhao", "")]
    [InlineData("C08", "好。你也保重", "bg04", "tang-jianyue", "")]
    [InlineData("C08", "我收到那件外套", "bg11", "ye-zhao", "")]
    [InlineData("C08", "寄走东西以后", "bg04", "tang-jianyue", "")]
    [InlineData("A06", "搬家一样搬工作台", "bg08-furnished", "tang-jianyue", "ye-zhao")]
    public void PhysicalPresenceAndLocationFollowTheScript(string section, string text, string background, string portrait, string secondary)
    {
        var node = Assert.Single(Story().Nodes, n => n.Key.StartsWith(section + "-", StringComparison.Ordinal) && n.Value.Text.StartsWith(text, StringComparison.Ordinal)).Value;
        Assert.Equal(background, node.Scene!.Background?.Replace("-night", "").Replace("-sorted", "").Replace("-winter", "").Replace("-spring", "").Replace("-summer", ""));
        static string? Identity(string? texture) => texture?.Replace("-late-cardigan", "").Replace("-phone-tense", "").Replace("-phone", "").Replace("-thoughtful", "").Replace("-empty", "").Replace("-home", "").Replace("-measuring", "").Replace("-residency", "");
        Assert.Equal(portrait, Identity(node.Scene.Portrait));
        Assert.Equal(secondary, Identity(node.Scene.SecondaryPortrait));
    }

    [Fact]
    public void PersonalMemoryAndTwoStageWorkroomRemainDistinct()
    {
        var story = Story();
        var memory = Assert.Single(story.Nodes.Values, n => n.Speaker == "程雪（回忆）");
        Assert.Equal("memory", memory.Scene!.Tone);
        Assert.Contains("回忆", memory.Scene.Time);
        Assert.Equal("ye-zhao-thoughtful", memory.Scene.Portrait);
        Assert.Equal("", memory.Scene.SecondaryPortrait);
        Assert.DoesNotContain(story.Nodes.Where(n => n.Key.StartsWith("纱S07-", StringComparison.Ordinal)),
            n => n.Value.Scene?.Background == "bg08-furnished");
        Assert.DoesNotContain(story.Nodes.Where(n => n.Key.StartsWith("C", StringComparison.Ordinal)),
            n => n.Value.Scene?.Background == "cg09");
    }

    [Fact]
    public void DifficultCallsAndFinalSolitudeUseThoughtfulPortraits()
    {
        var nodes = Story().Nodes;
        var call = Assert.Single(nodes, n => n.Key.StartsWith("C06-", StringComparison.Ordinal) && n.Value.Text.StartsWith("信我写了", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-residency-phone-tense", call.Scene!.Portrait);
        var alone = Assert.Single(nodes, n => n.Key.StartsWith("C08-", StringComparison.Ordinal) && n.Value.Text.StartsWith("寄走东西以后", StringComparison.Ordinal)).Value;
        Assert.Equal("tang-jianyue-late-cardigan", alone.Scene!.Portrait);
        Assert.Equal("", alone.Scene.SecondaryPortrait);
    }

    [Fact]
    public void FamilyQuestionsAndReadingTheLetterKeepTheRequestedQuiet()
    {
        var nodes = Story().Nodes;
        var family = Assert.Single(nodes, n => n.Key.StartsWith("纱S06-", StringComparison.Ordinal) && n.Value.Speaker == "唐玉芬" && n.Value.Text.StartsWith("见月，我给你", StringComparison.Ordinal)).Value;
        Assert.Equal("", family.Scene!.Music);
        Assert.All(nodes.Values.Where(n => n.Scene?.Background == "cg08"), n => Assert.Equal("", n.Scene!.Music));
        var choice = nodes["q3"];
        Assert.Equal("letter", choice.Scene!.Music);
    }

    [Fact]
    public void DomesticConversationsPutAwayWorkPropsAndUseTheAppropriateExpression()
    {
        var nodes = Story().Nodes;
        var calm = Assert.Single(nodes, n => n.Key.StartsWith("A03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("第二天早上", StringComparison.Ordinal)).Value;
        Assert.Equal("tang-jianyue-home", calm.Scene!.Portrait);
        Assert.Equal("ye-zhao-home", calm.Scene.SecondaryPortrait);
        var difficult = Assert.Single(nodes, n => n.Key.StartsWith("A03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我们第三天就吵了一点", StringComparison.Ordinal)).Value;
        Assert.Equal("tang-jianyue-home-thoughtful", difficult.Scene!.Portrait);
        Assert.Equal("ye-zhao-home-thoughtful", difficult.Scene.SecondaryPortrait);
        var alone = Assert.Single(nodes, n => n.Key.StartsWith("纱S09-", StringComparison.Ordinal) && n.Value.Text.StartsWith("楼上修好了一次", StringComparison.Ordinal)).Value;
        Assert.Equal("tang-jianyue-home-sore-wrist", alone.Scene!.Portrait);
        Assert.Equal("", alone.Scene.SecondaryPortrait);
    }

    [Fact]
    public void ShoulderMeasurementsUseTheActionPoseAndEndBeforeTheDomesticScene()
    {
        var nodes = Story().Nodes;
        var measure = Assert.Single(nodes, n => n.Key.StartsWith("纱S04-", StringComparison.Ordinal) && n.Value.Text == "胳膊抬一下。").Value;
        Assert.Equal("tang-jianyue-measuring", measure.Scene!.Portrait);
        Assert.Equal("ye-zhao", measure.Scene.SecondaryPortrait);
        var later = Assert.Single(nodes, n => n.Key.StartsWith("A02-", StringComparison.Ordinal) && n.Value.Text.StartsWith("先说房子吧", StringComparison.Ordinal)).Value;
        Assert.Equal("tang-jianyue-home-thoughtful", later.Scene!.Portrait);
        Assert.Equal("bg07-sorted-winter-night", later.Scene.Background);
    }

    [Fact]
    public void TemporaryBorrowedStudioIsDistinctFromTheFinalSharedWorkroom()
    {
        var nodes = Story().Nodes;
        var temporary = nodes.Where(n => n.Value.Scene?.Background == "bg06-temporary").ToArray();
        Assert.NotEmpty(temporary);
        Assert.All(temporary, n => Assert.StartsWith("A04-", n.Key));
        Assert.Contains(temporary, n => n.Value.Text.StartsWith("搬桌子比我想得累", StringComparison.Ordinal));
        Assert.DoesNotContain(nodes.Where(n => n.Key.StartsWith("A04-", StringComparison.Ordinal)), n => n.Value.Scene?.Background == "bg08-furnished");
    }

    [Theory]
    [InlineData("纱S06", "见月，我给你拿了点", "tang-yufen")]
    [InlineData("纱S06", "叶摄影？你怎么也在楼上", "tang-yufen-stern")]
    [InlineData("纱S06", "我跟见月说几句话", "zhou-suqin")]
    public void FamilyVisitsKeepBothLeadsOnStageWithTheirVisitor(string section, string text, string visitor)
    {
        var node = Assert.Single(Story().Nodes, n => n.Key.StartsWith(section + "-", StringComparison.Ordinal) && n.Value.Text.StartsWith(text, StringComparison.Ordinal)).Value;
        Assert.StartsWith("tang-jianyue", node.Scene!.Portrait);
        Assert.StartsWith("ye-zhao", node.Scene.SecondaryPortrait);
        Assert.Equal(visitor, node.Scene.TertiaryPortrait);
    }

    [Theory]
    [InlineData("纱S09", "你真要跟她这么过", "tang-yufen-stern")]
    [InlineData("纱S09", "这些怎么都收起来了", "zhou-suqin-empty-handed")]
    public void VisitsDuringSeparationShowTheActualVisitorBesideTang(string section, string text, string visitor)
    {
        var node = Assert.Single(Story().Nodes, n => n.Key.StartsWith(section + "-", StringComparison.Ordinal) && n.Value.Text.StartsWith(text, StringComparison.Ordinal)).Value;
        Assert.StartsWith("tang-jianyue", node.Scene!.Portrait);
        Assert.Equal(visitor, node.Scene.SecondaryPortrait);
        Assert.Equal("", node.Scene.TertiaryPortrait);
    }

    [Theory]
    [InlineData("纱S06", "我买了一瓶水")]
    [InlineData("纱S09", "我给叶照打了电话")]
    [InlineData("A05", "我是不是让你很难堪")]
    [InlineData("C05a", "姨妈气得走了")]
    public void FamilySpritesLeaveAtTheAuthoredDeparture(string section, string text)
    {
        var node = Assert.Single(Story().Nodes, n => n.Key.StartsWith(section + "-", StringComparison.Ordinal) && n.Value.Text.StartsWith(text, StringComparison.Ordinal)).Value;
        var portraits = new[] { node.Scene!.Portrait, node.Scene.SecondaryPortrait, node.Scene.TertiaryPortrait };
        Assert.DoesNotContain(portraits, id => id!.StartsWith("zhou-suqin", StringComparison.Ordinal) || id.StartsWith("tang-yufen", StringComparison.Ordinal));
    }

    [Fact]
    public void NightWindowsFollowTimeAndNeverReplaceTheDaytimeOrCgArt()
    {
        var nodes = Story().Nodes;
        var night = Assert.Single(nodes, n => n.Key.StartsWith("A03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我们第三天就吵了一点", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-sorted-winter-night", night.Scene!.Background);
        Assert.Equal("night-painted", night.Scene.Tone);
        var morning = Assert.Single(nodes, n => n.Key.StartsWith("A03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("第二天早上", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-sorted-winter", morning.Scene!.Background);
        Assert.Equal("day", morning.Scene.Tone);
        var distance = Assert.Single(nodes, n => n.Key.StartsWith("C06-", StringComparison.Ordinal) && n.Value.Text.StartsWith("信我写了", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-summer-night", distance.Scene!.Background);
        Assert.Equal("", distance.Scene.SecondaryPortrait);
        Assert.Equal("", distance.Scene.TertiaryPortrait);
        Assert.All(nodes.Values.Where(n => n.Scene?.Background?.StartsWith("cg", StringComparison.Ordinal) == true), n => Assert.Equal("day", n.Scene!.Tone));
        Assert.Contains(nodes.Values, n => n.Scene?.Background == "bg06-night" && n.Scene.Tone == "rain-night-painted");
    }

    [Fact]
    public void GroceryBagIsPutDownAfterItIsAcceptedAndBeforeDinner()
    {
        var nodes = Story().Nodes;
        var arrival = Assert.Single(nodes, n => n.Key.StartsWith("A05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("周素琴来的时候", StringComparison.Ordinal)).Value;
        Assert.Equal("zhou-suqin", arrival.Scene!.TertiaryPortrait);
        var handedOver = Assert.Single(nodes, n => n.Key.StartsWith("A05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我回去还有事", StringComparison.Ordinal)).Value;
        Assert.Equal("zhou-suqin-empty-handed", handedOver.Scene!.TertiaryPortrait);
        var dinner = Assert.Single(nodes, n => n.Key.StartsWith("A05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("地滑，慢点", StringComparison.Ordinal)).Value;
        Assert.Equal("cg30", dinner.Scene!.Background);
        Assert.Equal("", dinner.Scene.TertiaryPortrait);
    }

    [Fact]
    public void BowlAndSpongeAppearOnlyDuringWashingAndArePutDownBeforeTheTalk()
    {
        var nodes = Story().Nodes;
        var first = Assert.Single(nodes, n => n.Key.StartsWith("纱S03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("饭后我抢着洗碗", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-washing", first.Scene!.SecondaryPortrait);
        var tense = Assert.Single(nodes, n => n.Key.StartsWith("A05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("她走时仍不肯让我送", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-washing-thoughtful", tense.Scene!.SecondaryPortrait);
        var dry = Assert.Single(nodes, n => n.Key.StartsWith("A05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("她低头。我把水关了", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-home-thoughtful", dry.Scene!.SecondaryPortrait);
    }

    [Fact]
    public void TearsFollowExplicitCryingAndDoNotAppearInTheSceneThatSaysSheDidNotCry()
    {
        var nodes = Story().Nodes;
        var crying = Assert.Single(nodes, n => n.Key.StartsWith("纱S09-", StringComparison.Ordinal) && n.Value.Text.StartsWith("说完我哭了", StringComparison.Ordinal)).Value;
        Assert.Equal("cg29", crying.Scene!.Background);
        Assert.Equal("", crying.Scene.Portrait);
        Assert.Equal("", crying.Scene.SecondaryPortrait);
        var notCrying = Assert.Single(nodes, n => n.Key.StartsWith("A03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我坐在地上，抱了一会儿", StringComparison.Ordinal)).Value;
        Assert.Equal("cg23", notCrying.Scene!.Background);
        Assert.Equal("", notCrying.Scene.Portrait);
        Assert.Contains("没哭", notCrying.Text);
    }

    [Fact]
    public void SupplementalTowelCgCoversTheExactActionAndReturnsToTearfulPortraits()
    {
        var story = Story();
        var cg = Assert.Single(story.Nodes, n => n.Value.Scene?.Background == "cg11");
        Assert.StartsWith("A05-", cg.Key);
        Assert.Contains("见月拿毛巾给我擦手", cg.Value.Text);
        Assert.Equal("", cg.Value.Scene!.Portrait);
        Assert.Equal("", cg.Value.Scene.SecondaryPortrait);
        Assert.Equal("", cg.Value.Scene.TertiaryPortrait);
        var after = story.Nodes[cg.Value.Next!];
        Assert.StartsWith("过了一会儿，我说菜做得有点多", after.Text);
        Assert.Equal("bg07-repaired-night", after.Scene!.Background);
        Assert.Equal("ye-zhao-home-crying", after.Scene.SecondaryPortrait);
        Assert.Equal("tang-jianyue-home-tearful", after.Scene.Portrait);
        Assert.DoesNotContain(story.Nodes.Where(n => n.Key.StartsWith("B", StringComparison.Ordinal) || n.Key.StartsWith("C", StringComparison.Ordinal)), n => n.Value.Scene?.Background == "cg11");
    }

    [Fact]
    public void DepartureMorningUsesDaylightAndLeavesOnlyTangWithTheTowel()
    {
        var nodes = Story().Nodes;
        var morning = Assert.Single(nodes, n => n.Key.StartsWith("B02-", StringComparison.Ordinal) && n.Value.Text.StartsWith("第三天早上，行李箱放到门边", StringComparison.Ordinal)).Value;
        Assert.Equal("day", morning.Scene!.Tone);
        Assert.Equal("bg07-sorted-winter", morning.Scene.Background);
        Assert.Contains("早晨", morning.Scene.Time);
        Assert.Equal("ye-zhao-washing-thoughtful", morning.Scene.SecondaryPortrait);
        var doorway = Assert.Single(nodes, n => n.Key.StartsWith("B02-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我点头。门铃响了一声", StringComparison.Ordinal)).Value;
        Assert.Equal("bg03", doorway.Scene!.Background);
        Assert.Equal("tang-jianyue-towel", doorway.Scene.Portrait);
        Assert.Equal("", doorway.Scene.SecondaryPortrait);
        Assert.Equal("叶照", doorway.Scene.Viewpoint);
    }

    [Fact]
    public void ResidencyRoomPutsTheCameraAwayWhileKeepingTheVisitorPresent()
    {
        var story = Story();
        var residency = story.Nodes.Values.Where(n => n.Scene?.Background is "bg09" or "bg09-night" or "bg09-winter" or "bg09-winter-night" or "bg09-spring" or "bg09-spring-night" or "bg09-summer" or "bg09-summer-night" or "bg11" or "bg11-night").ToArray();
        Assert.NotEmpty(residency);
        foreach (var node in residency)
            foreach (var portrait in new[] { node.Scene!.Portrait, node.Scene.SecondaryPortrait, node.Scene.TertiaryPortrait })
                if (portrait?.StartsWith("ye-zhao", StringComparison.Ordinal) == true)
                    Assert.True(portrait.StartsWith("ye-zhao-residency", StringComparison.Ordinal) ||
                        portrait is "ye-zhao-returned-coat-crying" or "ye-zhao-returned-coat-message");
        var visit = Assert.Single(story.Nodes, n => n.Key.StartsWith("B03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我去拿水，再回来", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-residency", visit.Scene!.SecondaryPortrait);
        Assert.Equal("tang-jianyue-home", visit.Scene.Portrait);
    }

    [Fact]
    public void NightTailoringAndDaytimeCoatReturnKeepTheirOwnLighting()
    {
        var story = Story();
        var contract = Assert.Single(story.Nodes, n => n.Key.StartsWith("纱S08-", StringComparison.Ordinal) && n.Value.Text.StartsWith("她没答。我不敢再等", StringComparison.Ordinal));
        Assert.Equal("bg04-night", contract.Value.Scene!.Background);
        Assert.Equal("cool-painted", contract.Value.Scene.Tone);
        var closure = Assert.Single(story.Nodes, n => n.Key.StartsWith("C08-", StringComparison.Ordinal) && n.Value.Text.StartsWith("寄走东西以后", StringComparison.Ordinal)).Value;
        Assert.Equal("bg04-winter-night", closure.Scene!.Background);
        Assert.Equal("night-painted", closure.Scene.Tone);
        var day = Assert.Single(story.Nodes, n => n.Key.StartsWith("C08-", StringComparison.Ordinal) && n.Value.Text.StartsWith("好。你也保重", StringComparison.Ordinal)).Value;
        Assert.Equal("bg04-winter", day.Scene!.Background);
    }

    [Fact]
    public void SortingAppearsWhenTheMotherReturnsAndDoesNotHappenBeforeOctober()
    {
        var nodes = Story().Nodes;
        var early = Assert.Single(nodes, n => n.Key.StartsWith("纱S04-", StringComparison.Ordinal) && n.Value.Text.StartsWith("她上楼拿布", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07", early.Scene!.Background);
        var before = Assert.Single(nodes, n => n.Key.StartsWith("纱S09-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我给叶照打了电话", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-autumn", before.Scene!.Background);
        var sorted = Assert.Single(nodes, n => n.Key.StartsWith("纱S09-", StringComparison.Ordinal) && n.Value.Text.StartsWith("这些怎么都收起来了", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-sorted-autumn", sorted.Scene!.Background);
        Assert.DoesNotContain(nodes.Where(n => n.Key.StartsWith("纱S07-", StringComparison.Ordinal)), n => n.Value.Scene?.Background?.Contains("sorted", StringComparison.Ordinal) == true);
    }

    [Theory]
    [InlineData("A03")]
    [InlineData("B05")]
    [InlineData("C07")]
    public void EveryBranchKeepsTheOrganizedRoomInsteadOfRestoringTheOldClutter(string section)
    {
        var roomScenes = Story().Nodes.Where(n => n.Key.StartsWith(section + "-", StringComparison.Ordinal) && n.Value.Scene?.Background?.StartsWith("bg07", StringComparison.Ordinal) == true).ToArray();
        Assert.NotEmpty(roomScenes);
        Assert.All(roomScenes, n => Assert.StartsWith("bg07-sorted", n.Value.Scene!.Background));
    }

    [Fact]
    public void RainNightAndDawnShareTheirWetRoomStateUntilTheCutBackToWork()
    {
        var nodes = Story().Nodes;
        var night = Assert.Single(nodes, n => n.Key.StartsWith("纱S07-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我拿着背景布过去", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-rain-night", night.Scene!.Background);
        Assert.Equal("rain-night-painted", night.Scene.Tone);
        var dawn = Assert.Single(nodes, n => n.Key.StartsWith("纱S07-", StringComparison.Ordinal) && n.Value.Text.Contains("快天亮时", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-rain-dawn", dawn.Scene!.Background);
        Assert.Contains("清晨", dawn.Scene.Time);
        Assert.Equal("rain", dawn.Scene.Tone);
        var work = Assert.Single(nodes, n => n.Key.StartsWith("纱S07-", StringComparison.Ordinal) && n.Value.Text.StartsWith("驻留的日期确认了", StringComparison.Ordinal)).Value;
        Assert.Equal("bg06", work.Scene!.Background);
    }

    [Fact]
    public void LandlordAttendsViewingsAndSigningButLeavesBeforeThePrivateConfession()
    {
        var nodes = Story().Nodes;
        var viewing = Assert.Single(nodes, n => n.Key.StartsWith("纱S07-", StringComparison.Ordinal) && n.Value.Text.StartsWith("这里能做摄影", StringComparison.Ordinal)).Value;
        Assert.Equal("liu-qing", viewing.Scene!.TertiaryPortrait);
        Assert.Equal("bg08", viewing.Scene.Background);
        var message = Assert.Single(nodes, n => n.Key.StartsWith("A06-", StringComparison.Ordinal) && n.Value.Text.StartsWith("那间房后来租给过别人", StringComparison.Ordinal)).Value;
        Assert.Equal("", message.Scene!.TertiaryPortrait);
        var signing = Assert.Single(nodes, n => n.Key.StartsWith("A06-", StringComparison.Ordinal) && n.Value.Text.StartsWith("正式签约那天", StringComparison.Ordinal)).Value;
        Assert.Equal("liu-qing-confirming", signing.Scene!.TertiaryPortrait);
        var confession = Assert.Single(nodes, n => n.Key.StartsWith("A06-", StringComparison.Ordinal) && n.Value.Text.StartsWith("叶照，我喜欢你。我想和你一起生活", StringComparison.Ordinal)).Value;
        Assert.Equal("", confession.Scene!.TertiaryPortrait);
        Assert.StartsWith("tang-jianyue", confession.Scene.Portrait);
        Assert.StartsWith("ye-zhao", confession.Scene.SecondaryPortrait);
    }

    [Fact]
    public void QinJiaAppearsForInPersonWorkAndNeverOccupiesThePrivateGuestRoom()
    {
        var nodes = Story().Nodes;
        var discussion = Assert.Single(nodes, n => n.Key.StartsWith("纱S09-", StringComparison.Ordinal) && n.Value.Text.StartsWith("你这组先留着", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-work-area", discussion.Scene!.Background);
        Assert.Equal("qin-jia-thinking", discussion.Scene.SecondaryPortrait);
        var encouragement = Assert.Single(nodes, n => n.Key.StartsWith("纱S09-", StringComparison.Ordinal) && n.Value.Text.StartsWith("那就先不用按原来拍", StringComparison.Ordinal)).Value;
        Assert.Equal("qin-jia", encouragement.Scene!.SecondaryPortrait);
        var call = Assert.Single(nodes, n => n.Key.StartsWith("纱S07-", StringComparison.Ordinal) && n.Value.Text.StartsWith("驻留的日期确认了", StringComparison.Ordinal)).Value;
        Assert.Equal("", call.Scene!.SecondaryPortrait);
        var privateScenes = nodes.Values.Where(n => n.Scene?.Background is "bg09" or "bg09-night" or "bg09-winter" or "bg09-winter-night" or "bg09-spring" or "bg09-spring-night" or "bg09-summer" or "bg09-summer-night" or "bg11" or "bg11-night");
        Assert.All(privateScenes, n => Assert.DoesNotContain(new[] { n.Scene!.Portrait, n.Scene.SecondaryPortrait, n.Scene.TertiaryPortrait }, id => id?.StartsWith("qin-jia", StringComparison.Ordinal) == true));
    }

    [Fact]
    public void SecondWeekNightAndTheFollowingWorkdayKeepTheirOwnTimeAndCast()
    {
        var nodes = Story().Nodes;
        var night = Assert.Single(nodes, n => n.Key.StartsWith("纱S09-", StringComparison.Ordinal) && n.Value.Text.StartsWith("第二周晚上，我收到见月的消息", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-night", night.Scene!.Background);
        Assert.Equal("", night.Scene.SecondaryPortrait);
        var meeting = Assert.Single(nodes, n => n.Key.StartsWith("A01-", StringComparison.Ordinal) && n.Value.Text.StartsWith("第二天我和秦嘉聊工作", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-work-area", meeting.Scene!.Background);
        Assert.Equal("day", meeting.Scene.Tone);
        Assert.Contains("11月27日", meeting.Scene.Time);
        var returned = Assert.Single(nodes, n => n.Key.StartsWith("A01-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我把文件带回房间", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09", returned.Scene!.Background);
        Assert.Equal("", returned.Scene.SecondaryPortrait);
        var dinner = Assert.Single(nodes, n => n.Key.StartsWith("A01-", StringComparison.Ordinal) && n.Value.Text.StartsWith("晚饭时，我拍了外地的最后一份盒饭", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-night", dinner.Scene!.Background);
    }

    [Fact]
    public void SewingLessonMovesOutsideInDaylightAndEndsBeforeTheNightKiss()
    {
        var nodes = Story().Nodes;
        var working = Assert.Single(nodes, n => n.Key.StartsWith("纱S05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("店里热得站不住", StringComparison.Ordinal)).Value;
        Assert.Equal("bg05-day", working.Scene!.Background);
        Assert.Equal("tang-jianyue-handsewing", working.Scene.Portrait);
        Assert.Equal("ye-zhao", working.Scene.SecondaryPortrait);
        Assert.Contains("傍晚", working.Scene.Time);
        var touch = Assert.Single(nodes, n => n.Key.StartsWith("纱S05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("她捏住我拿衣服的手", StringComparison.Ordinal)).Value;
        Assert.Equal("bg05", touch.Scene!.Background);
        Assert.Equal("tang-jianyue-shy", touch.Scene.Portrait);
        Assert.Contains("入夜", touch.Scene.Time);
        Assert.Contains(nodes.Values, n => n.Scene?.Background == "cg04");
    }

    [Fact]
    public void SupplementalSewingCgShowsTheMistakeAndLaughterWithoutChangingTheText()
    {
        var cg = Story().Nodes.Where(n => n.Value.Scene?.Background == "cg12").ToArray();
        Assert.Equal(5, cg.Length);
        Assert.All(cg, n => Assert.StartsWith("纱S05-", n.Key));
        Assert.StartsWith("她教了我两针", cg[0].Value.Text);
        Assert.Contains(cg, n => n.Value.Text.Contains("袖子和衣身牢牢连在了一起", StringComparison.Ordinal));
        Assert.Contains(cg, n => n.Value.Text.Contains("她低着头笑", StringComparison.Ordinal));
        Assert.All(cg, n => Assert.Equal("", n.Value.Scene!.Portrait));
    }

    [Fact]
    public void JanuaryLeakAndRepairedRoomStartAtTheirOwnEvents()
    {
        var nodes = Story().Nodes;
        var leak = Assert.Single(nodes, n => n.Key.StartsWith("A04-", StringComparison.Ordinal) && n.Value.Text.StartsWith("新年第一场雨", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-january-leak", leak.Scene!.Background);
        Assert.Equal("rain", leak.Scene.Tone);
        var before = nodes.Where(n => n.Key.StartsWith("A03-", StringComparison.Ordinal));
        Assert.DoesNotContain(before, n => n.Value.Scene?.Background?.StartsWith("bg07-repaired", StringComparison.Ordinal) == true);
        var returned = Assert.Single(nodes, n => n.Key.StartsWith("A05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("她不说留，也没有立刻走", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-repaired", returned.Scene!.Background);
        var dinner = Assert.Single(nodes, n => n.Key.StartsWith("A05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("那顿饭比我拍过", StringComparison.Ordinal)).Value;
        Assert.Equal("cg30", dinner.Scene!.Background);
        Assert.Equal("day", dinner.Scene.Tone);
        var later = Assert.Single(nodes, n => n.Key.StartsWith("A06-", StringComparison.Ordinal) && n.Value.Text.StartsWith("回去后我们为了钱", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-repaired", later.Scene!.Background);
    }

    [Fact]
    public void IroningCgEndsWithASeparateAfterClosingDiscussionAndLaterDaytime()
    {
        var nodes = Story().Nodes;
        var cg = Assert.Single(nodes, n => n.Value.Scene?.Background == "cg13");
        Assert.StartsWith("临时共享工作室的第一天", cg.Value.Text);
        var closed = nodes[cg.Value.Next!];
        Assert.StartsWith("关门后我们把时间表贴出来", closed.Text);
        Assert.Equal("bg06-temporary-night", closed.Scene!.Background);
        Assert.Equal("night-painted", closed.Scene.Tone);
        Assert.StartsWith("tang-jianyue", closed.Scene.Portrait);
        Assert.StartsWith("ye-zhao", closed.Scene.SecondaryPortrait);
        var later = nodes[closed.Next!];
        Assert.StartsWith("几天后", later.Text);
        Assert.Equal("bg06-temporary", later.Scene!.Background);
        Assert.Equal("day", later.Scene.Tone);
    }

    [Theory]
    [InlineData("B")]
    [InlineData("C")]
    public void JanuaryRepairsAndIroningDoNotLeakIntoTheOtherRoutes(string route)
    {
        var nodes = Story().Nodes.Where(n => n.Key.StartsWith(route, StringComparison.Ordinal));
        Assert.DoesNotContain(nodes, n => n.Value.Scene?.Background?.StartsWith("bg07-repaired", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(nodes, n => n.Value.Scene?.Background is "bg07-january-leak" or "bg06-temporary-night" or "cg13");
        Assert.Contains(nodes, n => n.Value.Scene?.Background?.StartsWith("bg07-sorted", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void VenueStaffStayAtTheirOwnSideOfTheStorageDoor()
    {
        var nodes = Story().Nodes;
        var guide = Assert.Single(nodes, n => n.Value.Speaker == "罗小安").Value;
        Assert.Equal("bg02-hallway", guide.Scene!.Background);
        Assert.Equal("luo-xiaoan", guide.Scene.SecondaryPortrait);
        var entered = Assert.Single(nodes, n => n.Key.StartsWith("纱S01-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我侧身挤进储物间", StringComparison.Ordinal)).Value;
        Assert.Equal("bg01", entered.Scene!.Background);
        Assert.Equal("", entered.Scene.TertiaryPortrait);
        Assert.Equal("ye-zhao-work", entered.Scene.SecondaryPortrait);
        var manager = Assert.Single(nodes, n => n.Value.Speaker == "潘成").Value;
        Assert.Equal("bg02-hallway", manager.Scene!.Background);
        Assert.Equal("pan-cheng", manager.Scene.TertiaryPortrait);
        var left = Assert.Single(nodes, n => n.Key.StartsWith("纱S01-", StringComparison.Ordinal) && n.Value.Text.StartsWith("那个女人终于笑出声", StringComparison.Ordinal)).Value;
        Assert.Equal("bg02", left.Scene!.Background);
        Assert.Equal("", left.Scene.TertiaryPortrait);
    }

    [Fact]
    public void BrideRespondsBeforeAndAfterRepairAndLeavesForTheWedding()
    {
        var nodes = Story().Nodes;
        var worried = Assert.Single(nodes, n => n.Value.Text.StartsWith("是不是很明显", StringComparison.Ordinal)).Value;
        Assert.Equal("he-qing-worried", worried.Scene!.TertiaryPortrait);
        Assert.Equal("bg02", worried.Scene.Background);
        var relieved = Assert.Single(nodes, n => n.Value.Text.StartsWith("那我是不是该请你吃饭", StringComparison.Ordinal)).Value;
        Assert.Equal("he-qing-relieved", relieved.Scene!.TertiaryPortrait);
        var exit = Assert.Single(nodes, n => n.Value.Text.StartsWith("新人终于进场", StringComparison.Ordinal)).Value;
        Assert.Equal("bg02-hallway", exit.Scene!.Background);
        Assert.Equal("", exit.Scene.TertiaryPortrait);
        Assert.DoesNotContain(nodes.Where(n => !n.Key.StartsWith("纱S02-", StringComparison.Ordinal)), n => n.Value.Scene?.TertiaryPortrait?.StartsWith("he-qing", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void VisitorPutsAwayTheSuitcaseAndFoodBeforeSettlingIntoTheRoom()
    {
        var nodes = Story().Nodes;
        var arrival = Assert.Single(nodes, n => n.Key.StartsWith("B03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("见月第一次到外地", StringComparison.Ordinal)).Value;
        Assert.Equal("tang-jianyue-travel", arrival.Scene!.Portrait);
        Assert.Equal("ye-zhao-residency", arrival.Scene.SecondaryPortrait);
        Assert.Equal("bg09-winter", arrival.Scene.Background);
        var settled = Assert.Single(nodes, n => n.Key.StartsWith("B03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("房间比她想象的小", StringComparison.Ordinal)).Value;
        Assert.Equal("tang-jianyue-home", settled.Scene!.Portrait);
        Assert.DoesNotContain(nodes.Where(n => !n.Key.StartsWith("B03-", StringComparison.Ordinal)), n => n.Value.Scene?.Portrait == "tang-jianyue-travel");
    }

    [Fact]
    public void SeatedHandworkCgEndsWhenThePhotographerReturnsAtThreeThirty()
    {
        var nodes = Story().Nodes;
        var cg = Assert.Single(nodes, n => n.Value.Scene?.Background == "cg14");
        Assert.StartsWith("B03-", cg.Key);
        Assert.StartsWith("她真带了针线", cg.Value.Text);
        Assert.Equal("", cg.Value.Scene!.Portrait);
        var returned = nodes[cg.Value.Next!];
        Assert.StartsWith("两点没回来", returned.Text);
        Assert.Equal("bg09-winter", returned.Scene!.Background);
        Assert.Equal("day", returned.Scene.Tone);
        Assert.Contains("15:30", returned.Scene.Time);
        Assert.Equal("tang-jianyue-home-thoughtful", returned.Scene.Portrait);
        Assert.Equal("ye-zhao-residency-thoughtful", returned.Scene.SecondaryPortrait);
    }

    [Fact]
    public void ThirdNightAndFourthDayDepartureStopShowingAnAwakeOrAbsentVisitor()
    {
        var nodes = Story().Nodes;
        var night = Assert.Single(nodes, n => n.Key.StartsWith("B03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("晚上她睡着了", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-winter-night", night.Scene!.Background);
        Assert.Equal("night-painted", night.Scene.Tone);
        Assert.Equal("唐见月", night.Scene.Viewpoint);
        Assert.Equal("", night.Scene.Portrait);
        Assert.Equal("", night.Scene.SecondaryPortrait);
        var gone = Assert.Single(nodes, n => n.Key.StartsWith("B03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("她走后，抽屉少了一半", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-winter", gone.Scene!.Background);
        Assert.Equal("day", gone.Scene.Tone);
        Assert.Equal("叶照", gone.Scene.Viewpoint);
        Assert.Equal("ye-zhao-residency-thoughtful", gone.Scene.Portrait);
        Assert.Equal("", gone.Scene.SecondaryPortrait);
    }

    [Fact]
    public void WinterRoomFollowsExplicitWinterDatesWhileNovemberKeepsAutumn()
    {
        var nodes = Story().Nodes;
        var november = nodes.First(n => n.Key.StartsWith("A01-", StringComparison.Ordinal) && n.Value.Scene is not null).Value;
        Assert.Equal("bg09-night", november.Scene!.Background);
        var december = nodes.First(n => n.Key.StartsWith("C02-", StringComparison.Ordinal) && n.Value.Scene is not null).Value;
        Assert.Equal("bg09-winter", december.Scene!.Background);
        var january = nodes.First(n => n.Key.StartsWith("B04-", StringComparison.Ordinal) && n.Value.Scene is not null).Value;
        Assert.Equal("bg09-winter-night", january.Scene!.Background);
        var late = Assert.Single(nodes, n => n.Key.StartsWith("C08-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我收到那件外套", StringComparison.Ordinal)).Value;
        Assert.Equal("bg11", late.Scene!.Background);
    }

    [Fact]
    public void InvitationMonthsAdvanceAndThePhoneIsPutAwayAfterTheJuneCall()
    {
        var nodes = Story().Nodes;
        var march = Assert.Single(nodes, n => n.Key.StartsWith("C04-", StringComparison.Ordinal) && n.Value.Text.StartsWith("三月她发了一张屋顶修后的照片", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-spring", march.Scene!.Background);
        var may = Assert.Single(nodes, n => n.Value.Text.StartsWith("五月余下那些日子都很好", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-residency-phone", may.Scene!.Portrait);
        Assert.Equal("bg09-spring", may.Scene.Background);
        var june = Assert.Single(nodes, n => n.Value.Text.StartsWith("十二号那天可能去不了", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-summer", june.Scene!.Background);
        Assert.Equal("ye-zhao-residency-phone-tense", june.Scene.Portrait);
        var ended = Assert.Single(nodes, n => n.Value.Text.StartsWith("我挂断了电话。过了十分钟", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-residency-thoughtful", ended.Scene!.Portrait);
        Assert.Contains("通话后", ended.Scene.Time);
    }

    [Fact]
    public void ExhibitionDayReturnsToSummerNightBeforeTheCallAndViewpointCut()
    {
        var nodes = Story().Nodes;
        var opening = Assert.Single(nodes, n => n.Key.StartsWith("C05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("开幕那天", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-summer", opening.Scene!.Background);
        Assert.Equal("day", opening.Scene.Tone);
        var returned = Assert.Single(nodes, n => n.Value.Text.StartsWith("晚上回房，手机里有她", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-summer-night", returned.Scene!.Background);
        var call = Assert.Single(nodes, n => n.Value.Text.StartsWith("她打电话，我接了", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-residency-phone", call.Scene!.Portrait);
        var cut = Assert.Single(nodes, n => n.Value.Text.StartsWith("其实那件裙子可以再晚半天", StringComparison.Ordinal)).Value;
        Assert.Equal("bg04-night", cut.Scene!.Background);
        Assert.Equal("唐见月", cut.Scene.Viewpoint);
        Assert.Equal("", cut.Scene.SecondaryPortrait);
    }

    [Fact]
    public void SpringCallShowsOnlyTheLocalSpeakerAndEndsBeforeTheOtherRoom()
    {
        var nodes = Story().Nodes;
        var call = Assert.Single(nodes, n => n.Key.StartsWith("B07a-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我打过去。她接得很快", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-spring-night", call.Scene!.Background);
        Assert.Equal("ye-zhao-residency-phone", call.Scene.Portrait);
        var ended = Assert.Single(nodes, n => n.Value.Text.StartsWith("挂断之后，我没有轻松起来", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-residency-thoughtful", ended.Scene!.Portrait);
        Assert.Equal("bg09-spring-night", ended.Scene.Background);
        var phones = nodes.Values.Where(n => n.Scene?.Portrait is "ye-zhao-residency-phone" or "ye-zhao-residency-phone-tense").ToArray();
        Assert.NotEmpty(phones);
        Assert.All(phones, n => { Assert.Equal("", n.Scene!.SecondaryPortrait); Assert.Equal("", n.Scene.TertiaryPortrait); });
    }

    [Fact]
    public void AugustArgumentEndsBeforeNovemberAutumnAndUnreadMessages()
    {
        var nodes = Story().Nodes;
        var call = Assert.Single(nodes, n => n.Key.StartsWith("C06-", StringComparison.Ordinal) && n.Value.Text.StartsWith("过几天，我们打了一次长电话", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-summer-night", call.Scene!.Background);
        Assert.Equal("ye-zhao-residency-phone-tense", call.Scene.Portrait);
        var ended = Assert.Single(nodes, n => n.Value.Text.StartsWith("我没有补上解释。以为今晚停住", StringComparison.Ordinal)).Value;
        Assert.Equal("ye-zhao-residency-thoughtful", ended.Scene!.Portrait);
        var november = Assert.Single(nodes, n => n.Value.Text.StartsWith("十一月有一晚", StringComparison.Ordinal)).Value;
        Assert.Equal("bg09-night", november.Scene!.Background);
        Assert.Contains("11月", november.Scene.Time);
        Assert.Equal("ye-zhao-residency-thoughtful", november.Scene.Portrait);
    }

    [Fact]
    public void MovedResidencePersistsFromSpring2031ToTheReturnedCoatIn2033()
    {
        var nodes = Story().Nodes;
        var moved = Assert.Single(nodes, n => n.Value.Text.StartsWith("第二年春天，我换了住处", StringComparison.Ordinal)).Value;
        Assert.Equal("bg11", moved.Scene!.Background);
        Assert.Equal("外地 · 新住处", moved.Scene.Location);
        Assert.Equal("ye-zhao-residency-thoughtful", moved.Scene.Portrait);
        var later = Assert.Single(nodes, n => n.Key.StartsWith("C08-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我收到那件外套", StringComparison.Ordinal)).Value;
        Assert.Equal("bg11", later.Scene!.Background);
        Assert.Contains("2033", later.Scene.Time);
        Assert.DoesNotContain(nodes.Where(n => n.Value.Scene?.Background?.StartsWith("bg11", StringComparison.Ordinal) == true), n => !n.Key.StartsWith("C07-", StringComparison.Ordinal) && !n.Key.StartsWith("C08-", StringComparison.Ordinal));
    }

    [Fact]
    public void NewHomeRainAndTicketSearchClearTheirOwnLightingWithoutDialling()
    {
        var nodes = Story().Nodes;
        var rain = Assert.Single(nodes, n => n.Value.Text.StartsWith("有一晚下大雨，我差点", StringComparison.Ordinal)).Value;
        Assert.Equal("bg11-night", rain.Scene!.Background);
        Assert.Equal("rain-night-painted", rain.Scene.Tone);
        Assert.Equal("ye-zhao-residency-thoughtful", rain.Scene.Portrait);
        var photo = Assert.Single(nodes, n => n.Value.Text.StartsWith("我在照片里看到一对普通人", StringComparison.Ordinal)).Value;
        Assert.Equal("bg11", photo.Scene!.Background);
        Assert.Equal("day", photo.Scene.Tone);
        var night = Assert.Single(nodes, n => n.Value.Text.StartsWith("那晚我很想她", StringComparison.Ordinal)).Value;
        Assert.Equal("bg11-night", night.Scene!.Background);
        Assert.Equal("night-painted", night.Scene.Tone);
        var later = Assert.Single(nodes, n => n.Value.Text.StartsWith("后来我不再每天想起她", StringComparison.Ordinal)).Value;
        Assert.Equal("bg11", later.Scene!.Background);
        Assert.Equal("day", later.Scene.Tone);
    }

    [Fact]
    public void ReunionChangesFromCarryingToWearingOnlyAtTheWrittenFitting()
    {
        var nodes = Story().Nodes;
        var arrival = nodes["B08-003"].Scene!;
        Assert.Equal("bg03-reunion", arrival.Background);
        Assert.Equal("tang-jianyue-reunion", arrival.Portrait);
        Assert.Equal("ye-zhao-reunion-carrying", arrival.SecondaryPortrait);
        var before = nodes["B08-011"].Scene!;
        Assert.Equal("ye-zhao-reunion-carrying", before.SecondaryPortrait);
        Assert.Contains("把新外套穿上", nodes["B08-012"].Text);
        Assert.Equal("ye-zhao-reunion-wearing", nodes["B08-012"].Scene!.SecondaryPortrait);
        Assert.DoesNotContain(nodes.Where(n => !n.Key.StartsWith("B08-", StringComparison.Ordinal)),
            n => n.Value.Scene?.Portrait is "tang-jianyue-reunion" or "ye-zhao-reunion-wearing" ||
                 n.Value.Scene?.SecondaryPortrait is "ye-zhao-reunion-carrying" or "ye-zhao-reunion-wearing");
    }

    [Fact]
    public void PickupKeepsTheTwoDayGapAndFittingCgEndsBeforeTeaAndDeparture()
    {
        var story = Story();
        var pickup = story.Nodes["B08-013"].Scene!;
        Assert.Equal("bg04-winter", pickup.Background);
        Assert.Contains("两日后", pickup.Time);
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "B08-016", Scene = new("cg10b", "", "letter") });
        Assert.Equal("cg10b", player.Scene.Background);
        player.Advance();
        Assert.StartsWith("我们喝了一杯茶", player.CurrentNode.Text);
        Assert.Equal("bg03-reunion", player.Scene.Background);
        Assert.Equal("tang-jianyue-reunion", player.Scene.Portrait);
        Assert.Equal("ye-zhao-reunion-wearing", player.Scene.SecondaryPortrait);
        var outside = story.Nodes["B08-023"].Scene!;
        Assert.Equal("bg05-winter", outside.Background);
        Assert.Equal("ye-zhao-reunion-pocket", outside.Portrait);
        Assert.Equal("", outside.SecondaryPortrait);
        Assert.Contains("没有镜头盖", story.Nodes["B08-023"].Text);
    }

    [Fact]
    public void TangDoesNotCryEarlyAndReturnsToWorkAfterTheNewSolitaryCg()
    {
        var nodes = Story().Nodes;
        Assert.Equal("tang-jianyue-late-cardigan", nodes["C08-001"].Scene!.Portrait);
        Assert.StartsWith("我没有立即哭", nodes["C08-004"].Text);
        Assert.Equal("cg10c", nodes["C08-004"].Scene!.Background);
        Assert.Equal("cg10c", nodes["C08-007"].Scene!.Background);
        Assert.StartsWith("我坐在工作台旁，终于哭了", nodes["C08-008"].Text);
        Assert.Equal("cg15", nodes["C08-008"].Scene!.Background);
        var customer = nodes["C08-009"].Scene!;
        Assert.Equal("bg04-winter", customer.Background);
        Assert.Equal("tang-jianyue-late-cardigan", customer.Portrait);
        Assert.Equal("", customer.SecondaryPortrait);
        Assert.Single(nodes, n => n.Value.Scene?.Background == "cg15");
    }

    [Fact]
    public void ReturnedCoatCryingMessagingAndFoldingAreSeparateSaveableMoments()
    {
        var story = Story();
        var receipt = story.Nodes["C08-016"].Scene!;
        Assert.Equal("ye-zhao-residency-thoughtful", receipt.Portrait);
        var cry = story.Nodes["C08-017"].Scene!;
        Assert.Contains("右内侧", story.Nodes["C08-017"].Text);
        Assert.Equal("ye-zhao-returned-coat-crying", cry.Portrait);
        Assert.Equal("bg11", cry.Background);
        Assert.Equal("", cry.SecondaryPortrait);
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "C08-017", Scene = new(cry.Background!, cry.Portrait!, cry.Music!) });
        var snapshot = player.Capture();
        player.Advance();
        player.Advance();
        Assert.Equal("ye-zhao-returned-coat-message", player.Scene.Portrait);
        Assert.Contains("谢谢你缝的口袋", player.CurrentNode.Text);
        player.Advance();
        player.Advance();
        Assert.Contains("把外套叠好", player.CurrentNode.Text);
        Assert.Equal("ye-zhao-residency-thoughtful", player.Scene.Portrait);
        player.Restore(snapshot);
        Assert.Equal("ye-zhao-returned-coat-crying", player.Scene.Portrait);
        Assert.Equal("C08-017", player.CurrentNodeId);
    }

    [Fact]
    public void WinterWorkroomAndAlleyFollowDatesWithoutChangingSummerHandwork()
    {
        var nodes = Story().Nodes;
        var winter = nodes.Values.Where(n => n.Scene?.Background is "bg04-winter" or "bg04-winter-night" or "bg05-winter").ToArray();
        Assert.NotEmpty(winter);
        Assert.All(winter, n => Assert.Matches(@"年(?:12|1|2)月|年冬(?:[ ·]|$)", n.Scene!.Time!));
        var summer = Assert.Single(nodes, n => n.Key.StartsWith("纱S05-", StringComparison.Ordinal) && n.Value.Text.StartsWith("店里热得站不住", StringComparison.Ordinal)).Value;
        Assert.Equal("bg05-day", summer.Scene!.Background);
        var closed = nodes["C08-022"].Scene!;
        Assert.Equal("bg04-winter-night", closed.Background);
        Assert.Equal("night-painted", closed.Tone);
        Assert.Equal("唐见月", closed.Viewpoint);
        Assert.Equal("tang-jianyue-late-cardigan", closed.Portrait);
    }

    [Fact]
    public void FinalRoutePosesNeverLeakIntoEarlierYearsOrOtherRoutes()
    {
        var nodes = Story().Nodes;
        var finalPoses = nodes.Where(n => new[] { n.Value.Scene?.Portrait, n.Value.Scene?.SecondaryPortrait }
            .Any(p => p is "tang-jianyue-late-cardigan" or "ye-zhao-returned-coat-crying" or "ye-zhao-returned-coat-message")).ToArray();
        Assert.NotEmpty(finalPoses);
        Assert.All(finalPoses, n =>
        {
            Assert.StartsWith("C08-", n.Key);
            Assert.Contains("2033", n.Value.Scene!.Time);
            Assert.Equal("", n.Value.Scene.SecondaryPortrait);
            Assert.Equal("", n.Value.Scene.TertiaryPortrait);
        });
    }

    [Fact]
    public void ShopWindowsFollowSpringAndSummerWhileWinterAndLaterFurnitureStayDistinct()
    {
        var nodes = Story().Nodes;
        var may = Assert.Single(nodes.Values, n => n.Text.StartsWith("那以后，我学会了不借东西", StringComparison.Ordinal));
        Assert.Equal("bg03-spring", may.Scene!.Background);
        var june = Assert.Single(nodes.Values, n => n.Text == "明天几点停？");
        Assert.Equal("bg03-summer", june.Scene!.Background);
        var july = Assert.Single(nodes.Values, n => n.Text.StartsWith("我擦手时没有擦干", StringComparison.Ordinal));
        Assert.Equal("bg03-summer", july.Scene!.Background);
        Assert.Equal("bg03-spring", nodes["A07-001"].Scene!.Background);
        var winter = Assert.Single(nodes.Values, n => n.Text == "又坏了？");
        Assert.Equal("bg03", winter.Scene!.Background);
        Assert.Equal("bg03-reunion", nodes["B08-003"].Scene!.Background);
        Assert.All(nodes.Values.Where(n => n.Scene?.Background is "bg03-spring" or "bg03-summer"),
            n => Assert.Equal("day", n.Scene!.Tone));
    }

    [Fact]
    public void SeparationKeepsTheOldCoatAndChangesFromTearfulLuggageToTheOnboardMessage()
    {
        var story = Story();
        Assert.Equal("ye-zhao-old-coat", story.Nodes["B07-002"].Scene!.SecondaryPortrait);
        Assert.Equal("ye-zhao-old-coat", story.Nodes["B07-010"].Scene!.SecondaryPortrait);
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "B07-010", Scene = new("bg03", "tang-jianyue-thoughtful", "letter", "ye-zhao-old-coat") });
        player.Advance();
        Assert.Contains("边哭边找纸巾", player.CurrentNode.Text);
        Assert.Equal("bg05-winter", player.Scene.Background);
        Assert.Equal("ye-zhao-old-coat-departing", player.Scene.Portrait);
        Assert.Equal("", player.Scene.SecondaryPortrait);
        player.Advance();
        Assert.StartsWith("上车以后", player.CurrentNode.Text);
        Assert.Equal("bg12", player.Scene.Background);
        Assert.Equal("返程 · 车厢", player.Scene.Location);
        Assert.Equal("ye-zhao-old-coat-message", player.Scene.Portrait);
        var aboard = player.Capture();
        Assert.True(player.Rollback());
        Assert.Equal("ye-zhao-old-coat-departing", player.Scene.Portrait);
        player.Restore(aboard);
        player.Advance();
        Assert.StartsWith("回外地的第一周", player.CurrentNode.Text);
        Assert.Equal("bg09-winter", player.Scene.Background);
        Assert.Equal("ye-zhao-residency-thoughtful", player.Scene.Portrait);
        Assert.DoesNotContain(story.Nodes.Where(n => !n.Key.StartsWith("B07-", StringComparison.Ordinal)),
            n => n.Value.Scene?.Background == "bg12");
    }

    [Fact]
    public void SeatedBoxCgFollowsPocketDiscoveryAndEndsWhenYePicksUpThePhone()
    {
        var story = Story();
        Assert.Equal("ye-zhao-returned-coat-crying", story.Nodes["C08-017"].Scene!.Portrait);
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "C08-017", Scene = new("bg11", "ye-zhao-returned-coat-crying", "distance") });
        player.Advance();
        Assert.StartsWith("原来她那时候已经做了", player.CurrentNode.Text);
        Assert.Equal("cg16", player.Scene.Background);
        Assert.Equal("", player.Scene.Portrait);
        Assert.Equal("", player.Scene.SecondaryPortrait);
        Assert.Equal("", player.Scene.TertiaryPortrait);
        player.Advance();
        Assert.StartsWith("我拿起手机", player.CurrentNode.Text);
        Assert.Equal("bg11", player.Scene.Background);
        Assert.Equal("ye-zhao-returned-coat-message", player.Scene.Portrait);
        Assert.Single(story.Nodes, n => n.Value.Scene?.Background == "cg16");
    }

    [Fact]
    public void HopefulEndingReturnsFromTheStreetToShopKitchenAndDoorWithAnOffstageCaller()
    {
        var nodes = Story().Nodes;
        Assert.Equal("bg10", nodes["A07-022"].Scene!.Background);
        Assert.StartsWith("回店后", nodes["A07-023"].Text);
        Assert.Equal("bg03-spring", nodes["A07-023"].Scene!.Background);
        Assert.Equal("bg07-repaired-spring", nodes["A07-024"].Scene!.Background);
        Assert.Equal("tang-jianyue-home", nodes["A07-024"].Scene!.Portrait);
        Assert.Equal("ye-zhao-home", nodes["A07-024"].Scene!.SecondaryPortrait);
        Assert.Contains("她在楼上叫我", nodes["A07-025"].Text);
        Assert.Equal("bg03-spring", nodes["A07-025"].Scene!.Background);
        Assert.Equal("ye-zhao", nodes["A07-025"].Scene!.Portrait);
        Assert.Equal("", nodes["A07-025"].Scene!.SecondaryPortrait);
    }

    [Fact]
    public void SortedRoomKeepsItsClearedStateAcrossWinterDayNightAndSpringRepairVariants()
    {
        var nodes = Story().Nodes;
        var october = Assert.Single(nodes, n => n.Key.StartsWith("纱S09-", StringComparison.Ordinal) && n.Value.Text.StartsWith("这些怎么都收起来了", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-sorted-autumn", october.Scene!.Background);
        var decemberNight = Assert.Single(nodes, n => n.Key.StartsWith("A03-", StringComparison.Ordinal) && n.Value.Text.StartsWith("我们第三天就吵了一点", StringComparison.Ordinal)).Value;
        Assert.Equal("bg07-sorted-winter-night", decemberNight.Scene!.Background);
        Assert.Equal("night-painted", decemberNight.Scene.Tone);
        Assert.Equal("bg07-sorted-winter", nodes["B07-002"].Scene!.Background);
        Assert.Equal("bg07-repaired-spring", nodes["A07-024"].Scene!.Background);
        var winter = nodes.Values.Where(n => n.Scene?.Background?.StartsWith("bg07-sorted-winter", StringComparison.Ordinal) == true).ToArray();
        Assert.NotEmpty(winter);
        Assert.All(winter, n => Assert.Matches(@"年(?:12|1|2)月|年冬(?:[ ·]|$)", n.Scene!.Time!));
    }

    [Fact]
    public void KitchenPreparationEndsBeforeTheAuntEntersTheDomesticConversation()
    {
        var nodes = Story().Nodes;
        Assert.Equal("cg18", nodes["纱S06-001"].Scene!.Background);
        Assert.Single(nodes.Values, n => n.Scene?.Background == "cg18");
        var next = nodes["纱S06-002"].Scene!;
        Assert.Equal("bg07", next.Background);
        Assert.Equal("tang-jianyue-home", next.Portrait);
        Assert.Equal("ye-zhao-home", next.SecondaryPortrait);
        Assert.Equal("tang-yufen", next.TertiaryPortrait);
        Assert.Equal("", next.Music);
    }

    [Fact]
    public void DifficultConversationsUseEmptyHandsWhileActualMeasurementKeepsTheTape()
    {
        var nodes = Story().Nodes;
        foreach (var (section, text) in new[] { ("纱S06", "你一直在？"), ("A04", "关门后我们把时间表贴出来"),
                     ("A06", "正式签约那天"), ("B04", "我们找出二月的两周") })
        {
            var node = Assert.Single(nodes, n => n.Key.StartsWith(section + "-", StringComparison.Ordinal) && n.Value.Text.StartsWith(text, StringComparison.Ordinal)).Value;
            Assert.StartsWith("tang-jianyue-empty", node.Scene!.Portrait);
            Assert.StartsWith("ye-zhao", node.Scene.SecondaryPortrait);
        }
        Assert.Equal("tang-jianyue-empty", nodes["A07-023"].Scene!.Portrait);
        Assert.Equal("tang-jianyue-measuring", nodes["B06-026"].Scene!.Portrait);
    }

    [Fact]
    public void PartingMovesFromLateNightTearsToMorningAndTheFinalCoatAdjustment()
    {
        var nodes = Story().Nodes;
        Assert.Equal("bg07-sorted-winter-night", nodes["B06-001"].Scene!.Background);
        Assert.Equal("night-painted", nodes["B06-001"].Scene!.Tone);
        foreach (var id in new[] { "B06-012", "B06-016" })
            Assert.Equal("bg07-sorted-winter-night", nodes[id].Scene!.Background);
        Assert.All(nodes.Where(n => n.Value.Scene?.Background == "cg17"), n =>
        {
            Assert.StartsWith("B06-", n.Key);
            Assert.Equal("", n.Value.Scene!.Portrait);
            Assert.Equal("", n.Value.Scene.SecondaryPortrait);
        });
        Assert.Equal("cg17", nodes["B06-020"].Scene!.Background);
        Assert.Equal("cg17", nodes["B06-024"].Scene!.Background);
        Assert.Equal("bg07-sorted-winter", nodes["B06-025"].Scene!.Background);
        Assert.Equal("day", nodes["B06-025"].Scene!.Tone);
        Assert.Contains("次日清晨", nodes["B06-025"].Scene!.Time);
        Assert.Equal("bg04-winter", nodes["B06-026"].Scene!.Background);
        Assert.Equal("ye-zhao-old-coat", nodes["B06-026"].Scene!.SecondaryPortrait);
        Assert.Contains("次日午后", nodes["B06-026"].Scene!.Time);
    }

    [Fact]
    public void WinterWalkTearsHaveNoLuggageAndColdHandsEndBeforePackingTheNextDay()
    {
        var story = Story();
        var player = new StoryPlayer(story);
        var walk = story.Nodes["B06-030"].Scene!;
        Assert.Equal("bg05-winter-night", walk.Background);
        Assert.Equal("night-painted", walk.Tone);
        Assert.Equal("tang-jianyue-winter-empty", walk.Portrait);
        Assert.Equal("ye-zhao-old-coat", walk.SecondaryPortrait);
        player.Restore(player.Capture() with { NodeId = "B06-035", Scene = new("bg05-winter-night", "tang-jianyue-winter-empty", "letter", "ye-zhao-old-coat-tearful") });
        player.Advance();
        Assert.StartsWith("回去时她手很冷", player.CurrentNode.Text);
        Assert.Equal("cg21", player.Scene.Background);
        Assert.Equal("", player.Scene.Portrait);
        var hands = player.Capture();
        Assert.True(player.Rollback());
        Assert.Equal("ye-zhao-old-coat-tearful", player.Scene.SecondaryPortrait);
        player.Restore(hands);
        player.Advance();
        Assert.StartsWith("B07-", player.CurrentNodeId);
        Assert.Equal("bg07-sorted-winter", player.Scene.Background);
        Assert.Equal("day", player.Scene.Tone);
        Assert.Equal("ye-zhao-home-thoughtful", player.Scene.SecondaryPortrait);
    }

    [Fact]
    public void IroningStopsAtTheUprightIronAndVisitorLeavesBeforeTyping()
    {
        var story = Story();
        Assert.All(story.Nodes.Where(n => n.Value.Scene?.Background == "cg19"), n => Assert.StartsWith("C05a-", n.Key));
        Assert.Equal("cg19", story.Nodes["C05a-004"].Scene!.Background);
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "C05a-004", Scene = new("cg19", "", "letter") });
        player.Advance();
        Assert.StartsWith("我说得很突然", player.CurrentNode.Text);
        Assert.Equal("cg22", player.Scene.Background);
        Assert.Equal("", player.Scene.Portrait);
        Assert.Equal("", player.Scene.SecondaryPortrait);
        Assert.Equal("cg22", story.Nodes["C05a-009"].Scene!.Background);
        Assert.Equal("bg03-ironing", story.Nodes["C05a-010"].Scene!.Background);
        Assert.Equal("tang-jianyue-empty-thoughtful", story.Nodes["C05a-010"].Scene!.Portrait);
        Assert.Equal("", story.Nodes["C05a-010"].Scene!.SecondaryPortrait);
        Assert.Equal("tang-jianyue-work-message", story.Nodes["C05a-011"].Scene!.Portrait);
        Assert.Equal("", story.Nodes["C05a-011"].Scene!.SecondaryPortrait);
        Assert.Equal("tang-jianyue-empty-thoughtful", story.Nodes["C05a-013"].Scene!.Portrait);
        Assert.Equal("tang-jianyue-work-message", story.Nodes["C05a-014"].Scene!.Portrait);
    }

    [Fact]
    public void SolitarySewingPutsThePhoneAwayAndEndsAtTheNextViewpoint()
    {
        var story = Story();
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "C05a-014", Scene = new("bg03-ironing", "tang-jianyue-work-message", "letter") });
        player.Advance();
        Assert.StartsWith("我坐下来继续缝衣", player.CurrentNode.Text);
        Assert.Equal("cg20", player.Scene.Background);
        Assert.Equal("", player.Scene.Portrait);
        Assert.Equal("", player.Scene.SecondaryPortrait);
        Assert.Equal("", player.Scene.TertiaryPortrait);
        Assert.Single(story.Nodes.Values, n => n.Scene?.Background == "cg20");
        player.Advance();
        Assert.StartsWith("C06-", player.CurrentNodeId);
        Assert.Equal("bg09-summer-night", player.Scene.Background);
        Assert.Equal("ye-zhao-residency-thoughtful", player.Scene.Portrait);
        Assert.Equal("叶照", player.Scene.Viewpoint);
    }

    [Fact]
    public void WinterSigningKeepsTheRoomEmptyUntilMovingInAndLeavesTheSeptemberViewingUnchanged()
    {
        var nodes = Story().Nodes;
        Assert.Equal("bg08-winter", nodes["A06-001"].Scene!.Background);
        Assert.Equal("bg08-winter", nodes["A06-012"].Scene!.Background);
        Assert.Equal("bg08-winter", nodes["A06-017"].Scene!.Background);
        Assert.Equal("bg08-furnished", nodes["A06-018"].Scene!.Background);
        var autumn = Assert.Single(nodes, n => n.Key.StartsWith("纱S07-", StringComparison.Ordinal) && n.Value.Text.StartsWith("这里能做摄影", StringComparison.Ordinal)).Value;
        Assert.Equal("bg08", autumn.Scene!.Background);
        Assert.All(nodes.Where(n => n.Value.Scene?.Background == "bg08-winter"), n =>
        {
            Assert.StartsWith("A06-", n.Key);
            Assert.Matches(@"年(?:12|1|2)月", n.Value.Scene!.Time!);
            Assert.Equal("day", n.Value.Scene.Tone);
        });
    }

    [Fact]
    public void MidnightEditingEndsAtDaytimeLaundryAndWetClothesLeaveBeforeReturningToWork()
    {
        var story = Story();
        Assert.Equal("bg03-night", story.Nodes["A03-011"].Scene!.Background);
        Assert.Equal("cg25", story.Nodes["A03-012"].Scene!.Background);
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "A03-016", Scene = new("cg25", "", "tender") });
        player.Advance();
        Assert.StartsWith("洗衣也没那么容易", player.CurrentNode.Text);
        Assert.Equal("bg13", player.Scene.Background);
        Assert.Equal("见月衣坊 · 楼上阳台", player.Scene.Location);
        Assert.Equal("day", player.Scene.Tone);
        Assert.Contains("数日后", player.Scene.Time);
        Assert.Equal("tang-jianyue-laundry", player.Scene.Portrait);
        Assert.Equal("ye-zhao-laundry", player.Scene.SecondaryPortrait);
        player.Advance();
        Assert.Equal("bg04-winter", player.Scene.Background);
        Assert.Equal("tang-jianyue-thoughtful", player.Scene.Portrait);
        Assert.Equal("ye-zhao-thoughtful", player.Scene.SecondaryPortrait);
        Assert.Single(story.Nodes.Values, n => n.Scene?.Background == "bg13");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FoldedShirtAndSeatedCompanionshipPersistAcrossBothMemoryReactionsUntilDinner(int firstChoice)
    {
        var story = Story();
        var player = new StoryPlayer(story);
        var sawOldShirt = false;
        var sawSeatedCompanionship = false;
        for (var step = 0; step < 3000 && player.CurrentNodeId != "A03-045"; step++)
        {
            if (player.Scene.Background == "cg23")
            {
                sawOldShirt = true;
                Assert.Equal("", player.Scene.Portrait);
                Assert.Equal("", player.Scene.SecondaryPortrait);
            }
            if (player.Scene.Background == "cg24")
            {
                sawSeatedCompanionship = true;
                Assert.True(sawOldShirt);
                Assert.Equal("", player.Scene.Portrait);
                Assert.Equal("", player.Scene.SecondaryPortrait);
                Assert.Equal("唐见月", player.Scene.Viewpoint);
            }
            if (player.CurrentNode.Kind == NodeKind.Choice) player.Choose(player.CurrentNodeId == "q1" ? firstChoice : 0);
            else player.Advance();
        }
        Assert.True(sawOldShirt && sawSeatedCompanionship);
        Assert.Equal("A03-045", player.CurrentNodeId);
        Assert.Equal("bg07-sorted-winter-night", player.Scene.Background);
        Assert.Equal("tang-jianyue-home-thoughtful", player.Scene.Portrait);
        Assert.Equal("ye-zhao-home-thoughtful", player.Scene.SecondaryPortrait);
        Assert.Contains("没有抱住我", story.Nodes["A03-033"].Text);
        Assert.Equal("cg24", story.Nodes["A03-033"].Scene!.Background);
    }

    [Fact]
    public void PhotographyMovesFromTheCameraToTimerThenTheShopWithNoCameraOnTheSubject()
    {
        var story = Story();
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "A05a-021", Scene = new("bg06-winter", "tang-jianyue-empty", "tender", "ye-zhao-home") });
        player.Advance();
        Assert.Equal("cg26", player.Scene.Background);
        Assert.Equal("", player.Scene.Portrait);
        while (player.CurrentNodeId != "A05a-026") player.Advance();
        Assert.Equal("bg06-winter", player.Scene.Background);
        Assert.Equal("tang-jianyue-empty", player.Scene.Portrait);
        Assert.Equal("ye-zhao-home", player.Scene.SecondaryPortrait);
        while (player.CurrentNodeId != "A05a-034") player.Advance();
        Assert.Equal("cg27", player.Scene.Background);
        var timer = player.Capture();
        Assert.True(player.Rollback());
        Assert.Equal("bg06-winter", player.Scene.Background);
        player.Restore(timer);
        player.Advance();
        Assert.StartsWith("我把这张洗出来", player.CurrentNode.Text);
        Assert.Equal("bg03", player.Scene.Background);
        Assert.Equal("ye-zhao-home", player.Scene.SecondaryPortrait);
        Assert.Contains("相片洗出后", player.Scene.Time);
        Assert.All(story.Nodes.Where(n => n.Value.Scene?.Background is "cg26" or "cg27"), n => Assert.StartsWith("A05a-", n.Key));
    }

    [Fact]
    public void InjuredWristPersistsThroughBothVisitsBeforeTheOldPhotosComeOut()
    {
        var nodes = Story().Nodes;
        for (var index = 11; index <= 20; index++)
        {
            var scene = nodes[$"纱S09-{index:000}"].Scene!;
            Assert.Equal("tang-jianyue-home-sore-wrist", scene.Portrait);
            Assert.Equal(index < 18 ? "bg07-autumn" : "bg07-sorted-autumn", scene.Background);
            Assert.DoesNotContain(new[] { scene.Portrait, scene.SecondaryPortrait, scene.TertiaryPortrait },
                id => id?.StartsWith("ye-zhao", StringComparison.Ordinal) == true);
        }
        Assert.Equal("tang-yufen-stern", nodes["纱S09-014"].Scene!.SecondaryPortrait);
        Assert.Equal("", nodes["纱S09-017"].Scene!.SecondaryPortrait);
        Assert.Equal("zhou-suqin-empty-handed", nodes["纱S09-018"].Scene!.SecondaryPortrait);
    }

    [Fact]
    public void OldPhotosUseTheDryEyedCgUntilTheExplicitPresentTenseCrying()
    {
        var story = Story();
        var nodes = story.Nodes;
        Assert.StartsWith("上一次她这么问", nodes["纱S09-021"].Text);
        Assert.Contains("贴歪的创可贴", nodes["纱S09-023"].Text);
        for (var index = 21; index <= 30; index++)
            Assert.Equal("cg28", nodes[$"纱S09-{index:000}"].Scene!.Background);
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "纱S09-030", Scene = new("cg28", "", "tender") });
        player.Advance();
        Assert.StartsWith("说完我哭了", player.CurrentNode.Text);
        Assert.Equal("cg29", player.Scene.Background);
        Assert.True(player.Rollback());
        Assert.Equal("cg28", player.Scene.Background);
    }

    [Fact]
    public void ClosingTheAlbumEndsAtTheOriginalCabinetCgWithoutAnExtraVisitor()
    {
        var story = Story();
        Assert.Equal("cg29", story.Nodes["纱S09-032"].Scene!.Background);
        Assert.Contains("没有说接受", story.Nodes["纱S09-031"].Text);
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "纱S09-032", Scene = new("cg29", "", "tender") });
        player.Advance();
        Assert.StartsWith("桌面空了以后", player.CurrentNode.Text);
        Assert.Equal("cg07", player.Scene.Background);
        Assert.Equal("", player.Scene.Portrait);
        Assert.Equal("", player.Scene.SecondaryPortrait);
        Assert.Equal("", player.Scene.TertiaryPortrait);
    }

    [Fact]
    public void DinnerAnswerHasItsOwnCgAndEndsWhenTheVisitorLeavesBeforeWashing()
    {
        var story = Story();
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "A05-007", Scene = new("bg07-repaired", "tang-jianyue-home", "tender", "ye-zhao-home") });
        player.Advance();
        for (var index = 8; index <= 19; index++)
        {
            Assert.Equal($"A05-{index:000}", player.CurrentNodeId);
            Assert.Equal(index < 12 ? "cg30" : "cg31", player.Scene.Background);
            Assert.Equal("", player.Scene.Portrait);
            Assert.Equal("", player.Scene.SecondaryPortrait);
            Assert.Equal("", player.Scene.TertiaryPortrait);
            player.Advance();
        }
        Assert.Equal("A05-020", player.CurrentNodeId);
        Assert.Equal("bg07-repaired-night", player.Scene.Background);
        Assert.Equal("ye-zhao-washing-thoughtful", player.Scene.SecondaryPortrait);
        Assert.Equal("", player.Scene.TertiaryPortrait);
        Assert.True(player.Rollback());
        Assert.Equal("cg31", player.Scene.Background);
    }

    [Fact]
    public void BedsidePhoneCgEndsAtChargingAndThePhoneIsPutDownAfterHangingUp()
    {
        var story = Story();
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "B07a-005", Scene = new("bg09-spring-night", "ye-zhao-residency-phone", "distance") });
        player.Advance();
        Assert.Equal("cg32", player.Scene.Background);
        for (var index = 6; index <= 17; index++)
        {
            Assert.Equal("cg32", player.Scene.Background);
            Assert.Equal("", player.Scene.Portrait);
            player.Advance();
        }
        Assert.Equal("B07a-018", player.CurrentNodeId);
        Assert.Equal("bg09-spring-night", player.Scene.Background);
        Assert.Equal("ye-zhao-residency-phone-charging", player.Scene.Portrait);
        var charging = player.Capture();
        while (player.CurrentNodeId != "B07a-021") player.Advance();
        Assert.Equal("ye-zhao-residency-thoughtful", player.Scene.Portrait);
        player.Restore(charging);
        Assert.Equal("ye-zhao-residency-phone-charging", player.Scene.Portrait);
        Assert.Equal("", player.Scene.SecondaryPortrait);
    }

    [Fact]
    public void ApronFoldingCutsToAnEmptyUnlitRoomThenTheFollowingYearAndViewpoint()
    {
        var story = Story();
        var player = new StoryPlayer(story);
        player.Restore(player.Capture() with { NodeId = "B07a-023", Scene = new("bg07-sorted-night", "tang-jianyue-home-thoughtful", "distance") });
        player.Advance();
        Assert.Equal("cg33", player.Scene.Background);
        Assert.Equal("唐见月", player.Scene.Viewpoint);
        player.Advance();
        var unlit = player.Capture();
        Assert.Equal("bg07-lights-out", player.Scene.Background);
        Assert.Equal("night-painted", player.Scene.Tone);
        Assert.Equal("", player.Scene.Portrait);
        Assert.Equal("", player.Scene.SecondaryPortrait);
        Assert.Equal("", player.Scene.TertiaryPortrait);
        Assert.Contains("关灯", player.Scene.Time);
        player.Advance();
        Assert.Equal("bg09", player.Scene.Background);
        Assert.Equal("叶照", player.Scene.Viewpoint);
        Assert.Equal("2031年 · 外地", player.Scene.Time);
        Assert.Equal("day", player.Scene.Tone);
        player.Restore(unlit);
        Assert.Equal("bg07-lights-out", player.Scene.Background);
        Assert.Equal("", player.Scene.Portrait);
    }

    [Fact]
    public void AutumnRoomVariantsOnlyFollowExplicitAutumnDaytimeAndKeepTheSortingState()
    {
        var nodes = Story().Nodes;
        var autumn = nodes.Values.Where(n => n.Scene?.Background is "bg07-autumn" or "bg07-sorted-autumn").ToArray();
        Assert.NotEmpty(autumn);
        Assert.All(autumn, n =>
        {
            Assert.Matches(@"年(?:9|10|11)月|年秋", n.Scene!.Time!);
            Assert.Equal("day", n.Scene.Tone);
        });
        Assert.Contains(nodes.Where(n => n.Key.StartsWith("C07-", StringComparison.Ordinal)), n => n.Value.Scene?.Background == "bg07-sorted-autumn");
        Assert.DoesNotContain(nodes.Where(n => n.Key.StartsWith("A", StringComparison.Ordinal) || n.Key.StartsWith("B", StringComparison.Ordinal)),
            n => n.Value.Scene?.Background == "bg07-autumn");
        var septemberNight = Assert.Single(nodes.Values, n => n.Text.StartsWith("晚上我把日历给她看", StringComparison.Ordinal)).Scene!;
        Assert.Equal("bg07-night", septemberNight.Background);
        Assert.Equal("night-painted", septemberNight.Tone);
        Assert.Equal("2029年9月 · 夜", septemberNight.Time);
    }

    [Fact]
    public void WinterStudioChangesDayAndNightWithoutChangingAutumnOrTheTemporaryWorkspace()
    {
        var nodes = Story().Nodes;
        Assert.Equal("bg06-winter-night", nodes["A05a-001"].Scene!.Background);
        Assert.Equal("night-painted", nodes["A05a-001"].Scene!.Tone);
        Assert.Equal("bg06-winter", nodes["A05a-018"].Scene!.Background);
        Assert.Equal("day", nodes["A05a-018"].Scene!.Tone);
        var autumn = Assert.Single(nodes, n => n.Key.StartsWith("纱S07-", StringComparison.Ordinal) && n.Value.Text.StartsWith("驻留的日期确认了", StringComparison.Ordinal)).Value;
        Assert.Equal("bg06", autumn.Scene!.Background);
        Assert.Contains(nodes.Values, n => n.Scene?.Background == "bg06-temporary-night");
        Assert.All(nodes.Values.Where(n => n.Scene?.Background is "bg06-winter" or "bg06-winter-night"),
            n => Assert.Matches(@"年(?:12|1|2)月|年冬(?:[ ·]|$)", n.Scene!.Time!));
    }
}
