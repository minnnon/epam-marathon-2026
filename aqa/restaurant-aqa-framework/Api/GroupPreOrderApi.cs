using System.Globalization;
using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>
/// The group pre-order: the owner's invite, guests joining and filling their own carts, and the owner's view of
/// (and control over) everyone who joined. Two kinds of credential are in play, both sent as a bearer token:
/// a Cognito id token for customers (owner, other customers, waiters) and the guest session token a guest got
/// when joining. Confirmed against the live backend:
/// <list type="bullet">
/// <item>GET …/sessions and DELETE …/sessions/{id} sit behind the Cognito authorizer: no token or a guest token → 401,
/// a customer who is not the owner → 403, an unknown group → 404.</item>
/// <item>DELETE of a session that belongs to another group → 400; of an unknown session → 404.</item>
/// <item>The owner has no guest session of their own here, so the list holds guests only.</item>
/// </list>
/// </summary>
public sealed class GroupPreOrderApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> GetInviteTokenAsync(string reservationId, string? bearerToken) =>
        GetAsync(Url($"/reservations/{reservationId}/invite-token"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> GetInviteAsync(string inviteToken) =>
        GetAsync(Url($"/guest/invites/{Uri.EscapeDataString(inviteToken)}"));

    public Task<IAPIResponse> JoinAsync(JoinGroupRequest request) =>
        Api.PostAsync(Url("/guest/sessions"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(),
        });

    public Task<IAPIResponse> GetGuestMenuAsync(string guestSessionId, string sessionToken) =>
        GetAsync(Url($"/guest/sessions/{guestSessionId}/menu"), new APIRequestContextOptions
        {
            Headers = Headers(sessionToken),
        });

    public Task<IAPIResponse> AddDishAsync(string guestSessionId, string sessionToken, GuestDishRequest request) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/dishes"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(sessionToken),
        });

    public Task<IAPIResponse> RemoveDishAsync(string guestSessionId, string sessionToken, GuestDishRequest request) =>
        DeleteAsync(Url($"/guest/sessions/{guestSessionId}/dishes"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(sessionToken),
        });

    public Task<IAPIResponse> SubmitCartAsync(string guestSessionId, string sessionToken) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/submit"), new APIRequestContextOptions
        {
            Headers = Headers(sessionToken),
        });

    public Task<IAPIResponse> GetParticipantsAsync(string groupReservationId, string? bearerToken) =>
        GetAsync(Url($"/guest/groups/{groupReservationId}/sessions"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> RemoveParticipantAsync(string groupReservationId, string guestSessionId, string? bearerToken) =>
        DeleteAsync(Url($"/guest/groups/{groupReservationId}/sessions/{guestSessionId}"), new APIRequestContextOptions
        {
            Headers = Headers(bearerToken),
        });

    /// <summary>Free slots of one table on one day. The backend wants the day as <c>startTime</c> in plain ISO 8601
    /// without fractions (<c>2026-11-05T00:00:00Z</c>); TablesApi's older <c>date=</c> variant is refused with 400.</summary>
    public Task<IAPIResponse> GetFreeTimeSlotsAsync(string tableId, DateTime dayUtc) =>
        GetAsync(Url($"/tables/{tableId}/available-time-slots", new Dictionary<string, string?>
        {
            ["startTime"] = dayUtc.Date.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
        }));
}
