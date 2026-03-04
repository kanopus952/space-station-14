// Overlaps with existing namespace.
using Content.Shared.Cargo;
using Robust.Shared.Containers;

namespace Content.Server.Cargo.Systems;

public sealed partial class PricingSystem
{
    /// <summary>
    /// Appraises an entity while ignoring <c>DontSell</c> event handling.
    /// </summary>
    /// <param name="uid">The entity to appraise.</param>
    /// <param name="includeContents">Whether to include contained entities.</param>
    /// <returns>The price of the entity.</returns>
    /// <remarks>
    /// Calculating the price of an entity that somehow contains itself will likely hang.
    /// </remarks>
    public double GetPriceIgnoreDontSell(EntityUid uid, bool includeContents = true)
    {
        var ev = new PriceCalculationEvent
        {
            IgnoreDontSell = true
        };
        RaiseLocalEvent(uid, ref ev);

        if (ev.Handled)
            return ev.Price;

        var price = ev.Price;

        // TODO: Add an OpaqueToAppraisal component or similar for blocking the recursive descent into containers, or preventing material pricing.
        // DO NOT FORGET TO UPDATE ESTIMATED PRICING
        price += GetMaterialsPrice(uid);
        price += GetSolutionsPrice(uid);

        // Can't use static price with stackprice
        var oldPrice = price;
        price += GetStackPrice(uid);

        if (oldPrice.Equals(price))
        {
            price += GetStaticPrice(uid);
        }

        if (includeContents && TryComp<ContainerManagerComponent>(uid, out var containers))
        {
            foreach (var container in containers.Containers.Values)
            {
                foreach (var ent in container.ContainedEntities)
                {
                    price += GetPriceIgnoreDontSell(ent, includeContents: true);
                }
            }
        }

        return price;
    }
}
