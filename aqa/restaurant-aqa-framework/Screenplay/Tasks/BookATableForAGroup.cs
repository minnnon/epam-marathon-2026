using System.Security.Cryptography;
using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrder;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Tables;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>
/// The owner books a table for <paramref name="guestCount"/> people and opens it for a group pre-order: books the
/// table, fetches the invite token and follows the invite, the way a guest's link would. Remembers the
/// <see cref="GroupBooking"/> under <paramref name="groupName"/> and schedules the reservation's cancellation.
///
/// The environment is shared and changes, so nothing is hard-coded: the task looks for a table that seats the group
/// (or uses <c>TESTTABLE_ID</c> when it is configured) on a random day 2–6 months ahead, and takes a random free slot
/// — so parallel scenarios and repeated runs do not collide. Tables at a location with no waiter cannot be booked
/// (the backend refuses: "No waiters found for location"), so such a table is skipped and the next one tried.
///
/// A group of N people leaves room for N − 1 guests: the owner is one of the N.
/// </summary>
public sealed class BookATableForAGroup(int guestCount, string ownerRole = GroupNames.Owner, string groupName = GroupNames.TheGroup) : ITask
{
    private const int DaysToTryPerTable = 3;

    public async Task PerformAsAsync(Actor actor)
    {
        var owner = actor.Recall<CustomerAccount>(ownerRole);
        var reservations = new ReservationsApi(actor.UsingTheApi());
        var groups = new GroupPreOrderApi(actor.UsingTheApi());

        var reservation = await BookAsync(actor, owner, reservations, groups);

        actor.Recall<CleanupScope>().TrackDelete(
            $"reservation:{reservation.Id}",
            () => reservations.CancelReservationAsync(reservation.Id, owner.IdToken));

        var tokenResponse = await groups.GetInviteTokenAsync(reservation.Id, owner.IdToken);
        if (!tokenResponse.Ok)
            throw new InvalidOperationException(
                $"Getting the invite for reservation {reservation.Id} failed with {tokenResponse.Status}: {await tokenResponse.ReadErrorMessageAsync()}");
        var inviteToken = (await tokenResponse.ReadAsAsync<InviteTokenResponse>()).Token;

        var inviteResponse = await groups.GetInviteAsync(inviteToken);
        if (!inviteResponse.Ok)
            throw new InvalidOperationException(
                $"Following the invite failed with {inviteResponse.Status}: {await inviteResponse.ReadErrorMessageAsync()}");
        var invite = await inviteResponse.ReadAsAsync<GroupInviteResponse>();

        actor.Remember(groupName, new GroupBooking(groupName, reservation.Id, invite.GroupReservationId, ownerRole));
    }

    private async Task<ReservationResponse> BookAsync(
        Actor actor, CustomerAccount owner, ReservationsApi reservations, GroupPreOrderApi groups)
    {
        var refusals = new List<string>();

        foreach (var tableId in await CandidateTablesAsync(actor))
        {
            for (var attempt = 0; attempt < DaysToTryPerTable; attempt++)
            {
                var day = DateTime.UtcNow.Date.AddDays(RandomNumberGenerator.GetInt32(60, 180));
                var slotsResponse = await groups.GetFreeTimeSlotsAsync(tableId, day);
                if (!slotsResponse.Ok)
                {
                    refusals.Add($"slots of table {tableId}: {slotsResponse.Status} {await slotsResponse.ReadErrorMessageAsync()}");
                    continue;
                }

                var slots = await slotsResponse.ReadAsAsync<List<FreeTimeSlot>>();
                if (slots.Count == 0)
                    continue;

                var slot = slots[RandomNumberGenerator.GetInt32(slots.Count)];
                var booked = await reservations.CreateReservationAsync(
                    new CreateReservationRequest(tableId, slot.Start.UtcDateTime, slot.End.UtcDateTime, guestCount),
                    owner.IdToken);

                if (booked.Ok)
                    return await booked.ReadAsAsync<ReservationResponse>();

                var error = await booked.ReadErrorMessageAsync();
                refusals.Add($"table {tableId} at {slot.Start:O}: {booked.Status} {error}");

                // A location without a waiter refuses every booking; another day will not help.
                if (error.Contains("No waiters", StringComparison.OrdinalIgnoreCase))
                    break;
            }
        }

        throw new InvalidOperationException(
            $"Could not book a table for {guestCount} people. Attempts:{Environment.NewLine}{string.Join(Environment.NewLine, refusals)}");
    }

    /// <summary>The configured test table, or every table that seats the group, shuffled.</summary>
    private async Task<List<string>> CandidateTablesAsync(Actor actor)
    {
        if (Settings.Current.TestTableId is { Length: > 0 } configured)
            return [configured];

        var api = actor.UsingTheApi();
        var locationsResponse = await new LocationsApi(api).GetLocationsAsync();
        if (!locationsResponse.Ok)
            throw new InvalidOperationException(
                $"Listing locations failed with {locationsResponse.Status}: {await locationsResponse.ReadErrorMessageAsync()}");

        var tables = new TablesApi(api);
        var lookAt = DateTime.UtcNow.Date.AddDays(60);
        var candidates = new List<string>();
        foreach (var location in await locationsResponse.ReadAsAsync<List<LocationResponse>>())
        {
            var tablesResponse = await tables.GetAvailableTablesAsync(location.Id, guestCount, lookAt);
            if (tablesResponse.Ok)
                candidates.AddRange((await tablesResponse.ReadAsAsync<List<TableResponse>>()).Select(t => t.Id));
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException($"No location has a table that seats {guestCount} people.");

        return [.. candidates.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))];
    }
}
