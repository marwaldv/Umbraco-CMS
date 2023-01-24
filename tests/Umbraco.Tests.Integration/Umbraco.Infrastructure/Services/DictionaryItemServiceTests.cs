﻿using System.Diagnostics;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Cms.Tests.Integration.Umbraco.Infrastructure.Services;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class DictionaryItemServiceTests : UmbracoIntegrationTest
{
    private Guid _parentItemId;
    private Guid _childItemId;

    private IDictionaryItemService DictionaryItemService => GetRequiredService<IDictionaryItemService>();

    private ILanguageService LanguageService => GetRequiredService<ILanguageService>();

    [SetUp]
    public async Task SetUp() => await CreateTestData();

    [Test]
    public async Task Can_Get_Root_Dictionary_Items()
    {
        var rootItems = await DictionaryItemService.GetAtRootAsync();

        Assert.NotNull(rootItems);
        Assert.IsTrue(rootItems.Any());
    }

    [Test]
    public async Task Can_Determine_If_DictionaryItem_Exists()
    {
        var exists = await DictionaryItemService.ExistsAsync("Parent");
        Assert.IsTrue(exists);
    }

    [Test]
    public async Task Can_Get_Dictionary_Item_By_Id()
    {
        var parentItem = await DictionaryItemService.GetAsync(_parentItemId);
        Assert.NotNull(parentItem);

        var childItem = await DictionaryItemService.GetAsync(_childItemId);
        Assert.NotNull(childItem);
    }

    [Test]
    public async Task Can_Get_Dictionary_Items_By_Ids()
    {
        var items = await DictionaryItemService.GetManyAsync(_parentItemId, _childItemId);
        Assert.AreEqual(2, items.Count());
        Assert.NotNull(items.FirstOrDefault(i => i.Key == _parentItemId));
        Assert.NotNull(items.FirstOrDefault(i => i.Key == _childItemId));
    }

    [Test]
    public async Task Can_Get_Dictionary_Item_By_Key()
    {
        var parentItem = await DictionaryItemService.GetAsync("Parent");
        Assert.NotNull(parentItem);

        var childItem = await DictionaryItemService.GetAsync("Child");
        Assert.NotNull(childItem);
    }

    [Test]
    public async Task Can_Get_Dictionary_Items_By_Keys()
    {
        var items = await DictionaryItemService.GetManyAsync("Parent", "Child");
        Assert.AreEqual(2, items.Count());
        Assert.NotNull(items.FirstOrDefault(i => i.ItemKey == "Parent"));
        Assert.NotNull(items.FirstOrDefault(i => i.ItemKey == "Child"));
    }

    [Test]
    public async Task Does_Not_Fail_When_DictionaryItem_Doesnt_Exist()
    {
        var item = await DictionaryItemService.GetAsync("RandomKey");
        Assert.Null(item);
    }

    [Test]
    public async Task Can_Get_Dictionary_Item_Children()
    {
        var item = await DictionaryItemService.GetChildrenAsync(_parentItemId);
        Assert.NotNull(item);
        Assert.That(item.Count(), Is.EqualTo(1));

        foreach (var dictionaryItem in item)
        {
            Assert.AreEqual(_parentItemId, dictionaryItem.ParentId);
            Assert.IsFalse(string.IsNullOrEmpty(dictionaryItem.ItemKey));
        }
    }

    [Test]
    public async Task Can_Get_Dictionary_Item_Descendants()
    {
        using (var scope = ScopeProvider.CreateScope())
        {
            var en = await LanguageService.GetAsync("en-GB");
            var dk = await LanguageService.GetAsync("da-DK");

            var currParentId = _childItemId;
            for (var i = 0; i < 25; i++)
            {
                // Create 2 per level
                var result = await DictionaryItemService.CreateAsync(
                    new DictionaryItem(currParentId, "D1" + i)
                    {
                        Translations = new List<IDictionaryTranslation>
                        {
                            new DictionaryTranslation(en, "ChildValue1 " + i),
                            new DictionaryTranslation(dk, "BørnVærdi1 " + i)
                        }
                    });

                Assert.IsTrue(result.Success);

                await DictionaryItemService.CreateAsync(
                    new DictionaryItem(currParentId, "D2" + i)
                    {
                        Translations = new List<IDictionaryTranslation>
                        {
                            new DictionaryTranslation(en, "ChildValue2 " + i),
                            new DictionaryTranslation(dk, "BørnVærdi2 " + i)
                        }
                    });

                currParentId = result.Result!.Key;
            }

            ScopeAccessor.AmbientScope.Database.AsUmbracoDatabase().EnableSqlTrace = true;
            ScopeAccessor.AmbientScope.Database.AsUmbracoDatabase().EnableSqlCount = true;

            var items = (await DictionaryItemService.GetDescendantsAsync(_parentItemId)).ToArray();

            Debug.WriteLine("SQL CALLS: " + ScopeAccessor.AmbientScope.Database.AsUmbracoDatabase().SqlCount);

            Assert.AreEqual(51, items.Length);

            // There's a call or two to get languages, so apart from that there should only be one call per level.
            Assert.Less(ScopeAccessor.AmbientScope.Database.AsUmbracoDatabase().SqlCount, 30);
        }
    }

    [Test]
    public async Task Can_Create_DictionaryItem_At_Root()
    {
        var english = await LanguageService.GetAsync("en-US");

        var result = await DictionaryItemService.CreateAsync(
            new DictionaryItem("Testing123")
            {
                Translations = new List<IDictionaryTranslation> { new DictionaryTranslation(english, "Hello world") }
            });
        Assert.True(result.Success);

        // re-get
        var item = await DictionaryItemService.GetAsync(result.Result!.Key);
        Assert.NotNull(item);

        Assert.Greater(item.Id, 0);
        Assert.IsTrue(item.HasIdentity);
        Assert.IsFalse(item.ParentId.HasValue);
        Assert.AreEqual("Testing123", item.ItemKey);
        Assert.AreEqual(1, item.Translations.Count());
    }

    [Test]
    public async Task Can_Create_DictionaryItem_At_Root_With_All_Languages()
    {
        var allLangs = (await LanguageService.GetAllAsync()).ToArray();
        Assert.Greater(allLangs.Length, 0);

        var translations = allLangs.Select(language => new DictionaryTranslation(language, $"Translation for: {language.IsoCode}")).ToArray();
        var result = await DictionaryItemService.CreateAsync(
            new DictionaryItem("Testing12345") { Translations = translations }
        );

        Assert.IsTrue(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.Success, result.Status);
        Assert.NotNull(result.Result);

        // re-get
        var item = await DictionaryItemService.GetAsync(result.Result!.Key);

        Assert.IsNotNull(item);
        Assert.Greater(item.Id, 0);
        Assert.IsTrue(item.HasIdentity);
        Assert.IsFalse(item.ParentId.HasValue);
        Assert.AreEqual("Testing12345", item.ItemKey);
        foreach (var language in allLangs)
        {
            Assert.AreEqual($"Translation for: {language.IsoCode}",
                item.Translations.Single(x => x.Language.CultureName == language.CultureName).Value);
        }
    }

    [Test]
    public async Task Can_Create_DictionaryItem_At_Root_With_Some_Languages()
    {
        var allLangs = (await LanguageService.GetAllAsync()).ToArray();
        Assert.Greater(allLangs.Length, 1);

        var firstLanguage = allLangs.First();
        var translations = new[] { new DictionaryTranslation(firstLanguage, $"Translation for: {firstLanguage.IsoCode}") };
        var result = await DictionaryItemService.CreateAsync(
            new DictionaryItem("Testing12345") { Translations = translations }
        );

        Assert.IsTrue(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.Success, result.Status);
        Assert.NotNull(result.Result);

        // re-get
        var item = await DictionaryItemService.GetAsync(result.Result!.Key);

        Assert.IsNotNull(item);
        Assert.Greater(item.Id, 0);
        Assert.IsTrue(item.HasIdentity);
        Assert.IsFalse(item.ParentId.HasValue);
        Assert.AreEqual("Testing12345", item.ItemKey);
        Assert.AreEqual(1, item.Translations.Count());
        Assert.AreEqual(firstLanguage.Id, item.Translations.First().LanguageId);
    }

    [Test]
    public async Task Can_Create_DictionaryItem_With_Explicit_Key()
    {
        var english = await LanguageService.GetAsync("en-US");
        // the package install needs to be able to create dictionary items with explicit keys
        var key = Guid.NewGuid();

        var result = await DictionaryItemService.CreateAsync(
            new DictionaryItem("Testing123")
            {
                Key = key,
                Translations = new List<IDictionaryTranslation> { new DictionaryTranslation(english, "Hello world") }
            });
        Assert.True(result.Success);
        Assert.AreEqual(key, result.Result.Key);

        // re-get
        var item = await DictionaryItemService.GetAsync(result.Result!.Key);
        Assert.NotNull(item);
        Assert.AreEqual(key, item.Key);
    }

    [Test]
    public async Task Can_Add_Translation_To_Existing_Dictionary_Item()
    {
        var english = await LanguageService.GetAsync("en-US");

        var result = await DictionaryItemService.CreateAsync(new DictionaryItem("Testing12345"));
        Assert.True(result.Success);

        // re-get
        var item = await DictionaryItemService.GetAsync(result.Result!.Key);
        Assert.NotNull(item);

        item.Translations = new List<IDictionaryTranslation> { new DictionaryTranslation(english, "Hello world") };

        result = await DictionaryItemService.UpdateAsync(item);
        Assert.True(result.Success);

        Assert.AreEqual(1, item.Translations.Count());
        foreach (var translation in item.Translations)
        {
            Assert.AreEqual("Hello world", translation.Value);
        }

        item.Translations = new List<IDictionaryTranslation>(item.Translations)
        {
            new DictionaryTranslation(
                await LanguageService.GetAsync("en-GB"),
                "My new value")
        };

        result = await DictionaryItemService.UpdateAsync(item);
        Assert.True(result.Success);

        // re-get
        item = await DictionaryItemService.GetAsync(item.Key);
        Assert.NotNull(item);

        Assert.AreEqual(2, item.Translations.Count());
        Assert.AreEqual("Hello world", item.Translations.First().Value);
        Assert.AreEqual("My new value", item.Translations.Last().Value);
    }

    [Test]
    public async Task Can_Delete_DictionaryItem()
    {
        var item = await DictionaryItemService.GetAsync("Child");
        Assert.NotNull(item);

        var result = await DictionaryItemService.DeleteAsync(item.Key);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.Success, result.Status);

        var deletedItem = await DictionaryItemService.GetAsync("Child");
        Assert.Null(deletedItem);
    }

    [Test]
    public async Task Can_Update_Existing_DictionaryItem()
    {
        var item = await DictionaryItemService.GetAsync("Child");
        foreach (var translation in item.Translations)
        {
            translation.Value += "UPDATED";
        }

        var result = await DictionaryItemService.UpdateAsync(item);
        Assert.True(result.Success);

        var updatedItem = await DictionaryItemService.GetAsync("Child");
        Assert.NotNull(updatedItem);

        foreach (var translation in updatedItem.Translations)
        {
            Assert.That(translation.Value.EndsWith("UPDATED"), Is.True);
        }
    }

    [Test]
    public async Task Cannot_Add_Duplicate_DictionaryItem_ItemKey()
    {
        var item = await DictionaryItemService.GetAsync("Child");
        Assert.IsNotNull(item);

        item.ItemKey = "Parent";

        var result = await DictionaryItemService.UpdateAsync(item);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.DuplicateItemKey, result.Status);

        var item2 = await DictionaryItemService.GetAsync("Child");
        Assert.IsNotNull(item2);
        Assert.AreEqual(item.Key, item2.Key);
    }

    [Test]
    public async Task Cannot_Create_Child_DictionaryItem_Under_Missing_Parent()
    {
        var itemKey = Guid.NewGuid().ToString("N");

        var result = await DictionaryItemService.CreateAsync(new DictionaryItem(Guid.NewGuid(), itemKey));
        Assert.IsFalse(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.ParentNotFound, result.Status);

        var item = await DictionaryItemService.GetAsync(itemKey);
        Assert.IsNull(item);
    }

    [Test]
    public async Task Cannot_Create_Multiple_DictionaryItems_With_Same_ItemKey()
    {
        var itemKey = Guid.NewGuid().ToString("N");
        var result = await DictionaryItemService.CreateAsync(new DictionaryItem(itemKey));

        Assert.IsTrue(result.Success);

        result = await DictionaryItemService.CreateAsync(new DictionaryItem(itemKey));
        Assert.IsFalse(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.DuplicateItemKey, result.Status);
    }

    [Test]
    public async Task Cannot_Update_Non_Existant_DictionaryItem()
    {
        var result = await DictionaryItemService.UpdateAsync(new DictionaryItem("NoSuchItemKey"));
        Assert.False(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.ItemNotFound, result.Status);
    }

    [Test]
    public async Task Cannot_Update_DictionaryItem_With_Empty_Id()
    {
        var item = await DictionaryItemService.GetAsync("Child");
        Assert.IsNotNull(item);

        item = new DictionaryItem(item.ParentId, item.ItemKey) { Key = item.Key, Translations = item.Translations };

        var result = await DictionaryItemService.UpdateAsync(item);
        Assert.False(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.ItemNotFound, result.Status);
    }

    [Test]
    public async Task Cannot_Delete_Non_Existant_DictionaryItem()
    {
        var result = await DictionaryItemService.DeleteAsync(Guid.NewGuid());
        Assert.IsFalse(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.ItemNotFound, result.Status);
    }

    [Test]
    public async Task Cannot_Create_DictionaryItem_With_Duplicate_Key()
    {
        var english = await LanguageService.GetAsync("en-US");
        var key = Guid.NewGuid();

        var result = await DictionaryItemService.CreateAsync(
            new DictionaryItem("Testing123")
            {
                Key = key,
                Translations = new List<IDictionaryTranslation> { new DictionaryTranslation(english, "Hello world") }
            });
        Assert.True(result.Success);

        result = await DictionaryItemService.CreateAsync(
            new DictionaryItem("Testing456")
            {
                Key = key,
                Translations = new List<IDictionaryTranslation> { new DictionaryTranslation(english, "Hello world") }
            });
        Assert.False(result.Success);
        Assert.AreEqual(DictionaryItemOperationStatus.DuplicateKey, result.Status);

        // re-get
        var item = await DictionaryItemService.GetAsync("Testing123");
        Assert.NotNull(item);
        Assert.AreEqual(key, item.Key);

        item = await DictionaryItemService.GetAsync("Testing456");
        Assert.Null(item);
    }

    private async Task CreateTestData()
    {
        var languageDaDk = new LanguageBuilder()
            .WithCultureInfo("da-DK")
            .Build();
        var languageEnGb = new LanguageBuilder()
            .WithCultureInfo("en-GB")
            .Build();

        var languageResult = await LanguageService.CreateAsync(languageDaDk, 0);
        Assert.IsTrue(languageResult.Success);
        languageResult = await LanguageService.CreateAsync(languageEnGb, 0);
        Assert.IsTrue(languageResult.Success);

        var dictionaryResult = await DictionaryItemService.CreateAsync(
            new DictionaryItem("Parent")
            {
                Translations = new List<IDictionaryTranslation>
                {
                    new DictionaryTranslation(languageEnGb, "ParentValue"),
                    new DictionaryTranslation(languageDaDk, "ForældreVærdi")
                }
            });
        Assert.True(dictionaryResult.Success);
        IDictionaryItem parentItem = dictionaryResult.Result!;

        _parentItemId = parentItem.Key;

        dictionaryResult = await DictionaryItemService.CreateAsync(
            new DictionaryItem(
                parentItem.Key,
                "Child"
            )
            {
                Translations = new List<IDictionaryTranslation>
                {
                    new DictionaryTranslation(languageEnGb, "ChildValue"),
                    new DictionaryTranslation(languageDaDk, "BørnVærdi")
                }
            });
        Assert.True(dictionaryResult.Success);
        IDictionaryItem childItem = dictionaryResult.Result!;

        _childItemId = childItem.Key;
    }
}
