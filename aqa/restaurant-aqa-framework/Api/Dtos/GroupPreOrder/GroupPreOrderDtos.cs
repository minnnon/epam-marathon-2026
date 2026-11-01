namespace Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrder;

/// <summary>GET /reservations/{reservationId}/invite-token — the token the owner shares in the invite link.</summary>
public sealed record InviteTokenResponse(string Token);

/// <summary>GET /guest/invites/{token}. Confirmed live: this one response is PascalCase on the wire
/// ("GroupReservationId"), which <see cref="JsonDefaults"/> reads anyway because it is case-insensitive.</summary>
public sealed record GroupInviteResponse(string GroupReservationId, int MaxGuestCount, string InviterName);

/// <summary>POST /guest/sessions — a guest joins the group under a display name. No account, no bearer token.</summary>
public sealed record JoinGroupRequest(string GroupReservationId, string GuestName);

/// <summary>What joining returns (PascalCase on the wire, like the invite). The token authorises every later
/// call the guest makes on their own cart: <c>Authorization: Bearer {SessionToken}</c>.</summary>
public sealed record GuestSessionResponse(string SessionId, string SessionToken);

/// <summary>POST / DELETE /guest/sessions/{id}/dishes — adds, or takes out, <paramref name="Quantity"/> portions.</summary>
public sealed record GuestDishRequest(string DishId, int Quantity);

/// <summary>GET /guest/groups/{groupReservationId}/sessions, and what DELETE …/sessions/{id} returns after a removal.</summary>
public sealed record GroupParticipantsResponse(IReadOnlyList<GroupParticipant> Participants, decimal Total);

/// <summary>One guest in the group. <paramref name="Id"/> is the guest session id.</summary>
public sealed record GroupParticipant(
    string Id,
    string Type,
    string Name,
    IReadOnlyList<GroupParticipantItem> Items,
    decimal Total,
    bool IsSubmitted);

public sealed record GroupParticipantItem(GroupParticipantDish Dish, int Quantity);

/// <summary>Only the fields the tests compare; the rest of the dish is ignored on purpose.</summary>
public sealed record GroupParticipantDish(string Id, string Name, decimal Price);

/// <summary>GET /tables/{id}/available-time-slots. Read as <see cref="DateTimeOffset"/> so the slot is sent back
/// exactly as the backend gave it, whatever the time zone of the machine running the tests.</summary>
public sealed record FreeTimeSlot(DateTimeOffset Start, DateTimeOffset End);

/// <summary>GET /guest/sessions/{id}/menu — the menu of the location the group booked, grouped by dish type
/// (<c>{"dishes": {"MainCourse": [...]}}</c>). This, not the global /dishes catalogue, is what a guest may order:
/// adding a dish the location does not serve is refused with 400.</summary>
public sealed record GuestMenuResponse(IReadOnlyDictionary<string, IReadOnlyList<GuestMenuDish>> Dishes);

public sealed record GuestMenuDish(string Id, string Name, decimal Price, string? State, bool? Selectable);
