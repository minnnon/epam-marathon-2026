import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map, switchMap } from 'rxjs';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import { OwnerSessionResponse } from '@app/models/group-orders/owner-session.model';
import { SubmittedDish } from '@app/models/group-orders/submit-group-pre-order.model';

@Injectable({ providedIn: 'root' })
export class GroupOrdersService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);
  private readonly inviteService = inject(GuestInviteService);

  /** reservationId → invite-token → invite (у ньому GroupReservationId і дані для шапки). */
  getGroupInvite$(reservationId: string): Observable<GuestInvite> {
    return this.inviteService
      .getInviteToken$(reservationId)
      .pipe(switchMap((response) => this.inviteService.getInvite$(response.token)));
  }

  /** GET /guest/groups/{groupId}/sessions — учасники та їхні кошики. */
  getSessions$(groupId: string): Observable<GroupSessionsResponse> {
    return this.http.get<GroupSessionsResponse>(`${this.baseUrl}/guest/groups/${groupId}/sessions`);
  }

  /** GET /reservations/{reservationId}/guest-session-id — id власної сесії Owner. */
  getOwnerSessionId$(reservationId: string): Observable<string> {
    return this.http
      .get<OwnerSessionResponse>(`${this.baseUrl}/reservations/${reservationId}/guest-session-id`)
      .pipe(map((response) => response.guestSessionId));
  }

  /** GET /guest/groups/{groupId}/status — Open / Submitted / Closed. */
  getStatus$(groupId: string): Observable<GroupStatusResponse> {
    return this.http.get<GroupStatusResponse>(`${this.baseUrl}/guest/groups/${groupId}/status`);
  }

  /** POST /group-reservations/{groupId}/submit, тіло { reservationId }. */
  submit$(groupId: string, reservationId: string): Observable<SubmittedDish[]> {
    return this.http.post<SubmittedDish[]>(`${this.baseUrl}/group-reservations/${groupId}/submit`, {
      reservationId,
    });
  }
}
