using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client._Sunrise.Sheetlets;
using Content.Client.Stylesheets;
using Content.Shared._Sunrise.SponsorInventory;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Sunrise.Interfaces.Shared;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.SponsorInventory;

public sealed partial class InventoryWindow
{
    private const string TooltipHeadingColor = "#d7c7a5";
    private const string TooltipTextMutedColor = "#9eb4c1";
    private const string TooltipTextColor = "#d8dde3";
    private const string TooltipGoodColor = "#72d979";
    private const string TooltipWarningColor = "#d7b45f";
    private const string TooltipBadColor = "#e45f5f";
    private const string TooltipSeparator = " \u00b7 ";
    private const string TooltipGoodStatus = "good";
    private const string TooltipBadStatus = "bad";
    private const string TooltipWarningStatus = "warning";

    /*
     * Small formatting, localization, lookup, and cleanup helpers used across the inventory window partials.
     */
    private string? GetSlotPrototype(string slot)
    {
        if (_previewDummy == null ||
            !_inventory.TryGetSlotEntity(_previewDummy.Value, slot, out var entity))
        {
            return null;
        }

        return _entManager.GetComponent<MetaDataComponent>(entity.Value).EntityPrototype?.ID;
    }

    private string GetSlotLabel(SlotDefinition slot)
    {
        if (TryGetPocketIndex(slot.Name, out var pocketIndex))
            return Loc.GetString("sunrise-inventory-slot-pocket", ("index", pocketIndex));

        var locKey = slot.Name switch
        {
            "outerClothing" => "sunrise-inventory-slot-outer-clothing",
            _ => $"sunrise-inventory-slot-{slot.Name.ToLowerInvariant()}",
        };

        if (Loc.TryGetString(locKey, out var localized))
            return localized;

        return string.IsNullOrWhiteSpace(slot.DisplayName)
            ? slot.Name
            : slot.DisplayName;
    }

    private string GetSlotLabel(string slot)
    {
        if (_slotLabels.TryGetValue(slot, out var label))
            return label;

        if (TryGetPocketIndex(slot, out var pocketIndex))
            return Loc.GetString("sunrise-inventory-slot-pocket", ("index", pocketIndex));

        return slot;
    }

