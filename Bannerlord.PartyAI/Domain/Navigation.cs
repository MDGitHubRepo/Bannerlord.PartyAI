using Helpers;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace Bannerlord.PartyAI.Domain;

public static class Navigation
{
    /// <summary>
    /// Mirrors vanilla AiVisitSettlementBehavior.GetBestNavigationDataForVisitingSettlement.
    /// Computes the best navigation type and port transition flags for reaching a settlement.
    /// </summary>
    public static bool TryGetBestNavigationDataForSettlement(
      MobileParty party,
      Settlement settlement,
      out MobileParty.NavigationType navigationType,
      out bool isFromPort,
      out bool isTargetingPort)
    {
        navigationType = MobileParty.NavigationType.None;
        isFromPort = false;
        isTargetingPort = false;

        if (party == null || settlement == null)
        {
            return false;
        }

        // Normalize to canonical Settlement instance to avoid cache-key identity mismatches
        Settlement normalizedSettlement = Settlement.Find(settlement.StringId) ?? settlement;

        MobileParty.NavigationType bestNavType = MobileParty.NavigationType.None;
        float bestDistance = float.MaxValue;
        bool bestIsFromPort = false;

        bool portBlocked = normalizedSettlement.HasPort &&
                           normalizedSettlement.SiegeEvent != null &&
                           normalizedSettlement.SiegeEvent.IsBlockadeActive;

        // Try non-port targeting first (unless blockade prevents portless approach for naval-capable parties).
        if (!portBlocked || !party.HasNavalNavigationCapability)
        {
            AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(
              party,
              normalizedSettlement,
              false,
              out bestNavType,
              out bestDistance,
              out bestIsFromPort);
        }

        // If the party can travel by sea and the settlement has a port, also try targeting the port.
        if (party.HasNavalNavigationCapability && normalizedSettlement.HasPort)
        {
            AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(
              party,
              normalizedSettlement,
              true,
              out MobileParty.NavigationType portNavType,
              out float portDistance,
              out bool portIsFromPort);

            if (portDistance < bestDistance)
            {
                navigationType = portNavType;
                isFromPort = portIsFromPort;
                isTargetingPort = true;
                return navigationType != MobileParty.NavigationType.None;
            }
        }

        navigationType = bestNavType;
        isFromPort = bestIsFromPort;
        isTargetingPort = false;

        return navigationType != MobileParty.NavigationType.None;
    }

    public static Settlement? FindNearestSettlement(
        Func<Settlement, bool> condition,
        IMapPoint toMapPoint,
        IEnumerable<Settlement>? settlements = null)
    {
        Settlement? result = null;
        settlements ??= Settlement.All;

        // Get the "origin" position from the map point
        Vec2 originPos;

        if (toMapPoint is Settlement originSettlement)
        {
            originPos = originSettlement.GetPosition2D;
        }
        else if (toMapPoint is MobileParty originParty)
        {
            originPos = originParty.GetPosition2D;
        }
        else
        {
            // Fallback: use main party position if don't recognize the IMapPoint type
            originPos = MobileParty.MainParty.GetPosition2D;
        }

        float bestDistSq = float.MaxValue;

        foreach (Settlement item in settlements)
        {
            if (condition != null && !condition(item))
                continue;

            // Distance in 2D map space
            float distSq = originPos.DistanceSquared(item.GetPosition2D);

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                result = item;
            }
        }

        return result;
    }
}