    private static bool TryGetPocketIndex(string slotName, out int index)
    {
        const string pocketPrefix = "pocket";
        if (slotName.StartsWith(pocketPrefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(slotName.AsSpan(pocketPrefix.Length), out index))
        {
            return true;
        }

        index = 0;
        return false;
    }

    private void SetPreviewRotation(Direction direction)
    {
        CharacterPreview.OverrideDirection = (Direction) ((int) direction % 4 * 2);
    }


    private static string GetPlacementText(InventoryPaletteEntry entry)
    {
        if (entry.TargetSlots.Count > 0 && entry.BagPrototypes.Count > 0 && entry.CanPlaceInBag)
            return Loc.GetString("sunrise-inventory-placement-slot-bag");

        if (entry.TargetSlots.Count > 0)
            return Loc.GetString("sunrise-inventory-placement-slot");

        if (entry.BagPrototypes.Count > 0 && entry.CanPlaceInBag)
            return Loc.GetString("sunrise-inventory-placement-bag");

        return Loc.GetString("sunrise-inventory-placement-no-bag");
    }

    private static string GetSourceText(InventoryPaletteEntrySource source)
    {
        return Loc.GetString(source == InventoryPaletteEntrySource.Sponsor
            ? "sunrise-inventory-source-sponsor"
            : "sunrise-inventory-source-loadout");
    }

    private static FormattedMessage BuildPaletteTooltip(InventoryPaletteEntry entry)
    {
        var message = new FormattedMessage();
        AddTooltipColoredText(message, entry.Name, TooltipHeadingColor, bold: true);

        AddTooltipMetaLine(message, entry);

        if (entry.GroupMaxLimit != null)
        {
            message.PushNewline();
            var limit = Loc.GetString(
                "sunrise-inventory-bag-group-count",
                ("selected", entry.GroupSelectedCount),
                ("max", entry.GroupMaxLimit.Value));
            var limitLabel = StripTooltipLabel(Loc.GetString("sunrise-inventory-tooltip-limit"));
            var limitColor = entry.GroupSelectedCount >= entry.GroupMaxLimit.Value
                ? TooltipWarningColor
                : TooltipTextMutedColor;
            AddTooltipColoredText(message, $"{limitLabel}: {limit}", limitColor);
        }

        if (!string.IsNullOrWhiteSpace(entry.Description))
        {
            message.PushNewline();
            message.PushNewline();
            AddTooltipColoredText(message, entry.Description, TooltipTextColor);
        }

        if (!string.IsNullOrWhiteSpace(entry.Requirements))
        {
            message.PushNewline();
            message.PushNewline();
            AddTooltipStatusMarker(message, entry.Enabled);
            message.AddMarkupPermissive(entry.Requirements);
        }

        if (entry.Selected && !entry.CanRemove)
        {
            message.PushNewline();
            AddTooltipStatusMarker(message, enabled: true, statusType: TooltipWarningStatus, color: TooltipWarningColor);
            message.AddText(Loc.GetString("sunrise-inventory-loadout-required"));
        }

        return message;
    }

    private static void AddTooltipMetaLine(FormattedMessage message, InventoryPaletteEntry entry)
    {
        var parts = new List<string>
        {
            GetSourceText(entry.Source),
            GetPlacementText(entry),
        };

        if (!string.IsNullOrWhiteSpace(entry.GroupName))
            parts.Add(entry.GroupName);

        message.PushNewline();
        message.PushNewline();
        AddTooltipColoredText(message, string.Join(TooltipSeparator, parts), TooltipTextMutedColor);
    }

    private static void AddTooltipStatusMarker(
        FormattedMessage message,
        bool enabled,
        string? statusType = null,
        string? color = null)
    {
        statusType ??= enabled ? TooltipGoodStatus : TooltipBadStatus;
        color ??= enabled ? TooltipGoodColor : TooltipBadColor;
        message.AddMarkupOrThrow($"[sinvstatus type=\"{statusType}\" color=\"{color}\" /] ");
    }

    private static void AddTooltipColoredText(FormattedMessage message, string text, string color, bool bold = false)
    {
        var escapedText = FormattedMessage.EscapeText(text);
        var boldStart = bold ? "[bold]" : string.Empty;
        var boldEnd = bold ? "[/bold]" : string.Empty;
        message.AddMarkupOrThrow($"[color={color}]{boldStart}{escapedText}{boldEnd}[/color]");
    }

    private static string StripTooltipLabel(string label)
    {
        return label.Trim().TrimEnd(':');
    }

    private static string GetSlotTexturePath(string textureName)
    {
        return textureName.StartsWith("Slots/", StringComparison.Ordinal)
            ? textureName
            : $"Slots/{textureName}";
    }

    private bool TryGetSponsorItem(string itemId, out SponsorInventoryItemInfo item)
    {
        foreach (var sponsorItem in _sponsorInventory.GetSponsorInventoryConfig().Items ?? [])
        {
            if (sponsorItem == null)
                continue;

            if (sponsorItem.Id != itemId)
                continue;

            item = sponsorItem;
            return true;
        }

        item = default!;
        return false;
    }

    private HashSet<string> GetAvailablePets(ICommonSession? localSession)
    {
        if (localSession != null &&
            _sponsors != null &&
            _sponsors.TryGetPets(localSession.UserId, out var availablePets) &&
            availablePets != null)
        {
            return availablePets.ToHashSet();
        }

        return new HashSet<string>();
    }

    private string GetSponsorItemName(SponsorInventoryItemInfo item)
    {
        return GetEntityName(item.EntityPrototype, item.Id);
    }

    private string GetEntityDescription(string entityPrototype)
    {
        return _prototype.TryIndex<EntityPrototype>(entityPrototype, out var prototype)
            ? prototype.Description
            : string.Empty;
    }

    private string GetEntityName(EntityUid entity)
    {
        return _entManager.GetComponent<MetaDataComponent>(entity).EntityName;
    }

    private string GetEntityName(string entityPrototype, string fallback)
    {
        return _prototype.TryIndex<EntityPrototype>(entityPrototype, out var prototype)
            ? prototype.Name
            : fallback;
    }

    private static string GetLoadoutRequirements(bool enabled, FormattedMessage? reason)
    {
        return enabled
            ? Loc.GetString("sunrise-inventory-requirement-loadout")
            : reason?.ToMarkup() ?? Loc.GetString("sunrise-inventory-requirement-unavailable-short");
    }

    private string GetSponsorRequirements(SponsorInventoryItemInfo item, string? reason)
    {
        var lines = new List<string>();
        var purchased = _sponsorInventory.GetPurchasedInventoryItems().Contains(item.Id);

        if (purchased)
        {
            lines.Add(Loc.GetString("sunrise-inventory-requirement-owned"));
        }
        else
        {
            if (item.SponsorLevel != null)
            {
                lines.Add(Loc.GetString(
                    "sunrise-inventory-requirement-sponsor-tier",
                    ("level", item.SponsorLevel.Value)));
            }

            if (item.Price > 0)
            {
                lines.Add(Loc.GetString(
                    "sunrise-inventory-requirement-price",
                    ("price", item.Price)));
            }
        }

        if (item.AvailableJobs is { Length: > 0 })
        {
            var jobs = string.Join(", ", item.AvailableJobs.Select(GetJobName));
            lines.Add(Loc.GetString("sunrise-inventory-requirement-jobs", ("jobs", jobs)));
        }

        if (reason != null)
            lines.Add(Loc.GetString("sunrise-inventory-requirement-unavailable", ("reason", reason)));

        return lines.Count == 0
            ? Loc.GetString("sunrise-inventory-requirement-none")
            : string.Join("\n", lines);
    }

    private string GetJobName(string jobId)
    {
        return _prototype.TryIndex<JobPrototype>(jobId, out var job)
            ? job.LocalizedName
            : jobId;
    }

    private List<EntityUid> TakeStorageItems(EntityUid storageEntity)
    {
        var items = new List<EntityUid>();
        if (!_entManager.TryGetComponent<StorageComponent>(storageEntity, out var storage))
            return items;

        foreach (var contained in storage.Container.ContainedEntities.ToArray())
        {
            if (!_container.Remove(contained, storage.Container, reparent: false, force: true))
                continue;

            items.Add(contained);
        }

        return items;
    }

    private bool TryMoveStorageItems(EntityUid storageEntity, List<EntityUid> items)
    {
        if (!_entManager.TryGetComponent<StorageComponent>(storageEntity, out var storage))
            return items.Count == 0;

        var inserted = new List<EntityUid>();
        foreach (var item in items)
        {
            if (!_entManager.TryGetComponent<ItemComponent>(item, out var itemComp) ||
                !_storage.TryGetAvailableGridSpace((storageEntity, storage), (item, itemComp), out var location) ||
                !_storage.InsertAt((storageEntity, storage), (item, itemComp), location.Value, out _, playSound: false, stackAutomatically: false))
            {
                foreach (var insertedItem in inserted)
                {
                    _container.Remove(insertedItem, storage.Container, reparent: false, force: true);
                }

                return false;
            }

            inserted.Add(item);
        }

        return true;
    }

    private void MoveStorageItemsOrDelete(EntityUid storageEntity, List<EntityUid> items)
    {
        if (TryMoveStorageItems(storageEntity, items))
            return;

        foreach (var item in items)
        {
            _entManager.DeleteEntity(item);
        }
    }

    private void AddPaletteInfo(string locId)
    {
        ItemPalette.AddChild(new Label
        {
            Text = Loc.GetString(locId),
            StyleClasses = { StyleClass.LabelSubText },
            Margin = new Thickness(8)
        });
    }

    private static TextureButton CreateRemoveButton(Action onPressed, string tooltip)
    {
        var button = new TextureButton
        {
            StyleClasses = { DefaultWindow.StyleClassWindowCloseButton },
            SetWidth = 18,
            SetHeight = 18,
            HorizontalAlignment = Control.HAlignment.Right,
            VerticalAlignment = Control.VAlignment.Top,
            Margin = new Thickness(0, -3, -3, 0),
            ToolTip = Loc.GetString(tooltip),
            MouseFilter = Control.MouseFilterMode.Stop,
            ModulateSelfOverride = Color.DarkRed
        };

        button.OnPressed += _ => onPressed();
        return button;
    }

    private void ClearPreviewDummy()
    {
        if (_previewDummy != null)
            _entManager.DeleteEntity(_previewDummy.Value);

        _previewDummy = null;
        _sponsorBagPreviewItems.Clear();
    }

    private void ClearDragGhost()
    {
        _dragGhost?.Orphan();
        _dragGhost = null;
    }
}
